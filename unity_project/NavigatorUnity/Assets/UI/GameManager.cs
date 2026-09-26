// Navigator 2.0 — GameManager.cs
// Máquina de estados: Lobby → Diseño → Cuenta atrás → Vuelo → Ciencia → Scoreboard
// Es el director que conecta todos los demás scripts
// Guacalita Inc. — SAL / Claude

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TMPro;
using Navigator.Core;
using Navigator.Game;
using Navigator.Network;
using Navigator.AR;

namespace Navigator.UI
{
    public enum GameState
    {
        Lobby,
        MissionDesign,
        Countdown,
        InFlight,
        ScienceOps,
        Scoreboard,
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // ── Inspector refs ─────────────────────────────────────────────────────
        [Header("UI Panels")]
        public GameObject     LobbyPanel;
        public GameObject     MissionDesignPanel;
        public GameObject     CountdownPanel;
        public GameObject     HUDPanel;
        public GameObject     ScienceOpsPanel;
        public GameObject     ScoreboardPanel;
        public TMP_Text       CountdownLabel;     // "3" "2" "1" "¡GO!"
        public TMP_Text       CountdownSubLabel;  // "Destino: Marte"

        [Header("Scripts de juego")]
        public MissionDesignUI  MissionDesignUI;
        public ScienceOpsUI     ScienceOpsUI;
        public ScoreboardUI     ScoreboardUI;
        public CockpitHUD       HUD;
        public LobbyManager     Lobby;
        public MissionScore     Score;

        [Header("Nave local")]
        public ShipController   Ship;
        public PhysicsEngine    Physics;
        public ARShipController ARShip;

        [Header("AR")]
        public ARSpaceManager       SpaceManager;
        public ARCompassTracker     Compass;
        public CameraFeedBackground CameraFeed;
        public GyroCameraRig        Rig;

        [Header("Espacio")]
        public PowerupSystem Powerups;
        public AsteroidField Asteroids;

        [Header("Red")]
        public SendaBridge       Senda;
        public ShipSyncService   SyncService;
        public P2PChatSystem     Chat;
        public RemoteShipManager RemoteShips;

        [Header("Reglas")]
        public float ArrivalMargin  = 40f;    // distancia a la superficie que cuenta como llegada
        public float LaneSpacing    = 6f;     // separación lateral entre jugadores en la salida
        public float MaxFlightTimeS = 240f;   // sin llegar a tiempo = misión fallida
        public string PlayerName => Loc.T("player.default");

        // ── Estado runtime ─────────────────────────────────────────────────────
        public  GameState     State      { get; private set; } = GameState.Lobby;
        public  MissionDesign Mission    { get; private set; }
        public  int           ArrivalPos { get; private set; } = 0;  // 0 = no llegó
        public  float         FlightTime => _flightTimer;
        public  string        FailReason { get; private set; }

        public UnityEvent<GameState> OnStateChanged = new();

        // Everything the debrief needs to explain how the design shaped the result
        public MissionOutcome Outcome { get; } = new();

        private float  _flightTimer;
        private bool   _rivalArrived;
        private int    _remoteScore = Navigator.UI.ScoreboardUI.RemotePending;
        private string _lastPlanet  = "mars";

        // ── Init ──────────────────────────────────────────────────────────────

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // Gyro mapping assumes portrait; the AR view must not rotate under the player
            Screen.orientation         = ScreenOrientation.Portrait;
            Screen.sleepTimeout        = SleepTimeout.NeverSleep;
            Application.targetFrameRate = 60;
        }

        void Start()
        {
            if (Senda != null && !Senda.Initialized) Senda.Init();
            Senda?.OnMessage.AddListener(OnNetMessage);

            if (SyncService != null)
            {
                SyncService.OnRemoteScore.AddListener(OnRemoteScore);
                SyncService.OnRemoteArrived.AddListener(_ => OnRemoteArrived());
                SyncService.OnRemoteName.AddListener((_, n) => ScoreboardUI?.SetRemotePlayerName(n));
            }

            EnterLobby();
        }

        // ── Máquina de estados ─────────────────────────────────────────────────

        void EnterLobby()
        {
            SetState(GameState.Lobby);
            SetPanels(lobby: true);
            SetControls(false);
            // The lobby QR scanner needs the camera to itself
            if (CameraFeed != null) CameraFeed.Stop();
        }

        // Lobby "Despegar" / Scoreboard "Jugar otra vez"
        public void OnLobbyReady()
        {
            if (State != GameState.Lobby && State != GameState.Scoreboard) return;

            _rivalArrived = false;
            _remoteScore  = Navigator.UI.ScoreboardUI.RemotePending;
            ArrivalPos    = 0;
            FailReason    = null;
            Mission       = null;

            if (SpaceManager != null) SpaceManager.Locked = false;
            Powerups?.ClearAll();
            Asteroids?.Clear();
            SetShipFrozen(false);
            ResetShip(Vector3.forward);
            if (Score != null) Score.Clear();
            Outcome.Clear();

            SetState(GameState.MissionDesign);
            SetPanels(missionDesign: true);

            // The lobby QR scanner has released the camera by now
            if (CameraFeed != null) CameraFeed.Play();

            // "Play again" keeps the previous design so the player can iterate on it
            MissionDesignUI?.Show(_lastPlanet, keepDesign: true);
        }

        // MissionDesignUI → botón LANZAR
        public void OnLaunchConfirmed()
        {
            if (State != GameState.MissionDesign) return;

            Mission = MissionDesignUI?.GetDesign();
            if (Mission == null || !Mission.IsValid) return;
            _lastPlanet = Mission.TargetPlanetId;

            SetState(GameState.Countdown);
            SetPanels(countdown: true);

            // Place the planet now so NASA Horizons can refine it during the countdown
            if (Compass != null) Compass.SetTarget(Mission.TargetPlanetId);
            else SpaceManager?.SetPlanetDirectionFallback(
                Mission.TargetPlanetId, Vector3.forward,
                ARSpaceManager.ApproxEarthDistanceAU(Mission.TargetPlanetId));

            StartCoroutine(CountdownCoroutine());
        }

        IEnumerator CountdownCoroutine()
        {
            if (CountdownSubLabel != null)
                CountdownSubLabel.text = Loc.F("count.sub", Loc.Planet(Mission.TargetPlanetId));

            foreach (string l in new[] { "3", "2", "1", Loc.T("count.liftoff") })
            {
                if (CountdownLabel != null) CountdownLabel.text = l;
                yield return new WaitForSeconds(1f);
            }
            StartFlight();
        }

        void StartFlight()
        {
            Vector3 target = SpaceManager != null ? SpaceManager.TargetPosition : Vector3.forward * 1400f;
            Vector3 launch = SpaceManager != null ? SpaceManager.LaunchPoint    : Vector3.zero;
            if (SpaceManager != null) SpaceManager.Locked = true;

            SetShipFrozen(false);
            ResetShip((target - launch).normalized);
            ConfigureShipFromDesign();
            Outcome.Clear();

            // North-align the view only when the planet came from real sky data
            Rig?.Calibrate(Compass != null && Compass.GpsReady);

            if (Physics != null)
            {
                Physics.SetEngineConfig(Mission.Engine);
                Physics.RegisterPlanetGravity(Mission.TargetPlanetId, target);
            }

            Powerups?.SpawnAlongRoute(launch, target);
            if (Ship != null) Asteroids?.SpawnAlongRoute(launch, target, Ship.transform);

            if (HUD != null)
            {
                if (MissionDesign.CommDelayLabel.TryGetValue(Mission.TargetPlanetId, out var delay))
                    HUD.SetCommDelay(delay);
                HUD.SetRacePosition(1);
            }

            _flightTimer = 0f;
            SetState(GameState.InFlight);
            SetPanels(hud: true);
            SetControls(true);

            Debug.Log($"[GM] Vuelo iniciado → {Mission.TargetPlanetId}");
        }

        void ResetShip(Vector3 facing)
        {
            if (Ship == null) return;
            if (facing.sqrMagnitude < 1e-6f) facing = Vector3.forward;
            Vector3 start = (SpaceManager != null ? SpaceManager.LaunchPoint : Vector3.zero)
                          + LaneOffset(facing);
            Ship.transform.SetPositionAndRotation(start, Quaternion.LookRotation(facing));

            var rb = Ship.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = start;
                rb.rotation = Quaternion.LookRotation(facing);
                rb.linearVelocity  = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        // Two ships starting on the same point would overlap colliders: each player gets a
        // side of the launch pad, agreed on without messages by comparing NodeIds
        Vector3 LaneOffset(Vector3 facing)
        {
            if (Senda == null || string.IsNullOrEmpty(Senda.NodeId) || Senda.ConnectedPeers.Count == 0)
                return Vector3.zero;
            float side = string.CompareOrdinal(Senda.NodeId, Senda.ConnectedPeers[0]) < 0 ? -1f : 1f;
            Vector3 right = Vector3.Cross(Vector3.up, facing);
            if (right.sqrMagnitude < 1e-6f) right = Vector3.right;
            return right.normalized * side * LaneSpacing;
        }

        void ConfigureShipFromDesign()
        {
            if (Ship == null || Mission == null) return;
            Ship.Mission     = Mission;
            Ship.NodeId      = Senda != null ? Senda.NodeId : null;
            Ship.DisplayName = PlayerName;

            Ship.GetComponent<FuelSystem>()?.InitFromDesign(Mission);
            Ship.GetComponent<BurnoutSystem>()?.ResetState();

            var col = Ship.GetComponent<CollisionSystem>();
            if (col != null)
            {
                col.Lives      = 3;
                col.FuelMassKg = Mission.FuelMassKg;
                col.DryMassKg  = Mission.DryMassKg;
            }
        }

        void Update()
        {
            if (State != GameState.InFlight || Ship == null) return;

            _flightTimer += Time.deltaTime;

            if (SpaceManager != null)
            {
                float dist = Vector3.Distance(Ship.transform.position, SpaceManager.TargetPosition);
                if (dist < SpaceManager.PlanetRadius + ArrivalMargin)
                {
                    Arrive();
                    return;
                }
            }

            var col = Ship.GetComponent<CollisionSystem>();
            if (col != null && col.Lives <= 0) { FailMission("fail.destroyed"); return; }

            // Out of fuel and coasted to a stop: no point waiting for the clock
            var fuel = Ship.GetComponent<FuelSystem>();
            var rb   = Ship.GetComponent<Rigidbody>();
            if (fuel != null && fuel.IsEmpty && rb != null && rb.linearVelocity.magnitude < 2f)
            {
                Outcome.RanOutOfFuel = true;
                FailMission("fail.nofuel");
                return;
            }
            if (_flightTimer >= MaxFlightTimeS) { FailMission("fail.timeout");  return; }
        }

        void RecordFlight(bool arrived)
        {
            var fuel = Ship != null ? Ship.GetComponent<FuelSystem>()      : null;
            var col  = Ship != null ? Ship.GetComponent<CollisionSystem>() : null;
            var burn = Ship != null ? Ship.GetComponent<BurnoutSystem>()   : null;
            Outcome.Arrived       = arrived;
            Outcome.FlightTimeS   = _flightTimer;
            Outcome.FuelAtArrival = fuel != null ? fuel.FuelFraction : 0f;
            Outcome.HullLeft      = col  != null ? col.Lives : 3;
            Outcome.Takedowns     = burn != null ? burn.TakedownCount : 0;
            if (fuel != null && fuel.IsEmpty) Outcome.RanOutOfFuel = true;
        }

        // Parked at the planet: stop gravity from pulling the ship into it
        void SetShipFrozen(bool frozen)
        {
            var rb = Ship != null ? Ship.GetComponent<Rigidbody>() : null;
            if (rb == null) return;
            if (frozen) rb.linearVelocity = Vector3.zero;
            rb.isKinematic = frozen;
        }

        void Arrive()
        {
            SetControls(false);
            StopShip();
            RecordFlight(true);
            SetShipFrozen(true);
            Powerups?.ClearAll();
            Asteroids?.Clear();

            ArrivalPos = _rivalArrived ? 2 : 1;
            SyncService?.BroadcastArrival(ArrivalPos);
            Chat?.ArrivalBroadcast(ArrivalPos);

            SetState(GameState.ScienceOps);
            SetPanels(scienceOps: true);
            ScienceOpsUI?.ShowOnArrival(Mission, ArrivalPos, Outcome);

            Debug.Log($"[GM] Llegamos a {Mission?.TargetPlanetId} — posición #{ArrivalPos}");
        }

        void FailMission(string reason)
        {
            SetControls(false);
            StopShip();
            RecordFlight(false);
            SetShipFrozen(true);
            Outcome.FailReason = reason;
            Powerups?.ClearAll();
            Asteroids?.Clear();

            FailReason = reason;
            ArrivalPos = 0;
            if (Score != null)
            {
                Score.ArrivalPosition = 0;
                Score.SciencePoints   = 0;
            }
            Debug.Log($"[GM] Misión fallida: {reason}");
            FinishMission();
        }

        // ScienceOpsUI → botón COMPLETAR MISIÓN
        public void OnMissionComplete()
        {
            if (State != GameState.ScienceOps) return;
            FinishMission();
        }

        void FinishMission()
        {
            int total = 0;
            if (Score != null)
            {
                var fuel    = Ship != null ? Ship.GetComponent<FuelSystem>()    : null;
                var burnout = Ship != null ? Ship.GetComponent<BurnoutSystem>() : null;
                Score.ArrivalPosition = ArrivalPos;
                Score.FuelFraction    = fuel != null ? fuel.FuelFraction : 0f;
                Score.TakedownCount   = burnout != null ? burnout.TakedownCount : 0;
                // No arrival = no arrival bonus and no fuel bonus
                total = ArrivalPos > 0 ? Score.TotalScore()
                                       : Score.TakedownCount * 150;
            }

            SyncService?.BroadcastMissionEnd(Senda?.NodeId ?? "", total);

            bool solo = Senda == null || Senda.ConnectedPeers.Count == 0;
            SetState(GameState.Scoreboard);
            SetPanels(scoreboard: true);
            ScoreboardUI?.SetLocalPlayerName(PlayerName);
            ScoreboardUI?.Show(total, solo ? Navigator.UI.ScoreboardUI.RemoteSolo : _remoteScore, Score, FailReason != null ? Loc.T(FailReason) : null,
                               MissionReport.Build(Mission, Outcome));
        }

        void StopShip()
        {
            var rb = Ship != null ? Ship.GetComponent<Rigidbody>() : null;
            if (rb != null) rb.linearVelocity = Vector3.zero;
        }

        // Scoreboard → "Menú principal": reinicia todo (incluida la conexión)
        public void ExitToLobby()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // ── Eventos de red ─────────────────────────────────────────────────────

        void OnNetMessage(NavMsg msg)
        {
            if (msg.t != nameof(NavMsgType.shipEvent) || Ship == null || State != GameState.InFlight) return;

            // Rival picked up an EMP pod: our Burnout is suppressed
            if (msg.pl_json.Contains("\"emp\""))
            {
                Ship.GetComponent<BurnoutSystem>()?.SuppressFor(Powerup.EmpSeconds);
                HUD?.Flash("hud.emp", UIStyle.Burnout, 0.4f);
            }

            // Rival rammed us: their device resolved the crash and sent our new velocity
            if (msg.pl_json.Contains("\"hit\""))
            {
                var sev = (CollisionSeverity)Mathf.Clamp((int)Num(msg.pl_json, "hit"), 0, 2);
                var v   = new Vector3(Num(msg.pl_json, "vx"), Num(msg.pl_json, "vy"), Num(msg.pl_json, "vz"));
                Ship.GetComponent<CollisionSystem>()?.ReceiveHit(sev, v);
            }
        }

        static float Num(string json, string key)
        {
            string k = $"\"{key}\":";
            int i = json.IndexOf(k);
            if (i < 0) return 0f;
            i += k.Length;
            int e = json.IndexOfAny(new[] { ',', '}' }, i);
            string v = e > i ? json[i..e] : "";
            return float.TryParse(v, System.Globalization.NumberStyles.Float,
                                  System.Globalization.CultureInfo.InvariantCulture, out var f) ? f : 0f;
        }

        void OnRemoteArrived()
        {
            _rivalArrived = true;
            if (State == GameState.InFlight) HUD?.SetRacePosition(2);
        }

        void OnRemoteScore(Dictionary<string, int> scores)
        {
            foreach (var v in scores.Values) _remoteScore = v;
            if (State == GameState.Scoreboard) ScoreboardUI?.SetRemoteScore(_remoteScore);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        void SetControls(bool on)
        {
            if (ARShip != null) ARShip.ControlsEnabled = on;
        }

        void SetState(GameState s)
        {
            State = s;
            OnStateChanged.Invoke(s);
        }

        void SetPanels(bool lobby = false, bool missionDesign = false,
                       bool countdown = false, bool hud = false,
                       bool scienceOps = false, bool scoreboard = false)
        {
            if (LobbyPanel         != null) LobbyPanel.SetActive(lobby);
            if (MissionDesignPanel != null) MissionDesignPanel.SetActive(missionDesign);
            if (CountdownPanel     != null) CountdownPanel.SetActive(countdown);
            if (HUDPanel           != null) HUDPanel.SetActive(hud);
            if (ScienceOpsPanel    != null) ScienceOpsPanel.SetActive(scienceOps);
            if (ScoreboardPanel    != null) ScoreboardPanel.SetActive(scoreboard);
        }
    }
}
