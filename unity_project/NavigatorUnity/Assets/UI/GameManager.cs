// Navigator 2.0 — GameManager.cs
// Máquina de estados del juego: Lobby → Diseño → Cuenta atrás → Vuelo → Llegada → Ciencia → Scoreboard
// Es el director que conecta todos los demás scripts
// Guacalita Inc. — SAL / Claude

using System.Collections;
using UnityEngine;
using UnityEngine.Events;
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

        [Header("Scripts de juego")]
        public MissionDesignUI  MissionDesignUI;
        public ScienceOpsUI     ScienceOpsUI;
        public ScoreboardUI     ScoreboardUI;
        public CockpitHUD       HUD;
        public LobbyManager     Lobby;

        [Header("Nave local")]
        public ShipController   Ship;
        public PhysicsEngine    Physics;
        public ARShipController ARShip;

        [Header("AR")]
        public ARSpaceManager   SpaceManager;
        public ARCompassTracker Compass;
        public CameraFeedBackground CameraFeed;

        [Header("Red")]
        public SendaBridge      Senda;
        public ShipSyncService  SyncService;
        public P2PChatSystem    Chat;

        [Header("Llegada al planeta")]
        public float ArrivalDistanceM  = 0.6f;   // metros en AR — distancia que dispara la llegada
        public float MaxFlightTimeS    = 300f;    // tiempo límite antes de forzar llegada

        // ── Estado runtime ─────────────────────────────────────────────────────
        public  GameState     State       { get; private set; } = GameState.Lobby;
        public  MissionDesign Mission     { get; private set; }
        public  int           ArrivalPos  { get; private set; } = 0;  // 0=nadie llegó, 1=primero, 2=segundo

        public UnityEvent<GameState> OnStateChanged = new();

        private float      _flightTimer  = 0f;
        private bool       _localArrived = false;
        private int        _remoteScore  = 0;

        // ── Init ──────────────────────────────────────────────────────────────

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            // Senda init
            if (Senda != null && !Senda.Initialized)
                Senda.Init();

            // Escuchar peer connections desde el Lobby
            Senda?.OnPeerConnected.AddListener(OnPeerConnected);

            // Escuchar estado final del rival
            SyncService?.OnRemoteScore.AddListener(OnRemoteScore);

            EnterLobby();
        }

        // ── Máquina de estados ─────────────────────────────────────────────────

        void EnterLobby()
        {
            SetState(GameState.Lobby);
            SetPanels(lobby: true);
        }

        // Llamado desde LobbyManager.StartGameButton (OnClick en Inspector)
        public void OnLobbyReady()
        {
            SetState(GameState.MissionDesign);
            SetPanels(missionDesign: true);

            // The lobby QR scanner has released the camera by now
            if (CameraFeed != null) CameraFeed.Play();

            string planet = "mars"; // default — se puede cambiar desde el diseño
            MissionDesignUI?.Show(planet);
        }

        // Llamado desde MissionDesignUI.LaunchButton (OnClick en Inspector)
        public void OnLaunchConfirmed()
        {
            Mission = MissionDesignUI?.GetDesign();
            if (Mission == null) return;

            SetState(GameState.Countdown);
            SetPanels(countdown: true);
            StartCoroutine(CountdownCoroutine());
        }

        IEnumerator CountdownCoroutine()
        {
            string[] labels = { "3", "2", "1", "¡GO!" };
            foreach (string l in labels)
            {
                if (CountdownLabel != null) CountdownLabel.text = l;
                yield return new WaitForSeconds(1f);
            }

            StartFlight();
        }

        void StartFlight()
        {
            SetState(GameState.InFlight);
            SetPanels(hud: true);
            _flightTimer  = 0f;
            _localArrived = false;

            // Configura física según diseño
            if (Physics != null && Mission != null)
            {
                Physics.SetEngineConfig(Mission.Engine);
                Physics.RegisterPlanetGravity(
                    Mission.TargetPlanetId,
                    SpaceManager != null
                        ? SpaceManager.transform.position + SpaceManager.DirectionToTarget * 3f
                        : Vector3.forward * 3f);
            }

            // Activa el giroscopio
            ARShip?.CalibrateGyro();

            // Configura el HUD con el delay de comunicación
            if (Mission != null && HUD != null &&
                MissionDesign.CommDelayLabel.TryGetValue(Mission.TargetPlanetId, out var delay))
                HUD.SetCommDelay(delay);

            // Inicia brújula hacia el planeta
            Compass?.SetTarget(Mission?.TargetPlanetId ?? "mars");

            Debug.Log($"[GM] Vuelo iniciado → {Mission?.TargetPlanetId}");
        }

        void EnterScienceOps()
        {
            if (_localArrived) return;
            _localArrived = true;

            ArrivalPos = ArrivalPos == 0 ? 1 : 2;   // primero o segundo

            SetState(GameState.ScienceOps);
            SetPanels(scienceOps: true);

            // Notifica al rival
            Chat?.ArrivalBroadcast(ArrivalPos);

            // Muestra pantalla de ciencia
            ScienceOpsUI?.ShowOnArrival(Mission, ArrivalPos);

            Debug.Log($"[GM] Llegamos a {Mission?.TargetPlanetId} — posición #{ArrivalPos}");
        }

        // Llamado desde ScienceOpsUI.CompleteMissionBtn (OnClick en Inspector)
        public void OnMissionComplete()
        {
            var score = FindObjectOfType<MissionScore>();
            int total = score != null ? score.TotalScore() : 0;

            SyncService?.BroadcastMissionEnd(Senda?.NodeId ?? "", total);

            SetState(GameState.Scoreboard);
            SetPanels(scoreboard: true);
            ScoreboardUI?.Show(total, _remoteScore, score);
        }

        // ── Update: detección de llegada en vuelo ──────────────────────────────

        void Update()
        {
            if (State != GameState.InFlight) return;

            _flightTimer += Time.deltaTime;

            // Detecta llegada: ship cerca del planeta en AR
            if (Ship != null && SpaceManager != null)
            {
                Vector3 planetPos = SpaceManager.transform.position
                                  + SpaceManager.DirectionToTarget * 3f;
                float dist = Vector3.Distance(Ship.transform.position, planetPos);

                if (dist < ArrivalDistanceM)
                {
                    EnterScienceOps();
                    return;
                }
            }

            // Tiempo límite agotado → llegada forzada
            if (_flightTimer >= MaxFlightTimeS)
                EnterScienceOps();
        }

        // ── Eventos de red ─────────────────────────────────────────────────────

        void OnPeerConnected(string nodeId)
        {
            Debug.Log($"[GM] Peer conectado: {nodeId}");
            // El lobby ya maneja el botón de inicio — aquí solo registramos
        }

        void OnRemoteScore(System.Collections.Generic.Dictionary<string, int> scores)
        {
            foreach (var v in scores.Values)
                _remoteScore = v;   // toma el último score recibido

            // Si el rival llegó primero y nosotros seguimos volando → somos segundo
            if (State == GameState.InFlight && ArrivalPos == 0)
            {
                ArrivalPos = 2;
                Chat?.ArrivalBroadcast(ArrivalPos);
            }
        }

        // ── Helpers UI ─────────────────────────────────────────────────────────

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
