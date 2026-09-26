// Navigator 2.0 — MissionDesignUI.cs
// Pantalla de diseño de misión — selección motor, combustible, instrumentos, lanzamiento
// Ported from Dart: navigator/lib/screens/mission_design_screen.dart
// Guacalita Inc. — SAL / Claude

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Navigator.Core;
using Navigator.Game;

namespace Navigator.UI
{
    public class MissionDesignUI : MonoBehaviour
    {
        // ── Panel principal ────────────────────────────────────────────────────
        [Header("Panel raíz")]
        public GameObject Panel;          // activar/desactivar según estado

        // ── Planeta objetivo ───────────────────────────────────────────────────
        [Header("Selección planeta")]
        public Button[] PlanetButtons;    // orden: mercury, venus, mars, jupiter, saturn, uranus, neptune
        public TMP_Text PlanetLabel;
        public TMP_Text CommDelayLabel;   // "Retraso señal: 3-22 min"

        // ── Motor ──────────────────────────────────────────────────────────────
        [Header("Motor")]
        public Button BtnChemical;
        public Button BtnIon;
        public Button BtnNuclear;
        public TMP_Text EngineFuelLabel;  // tipo de combustible
        public TMP_Text EngineNasaLabel;  // referencia NASA

        // ── Combustible ────────────────────────────────────────────────────────
        [Header("Combustible")]
        public Slider  FuelSlider;        // min=100, max=5000 (kg)
        public TMP_Text FuelValueLabel;   // "1,200 kg"

        // ── Delta-V ────────────────────────────────────────────────────────────
        [Header("Delta-V")]
        public Image   DeltaVBar;         // fill amount 0-1
        public TMP_Text DeltaVLabel;      // "8,420 m/s"
        public TMP_Text DeltaVRequiredLabel; // "Requerido: 5,600 m/s"
        public TMP_Text DeltaVStatusLabel;   // "OK" / "INSUFICIENTE"

        // ── Sistema de potencia ────────────────────────────────────────────────
        [Header("Potencia")]
        public Button BtnSolar;
        public Button BtnRTG;
        public Button BtnNuclearReactor;
        public TMP_Text PowerOutputLabel; // "280 W efectivos"
        public TMP_Text PowerUsedLabel;   // "Consumo: 150 W"
        public TMP_Text PowerNasaLabel;
        public GameObject SolarWarning;   // advertencia solar < 5% en planetas exteriores

        // ── Antena ────────────────────────────────────────────────────────────
        [Header("Antena")]
        public Button BtnLowGain;
        public Button BtnMediumGain;
        public Button BtnHighGain;
        public TMP_Text AntennaDataRateLabel; // "100 kbps"

        // ── Vehículo de lanzamiento ────────────────────────────────────────────
        [Header("Lanzador")]
        public Button BtnFalcon9;
        public Button BtnFalconHeavy;
        public Button BtnSLS;
        public Button BtnStarship;
        public TMP_Text LaunchCapacityLabel; // "Capacidad: 8,000 kg"

        // ── Instrumentos científicos ───────────────────────────────────────────
        [Header("Instrumentos")]
        public Toggle[] InstrumentToggles; // orden igual al enum ScienceInstrument
        public TMP_Text InstrumentMassLabel;
        public TMP_Text InstrumentScienceLabel; // "4,200 pts ciencia posibles"

        // ── Resumen ────────────────────────────────────────────────────────────
        [Header("Resumen")]
        public TMP_Text TotalMassLabel;    // "3,240 / 8,000 kg"
        public TMP_Text TotalCostLabel;    // "$420M / $1,000M"
        public TMP_Text MassFractionLabel; // "Fracción masa: 0.37"
        public Image    BudgetBar;
        public Image    MassBar;

        // ── Estado global ──────────────────────────────────────────────────────
        [Header("Errores / Launch")]
        public TMP_Text ErrorLabel;       // primer error activo
        public Button   LaunchButton;     // se habilita solo cuando IsValid

        // ── Objetivo ──────────────────────────────────────────────────────────
        [Header("Objetivo misión")]
        public Button BtnFlyby;
        public Button BtnOrbit;
        public Button BtnLanding;
        public TMP_Text ObjectiveInfoLabel;  // costo en ΔV y multiplicador de ciencia

        // ── Datos ─────────────────────────────────────────────────────────────
        private MissionDesign _design;
        private static readonly string[] _planetIds =
            { "mercury", "venus", "mars", "jupiter", "saturn", "uranus", "neptune" };
        private static readonly string[] _planetNames =
            { "Mercurio", "Venus", "Marte", "Júpiter", "Saturno", "Urano", "Neptuno" };

        void Awake()
        {
            _design = MissionDesign.DefaultFor("mars");
            WireButtons();
        }

        void Start()
        {
            SyncControls();
            Refresh();
        }

        // Pushes _design into sliders/toggles without firing their listeners
        void SyncControls()
        {
            if (FuelSlider != null) FuelSlider.SetValueWithoutNotify(_design.FuelMassKg);
            var instValues = (ScienceInstrument[])System.Enum.GetValues(typeof(ScienceInstrument));
            for (int i = 0; i < InstrumentToggles.Length && i < instValues.Length; i++)
                if (InstrumentToggles[i] != null)
                    InstrumentToggles[i].SetIsOnWithoutNotify(_design.Instruments.Contains(instValues[i]));
        }

        // ── Conecta todos los botones ──────────────────────────────────────────

        void WireButtons()
        {
            // Planetas
            for (int i = 0; i < PlanetButtons.Length && i < _planetIds.Length; i++)
            {
                int idx = i;
                PlanetButtons[i].onClick.AddListener(() => { _design.TargetPlanetId = _planetIds[idx]; Refresh(); });
            }

            // Motor
            BtnChemical?.onClick.AddListener(() => { _design.Engine = EngineType.Chemical;  Refresh(); });
            BtnIon      ?.onClick.AddListener(() => { _design.Engine = EngineType.Ion;       Refresh(); });
            BtnNuclear  ?.onClick.AddListener(() => { _design.Engine = EngineType.Nuclear;   Refresh(); });

            // Combustible
            if (FuelSlider != null)
            {
                FuelSlider.minValue = 100f;
                FuelSlider.maxValue = 5000f;
                FuelSlider.value    = _design.FuelMassKg;
                FuelSlider.onValueChanged.AddListener(v => { _design.FuelMassKg = v; Refresh(); });
            }

            // Potencia
            BtnSolar          ?.onClick.AddListener(() => { _design.Power = PowerSystem.Solar;          Refresh(); });
            BtnRTG            ?.onClick.AddListener(() => { _design.Power = PowerSystem.RTG;            Refresh(); });
            BtnNuclearReactor ?.onClick.AddListener(() => { _design.Power = PowerSystem.NuclearReactor; Refresh(); });

            // Antena
            BtnLowGain    ?.onClick.AddListener(() => { _design.Antenna = AntennaType.LowGain;    Refresh(); });
            BtnMediumGain ?.onClick.AddListener(() => { _design.Antenna = AntennaType.MediumGain; Refresh(); });
            BtnHighGain   ?.onClick.AddListener(() => { _design.Antenna = AntennaType.HighGain;   Refresh(); });

            // Lanzador
            BtnFalcon9    ?.onClick.AddListener(() => { _design.LaunchVeh = LaunchVehicle.Falcon9;     Refresh(); });
            BtnFalconHeavy?.onClick.AddListener(() => { _design.LaunchVeh = LaunchVehicle.FalconHeavy; Refresh(); });
            BtnSLS        ?.onClick.AddListener(() => { _design.LaunchVeh = LaunchVehicle.SLS;         Refresh(); });
            BtnStarship   ?.onClick.AddListener(() => { _design.LaunchVeh = LaunchVehicle.Starship;    Refresh(); });

            // Instrumentos
            var instValues = (ScienceInstrument[])System.Enum.GetValues(typeof(ScienceInstrument));
            for (int i = 0; i < InstrumentToggles.Length && i < instValues.Length; i++)
            {
                int idx = i;
                var inst = instValues[idx];
                InstrumentToggles[i].onValueChanged.AddListener(on =>
                {
                    if (on) { if (!_design.Instruments.Contains(inst)) _design.Instruments.Add(inst); }
                    else      _design.Instruments.Remove(inst);
                    Refresh();
                });
            }

            // Objetivo
            BtnFlyby  ?.onClick.AddListener(() => { _design.Objective = "flyby";   Refresh(); });
            BtnOrbit  ?.onClick.AddListener(() => { _design.Objective = "orbit";   Refresh(); });
            BtnLanding?.onClick.AddListener(() => { _design.Objective = "landing"; Refresh(); });

            // Lanzar
            LaunchButton?.onClick.AddListener(OnLaunch);
        }

        // ── Actualiza toda la UI según _design ────────────────────────────────

        void Refresh()
        {
            // Planeta
            int pIdx = System.Array.IndexOf(_planetIds, _design.TargetPlanetId);
            if (PlanetLabel != null)
                PlanetLabel.text = pIdx >= 0 ? _planetNames[pIdx] : _design.TargetPlanetId;
            if (CommDelayLabel != null && MissionDesign.CommDelayLabel.TryGetValue(_design.TargetPlanetId, out var delay))
                CommDelayLabel.text = $"Señal: {delay}";

            for (int i = 0; i < PlanetButtons.Length && i < _planetIds.Length; i++)
                SetButtonSelected(PlanetButtons[i], _design.TargetPlanetId == _planetIds[i]);
            SetButtonSelected(BtnFlyby,   _design.Objective == "flyby");
            SetButtonSelected(BtnOrbit,   _design.Objective == "orbit");
            SetButtonSelected(BtnLanding, _design.Objective == "landing");

            // Motor
            SetButtonSelected(BtnChemical, _design.Engine == EngineType.Chemical);
            SetButtonSelected(BtnIon,      _design.Engine == EngineType.Ion);
            SetButtonSelected(BtnNuclear,  _design.Engine == EngineType.Nuclear);
            if (EngineFuelLabel != null) EngineFuelLabel.text = MissionDesign.EngineFuelType[_design.Engine];
            if (EngineNasaLabel != null) EngineNasaLabel.text = MissionDesign.EngineNasaRef[_design.Engine];

            // Combustible
            if (FuelValueLabel != null) FuelValueLabel.text = $"{_design.FuelMassKg:N0} kg";

            // Delta-V
            float dv      = _design.DeltaVms;
            float dvReq   = _design.RequiredDeltaV;
            float dvRatio = Mathf.Clamp01(dv / (dvReq * 1.5f));
            if (DeltaVBar    != null) DeltaVBar.fillAmount = dvRatio;
            if (DeltaVLabel  != null) DeltaVLabel.text = $"{dv:N0} m/s";
            if (DeltaVRequiredLabel != null)
            {
                string parts = $"{_design.CruiseDeltaV:N0} viaje";
                if (_design.InsertionDeltaV > 0f) parts += $" + {_design.InsertionDeltaV:N0} inserción";
                if (_design.LandingDeltaV   > 0f) parts += $" + {_design.LandingDeltaV:N0} descenso";
                DeltaVRequiredLabel.text = $"Requerido: {dvReq:N0} m/s ({parts})";
            }
            if (ObjectiveInfoLabel != null)
            {
                string pid = _design.TargetPlanetId;
                ObjectiveInfoLabel.text = _design.Objective switch
                {
                    "orbit"   => $"+{_design.InsertionDeltaV:N0} m/s para frenar en órbita · ciencia ×1.5",
                    "landing" => _design.CanLandOnTarget
                        ? $"+{_design.InsertionDeltaV + _design.LandingDeltaV:N0} m/s, +{MissionDesign.LanderMassKg:F0} kg y +${MissionDesign.LanderCostM:F0}M de módulo · ciencia ×2"
                        : $"{PlanetDisplayName(pid)} no tiene superficie sólida",
                    _         => "Pasas de largo: sin costo extra · ciencia ×1",
                };
                ObjectiveInfoLabel.color = _design.IsObjectiveOk ? UIStyle.Muted : UIStyle.Bad;
            }
            if (DeltaVStatusLabel   != null)
            {
                DeltaVStatusLabel.text  = _design.HasDeltaV ? "OK" : "INSUFICIENTE";
                DeltaVStatusLabel.color = _design.HasDeltaV ? UIStyle.Good : UIStyle.Bad;
            }

            // Potencia
            SetButtonSelected(BtnSolar,         _design.Power == PowerSystem.Solar);
            SetButtonSelected(BtnRTG,            _design.Power == PowerSystem.RTG);
            SetButtonSelected(BtnNuclearReactor, _design.Power == PowerSystem.NuclearReactor);
            if (PowerOutputLabel != null) PowerOutputLabel.text = $"{_design.EffectivePowerW:F0} W efectivos";
            if (PowerUsedLabel   != null) PowerUsedLabel.text   = $"Consumo: {_design.PowerUsedW:F0} W";
            if (PowerNasaLabel   != null) PowerNasaLabel.text   = MissionDesign.PowerNasaRef[_design.Power];
            if (SolarWarning != null) SolarWarning.SetActive(!_design.IsSolarViable);

            // Antena
            SetButtonSelected(BtnLowGain,    _design.Antenna == AntennaType.LowGain);
            SetButtonSelected(BtnMediumGain, _design.Antenna == AntennaType.MediumGain);
            SetButtonSelected(BtnHighGain,   _design.Antenna == AntennaType.HighGain);
            if (AntennaDataRateLabel != null)
            {
                float kbps = MissionDesign.AntennaDataRateKbps[_design.Antenna];
                string real = kbps >= 1000f ? $"{kbps / 1000f:F0} Mbps" : $"{kbps} kbps";
                float  mbps = MissionDesign.GameDownlinkMbps[_design.Antenna];
                float  secs = _design.TotalDataMb / mbps;
                AntennaDataRateLabel.text = $"{real} reales · tus {_design.TotalDataMb:F0} Mb tardan {secs:F0} s en llegar";
            }

            // Lanzador
            SetButtonSelected(BtnFalcon9,     _design.LaunchVeh == LaunchVehicle.Falcon9);
            SetButtonSelected(BtnFalconHeavy, _design.LaunchVeh == LaunchVehicle.FalconHeavy);
            SetButtonSelected(BtnSLS,         _design.LaunchVeh == LaunchVehicle.SLS);
            SetButtonSelected(BtnStarship,    _design.LaunchVeh == LaunchVehicle.Starship);
            if (LaunchCapacityLabel != null)
                LaunchCapacityLabel.text = $"Capacidad: {MissionDesign.LaunchCapacityKg[_design.LaunchVeh]:N0} kg";

            // Instrumentos
            float instMass = 0f;
            int   instPts  = 0;
            foreach (var i in _design.Instruments)
            {
                instMass += MissionDesign.InstrumentMassKg[i];
                instPts  += MissionDesign.InstrumentSciencePoints[i];
            }
            if (InstrumentMassLabel    != null) InstrumentMassLabel.text    = $"Masa: {instMass:F0} kg";
            if (InstrumentScienceLabel != null) InstrumentScienceLabel.text = $"{instPts} pts ciencia posibles";

            // Resumen
            float maxMass = MissionDesign.LaunchCapacityKg[_design.LaunchVeh];
            if (TotalMassLabel  != null) TotalMassLabel.text  = $"{_design.WetMassKg:N0} / {maxMass:N0} kg";
            if (TotalCostLabel  != null) TotalCostLabel.text  = $"${_design.TotalCostM:N0}M / $1,000M";
            if (MassFractionLabel != null) MassFractionLabel.text = $"Fracción masa: {_design.MassFraction:F2}";
            if (BudgetBar != null) BudgetBar.fillAmount = Mathf.Clamp01(_design.TotalCostM / MissionDesign.BudgetLimitM);
            if (MassBar   != null) MassBar.fillAmount   = Mathf.Clamp01(_design.WetMassKg  / maxMass);

            // Error + botón launch
            bool valid = _design.IsValid;
            if (ErrorLabel != null)
            {
                if (!_design.IsBudgetOk)     ErrorLabel.text = "Presupuesto excedido";
                else if (!_design.IsMassOk)  ErrorLabel.text = "Nave demasiado pesada para el lanzador";
                else if (!_design.IsPowerOk) ErrorLabel.text = "Consumo eléctrico supera la generación";
                else if (!_design.IsObjectiveOk) ErrorLabel.text = "No se puede aterrizar en un gigante gaseoso";
                else if (!_design.HasDeltaV) ErrorLabel.text = "ΔV insuficiente: más combustible o mejor motor";
                else                         ErrorLabel.text = "";
                ErrorLabel.gameObject.SetActive(!valid);
            }
            if (LaunchButton != null) LaunchButton.interactable = valid;

            // Colores barra ΔV: verde=ok, rojo=insuficiente
            if (DeltaVBar != null)
                DeltaVBar.color = _design.HasDeltaV ? UIStyle.Good : UIStyle.Bad;
        }

        // ── Destaca el botón seleccionado ──────────────────────────────────────

        void SetButtonSelected(Button btn, bool selected)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null)
                img.color = selected ? UIStyle.Accent : UIStyle.ButtonIdle;
            var label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null) label.color = selected ? UIStyle.Ink : UIStyle.Text;
        }

        // ── Lanzar misión ──────────────────────────────────────────────────────

        // The ship itself is configured by GameManager.StartFlight from GetDesign()
        void OnLaunch()
        {
            if (!_design.IsValid) return;
            Debug.Log($"[Mission] Lanzando a {_design.TargetPlanetId} — ΔV {_design.DeltaVms:N0} m/s");
            GameManager.Instance?.OnLaunchConfirmed();
        }

        public static string PlanetDisplayName(string planetId)
        {
            int i = System.Array.IndexOf(_planetIds, planetId);
            return i >= 0 ? _planetNames[i] : planetId;
        }

        // ── API pública ────────────────────────────────────────────────────────

        public MissionDesign GetDesign() => _design;

        public void Show(string planetId = "mars", bool keepDesign = false)
        {
            if (!keepDesign || _design == null) _design = MissionDesign.DefaultFor(planetId);
            SyncControls();
            Refresh();
            if (Panel != null) Panel.SetActive(true);
        }
    }
}
