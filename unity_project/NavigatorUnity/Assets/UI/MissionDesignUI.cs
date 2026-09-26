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
        public TMP_Text CommDelayLabel;
        public TMP_Text PlanetScienceLabel;   // qué instrumentos valen más en este planeta

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

        void OnEnable()  => Loc.Changed += Refresh;
        void OnDisable() => Loc.Changed -= Refresh;

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

        void RefreshInstrumentLabels()
        {
            var instValues = (ScienceInstrument[])System.Enum.GetValues(typeof(ScienceInstrument));
            for (int i = 0; i < InstrumentToggles.Length && i < instValues.Length; i++)
            {
                var label = InstrumentToggles[i] != null ? InstrumentToggles[i].GetComponentInChildren<TMP_Text>() : null;
                if (label == null) continue;
                var inst = instValues[i];
                float mult = MissionDesign.PlanetScienceMult(_design.TargetPlanetId, inst);
                string hex = mult >= 1.5f ? "#52EB8C" : mult <= 0.5f ? "#FF5C5C" : "#8FA3C7";
                label.text = $"{Loc.Instrument(inst)} <color={hex}><b>×{mult:0.#}</b></color>\n<size=70%><color=#8FA3C7>" +
                             $"{MissionDesign.InstrumentMassKg[inst]:F0} kg · {MissionDesign.InstrumentPowerW[inst]:F0} W · " +
                             $"{_design.InstrumentPoints(inst):N0} pts · {MissionDesign.InstrumentDataMb[inst]:F0} Mb</color></size>";
            }
        }

        void Refresh()
        {
            RefreshInstrumentLabels();
            // Planeta
            if (PlanetLabel != null)
                PlanetLabel.text = PlanetDisplayName(_design.TargetPlanetId);
            if (PlanetScienceLabel != null) PlanetScienceLabel.text = Loc.T("scihint." + _design.TargetPlanetId);
            if (CommDelayLabel != null && MissionDesign.CommDelayLabel.TryGetValue(_design.TargetPlanetId, out var delay))
                CommDelayLabel.text = Loc.F("design.signal", delay);

            for (int i = 0; i < PlanetButtons.Length && i < _planetIds.Length; i++)
                SetButtonSelected(PlanetButtons[i], _design.TargetPlanetId == _planetIds[i]);
            SetButtonSelected(BtnFlyby,   _design.Objective == "flyby");
            SetButtonSelected(BtnOrbit,   _design.Objective == "orbit");
            SetButtonSelected(BtnLanding, _design.Objective == "landing");

            // Motor
            SetButtonSelected(BtnChemical, _design.Engine == EngineType.Chemical);
            SetButtonSelected(BtnIon,      _design.Engine == EngineType.Ion);
            SetButtonSelected(BtnNuclear,  _design.Engine == EngineType.Nuclear);
            if (EngineFuelLabel != null) EngineFuelLabel.text = Loc.T("fuel." + _design.Engine);
            if (EngineNasaLabel != null) EngineNasaLabel.text = Loc.T("nasa.engine." + _design.Engine);

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
                string parts = Loc.F("design.dv.cruise", _design.CruiseDeltaV);
                if (_design.InsertionDeltaV > 0f) parts += Loc.F("design.dv.insert", _design.InsertionDeltaV);
                if (_design.LandingDeltaV   > 0f) parts += Loc.F("design.dv.descent", _design.LandingDeltaV);
                DeltaVRequiredLabel.text = Loc.F("design.required", dvReq, parts);
            }
            if (ObjectiveInfoLabel != null)
            {
                string pid = _design.TargetPlanetId;
                ObjectiveInfoLabel.text = _design.Objective switch
                {
                    "orbit"   => Loc.F("design.obj.orbit", _design.InsertionDeltaV),
                    "landing" => _design.CanLandOnTarget
                        ? Loc.F("design.obj.landing", _design.InsertionDeltaV + _design.LandingDeltaV, MissionDesign.LanderMassKg, MissionDesign.LanderCostM)
                        : Loc.F("design.obj.nosurface", PlanetDisplayName(pid)),
                    _         => Loc.T("design.obj.flyby"),
                };
                ObjectiveInfoLabel.color = _design.IsObjectiveOk ? UIStyle.Muted : UIStyle.Bad;
            }
            if (DeltaVStatusLabel   != null)
            {
                DeltaVStatusLabel.text  = _design.HasDeltaV ? Loc.T("design.ok") : Loc.T("design.short");
                DeltaVStatusLabel.color = _design.HasDeltaV ? UIStyle.Good : UIStyle.Bad;
            }

            // Potencia
            SetButtonSelected(BtnSolar,         _design.Power == PowerSystem.Solar);
            SetButtonSelected(BtnRTG,            _design.Power == PowerSystem.RTG);
            SetButtonSelected(BtnNuclearReactor, _design.Power == PowerSystem.NuclearReactor);
            if (PowerOutputLabel != null) PowerOutputLabel.text = Loc.F("design.poweravail", _design.EffectivePowerW);
            if (PowerUsedLabel   != null) PowerUsedLabel.text   = Loc.F("design.powerused", _design.PowerUsedW);
            if (PowerNasaLabel   != null) PowerNasaLabel.text   = Loc.T("nasa.power." + _design.Power);
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
                AntennaDataRateLabel.text = Loc.F("design.antennarate", real, _design.TotalDataMb, secs);
            }

            // Lanzador
            SetButtonSelected(BtnFalcon9,     _design.LaunchVeh == LaunchVehicle.Falcon9);
            SetButtonSelected(BtnFalconHeavy, _design.LaunchVeh == LaunchVehicle.FalconHeavy);
            SetButtonSelected(BtnSLS,         _design.LaunchVeh == LaunchVehicle.SLS);
            SetButtonSelected(BtnStarship,    _design.LaunchVeh == LaunchVehicle.Starship);
            if (LaunchCapacityLabel != null)
                LaunchCapacityLabel.text = Loc.F("design.capacity", MissionDesign.LaunchCapacityKg[_design.LaunchVeh]);

            // Instrumentos
            float instMass = 0f;
            int   instPts  = 0;
            foreach (var i in _design.Instruments)
            {
                instMass += MissionDesign.InstrumentMassKg[i];
                instPts  += _design.InstrumentPoints(i);
            }
            if (InstrumentMassLabel    != null) InstrumentMassLabel.text    = Loc.F("design.instmass", instMass);
            if (InstrumentScienceLabel != null) InstrumentScienceLabel.text = Loc.F("design.instpts", instPts);

            // Resumen
            float maxMass = MissionDesign.LaunchCapacityKg[_design.LaunchVeh];
            if (TotalMassLabel  != null) TotalMassLabel.text  = $"{_design.WetMassKg:N0} / {maxMass:N0} kg";
            if (TotalCostLabel  != null) TotalCostLabel.text  = $"${_design.TotalCostM:N0}M / $1,000M";
            if (MassFractionLabel != null) MassFractionLabel.text = Loc.F("design.massfrac", _design.MassFraction);
            if (BudgetBar != null) BudgetBar.fillAmount = Mathf.Clamp01(_design.TotalCostM / MissionDesign.BudgetLimitM);
            if (MassBar   != null) MassBar.fillAmount   = Mathf.Clamp01(_design.WetMassKg  / maxMass);

            // Error + botón launch
            bool valid = _design.IsValid;
            if (ErrorLabel != null)
            {
                if (!_design.IsBudgetOk)     ErrorLabel.text = Loc.T("err.budget");
                else if (!_design.IsMassOk)  ErrorLabel.text = Loc.T("err.mass");
                else if (!_design.IsPowerOk) ErrorLabel.text = Loc.T("err.power");
                else if (!_design.IsObjectiveOk) ErrorLabel.text = Loc.T("err.objective");
                else if (!_design.HasDeltaV) ErrorLabel.text = Loc.T("err.deltav");
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

        public static string PlanetDisplayName(string planetId) => Loc.Planet(planetId);

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
