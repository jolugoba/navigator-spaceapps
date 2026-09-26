// Navigator 2.0 — ScienceOpsUI.cs
// Operaciones científicas al llegar al planeta. Cada instrumento pasa por:
//   energía → escaneo → cola de envío → transmisión (antena) → viaje a la luz → recibido en la Tierra
// Solo los datos que llegan a la Tierra antes de que cierre la ventana de comunicación suman puntos.
// Guacalita Inc. — SAL / Claude

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Navigator.Core;
using Navigator.Game;

namespace Navigator.UI
{
    public class ScienceOpsUI : MonoBehaviour
    {
        [Header("Panel raíz")]
        public GameObject Panel;

        [Header("Cabecera")]
        public TMP_Text PlanetNameLabel;       // "MARTE"
        public TMP_Text ArrivalPositionLabel;  // "#1 PRIMERO EN LLEGAR"
        public TMP_Text MissionObjectiveLabel; // "Órbita · ciencia ×1.5"
        public TMP_Text CommDelayLabel;        // "Señal: 3-22 min ida"

        [Header("Comunicaciones")]
        public TMP_Text WindowLabel;           // "Ventana: 0:52"
        public TMP_Text DownlinkLabel;         // "Antena media · 6 Mb/s · luz 2.5 s"

        [Header("Potencia")]
        public Image    PowerBar;
        public TMP_Text PowerLabel;            // "175 / 500 W"
        public TMP_Text PowerStatusLabel;      // "OK" / "ESPERANDO ENERGÍA"

        [Header("Instrumentos")]
        public Transform  InstrumentsContainer;
        public GameObject InstrumentItemPrefab; // hijos: "Label" TMP, "Bar" Image, "PtsLabel" TMP

        [Header("Score")]
        public TMP_Text SciencePointsLabel;
        public TMP_Text TotalScoreLabel;
        public Image    ScoreBar;

        [Header("Combustible")]
        public Image    FuelBar;
        public TMP_Text FuelLabel;

        [Header("Info planeta")]
        public TMP_Text GravityLabel;
        public TMP_Text AtmosphereLabel;

        [Header("Completar")]
        public Button   CompleteMissionBtn;
        public TMP_Text CompleteBtnLabel;

        [Header("Ventana de comunicación")]
        public float WindowBaseS = 75f;        // + retardo de la luz del planeta

        // ── Runtime ────────────────────────────────────────────────────────────
        enum Stage { WaitingPower, Scanning, Queued, Transmitting, InTransit, Received, Lost }

        class Op
        {
            public ScienceInstrument Inst;
            public Stage  Stage = Stage.WaitingPower;
            public float  Scan;        // 0..1
            public float  SentMb;
            public float  TransitLeft; // s
            public bool   FailedInCruise;
            public Transform Item;
        }

        private readonly List<Op>  _ops   = new();
        private readonly Queue<Op> _queue = new();
        private Op _transmitting;

        private MissionDesign  _design;
        private MissionOutcome _outcome;
        private MissionScore   _score;
        private FuelSystem     _fuel;

        private float _windowLeft;
        private float _downlinkMbps;
        private float _delayS;
        private float _activePowerW;
        private bool  _done;

        private static readonly Dictionary<ScienceInstrument, float> ScanTime = new()
        {
            { ScienceInstrument.WideAngleCamera,   8f  },
            { ScienceInstrument.NarrowAngleCamera, 12f },
            { ScienceInstrument.UVSpectrometer,    20f },
            { ScienceInstrument.InfraredMapper,    25f },
            { ScienceInstrument.Magnetometer,      6f  },
            { ScienceInstrument.ParticleDetector,  18f },
            { ScienceInstrument.RadioScience,      5f  },
            { ScienceInstrument.RadarAltimeter,    30f },
        };

        private static readonly Dictionary<string, float> SurfaceGravity = new()
        {
            { "mercury", 3.70f }, { "venus", 8.87f }, { "mars", 3.72f }, { "jupiter", 24.79f },
            { "saturn", 10.44f }, { "uranus", 8.87f }, { "neptune", 11.15f },
        };

        void Awake()
        {
            _score = FindObjectOfType<MissionScore>();
            var ship = GameObject.FindWithTag("LocalShip");
            if (ship != null) _fuel = ship.GetComponent<FuelSystem>();
            CompleteMissionBtn?.onClick.AddListener(CompleteMission);
        }

        // ── Entrada al planeta ─────────────────────────────────────────────────

        public void ShowOnArrival(MissionDesign design, int arrivalPosition, MissionOutcome outcome)
        {
            _design  = design;
            _outcome = outcome;
            _done    = false;

            _downlinkMbps = MissionDesign.GameDownlinkMbps[design.Antenna];
            _delayS       = MissionDesign.CommDelayGameS.TryGetValue(design.TargetPlanetId, out var d) ? d : 3f;
            _windowLeft   = WindowBaseS + _delayS;

            _ops.Clear();
            _queue.Clear();
            _transmitting = null;
            foreach (var inst in design.Instruments)
            {
                bool failed = outcome != null && outcome.LostInstruments.Contains(inst);
                _ops.Add(new Op { Inst = inst, FailedInCruise = failed, Stage = failed ? Stage.Lost : Stage.WaitingPower });
            }

            if (_outcome != null)
            {
                _outcome.InstrumentsPlanned = _ops.Count;
                _outcome.DataTotalMb        = design.TotalDataMb;
            }
            if (_score != null)
            {
                _score.ArrivalPosition = arrivalPosition;
                // Science earned during cruise decisions (e.g. measuring a solar storm)
                _score.SciencePoints   = _outcome != null ? _outcome.EventBonusPoints : 0;
            }

            BuildItems();
            RefreshHeader(arrivalPosition);
            SetCompleteButton(false, Loc.T("sci.busy"));
            if (Panel != null) Panel.SetActive(true);

            // A design with no instruments has nothing to do here
            if (_ops.Count == 0) Finish();
        }

        void Update()
        {
            if (_design == null || _done || Panel == null || !Panel.activeSelf) return;
            float dt = Time.deltaTime;

            _windowLeft -= dt;
            RunScans(dt);
            RunDownlink(dt);
            RunTransit(dt);

            RefreshItems();
            RefreshLiveLabels();

            bool allSettled = _ops.TrueForAll(o => o.Stage == Stage.Received || o.Stage == Stage.Lost);
            if (allSettled || _windowLeft <= 0f) Finish();
        }

        // Instruments scan in parallel as long as bus + active instruments fit the power budget
        void RunScans(float dt)
        {
            float available = AvailablePowerW;
            _activePowerW   = MissionDesign.BusPowerW;
            bool anyWaiting = false;

            foreach (var op in _ops)
            {
                if (op.Stage != Stage.WaitingPower && op.Stage != Stage.Scanning) continue;
                float need = MissionDesign.InstrumentPowerW[op.Inst];
                if (_activePowerW + need <= available)
                {
                    _activePowerW += need;
                    op.Stage = Stage.Scanning;
                    op.Scan += dt / (ScanTime.TryGetValue(op.Inst, out var t) ? t : 15f);
                    if (op.Scan >= 1f)
                    {
                        op.Scan  = 1f;
                        op.Stage = Stage.Queued;
                        _queue.Enqueue(op);
                        if (_outcome != null) _outcome.InstrumentsScanned++;
                    }
                }
                else
                {
                    op.Stage   = Stage.WaitingPower;
                    anyWaiting = true;
                }
            }
            if (anyWaiting && _outcome != null) _outcome.PowerWaitS += dt;
        }

        // One data product at a time through the antenna
        void RunDownlink(float dt)
        {
            if (_transmitting == null && _queue.Count > 0)
            {
                _transmitting = _queue.Dequeue();
                _transmitting.Stage = Stage.Transmitting;
            }
            if (_transmitting == null) return;

            float size = MissionDesign.InstrumentDataMb[_transmitting.Inst];
            float sent = Mathf.Min(_downlinkMbps * dt, size - _transmitting.SentMb);
            _transmitting.SentMb += sent;
            if (_outcome != null) _outcome.DataSentMb += sent;

            if (_transmitting.SentMb >= size - 1e-4f)
            {
                _transmitting.Stage       = Stage.InTransit;
                _transmitting.TransitLeft = _delayS;
                _transmitting = null;
            }
        }

        // The last bit still has to cross the solar system at the speed of light
        void RunTransit(float dt)
        {
            foreach (var op in _ops)
            {
                if (op.Stage != Stage.InTransit) continue;
                op.TransitLeft -= dt;
                if (op.TransitLeft > 0f) continue;

                op.Stage = Stage.Received;
                int pts = Points(op.Inst);
                if (_score != null) _score.SciencePoints += pts;
                if (_outcome != null)
                {
                    _outcome.InstrumentsReceived++;
                    _outcome.PointsReceived += pts;
                }
            }
        }

        float AvailablePowerW => _design.EffectivePowerW - (_outcome != null ? _outcome.PowerLossW : 0f);

        int Points(ScienceInstrument inst) =>
            _design.InstrumentPoints(inst);

        void Finish()
        {
            if (_done) return;
            _done = true;

            foreach (var op in _ops)
            {
                if (op.Stage == Stage.Received || op.FailedInCruise) continue;
                op.Stage = Stage.Lost;
                if (_outcome != null) _outcome.PointsLost += Points(op.Inst);
            }
            _transmitting = null;
            _queue.Clear();

            RefreshItems();
            RefreshLiveLabels();
            if (WindowLabel != null) WindowLabel.text = Loc.T("sci.windowclosed");
            SetCompleteButton(true, Loc.T("sci.complete"));
        }

        void CompleteMission()
        {
            if (!_done) return;
            if (_score != null) _score.FuelFraction = _fuel != null ? _fuel.FuelFraction : 1f;
            if (Panel != null) Panel.SetActive(false);
            GameManager.Instance?.OnMissionComplete();
        }

        // ── UI ─────────────────────────────────────────────────────────────────

        void BuildItems()
        {
            if (InstrumentsContainer == null) return;
            foreach (Transform child in InstrumentsContainer) Destroy(child.gameObject);
            if (InstrumentItemPrefab == null) return;

            foreach (var op in _ops)
            {
                var item = Instantiate(InstrumentItemPrefab, InstrumentsContainer);
                item.name = op.Inst.ToString();
                op.Item = item.transform;
                var label = op.Item.Find("Label")?.GetComponent<TMP_Text>();
                if (label != null) label.text = Loc.Instrument(op.Inst);
            }
        }

        void RefreshItems()
        {
            foreach (var op in _ops)
            {
                if (op.Item == null) continue;
                var bar = op.Item.Find("Bar")?.GetComponent<Image>();
                var pts = op.Item.Find("PtsLabel")?.GetComponent<TMP_Text>();
                float size = MissionDesign.InstrumentDataMb[op.Inst];

                (float fill, Color color, string text) = op.Stage switch
                {
                    Stage.WaitingPower => (op.Scan, UIStyle.Warn,   Loc.T("sci.st.power")),
                    Stage.Scanning     => (op.Scan, UIStyle.Accent, Loc.F("sci.st.scan", op.Scan)),
                    Stage.Queued       => (0f,      UIStyle.Muted,  Loc.T("sci.st.queued")),
                    Stage.Transmitting => (op.SentMb / size, UIStyle.Gold, Loc.F("sci.st.sending", op.SentMb, size)),
                    Stage.InTransit    => (1f,      UIStyle.Gold,   Loc.F("sci.st.transit", op.TransitLeft)),
                    Stage.Received     => (1f,      UIStyle.Good,   Loc.F("sci.st.received", Points(op.Inst))),
                    _                  => (1f,      UIStyle.Bad,    Loc.T(op.FailedInCruise ? "sci.st.failed" : "sci.st.lost")),
                };
                if (bar != null) { bar.fillAmount = fill; bar.color = color; }
                if (pts != null) { pts.text = text; pts.color = color; }
            }
        }

        void RefreshLiveLabels()
        {
            float avail = AvailablePowerW;
            if (PowerBar   != null) PowerBar.fillAmount = Mathf.Clamp01(_activePowerW / Mathf.Max(1f, avail));
            if (PowerLabel != null) PowerLabel.text     = $"{_activePowerW:F0} / {avail:F0} W";
            if (PowerStatusLabel != null)
            {
                bool waiting = _ops.Exists(o => o.Stage == Stage.WaitingPower);
                PowerStatusLabel.text  = waiting ? Loc.T("sci.waitpower") : Loc.T("design.ok");
                PowerStatusLabel.color = waiting ? UIStyle.Warn : UIStyle.Good;
            }

            if (WindowLabel != null && !_done)
            {
                float t = Mathf.Max(0f, _windowLeft);
                WindowLabel.text  = Loc.F("sci.window", (int)t / 60, (int)t % 60);
                WindowLabel.color = t < 15f ? UIStyle.Bad : UIStyle.Text;
            }

            if (_fuel != null)
            {
                if (FuelBar   != null) FuelBar.fillAmount = _fuel.FuelFraction;
                if (FuelLabel != null) FuelLabel.text     = Loc.F("sci.fuel", _fuel.FuelFraction);
            }

            if (_score != null)
            {
                _score.FuelFraction = _fuel != null ? _fuel.FuelFraction : 1f;
                int total = _score.TotalScore();
                if (SciencePointsLabel != null) SciencePointsLabel.text = Loc.F("sci.received", _score.SciencePoints);
                if (TotalScoreLabel    != null) TotalScoreLabel.text    = Loc.F("sci.total", total);
                if (ScoreBar           != null) ScoreBar.fillAmount     = Mathf.Clamp01(total / (float)ScoreboardUI.ScoreBarMax);
            }
        }

        void RefreshHeader(int arrivalPosition)
        {
            string planet = Loc.Planet(_design.TargetPlanetId);
            if (PlanetNameLabel != null) PlanetNameLabel.text = planet.ToUpper();
            if (ArrivalPositionLabel != null)
                ArrivalPositionLabel.text = arrivalPosition == 1 ? Loc.T("sci.first") : Loc.F("sci.nth", arrivalPosition);
            if (MissionObjectiveLabel != null)
                MissionObjectiveLabel.text = Loc.F("sci.objective", Loc.Objective(_design.Objective), _design.ScienceMultiplier);
            if (CommDelayLabel != null && MissionDesign.CommDelayLabel.TryGetValue(_design.TargetPlanetId, out var d))
                CommDelayLabel.text = Loc.F("sci.signal", d, _design.TotalDataMb);
            if (DownlinkLabel != null)
                DownlinkLabel.text = Loc.F("sci.downlink", Loc.T("antenna." + _design.Antenna), _downlinkMbps, _delayS);

            if (SurfaceGravity.TryGetValue(_design.TargetPlanetId, out var g))
            {
                if (GravityLabel    != null) GravityLabel.text    = Loc.F("sci.gravity", g);
                if (AtmosphereLabel != null) AtmosphereLabel.text = Loc.F("sci.atmosphere", Loc.T("atm." + _design.TargetPlanetId));
            }
        }

        void SetCompleteButton(bool on, string label)
        {
            if (CompleteMissionBtn != null) CompleteMissionBtn.interactable = on;
            if (CompleteBtnLabel   != null) CompleteBtnLabel.text = label;
        }

        // ── Nombres (kept for callers; all text comes from Loc) ────────────────
        public static string InstrumentDisplayName(ScienceInstrument inst) => Loc.Instrument(inst);
        public static string ObjectiveLabel(string obj) => Loc.Objective(obj);
    }
}
