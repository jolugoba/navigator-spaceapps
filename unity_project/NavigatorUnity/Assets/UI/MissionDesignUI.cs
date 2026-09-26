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

        // ── Datos ─────────────────────────────────────────────────────────────
        private MissionDesign _design;
        private static readonly string[] _planetIds =
            { "mercury", "venus", "mars", "jupiter", "saturn", "uranus", "neptune" };
        private static readonly string[] _planetNames =
            { "Mercurio", "Venus", "Marte", "Jupiter", "Saturno", "Urano", "Neptuno" };

        void Awake()
        {
            _design = MissionDesign.DefaultFor("mars");
            WireButtons();
        }

        void Start() => Refresh();

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
            float dvReq   = MissionDesign.MinDeltaV.TryGetValue(_design.TargetPlanetId, out var r) ? r : 5600f;
            float dvRatio = Mathf.Clamp01(dv / (dvReq * 1.5f));
            if (DeltaVBar    != null) DeltaVBar.fillAmount = dvRatio;
            if (DeltaVLabel  != null) DeltaVLabel.text = $"{dv:N0} m/s";
            if (DeltaVRequiredLabel != null) DeltaVRequiredLabel.text = $"Requerido: {dvReq:N0} m/s";
            if (DeltaVStatusLabel   != null)
            {
                DeltaVStatusLabel.text  = _design.HasDeltaV ? "OK" : "INSUFICIENTE";
                DeltaVStatusLabel.color = _design.HasDeltaV ? Color.green : Color.red;
            }

            // Potencia
            SetButtonSelected(BtnSolar,         _design.Power == PowerSystem.Solar);
            SetButtonSelected(BtnRTG,            _design.Power == PowerSystem.RTG);
            SetButtonSelected(BtnNuclearReactor, _design.Power == PowerSystem.NuclearReactor);
            if (PowerOutputLabel != null) PowerOutputLabel.text = $"{_design.EffectivePowerW:F0} W efectivos";
            if (PowerUsedLabel   != null) PowerUsedLabel.text   = $"Consumo: {_design.PowerUsedW:F0} W";
            if (PowerNasaLabel   != null) PowerNasaLabel.text   = MissionDesign.PowerNasaRef[_design.Power];
            SolarWarning?.SetActive(!_design.IsSolarViable);

            // Antena
            SetButtonSelected(BtnLowGain,    _design.Antenna == AntennaType.LowGain);
            SetButtonSelected(BtnMediumGain, _design.Antenna == AntennaType.MediumGain);
            SetButtonSelected(BtnHighGain,   _design.Antenna == AntennaType.HighGain);
            if (AntennaDataRateLabel != null)
            {
                float kbps = MissionDesign.AntennaDataRateKbps[_design.Antenna];
                AntennaDataRateLabel.text = kbps >= 1000f ? $"{kbps / 1000f:F0} Mbps" : $"{kbps} kbps";
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
                else                         ErrorLabel.text = "";
                ErrorLabel.gameObject.SetActive(!valid);
            }
            if (LaunchButton != null) LaunchButton.interactable = valid;

            // Colores barra ΔV: verde=ok, rojo=insuficiente
            if (DeltaVBar != null)
                DeltaVBar.color = _design.HasDeltaV ? Color.green : Color.red;
        }

        // ── Destaca el botón seleccionado ──────────────────────────────────────

        void SetButtonSelected(Button btn, bool selected)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null)
                img.color = selected ? new Color(0.2f, 0.8f, 1f) : new Color(0.15f, 0.15f, 0.2f);
        }

        // ── Lanzar misión ──────────────────────────────────────────────────────

        void OnLaunch()
        {
            var shipCtrl = GameObject.FindWithTag("LocalShip")?.GetComponent<ShipController>();
            if (shipCtrl == null) return;

            // Configura PhysicsEngine con los parámetros diseñados
            if (shipCtrl.Physics != null)
                shipCtrl.Physics.SetEngineConfig(_design.Engine);

            // Configura FuelSystem — motor + combustible completo desde el diseño
            var fuel = shipCtrl.GetComponent<FuelSystem>();
            if (fuel != null)
                fuel.InitFromDesign(_design);   // asigna Engine + MaxFuelKg + FuelFraction=1

            Debug.Log($"[Mission] Lanzando a {_design.TargetPlanetId} — ΔV {_design.DeltaVms:N0} m/s");
            Panel?.SetActive(false);
        }

        // ── API pública ────────────────────────────────────────────────────────

        public MissionDesign GetDesign() => _design;

        public void Show(string planetId = "mars")
        {
            _design = MissionDesign.DefaultFor(planetId);
            Refresh();
            Panel?.SetActive(true);
        }
    }
}
