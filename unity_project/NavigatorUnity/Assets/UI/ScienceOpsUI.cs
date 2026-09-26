// Navigator 2.0 — ScienceOpsUI.cs
// Pantalla de operaciones científicas al llegar al planeta objetivo
// Muestra el progreso de cada instrumento, presupuesto de potencia y score final
// Ported from Dart: navigator/lib/screens/science_ops_screen.dart
// Guacalita Inc. — SAL / Claude

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Navigator.Core;
using Navigator.Game;
using Navigator.Network;

namespace Navigator.UI
{
    public class ScienceOpsUI : MonoBehaviour
    {
        [Header("Panel raíz")]
        public GameObject Panel;

        // ── Cabecera ───────────────────────────────────────────────────────────
        [Header("Cabecera")]
        public TMP_Text PlanetNameLabel;     // "MARTE"
        public TMP_Text ArrivalPositionLabel; // "#1 PRIMERO"
        public TMP_Text MissionObjectiveLabel; // "Objetivo: Órbita"
        public TMP_Text CommDelayLabel;       // "Señal: 3-22 min ida"

        // ── Presupuesto de potencia ────────────────────────────────────────────
        [Header("Potencia")]
        public Image    PowerBar;            // fill: usado / disponible
        public TMP_Text PowerLabel;          // "148 / 280 W"
        public TMP_Text PowerStatusLabel;    // "OK" / "SIN POTENCIA"

        // ── Instrumentos ───────────────────────────────────────────────────────
        [Header("Instrumentos")]
        public Transform InstrumentsContainer; // padre donde se crean los items
        public GameObject InstrumentItemPrefab; // prefab: label + barra de progreso + pts

        // ── Score en vivo ──────────────────────────────────────────────────────
        [Header("Score")]
        public TMP_Text SciencePointsLabel;  // "2,400 pts ciencia"
        public TMP_Text TotalScoreLabel;     // "TOTAL: 4,650"
        public Image    ScoreBar;            // llena mientras se acumulan pts

        // ── Combustible restante ───────────────────────────────────────────────
        [Header("Combustible")]
        public Image    FuelBar;
        public TMP_Text FuelLabel;           // "38%"

        // ── Gravedad del planeta ───────────────────────────────────────────────
        [Header("Info planeta")]
        public TMP_Text GravityLabel;        // "g = 3.72 m/s²"
        public TMP_Text AtmosphereLabel;     // "Atmósfera: CO₂ delgada"

        // ── Botón completar ────────────────────────────────────────────────────
        [Header("Completar")]
        public Button   CompleteMissionBtn;
        public TMP_Text CompleteBtnLabel;    // "COMPLETAR MISIÓN" / "Operaciones en curso…"

        // ── Datos runtime ──────────────────────────────────────────────────────
        private MissionDesign   _design;
        private MissionScore    _score;
        private FuelSystem      _fuel;

        // Progreso de cada instrumento (0 a 1)
        private readonly Dictionary<ScienceInstrument, float> _instProgress = new();
        private readonly Dictionary<ScienceInstrument, int>   _instEarned   = new();
        private bool _operationsComplete = false;

        // Duración de escaneo por instrumento (segundos de juego)
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

        // Datos estáticos del planeta (gravedad superficial m/s², atmósfera)
        private static readonly Dictionary<string, (float g, string atm)> PlanetData = new()
        {
            { "mercury", (3.70f,  "Sin atmósfera") },
            { "venus",   (8.87f,  "CO₂ densa — 92 atm") },
            { "mars",    (3.72f,  "CO₂ delgada — 0.006 atm") },
            { "jupiter", (24.79f, "H₂ + He — sin superficie sólida") },
            { "saturn",  (10.44f, "H₂ + He — anillos de hielo") },
            { "uranus",  (8.87f,  "H₂O + NH₃ + CH₄ — planeta helado") },
            { "neptune", (11.15f, "H₂ + He + CH₄ — vientos 2,100 km/h") },
        };

        // ── Ciclo de vida ──────────────────────────────────────────────────────

        void Awake()
        {
            _score = FindObjectOfType<MissionScore>();

            var ship = GameObject.FindWithTag("LocalShip");
            if (ship != null) _fuel = ship.GetComponent<FuelSystem>();

            CompleteMissionBtn?.onClick.AddListener(CompleteMission);
        }

        void Update()
        {
            if (Panel == null || !Panel.activeSelf || _operationsComplete) return;

            if (_design != null)
            {
                UpdateInstrumentProgress();
                UpdatePowerBudget();
                UpdateFuel();
                UpdateScore();
            }
        }

        // ── Activar pantalla al llegar al planeta ──────────────────────────────

        public void ShowOnArrival(MissionDesign design, int arrivalPosition)
        {
            _design = design;

            if (_score != null)
            {
                _score.ArrivalPosition = arrivalPosition;
                _score.FuelFraction    = _fuel != null ? _fuel.FuelFraction : 1f;
            }

            // Inicia el progreso de todos los instrumentos
            _instProgress.Clear();
            _instEarned.Clear();
            foreach (var inst in _design.Instruments)
            {
                _instProgress[inst] = 0f;
                _instEarned[inst]   = 0;
            }
            _operationsComplete = false;

            BuildInstrumentItems();
            RefreshHeader();
            if (ArrivalPositionLabel != null)
                ArrivalPositionLabel.text = arrivalPosition == 1 ? "#1 PRIMERO EN LLEGAR" : $"#{arrivalPosition} EN LLEGAR";
            RefreshPlanetInfo();
            if (Panel != null) Panel.SetActive(true);

            Debug.Log($"[ScienceOps] Llegamos a {_design.TargetPlanetId} en posición #{arrivalPosition}");
        }

        // ── Construcción dinámica de items de instrumentos ─────────────────────

        void BuildInstrumentItems()
        {
            if (InstrumentsContainer == null || InstrumentItemPrefab == null) return;

            foreach (Transform child in InstrumentsContainer)
                Destroy(child.gameObject);

            foreach (var inst in _design.Instruments)
            {
                var item = Instantiate(InstrumentItemPrefab, InstrumentsContainer);
                item.name = inst.ToString();

                // El prefab debe tener: "Label" TMP_Text, "Bar" Image, "PtsLabel" TMP_Text
                var label = item.transform.Find("Label")?.GetComponent<TMP_Text>();
                if (label != null) label.text = InstrumentDisplayName(inst);

                var pts = item.transform.Find("PtsLabel")?.GetComponent<TMP_Text>();
                if (pts != null) pts.text = $"0 / {MissionDesign.InstrumentSciencePoints[inst]}";

                var bar = item.transform.Find("Bar")?.GetComponent<Image>();
                if (bar != null) bar.fillAmount = 0f;
            }
        }

        // ── Progreso de escaneo ────────────────────────────────────────────────

        void UpdateInstrumentProgress()
        {
            float powerAvail  = _design.EffectivePowerW;
            float powerActive = 0f;

            bool allDone = true;
            int  childIdx = 0;

            foreach (var inst in _design.Instruments)
            {
                if (_instProgress[inst] >= 1f)
                {
                    childIdx++;
                    continue;
                }
                allDone = false;

                // Progreso solo si hay potencia suficiente
                float needed = MissionDesign.InstrumentPowerW[inst] + MissionDesign.BusPowerW;
                if (powerAvail - powerActive >= needed)
                {
                    powerActive += MissionDesign.InstrumentPowerW[inst];
                    float scanT = ScanTime.TryGetValue(inst, out var t) ? t : 15f;
                    _instProgress[inst] += Time.deltaTime / scanT;
                    _instProgress[inst]  = Mathf.Clamp01(_instProgress[inst]);

                    if (_instProgress[inst] >= 1f)
                        _instEarned[inst] = MissionDesign.InstrumentSciencePoints[inst];
                }

                // Actualiza el ítem en la lista
                if (InstrumentsContainer != null && childIdx < InstrumentsContainer.childCount)
                {
                    var item = InstrumentsContainer.GetChild(childIdx);
                    var bar  = item.Find("Bar")?.GetComponent<Image>();
                    var pts  = item.Find("PtsLabel")?.GetComponent<TMP_Text>();
                    if (bar != null) bar.fillAmount = _instProgress[inst];
                    if (pts != null) pts.text = $"{_instEarned[inst]} / {MissionDesign.InstrumentSciencePoints[inst]}";
                }
                childIdx++;
            }

            // Todos los instrumentos completados
            if (allDone && !_operationsComplete)
            {
                _operationsComplete = true;
                int total = 0;
                foreach (var pts in _instEarned.Values) total += pts;
                if (_score != null) _score.SciencePoints = total;

                if (CompleteBtnLabel != null) CompleteBtnLabel.text = "COMPLETAR MISIÓN";
                if (CompleteMissionBtn != null) CompleteMissionBtn.interactable = true;
                Debug.Log($"[ScienceOps] Operaciones completas — {total} pts ciencia");
            }
        }

        // ── Actualizar potencia ────────────────────────────────────────────────

        void UpdatePowerBudget()
        {
            if (_design == null) return;
            float avail = _design.EffectivePowerW;
            float used  = _design.PowerUsedW;
            if (PowerBar    != null) PowerBar.fillAmount = Mathf.Clamp01(used / avail);
            if (PowerLabel  != null) PowerLabel.text = $"{used:F0} / {avail:F0} W";
            if (PowerStatusLabel != null)
            {
                PowerStatusLabel.text  = used <= avail ? "OK" : "SIN POTENCIA";
                PowerStatusLabel.color = used <= avail ? UIStyle.Good : UIStyle.Bad;
            }
        }

        // ── Actualizar combustible ─────────────────────────────────────────────

        void UpdateFuel()
        {
            if (_fuel == null) return;
            if (FuelBar   != null) FuelBar.fillAmount = _fuel.FuelFraction;
            if (FuelLabel != null) FuelLabel.text = $"{_fuel.FuelFraction:P0}";
        }

        // ── Actualizar score ───────────────────────────────────────────────────

        void UpdateScore()
        {
            if (_score == null) return;

            int sciTotal = 0;
            foreach (var v in _instEarned.Values) sciTotal += v;
            _score.SciencePoints = sciTotal;
            _score.FuelFraction  = _fuel != null ? _fuel.FuelFraction : 1f;

            int total = _score.TotalScore();
            if (SciencePointsLabel != null) SciencePointsLabel.text = $"{sciTotal} pts ciencia";
            if (TotalScoreLabel    != null) TotalScoreLabel.text    = $"TOTAL: {total}";
            if (ScoreBar           != null) ScoreBar.fillAmount     = Mathf.Clamp01(total / (float)ScoreboardUI.ScoreBarMax);
        }

        // ── Completar misión ───────────────────────────────────────────────────

        void CompleteMission()
        {
            if (_score == null) return;

            _score.FuelFraction = _fuel != null ? _fuel.FuelFraction : 1f;
            Debug.Log($"[ScienceOps] Misión completada — {_score.Summary()}");

            // GameManager broadcasts the final score and opens the scoreboard
            if (Panel != null) Panel.SetActive(false);
            GameManager.Instance?.OnMissionComplete();
        }

        // ── Cabecera ───────────────────────────────────────────────────────────

        void RefreshHeader()
        {
            if (PlanetNameLabel != null)
                PlanetNameLabel.text = MissionDesignUI.PlanetDisplayName(_design.TargetPlanetId).ToUpper();

            if (MissionObjectiveLabel != null)
                MissionObjectiveLabel.text = $"Objetivo: {ObjectiveLabel(_design.Objective)}";

            if (CommDelayLabel != null && MissionDesign.CommDelayLabel.TryGetValue(_design.TargetPlanetId, out var d))
                CommDelayLabel.text = $"Señal: {d} ida";

            if (CompleteBtnLabel != null)
                CompleteBtnLabel.text = "Operaciones en curso…";
            if (CompleteMissionBtn != null)
                CompleteMissionBtn.interactable = false;
        }

        void RefreshPlanetInfo()
        {
            if (!PlanetData.TryGetValue(_design.TargetPlanetId, out var data)) return;
            if (GravityLabel    != null) GravityLabel.text    = $"g = {data.g:F2} m/s²";
            if (AtmosphereLabel != null) AtmosphereLabel.text = $"Atmósfera: {data.atm}";
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        public static string InstrumentDisplayName(ScienceInstrument inst) => inst switch
        {
            ScienceInstrument.WideAngleCamera   => "Cámara gran angular",
            ScienceInstrument.NarrowAngleCamera => "Cámara teleobjetivo",
            ScienceInstrument.UVSpectrometer    => "Espectrómetro UV",
            ScienceInstrument.InfraredMapper    => "Mapeador infrarrojo",
            ScienceInstrument.Magnetometer      => "Magnetómetro",
            ScienceInstrument.ParticleDetector  => "Detector partículas",
            ScienceInstrument.RadioScience      => "Radio ciencia",
            ScienceInstrument.RadarAltimeter    => "Radar altímetro",
            _ => inst.ToString(),
        };

        string ObjectiveLabel(string obj) => obj switch
        {
            "flyby"   => "Sobrevuelo",
            "orbit"   => "Órbita",
            "landing" => "Aterrizaje",
            _ => obj,
        };
    }
}
