// Navigator 2.0 — GameManager.cs
// Máquina de estados: Lobby → Diseño → Cuenta atrás → Vuelo → (Mundo) → Ciencia → Scoreboard
// With the objective LANDING, arriving opens the planet's world (docs/mundos/): descent,
// landing and the surface mission; then the science phase and the debrief as always.
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
        World,
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

        [Header("Mundos (aterrizaje)")]
        public Navigator.Worlds.WorldDirector Worlds;

        [Header("Espacio")]
        public PowerupSystem Powerups;
        public AsteroidField Asteroids;
        public GravityField  Gravity;
        public FreeSpace     Free;      // free flight: the whole Solar System, no mission

        [Header("Red")]
        public SendaBridge       Senda;
        public ShipSyncService   SyncService;
        public P2PChatSystem     Chat;
        public RemoteShipManager RemoteShips;

        [Header("Reglas")]
        public float ArrivalMargin  = 40f;    // distancia a la superficie que cuenta como llegada
        public float LaneSpacing    = 6f;     // separación lateral entre jugadores en la salida
        public float MaxFlightTimeS = 240f;   // sin llegar a tiempo = misión fallida
        // Pilot name: set from the Flutter app (FlutterBridge) or the default "Cadet"
        const string NameKey = "navigator.name";
        public const int MaxNameLength = 16;
        public static event System.Action<string> PlayerNameChanged;

        public string PlayerName
        {
            get { string n = CustomPlayerName; return string.IsNullOrWhiteSpace(n) ? Loc.T("player.default") : n; }
            set
            {
                string clean = CleanName(value);
                if (clean == CustomPlayerName) return;
                PlayerPrefs.SetString(NameKey, clean);
                PlayerPrefs.Save();
                PlayerNameChanged?.Invoke(PlayerName);
            }
        }

        // What the player typed ("" = still the default "Cadet")
        public string CustomPlayerName => PlayerPrefs.GetString(NameKey, "");

        // Names travel inside our tiny JSON messages: drop the characters that would break them
        public static string CleanName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            var sb = new System.Text.StringBuilder();
            foreach (char c in raw)
                if (c >= ' ' && "\"\\,;{}:".IndexOf(c) < 0) sb.Append(c);
            string s = sb.ToString().Trim();
            return s.Length > MaxNameLength ? s[..MaxNameLength].Trim() : s;
        }

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

            // Always landscape (twin-stick controls); fixed so the view never flips mid-flight.
            // The browser can only lock from a tap in full screen: NavScreen.jspre does that.
#if !UNITY_WEBGL || UNITY_EDITOR
            Screen.autorotateToPortrait = Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = Screen.autorotateToLandscapeRight = false;
            Screen.orientation         = ScreenOrientation.LandscapeLeft;
#endif
            Screen.sleepTimeout        = SleepTimeout.NeverSleep;
            Application.targetFrameRate = 60;
        }

        void Start()
        {
            if (Senda != null && !Senda.Initialized) Senda.Init();
            Senda?.OnMessage.AddListener(OnNetMessage);
            // Someone who left will never send a final score: the scoreboard stops waiting for them
            Senda?.OnPeerDisconnected.AddListener(_ => { if (State == GameState.Scoreboard) ScoreboardUI?.RivalLeft(); });

            if (SyncService != null)
            {
                SyncService.OnRemoteScore.AddListener(OnRemoteScore);
                SyncService.OnRemoteArrived.AddListener(OnRemoteArrived);
            }
            GravityField.AssistGained += OnGravityAssist;
            FreeSpace.SunTouched += OnSunTouched;
            ShipTools.Scanned += OnScanDiscovery;
            GravityField.BodyImpact   += OnBodyImpact;

            EnterLobby();
        }

        void OnDestroy()
        {
            GravityField.AssistGained -= OnGravityAssist;
            FreeSpace.SunTouched -= OnSunTouched;
            ShipTools.Scanned -= OnScanDiscovery;
            GravityField.BodyImpact   -= OnBodyImpact;
        }

        // A real flyby: free ΔV and propellant saved, like Voyager and Cassini
        void OnGravityAssist(string body, float dv, float fuelSaved)
        {
            if (State != GameState.InFlight) return;
            Outcome.EventBonusPoints += Mathf.RoundToInt(dv * 5f);
            Outcome.EventLog.Add(Loc.F("grav.log", Loc.Planet(body), dv, fuelSaved * 100f));
            HUD?.Flash(null, UIStyle.Good, 0.35f, Loc.F("grav.assist", Loc.Planet(body), dv, fuelSaved * 100f));
        }

        IEnumerator FlybyHint(string target, string probe)
        {
            yield return new WaitForSeconds(2.5f);
            if (State != GameState.InFlight) yield break;
            var names = new List<string>();
            foreach (var b in GravityField.RealFlybys[target].bodies) names.Add(Loc.Planet(b));
            HUD?.Flash(null, UIStyle.Accent, 0f, Loc.F("grav.intro", probe, string.Join(" → ", names)));
        }

        // Scanning something new is science: it counts in the score and the debrief
        void OnScanDiscovery(ScanInfo info)
        {
            if (!info.Discovery || State != GameState.InFlight || FreeMode) return;
            Outcome.EventBonusPoints += ShipTools.DiscoveryPoints;
            Outcome.EventLog.Add(Loc.F("scan.log", info.Title));
        }

        void OnBodyImpact(string body)
        {
            if (State != GameState.InFlight) return;
            AudioFX.Play(Sfx.Explosion);
            HUD?.Flash(null, UIStyle.Bad, 1.2f, Loc.F("grav.crash", Loc.Planet(body)));
        }

        // ══ Free flight ══════════════════════════════════════════════════════
        // No design, no clock, no fuel limit: fly the real Solar System of today with whoever is
        // connected, scan everything into your logbook, race, wave, or play laser tag.
        public bool FreeMode { get; private set; }

        public void StartFreeFlight()
        {
            if (State != GameState.Lobby && State != GameState.Scoreboard) return;
            if (Free == null || Ship == null) return;

            Events?.Cancel();
            ActiveCampaign = null;
            FreeMode = true;
            ShipTools.FreeFlight = true;
            Mission = MissionDesign.DefaultFor("mars");
            if (SpaceManager != null) SpaceManager.Locked = true;
            if (CameraFeed != null) CameraFeed.Play();

            SetShipFrozen(false);
            ApplySelectedShip();
            ConfigureShipFromDesign();
            var fuel = Ship.GetComponent<FuelSystem>();
            if (fuel != null) { fuel.Infinite = true; fuel.Refuel(1f); }
            Outcome.Clear();

            Free.Build(Ship.transform);
            ResetShip(Free.StartFacing);
            Rig?.Calibrate(false);
            Physics?.RegisterPlanetGravity("", Vector3.zero);   // planets pull through GravityField
            HUD?.SetRacePosition(1);

            _flightTimer = 0f;
            SetState(GameState.InFlight);
            SetPanels(hud: true);
            SetControls(true);
            CosmicSkyFX.MeteorShower(4f);
            HUD?.Flash(null, UIStyle.Accent, 0f, Loc.T("free.welcome"));

            // Tell everyone connected: they can jump in and meet you (same map on every phone)
            if (Senda != null && Senda.Initialized)
                Senda.Broadcast(NavMsg.Build(NavMsgType.shipEvent, Senda.NodeId,
                    $"{{\"free\":1,\"name\":\"{PlayerName}\",\"text\":\"{Loc.F("free.invite", PlayerName)}\"}}"));
        }

        void RespawnFree()
        {
            var col = Ship.GetComponent<CollisionSystem>();
            ExplosionVFX.Spawn(col != null ? col.ExplosionPrefab : null, Ship.transform.position, true);
            ResetShip(Free != null ? Free.StartFacing : Vector3.forward);
            if (col != null) { col.Lives = 3; col.StartInvincibility(); }
            Ship.GetComponent<FuelSystem>()?.Refuel(1f);
            HUD?.Flash(null, UIStyle.Warn, 0.6f, Loc.T("free.respawn"));
        }

        public void ExitFreeFlight()
        {
            if (!FreeMode) return;
            FreeMode = false;
            ShipTools.FreeFlight = false;
            SetControls(false);
            StopShip();
            SetShipFrozen(true);
            var fuel = Ship != null ? Ship.GetComponent<FuelSystem>() : null;
            if (fuel != null) fuel.Infinite = false;
            Free?.Clear();
            if (SpaceManager != null) SpaceManager.Locked = false;
            EnterLobby();
        }

        void OnSunTouched()
        {
            if (State == GameState.InFlight) HUD?.Flash(null, UIStyle.Bad, 1.2f, Loc.T("free.sun"));
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
            Gravity?.Clear();
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
            if (Mission == null) return;   // an imperfect design may fly: the mission shows what it costs
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
            ApplySelectedShip();
            ResetShip((target - launch).normalized);
            var compromises = LaunchCompromises();   // before the tanks are filled from the design
            ConfigureShipFromDesign();
            Outcome.Clear();
            Outcome.EventBonusPoints -= compromises.penalty;
            Outcome.EventLog.AddRange(compromises.notes);

            // North-align the view only when the planet came from real sky data
            Rig?.Calibrate(Compass != null && Compass.GpsReady);

            if (Physics != null)
            {
                Physics.SetEngineConfig(Mission.Engine);
                Physics.RegisterPlanetGravity(Mission.TargetPlanetId, target);
            }

            Powerups?.SpawnAlongRoute(launch, target);
            if (Ship != null) Asteroids?.SpawnAlongRoute(launch, target, Ship.transform);
            if (Ship != null && Gravity != null)
            {
                Gravity.PrefabFor = id => SpaceManager != null ? SpaceManager.GetPlanetPrefab(id) : null;
                Gravity.SpawnAlongRoute(Mission.TargetPlanetId, launch, target, Ship.transform);
                if (!string.IsNullOrEmpty(Gravity.Probe)) StartCoroutine(FlybyHint(Mission.TargetPlanetId, Gravity.Probe));
            }
            CosmicSkyFX.MeteorShower(4f);
            ShipTools.ResetDiscoveries();

            if (HUD != null)
            {
                if (MissionDesign.CommDelayLabel.TryGetValue(Mission.TargetPlanetId, out var delay))
                    HUD.SetCommDelay(delay);
                HUD.SetRacePosition(1);
            }

            _flightTimer = 0f;
            _cpOld = _cpNew = null; _cpTimer = 0f; _cpUsed = false;
            Events?.BeginFlight(Mission, Outcome, Ship);
            _rivalsAtStart = Senda != null ? Senda.ConnectedPeers.Count : 0;
            SetState(GameState.InFlight);
            SetPanels(hud: true);
            SetControls(true);

            Debug.Log($"[GM] Vuelo iniciado → {Mission.TargetPlanetId}");
        }

        // A design over its limits still launches, and pays for it the way a real mission would:
        // a rocket that can't lift the mass leaves propellant behind, and money over budget costs
        // points. (Too little ΔV or power need nothing here: the tank and the instruments show it.)
        const int PointsPerOverBudgetM = 4;

        (int penalty, List<string> notes) LaunchCompromises()
        {
            var notes = new List<string>();
            int penalty = 0;
            if (Mission == null) return (penalty, notes);

            float capacity = MissionDesign.LaunchCapacityKg[Mission.LaunchVeh];
            float excess = Mission.WetMassKg - capacity;
            if (excess > 0f)
            {
                float left = Mathf.Min(excess, Mathf.Max(0f, Mission.FuelMassKg - 100f));
                Mission.FuelMassKg -= left;
                if (left > 0f) notes.Add(Loc.F("launch.offload", left));
            }

            float over = Mission.TotalCostM - Mission.BudgetM;
            if (over > 0f)
            {
                penalty = Mathf.RoundToInt(over * PointsPerOverBudgetM);
                notes.Add(Loc.F("launch.overbudget", over, penalty));
            }
            return (penalty, notes);
        }

        // Fly the hull picked in the hangar (only rebuilt when the choice changed)
        int _appliedShip = -1;
        void ApplySelectedShip()
        {
            var catalog = ShipCatalog.Instance;
            if (catalog == null || Ship == null || catalog.Count == 0) return;
            int pick = ShipCatalog.Selected;
            if (pick == _appliedShip || (_appliedShip < 0 && pick == 0)) { _appliedShip = pick; return; }   // NV-01 is already in the scene
            catalog.ApplyTo(Ship.transform, pick);
            _appliedShip = pick;
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

            if (FreeMode)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null && kb.escapeKey.wasPressedThisFrame) { ExitFreeFlight(); return; }   // Android back / Esc
                // Nothing to fail: lose your hull and you're back at Earth with a fresh one
                var hull = Ship.GetComponent<CollisionSystem>();
                if (hull != null && hull.Lives <= 0) RespawnFree();
                return;
            }

            SaveCheckpoint();
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

        // ── Checkpoints ────────────────────────────────────────────────────────
        // Safety net for a flight that ended up somewhere it can't recover from (spinning away,
        // stuck against something, or a bug): the ship goes back to where it was a few seconds
        // ago, stopped and pointing at the planet, with the fuel and hull it had then. Nothing is
        // gained by it, so it costs nothing; the debrief says it was used.
        class Checkpoint { public Vector3 Pos; public float Fuel; public int Lives; }
        const float CheckpointEveryS = 8f;
        Checkpoint _cpOld, _cpNew;
        float _cpTimer;
        bool  _cpUsed;

        void SaveCheckpoint()
        {
            _cpTimer -= Time.deltaTime;
            if (_cpTimer > 0f || Ship == null) return;
            var col  = Ship.GetComponent<CollisionSystem>();
            var fuel = Ship.GetComponent<FuelSystem>();
            if (col != null && col.Lives <= 0) return;   // never keep a wreck
            _cpTimer = CheckpointEveryS;
            _cpOld = _cpNew ?? _cpOld;
            _cpNew = new Checkpoint
            {
                Pos = Ship.transform.position,
                Fuel = fuel != null ? fuel.FuelFraction : 1f,
                Lives = col != null ? col.Lives : 3,
            };
            _cpOld ??= _cpNew;
        }

        // HUD → CHECKPOINT (missions only; free flight has nothing to lose)
        public void RestoreCheckpoint()
        {
            if (State != GameState.InFlight || FreeMode || Ship == null) return;
            var cp = _cpOld ?? _cpNew;   // the older one: the newest may already be the trouble
            if (cp == null) return;

            Vector3 target = SpaceManager != null ? SpaceManager.TargetPosition : cp.Pos + Vector3.forward;
            Vector3 facing = target - cp.Pos;
            if (facing.sqrMagnitude < 1e-6f) facing = Vector3.forward;
            var rot = Quaternion.LookRotation(facing.normalized);
            Ship.transform.SetPositionAndRotation(cp.Pos, rot);
            var rb = Ship.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                rb.position = cp.Pos; rb.rotation = rot;
                rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
            }
            Ship.GetComponent<FuelSystem>()?.Restore(cp.Fuel);
            var col = Ship.GetComponent<CollisionSystem>();
            if (col != null) { col.Lives = cp.Lives; col.StartInvincibility(); }

            _cpNew = null; _cpTimer = CheckpointEveryS;   // the next one is saved from here
            AudioFX.Play(Sfx.Beep, 0.6f);
            HUD?.Flash(null, UIStyle.Accent, 0.3f, Loc.T("hud.checkpoint.back"));
            if (!_cpUsed) { _cpUsed = true; Outcome.EventLog.Add(Loc.T("hud.checkpoint.log")); }
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
            AudioFX.Play(Sfx.Go, 0.7f);   // arrival is the moment the whole flight was for
            RecordFlight(true);
            SetShipFrozen(true);
            Powerups?.ClearAll();
            Asteroids?.Clear();
            Gravity?.Clear();

            CosmicSkyFX.MeteorShower(6f);
            ArrivalPos = _rivalsArrived.Count + 1;
            SyncService?.BroadcastArrival(ArrivalPos);
            Chat?.ArrivalBroadcast(ArrivalPos);

            // Landing: go down to the world first
            if (Worlds != null && Navigator.Worlds.WorldDirector.HasWorld(Mission))
            {
                EnterWorld();
                return;
            }
            StartScienceOps();
        }

        void EnterWorld()
        {
            SetState(GameState.World);
            SetPanels();
            if (Ship != null) Ship.gameObject.SetActive(false);
            if (CameraFeed != null) CameraFeed.Stop();   // the world is a full 3D scene
            Worlds.Open(Mission, Outcome, SkyBodyPrefab, OnWorldDone);
            Debug.Log($"[GM] Mundo: {Mission?.TargetPlanetId}");
        }

        GameObject SkyBodyPrefab(string id)
        {
            if (SpaceManager == null) return null;
            return id switch
            {
                "phobos" => SpaceManager.MoonPhobos,
                "titan"  => SpaceManager.MoonTitan,
                "triton" => SpaceManager.MoonTriton,
                _        => SpaceManager.GetPlanetPrefab(id),
            };
        }

        void OnWorldDone(Navigator.Worlds.WorldResult r)
        {
            Outcome.WorldPlayed  = true;
            Outcome.WorldSuccess = r.Success;
            Outcome.WorldLanded  = r.Landed;
            Outcome.WorldStars   = r.Stars;
            Outcome.WorldPoints  = r.Points;
            Outcome.WorldFailKey = r.FailKey;
            Outcome.WorldLines.Clear();
            Outcome.WorldLines.AddRange(r.Lines);
            Outcome.EventBonusPoints += r.Points;   // counts as science in the score
            if (Ship != null) Ship.gameObject.SetActive(true);
            if (CameraFeed != null) CameraFeed.Play();
            StartScienceOps();
        }

        void StartScienceOps()
        {
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
            AudioFX.Play(reason == "fail.destroyed" ? Sfx.Explosion : Sfx.Alert);
            RecordFlight(false);
            SetShipFrozen(true);
            Outcome.FailReason = reason;
            Powerups?.ClearAll();
            Asteroids?.Clear();
            Gravity?.Clear();

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
                ClassroomSession.Instance?.ReportResult(ActiveCampaign.Index, total, stars, ArrivalPos > 0);
                ScoreboardUI?.SetCampaignResult(ActiveCampaign, stars, improved, HasNextCampaignMission);
            }
            else ScoreboardUI?.SetCampaignResult(null, 0, false, false);

            ScoreboardUI?.Show(total, solo ? 0 : _rivalsAtStart, Score, FailReason != null ? Loc.T(FailReason) : null,
                               MissionReport.Build(Mission, Outcome));
            FlutterBridge.ReportMission(Mission, Outcome, total, ArrivalPos,
                                        ActiveCampaign != null && !ActiveCampaign.IsChallenge
                                            ? Campaign.StarsFor(ActiveCampaign, total, ArrivalPos > 0) : -1);
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
            // In a class, reloading would drop the connection to the teacher: go back in place
            // The same goes for a team: reloading would make everyone scan the QR codes again
            var cls = ClassroomSession.Instance;
            bool team = Senda != null && Senda.ConnectedPeers.Count > 0;
            if (team || (cls != null && cls.Mode != ClassroomSession.Role.None))
            {
                Events?.Cancel();
                SetControls(false);
                SetShipFrozen(true);
                ActiveCampaign = null;
                EnterLobby();
                if (Campaign.JustCompleted >= 0) FindFirstObjectByType<CampaignUI>()?.Show();
                return;
            }
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
            if (msg.pl_json.Contains("\"hit\"") && IsForMe(msg.pl_json))
            {
                var sev = (CollisionSeverity)Mathf.Clamp((int)Num(msg.pl_json, "hit"), 0, 2);
                var v   = new Vector3(Num(msg.pl_json, "vx"), Num(msg.pl_json, "vy"), Num(msg.pl_json, "vz"));
                Ship.GetComponent<CollisionSystem>()?.ReceiveHit(sev, v);
            }

            // A rival's laser hit us (one hull point unless shielded) or they scanned us to say hi
            if (msg.pl_json.Contains("\"laser\"") && IsForMe(msg.pl_json))
            {
                string who = Str(msg.pl_json, "name") ?? "Rival";
                bool hurt = Ship.GetComponent<CollisionSystem>()?.ReceiveLaser() ?? false;
                HUD?.Flash(null, hurt ? UIStyle.Bad : UIStyle.Accent, hurt ? 1f : 0.3f, Loc.F("laser.got", who));
            }
            if (msg.pl_json.Contains("\"scan\"") && IsForMe(msg.pl_json))
            {
                AudioFX.Play(Sfx.Scan, 0.6f);
                HUD?.Flash(null, UIStyle.Accent, 0f, Loc.F("scan.got", Str(msg.pl_json, "name") ?? "Rival"));
            }
        }

        // Events aimed at one ship carry "to"; older clients omit it (then it's for everyone)
        bool IsForMe(string json)
        {
            string to = Str(json, "to");
            return string.IsNullOrEmpty(to) || Senda == null || to == Senda.NodeId;
        }

        static string Str(string json, string key)
        {
            string k = $"\"{key}\":\"";
            int i = json.IndexOf(k);
            if (i < 0) return null;
            i += k.Length;
            int e = json.IndexOf('"', i);
            return e > i ? json[i..e] : "";
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
            if (State == GameState.Scoreboard) PushRivalScores();
        }

        // ── Team play: everyone connected is one mission team ────────────────
        public int TeamSize => Senda != null ? Senda.ConnectedPeers.Count + 1 : 1;

        // My jobs in the team (empty when playing alone)
        public List<int> MyRoles() =>
            Senda == null ? new List<int>() : TeamRoles.For(Senda.NodeId, Senda.ConnectedPeers);

        List<int> RolesOf(string nodeId)
        {
            if (Senda == null) return new List<int>();
            var others = new List<string>(Senda.ConnectedPeers) { Senda.NodeId };
            others.Remove(nodeId);
            return TeamRoles.For(nodeId, others);
        }

        void OnRemoteScore(Dictionary<string, int> scores)
        {
            foreach (var kv in scores) _rivalScores[kv.Key] = kv.Value;
            if (State == GameState.Scoreboard) PushRivalScores();
        }

        void PushRivalScores()
        {
            // Keyed by what the ranking shows: "Ana · SCIENCE". Two players with the same name
            // must stay two rows, so a repeated label gets a number
            var named = new Dictionary<string, int>();
            foreach (var kv in _rivalScores)
            {
                string label = RemoteShips != null ? RemoteShips.NameOf(kv.Key) : kv.Key;
                var roles = RolesOf(kv.Key);
                if (roles.Count > 0) label += " · " + TeamRoles.Names(roles, true);
                string unique = label;
                for (int n = 2; named.ContainsKey(unique); n++) unique = $"{label} ({n})";
                named[unique] = kv.Value;
            }
            int arrived = (ArrivalPos > 0 ? 1 : 0);
            foreach (var id in _rivalsArrived) if (_rivalScores.ContainsKey(id)) arrived++;
            var mine = MyRoles();
            ScoreboardUI?.SetTeam(mine.Count > 0 ? TeamRoles.Names(mine, true) : null, arrived, ArrivalPos > 0);
            ScoreboardUI?.SetRivalScores(named);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        void SetControls(bool on)
        {
            if (ARShip != null) ARShip.ControlsEnabled = on;
            var tools = Ship != null ? Ship.GetComponent<ShipTools>() : null;
            if (tools != null) tools.ControlsEnabled = on;
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
