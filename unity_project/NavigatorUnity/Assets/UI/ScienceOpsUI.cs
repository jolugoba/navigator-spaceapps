// Navigator 2.0 â€” ScienceOpsUI.cs
// Pantalla de operaciones cientÃ­ficas al llegar al planeta objetivo
// Muestra el progreso de cada instrumento, presupuesto de potencia y score final
// Ported from Dart: navigator/lib/screens/science_ops_screen.dart
// Guacalita Inc. â€” SAL / Claude

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
        [Header("Panel raÃ­z")]
        public GameObject Panel;

        // â”€â”€ Cabecera â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        [Header("Cabecera")]
        public TMP_Text PlanetNameLabel;     // "MARTE"
        public TMP_Text ArrivalPositionLabel; // "#1 PRIMERO"
        public TMP_Text MissionObjectiveLabel; // "Objetivo: Ã“rbita"
        public TMP_Text CommDelayLabel;       // "SeÃ±al: 3-22 min ida"

        // â”€â”€ Presupuesto de potencia â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        [Header("Potencia")]
        public Image    PowerBar;            // fill: usado / disponible
        public TMP_Text PowerLabel;          // "148 / 280 W"
        public TMP_Text PowerStatusLabel;    // "OK" / "SIN POTENCIA"

        // â”€â”€ Instrumentos â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        [Header("Instrumentos")]
        public Transform InstrumentsContainer; // padre donde se crean los items
        public GameObject InstrumentItemPrefab; // prefab: label + barra de progreso + pts

        // â”€â”€ Score en vivo â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        [Header("Score")]
        public TMP_Text SciencePointsLabel;  // "2,400 pts ciencia"
        public TMP_Text TotalScoreLabel;     // "TOTAL: 4,650"
        public Image    ScoreBar;            // llena mientras se acumulan pts

        // â”€â”€ Combustible restante â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        [Header("Combustible")]
        public Image    FuelBar;
        public TMP_Text FuelLabel;           // "38%"

        // â”€â”€ Gravedad del planeta â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        [Header("Info planeta")]
        public TMP_Text GravityLabel;        // "g = 3.72 m/sÂ²"
        public TMP_Text AtmosphereLabel;     // "AtmÃ³sfera: COâ‚‚ delgada"

        // â”€â”€ BotÃ³n completar â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        [Header("Completar")]
        public Button   CompleteMissionBtn;
        public TMP_Text CompleteBtnLabel;    // "COMPLETAR MISIÃ“N" / "Operaciones en cursoâ€¦"

        // â”€â”€ Datos runtime â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private MissionDesign   _design;
        private MissionScore    _score;
        private FuelSystem      _fuel;
        private P2PChatSystem   _chat;

        // Progreso de cada instrumento (0 a 1)
        private readonly Dictionary<ScienceInstrument, float> _instProgress = new();
        private readonly Dictionary<ScienceInstrument, int>   _instEarned   = new();
        private bool _operationsComplete = false;

        // DuraciÃ³n de escaneo por instrumento (segundos de juego)
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

        // Datos estÃ¡ticos del planeta (gravedad superficial m/sÂ², atmÃ³sfera)
        private static readonly Dictionary<string, (float g, string atm)> PlanetData = new()
        {
            { "mercury", (3.70f,  "Sin atmÃ³sfera") },
            { "venus",   (8.87f,  "COâ‚‚ densa â€” 92 atm") },
            { "mars",    (3.72f,  "COâ‚‚ delgada â€” 0.006 atm") },
            { "jupiter", (24.79f, "Hâ‚‚ + He â€” sin superficie sÃ³lida") },
            { "saturn",  (10.44f, "Hâ‚‚ + He â€” anillos de hielo") },
            { "uranus",  (8.87f,  "Hâ‚‚O + NHâ‚ƒ + CHâ‚„ â€” planeta helado") },
            { "neptune", (11.15f, "Hâ‚‚ + He + CHâ‚„ â€” vientos 2,100 km/h") },
        };

        // â”€â”€ Ciclo de vida â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        void Awake()
        {
            _score = FindObjectOfType<MissionScore>();
            _chat  = FindObjectOfType<P2PChatSystem>();

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

        // â”€â”€ Activar pantalla al llegar al planeta â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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
            RefreshPlanetInfo();
            if (Panel != null) Panel.SetActive(true);

            _chat?.ArrivalBroadcast(_score?.ArrivalPosition ?? 0);
            Debug.Log($"[ScienceOps] Llegamos a {_design.TargetPlanetId} en posiciÃ³n #{arrivalPosition}");
        }

        // â”€â”€ ConstrucciÃ³n dinÃ¡mica de items de instrumentos â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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

        // â”€â”€ Progreso de escaneo â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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

                // Actualiza el Ã­tem en la lista
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

                if (CompleteBtnLabel != null) CompleteBtnLabel.text = "COMPLETAR MISIÃ“N";
                if (CompleteMissionBtn != null) CompleteMissionBtn.interactable = true;
                Debug.Log($"[ScienceOps] Operaciones completas â€” {total} pts ciencia");
            }
        }

        // â”€â”€ Actualizar potencia â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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
                PowerStatusLabel.color = used <= avail ? Color.green : Color.red;
            }
        }

        // â”€â”€ Actualizar combustible â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        void UpdateFuel()
        {
            if (_fuel == null) return;
            if (FuelBar   != null) FuelBar.fillAmount = _fuel.FuelFraction;
            if (FuelLabel != null) FuelLabel.text = $"{_fuel.FuelFraction:P0}";
        }

        // â”€â”€ Actualizar score â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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
            if (ScoreBar           != null) ScoreBar.fillAmount     = Mathf.Clamp01(total / 5000f);
        }

        // â”€â”€ Completar misiÃ³n â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        void CompleteMission()
        {
            if (_score == null) return;

            _score.FuelFraction = _fuel != null ? _fuel.FuelFraction : 1f;
            string summary = _score.Summary();
            Debug.Log($"[ScienceOps] MisiÃ³n completada â€” {summary}");

            // EnvÃ­a resultado vÃ­a Senda
            var senda = FindObjectOfType<Navigator.Network.SendaBridge>();
            senda?.Broadcast(NavMsg.Build(
                NavMsgType.missionEnd, "",
                $"{{\"score\":{_score.TotalScore()},\"summary\":\"{summary}\"}}"));

            if (Panel != null) Panel.SetActive(false);
        }

        // â”€â”€ Cabecera â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        void RefreshHeader()
        {
            if (PlanetNameLabel != null)
                PlanetNameLabel.text = _design.TargetPlanetId.ToUpper();

            if (MissionObjectiveLabel != null)
                MissionObjectiveLabel.text = $"Objetivo: {ObjectiveLabel(_design.Objective)}";

            if (CommDelayLabel != null && MissionDesign.CommDelayLabel.TryGetValue(_design.TargetPlanetId, out var d))
                CommDelayLabel.text = $"SeÃ±al: {d} ida";

            if (CompleteBtnLabel != null)
                CompleteBtnLabel.text = "Operaciones en cursoâ€¦";
            if (CompleteMissionBtn != null)
                CompleteMissionBtn.interactable = false;
        }

        void RefreshPlanetInfo()
        {
            if (!PlanetData.TryGetValue(_design.TargetPlanetId, out var data)) return;
            if (GravityLabel    != null) GravityLabel.text    = $"g = {data.g:F2} m/sÂ²";
            if (AtmosphereLabel != null) AtmosphereLabel.text = $"AtmÃ³sfera: {data.atm}";
        }

        // â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        string InstrumentDisplayName(ScienceInstrument inst) => inst switch
        {
            ScienceInstrument.WideAngleCamera   => "CÃ¡mara gran angular",
            ScienceInstrument.NarrowAngleCamera => "CÃ¡mara teleobjetivo",
            ScienceInstrument.UVSpectrometer    => "EspectrÃ³metro UV",
            ScienceInstrument.InfraredMapper    => "Mapeador infrarrojo",
            ScienceInstrument.Magnetometer      => "MagnetÃ³metro",
            ScienceInstrument.ParticleDetector  => "Detector partÃ­culas",
            ScienceInstrument.RadioScience      => "Radio ciencia",
            ScienceInstrument.RadarAltimeter    => "Radar altÃ­metro",
            _ => inst.ToString(),
        };

        string ObjectiveLabel(string obj) => obj switch
        {
            "flyby"   => "Sobrevuelo",
            "orbit"   => "Ã“rbita",
            "landing" => "Aterrizaje",
            _ => obj,
        };
    }
}
