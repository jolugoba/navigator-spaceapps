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
        SkyFinder,
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

        [Header("Operaciones")]
        public MissionEventsUI Events;
        public TutorialUI      Tutorial;
        public SkyFinderUI     SkyFinder;
        public ShareChallengeUI Share;

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
        private int _lastTotal;
        private readonly HashSet<string>         _rivalsArrived = new();
        private readonly Dictionary<string, int> _rivalScores   = new();
        private int _rivalsAtStart;   // how many final scores the scoreboard waits for
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
                SyncService.OnRemoteArrived.AddListener(OnRemoteArrived);
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

        // Campaign mission being played (null = free play)
        public CampaignMission ActiveCampaign { get; private set; }

        // Lobby "Play solo / Lift off": free play
        public void OnLobbyReady()
        {
            if (State != GameState.Lobby && State != GameState.Scoreboard) return;
            ActiveCampaign = null;
            BeginDesign();
        }

        // Campaign screen: start mission i
        public void StartCampaignMission(int index)
        {
            if (State != GameState.Lobby && State != GameState.Scoreboard) return;
            if (index < 0 || index >= Campaign.Missions.Length || !Campaign.IsUnlocked(index)) return;
            ActiveCampaign = Campaign.Missions[index];
            BeginDesign();
        }

        // Scoreboard: same mission again, keeping the design to iterate on it
        public void Retry()
        {
            if (State != GameState.Scoreboard) return;
            BeginDesign();
        }

        public bool HasNextCampaignMission =>
            ActiveCampaign != null && !ActiveCampaign.IsChallenge &&
            ActiveCampaign.Index + 1 < Campaign.Missions.Length &&
            Campaign.IsUnlocked(ActiveCampaign.Index + 1);

        // Scanned or pasted challenge code: same target and objective, challenger's spending as the cap
        public void StartChallenge(CampaignMission challenge)
        {
            if (challenge == null) return;
            if (State != GameState.Lobby && State != GameState.Scoreboard) return;
            ActiveCampaign = challenge;
            BeginDesign();
        }

        // Scoreboard → share this mission as a challenge
        public void ShareChallenge()
        {
            if (State == GameState.Scoreboard && Mission != null && ArrivalPos > 0)
                Share?.Show(Mission, _lastTotal, PlayerName);
        }

        // ── "Where is it tonight?" ─────────────────────────────────────────────

        private GameState _skyReturn;

        public void OpenSkyFinder(string planetId)
        {
            if (SkyFinder == null || (State != GameState.Lobby && State != GameState.Scoreboard)) return;
            _skyReturn = State;
            SetState(GameState.SkyFinder);
            SetPanels();
            if (CameraFeed != null) CameraFeed.Play();
            if (Ship != null) Ship.gameObject.SetActive(false);   // nothing between you and the sky
            if (Rig != null) Rig.Target = null;
            SkyFinder.Show(planetId);
        }

        public void CloseSkyFinder()
        {
            if (State != GameState.SkyFinder) return;
            SkyFinder?.Hide();
            if (Ship != null) Ship.gameObject.SetActive(true);
            if (Rig != null && Ship != null) Rig.Target = Ship.transform;
            SetState(_skyReturn);
            if (_skyReturn == GameState.Lobby)
            {
                SetPanels(lobby: true);
                if (CameraFeed != null) CameraFeed.Stop();   // the QR scanner needs the camera
            }
            else SetPanels(scoreboard: true);
        }

        public void NextMission()
        {
            if (HasNextCampaignMission) StartCampaignMission(ActiveCampaign.Index + 1);
        }

        void BeginDesign()
        {
            Events?.Cancel();

            _rivalsArrived.Clear();
            _rivalScores.Clear();
            SyncService?.ClearRound();
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
            MissionDesignUI?.Show(_lastPlanet, keepDesign: true, ActiveCampaign);
            Tutorial?.ShowIfFirstTime();
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

            string[] labels = { "3", "2", "1", Loc.T("count.liftoff") };
            for (int i = 0; i < labels.Length; i++)
            {
                string l = labels[i];
                AudioFX.Play(i < 3 ? Sfx.Beep : Sfx.Go);
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
            Events?.BeginFlight(Mission, Outcome, Ship);
            _rivalsAtStart = Senda != null ? Senda.ConnectedPeers.Count : 0;
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

        // Ships starting on the same point would overlap colliders: every player takes a lane on
        // the launch pad, agreed on without messages by sorting everyone's NodeId
        Vector3 LaneOffset(Vector3 facing)
        {
            if (Senda == null || string.IsNullOrEmpty(Senda.NodeId) || Senda.ConnectedPeers.Count == 0)
                return Vector3.zero;
            var ids = new List<string>(Senda.ConnectedPeers) { Senda.NodeId };
            ids.Sort(string.CompareOrdinal);
            float lane = ids.IndexOf(Senda.NodeId) - (ids.Count - 1) * 0.5f;
            Vector3 right = Vector3.Cross(Vector3.up, facing);
            if (right.sqrMagnitude < 1e-6f) right = Vector3.right;
            return right.normalized * lane * LaneSpacing;
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

        // Cruise decisions (safe mode, waiting for a comm link) switch the engines off for a while;
        // the race keeps running, so it costs real distance to the rivals
        public void LockThrust(float seconds)
        {
            if (State != GameState.InFlight) return;
            if (_thrustLock != null) StopCoroutine(_thrustLock);
            _thrustLock = StartCoroutine(ThrustLockRoutine(seconds));
        }

        IEnumerator ThrustLockRoutine(float seconds)
        {
            SetControls(false);
            HUD?.Flash(null, UIStyle.Warn, 0f, Loc.F("hud.safemode", seconds));
            yield return new WaitForSeconds(seconds);
            if (State == GameState.InFlight) SetControls(true);
            _thrustLock = null;
        }

        private Coroutine _thrustLock;

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
            Events?.Cancel();
            SetControls(false);
            StopShip();
            RecordFlight(true);
            SetShipFrozen(true);
            Powerups?.ClearAll();
            Asteroids?.Clear();

            ArrivalPos = _rivalsArrived.Count + 1;
            SyncService?.BroadcastArrival(ArrivalPos);
            Chat?.ArrivalBroadcast(ArrivalPos);

            SetState(GameState.ScienceOps);
            SetPanels(scienceOps: true);
            ScienceOpsUI?.ShowOnArrival(Mission, ArrivalPos, Outcome);

            Debug.Log($"[GM] Llegamos a {Mission?.TargetPlanetId} — posición #{ArrivalPos}");
        }

        void FailMission(string reason)
        {
            Events?.Cancel();
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
            _lastTotal = total;
            if (ActiveCampaign != null && ActiveCampaign.IsChallenge)
            {
                bool beat = ArrivalPos > 0 && total > ActiveCampaign.TargetScore;
                ScoreboardUI?.SetCampaignResult(ActiveCampaign, beat ? 3 : 0, false, false);
            }
            else if (ActiveCampaign != null)
            {
                int stars    = Campaign.StarsFor(ActiveCampaign, total, ArrivalPos > 0);
                bool improved = Campaign.Record(ActiveCampaign.Index, stars);
                ScoreboardUI?.SetCampaignResult(ActiveCampaign, stars, improved, HasNextCampaignMission);
            }
            else ScoreboardUI?.SetCampaignResult(null, 0, false, false);

            ScoreboardUI?.Show(total, solo ? 0 : _rivalsAtStart, Score, FailReason != null ? Loc.T(FailReason) : null,
                               MissionReport.Build(Mission, Outcome));
            PushRivalScores();
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

        void OnRemoteArrived(string nodeId)
        {
            _rivalsArrived.Add(nodeId);
            if (State == GameState.InFlight) HUD?.SetRacePosition(_rivalsArrived.Count + 1);
        }

        void OnRemoteScore(Dictionary<string, int> scores)
        {
            foreach (var kv in scores) _rivalScores[kv.Key] = kv.Value;
            if (State == GameState.Scoreboard) PushRivalScores();
        }

        void PushRivalScores()
        {
            var named = new Dictionary<string, int>();
            foreach (var kv in _rivalScores)
                named[RemoteShips != null ? RemoteShips.NameOf(kv.Key) : kv.Key] = kv.Value;
            ScoreboardUI?.SetRivalScores(named);
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
