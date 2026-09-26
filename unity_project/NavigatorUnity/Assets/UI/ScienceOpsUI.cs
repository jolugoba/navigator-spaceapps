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

        private static readonly Dictionary<string, (float g, string atm)> PlanetData = new()
        {
            { "mercury", (3.70f,  "Sin atmósfera") },
            { "venus",   (8.87f,  "CO2 densa, 92 atm, 465 °C") },
            { "mars",    (3.72f,  "CO2 delgada, 0.006 atm") },
            { "jupiter", (24.79f, "H2 + He, sin superficie sólida") },
            { "saturn",  (10.44f, "H2 + He, anillos de hielo") },
            { "uranus",  (8.87f,  "H2 + He + CH4, gigante helado") },
            { "neptune", (11.15f, "H2 + He + CH4, vientos de 2,100 km/h") },
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
            foreach (var inst in design.Instruments) _ops.Add(new Op { Inst = inst });

            if (_outcome != null)
            {
                _outcome.InstrumentsPlanned = _ops.Count;
                _outcome.DataTotalMb        = design.TotalDataMb;
            }
            if (_score != null)
            {
                _score.ArrivalPosition = arrivalPosition;
                _score.SciencePoints   = 0;
            }

            BuildItems();
            RefreshHeader(arrivalPosition);
            SetCompleteButton(false, "Operaciones en curso…");
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
            float available = _design.EffectivePowerW;
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

        int Points(ScienceInstrument inst) =>
            Mathf.RoundToInt(MissionDesign.InstrumentSciencePoints[inst] * _design.ScienceMultiplier);

        void Finish()
        {
            if (_done) return;
            _done = true;

            foreach (var op in _ops)
            {
                if (op.Stage == Stage.Received) continue;
                op.Stage = Stage.Lost;
                if (_outcome != null) _outcome.PointsLost += Points(op.Inst);
            }
            _transmitting = null;
            _queue.Clear();

            RefreshItems();
            RefreshLiveLabels();
            if (WindowLabel != null) WindowLabel.text = "Ventana cerrada";
            SetCompleteButton(true, "COMPLETAR MISIÓN");
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
                if (label != null) label.text = InstrumentDisplayName(op.Inst);
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
                    Stage.WaitingPower => (op.Scan, UIStyle.Warn,   "Esperando energía"),
                    Stage.Scanning     => (op.Scan, UIStyle.Accent, $"Escaneando {op.Scan:P0}"),
                    Stage.Queued       => (0f,      UIStyle.Muted,  "En cola de envío"),
                    Stage.Transmitting => (op.SentMb / size, UIStyle.Gold, $"Enviando {op.SentMb:F0}/{size:F0} Mb"),
                    Stage.InTransit    => (1f,      UIStyle.Gold,   $"Viajando a la Tierra {op.TransitLeft:F0} s"),
                    Stage.Received     => (1f,      UIStyle.Good,   $"+{Points(op.Inst):N0} pts"),
                    _                  => (1f,      UIStyle.Bad,    "Perdido: sin ventana"),
                };
                if (bar != null) { bar.fillAmount = fill; bar.color = color; }
                if (pts != null) { pts.text = text; pts.color = color; }
            }
        }

        void RefreshLiveLabels()
        {
            float avail = _design.EffectivePowerW;
            if (PowerBar   != null) PowerBar.fillAmount = Mathf.Clamp01(_activePowerW / Mathf.Max(1f, avail));
            if (PowerLabel != null) PowerLabel.text     = $"{_activePowerW:F0} / {avail:F0} W";
            if (PowerStatusLabel != null)
            {
                bool waiting = _ops.Exists(o => o.Stage == Stage.WaitingPower);
                PowerStatusLabel.text  = waiting ? "ESPERANDO ENERGÍA" : "OK";
                PowerStatusLabel.color = waiting ? UIStyle.Warn : UIStyle.Good;
            }

            if (WindowLabel != null && !_done)
            {
                float t = Mathf.Max(0f, _windowLeft);
                WindowLabel.text  = $"Ventana de comunicación {(int)t / 60}:{(int)t % 60:00}";
                WindowLabel.color = t < 15f ? UIStyle.Bad : UIStyle.Text;
            }

            if (_fuel != null)
            {
                if (FuelBar   != null) FuelBar.fillAmount = _fuel.FuelFraction;
                if (FuelLabel != null) FuelLabel.text     = $"Combustible {_fuel.FuelFraction:P0}";
            }

            if (_score != null)
            {
                _score.FuelFraction = _fuel != null ? _fuel.FuelFraction : 1f;
                int total = _score.TotalScore();
                if (SciencePointsLabel != null) SciencePointsLabel.text = $"{_score.SciencePoints:N0} pts recibidos";
                if (TotalScoreLabel    != null) TotalScoreLabel.text    = $"TOTAL {total:N0}";
                if (ScoreBar           != null) ScoreBar.fillAmount     = Mathf.Clamp01(total / (float)ScoreboardUI.ScoreBarMax);
            }
        }

        void RefreshHeader(int arrivalPosition)
        {
            string planet = MissionDesignUI.PlanetDisplayName(_design.TargetPlanetId);
            if (PlanetNameLabel != null) PlanetNameLabel.text = planet.ToUpper();
            if (ArrivalPositionLabel != null)
                ArrivalPositionLabel.text = arrivalPosition == 1 ? "#1 PRIMERO EN LLEGAR" : $"#{arrivalPosition} EN LLEGAR";
            if (MissionObjectiveLabel != null)
                MissionObjectiveLabel.text = $"{ObjectiveLabel(_design.Objective)} · ciencia ×{_design.ScienceMultiplier:0.#}";
            if (CommDelayLabel != null && MissionDesign.CommDelayLabel.TryGetValue(_design.TargetPlanetId, out var d))
                CommDelayLabel.text = $"Señal real: {d} de ida · {_design.TotalDataMb:F0} Mb por enviar";
            if (DownlinkLabel != null)
                DownlinkLabel.text = $"{AntennaLabel(_design.Antenna)} · {_downlinkMbps:0.#} Mb/s · luz {_delayS:0.#} s";

            if (PlanetData.TryGetValue(_design.TargetPlanetId, out var data))
            {
                if (GravityLabel    != null) GravityLabel.text    = $"Gravedad superficial {data.g:F2} m/s²";
                if (AtmosphereLabel != null) AtmosphereLabel.text = $"Atmósfera: {data.atm}";
            }
        }

        void SetCompleteButton(bool on, string label)
        {
            if (CompleteMissionBtn != null) CompleteMissionBtn.interactable = on;
            if (CompleteBtnLabel   != null) CompleteBtnLabel.text = label;
        }

        // ── Nombres ────────────────────────────────────────────────────────────

        public static string InstrumentDisplayName(ScienceInstrument inst) => inst switch
        {
            ScienceInstrument.WideAngleCamera   => "Cámara gran angular",
            ScienceInstrument.NarrowAngleCamera => "Cámara teleobjetivo",
            ScienceInstrument.UVSpectrometer    => "Espectrómetro UV",
            ScienceInstrument.InfraredMapper    => "Mapeador infrarrojo",
            ScienceInstrument.Magnetometer      => "Magnetómetro",
            ScienceInstrument.ParticleDetector  => "Detector de partículas",
            ScienceInstrument.RadioScience      => "Radio ciencia",
            ScienceInstrument.RadarAltimeter    => "Radar altímetro",
            _ => inst.ToString(),
        };

        public static string ObjectiveLabel(string obj) => obj switch
        {
            "flyby"   => "Sobrevuelo",
            "orbit"   => "Órbita",
            "landing" => "Aterrizaje",
            _ => obj,
        };

        public static string AntennaLabel(AntennaType a) => a switch
        {
            AntennaType.LowGain    => "Antena baja ganancia",
            AntennaType.MediumGain => "Antena media ganancia",
            _                      => "Antena alta ganancia",
        };
    }
}
