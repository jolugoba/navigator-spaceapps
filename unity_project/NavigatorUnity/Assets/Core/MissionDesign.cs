// Navigator 2.0 — MissionDesign.cs
// Ported from Dart: navigator/lib/models/mission_design.dart
// Guacalita Inc. — SAL / Claude

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Navigator.Core
{
    public enum EngineType    { Chemical, Ion, Nuclear }
    public enum PowerSystem   { Solar, RTG, NuclearReactor }
    public enum AntennaType   { LowGain, MediumGain, HighGain }
    public enum LaunchVehicle { Falcon9, FalconHeavy, SLS, Starship }
    public enum ScienceInstrument
    {
        WideAngleCamera, NarrowAngleCamera, UVSpectrometer, InfraredMapper,
        Magnetometer, ParticleDetector, RadioScience, RadarAltimeter
    }

    [Serializable]
    public class MissionDesign
    {
        public const float BudgetLimitM      = 1000f;
        public const float SpacecraftBusKg   = 500f;
        public const float BusPowerW         = 80f;

        public string                    TargetPlanetId;
        public string                    Objective;   // "flyby" | "orbit" | "landing"
        public EngineType                Engine;
        public float                     FuelMassKg;
        public List<ScienceInstrument>   Instruments  = new();
        public PowerSystem               Power;
        public AntennaType               Antenna;
        public LaunchVehicle             LaunchVeh;

        // ── Static lookup tables ───────────────────────────────────────────────

        public static readonly Dictionary<EngineType, float> EngineIspS = new()
        {
            { EngineType.Chemical, 450f },
            { EngineType.Ion,      3000f },
            { EngineType.Nuclear,  900f },
        };
        public static readonly Dictionary<EngineType, float> EngineMassKg = new()
        {
            { EngineType.Chemical, 850f },
            { EngineType.Ion,      220f },
            { EngineType.Nuclear,  1200f },
        };
        public static readonly Dictionary<EngineType, float> EngineCostM = new()
        {
            { EngineType.Chemical, 80f },
            { EngineType.Ion,      140f },
            { EngineType.Nuclear,  320f },
        };
        public static readonly Dictionary<EngineType, float> EngineThrustMult = new()
        {
            { EngineType.Chemical, 1.0f },
            { EngineType.Ion,      0.55f },
            { EngineType.Nuclear,  1.4f },
        };
        public static readonly Dictionary<EngineType, string> EngineFuelType = new()
        {
            { EngineType.Chemical, "LOX / LH2 (Hidrogeno + Oxigeno liquido)" },
            { EngineType.Ion,      "Xenon ionizado (gas noble)" },
            { EngineType.Nuclear,  "LH2 calentado por reactor (NTR)" },
        };
        public static readonly Dictionary<EngineType, string> EngineNasaRef = new()
        {
            { EngineType.Chemical, "Space Shuttle, SLS — alto empuje, ideal planetas cercanos." },
            { EngineType.Ion,      "Sonda Dawn (Vesta+Ceres) — 8x mas eficiente en combustible." },
            { EngineType.Nuclear,  "Programa NERVA (NASA, probado) — mejor balance empuje/Isp." },
        };

        public static readonly Dictionary<PowerSystem, float> PowerOutputW = new()
        {
            { PowerSystem.Solar,          280f },
            { PowerSystem.RTG,            500f },
            { PowerSystem.NuclearReactor, 1200f },
        };
        public static readonly Dictionary<PowerSystem, float> PowerMassKg = new()
        {
            { PowerSystem.Solar,          180f },
            { PowerSystem.RTG,            55f },
            { PowerSystem.NuclearReactor, 350f },
        };
        public static readonly Dictionary<PowerSystem, float> PowerCostM = new()
        {
            { PowerSystem.Solar,          40f },
            { PowerSystem.RTG,            110f },
            { PowerSystem.NuclearReactor, 250f },
        };
        public static readonly Dictionary<PowerSystem, string> PowerNasaRef = new()
        {
            { PowerSystem.Solar,          "Juno (Jupiter) usa 72m2 de paneles. En Neptuno = 0.1% — inutiles." },
            { PowerSystem.RTG,            "Voyager, Cassini, New Horizons — potencia constante a cualquier distancia." },
            { PowerSystem.NuclearReactor, "Kilopower (NASA) — reactor compacto para planetas exteriores." },
        };

        public static readonly Dictionary<AntennaType, float> AntennaDataRateKbps = new()
        {
            { AntennaType.LowGain,    0.5f },
            { AntennaType.MediumGain, 100f },
            { AntennaType.HighGain,   6000f },
        };
        public static readonly Dictionary<AntennaType, float> AntennaMassKg = new()
        {
            { AntennaType.LowGain,    8f },
            { AntennaType.MediumGain, 35f },
            { AntennaType.HighGain,   95f },
        };
        public static readonly Dictionary<AntennaType, float> AntennaCostM = new()
        {
            { AntennaType.LowGain,    15f },
            { AntennaType.MediumGain, 55f },
            { AntennaType.HighGain,   120f },
        };

        public static readonly Dictionary<LaunchVehicle, float> LaunchCapacityKg = new()
        {
            { LaunchVehicle.Falcon9,      3500f },
            { LaunchVehicle.FalconHeavy,  8000f },
            { LaunchVehicle.SLS,          22000f },
            { LaunchVehicle.Starship,     100000f },
        };
        public static readonly Dictionary<LaunchVehicle, float> LaunchCostM = new()
        {
            { LaunchVehicle.Falcon9,      67f },
            { LaunchVehicle.FalconHeavy,  150f },
            { LaunchVehicle.SLS,          2000f },
            { LaunchVehicle.Starship,     100f },
        };

        public static readonly Dictionary<ScienceInstrument, float> InstrumentMassKg = new()
        {
            { ScienceInstrument.WideAngleCamera,   12f },
            { ScienceInstrument.NarrowAngleCamera, 18f },
            { ScienceInstrument.UVSpectrometer,    28f },
            { ScienceInstrument.InfraredMapper,    35f },
            { ScienceInstrument.Magnetometer,      8f  },
            { ScienceInstrument.ParticleDetector,  22f },
            { ScienceInstrument.RadioScience,      5f  },
            { ScienceInstrument.RadarAltimeter,    42f },
        };
        public static readonly Dictionary<ScienceInstrument, float> InstrumentPowerW = new()
        {
            { ScienceInstrument.WideAngleCamera,   15f },
            { ScienceInstrument.NarrowAngleCamera, 25f },
            { ScienceInstrument.UVSpectrometer,    40f },
            { ScienceInstrument.InfraredMapper,    60f },
            { ScienceInstrument.Magnetometer,      5f  },
            { ScienceInstrument.ParticleDetector,  30f },
            { ScienceInstrument.RadioScience,      10f },
            { ScienceInstrument.RadarAltimeter,    80f },
        };
        public static readonly Dictionary<ScienceInstrument, float> InstrumentCostM = new()
        {
            { ScienceInstrument.WideAngleCamera,   25f },
            { ScienceInstrument.NarrowAngleCamera, 45f },
            { ScienceInstrument.UVSpectrometer,    70f },
            { ScienceInstrument.InfraredMapper,    85f },
            { ScienceInstrument.Magnetometer,      30f },
            { ScienceInstrument.ParticleDetector,  55f },
            { ScienceInstrument.RadioScience,      20f },
            { ScienceInstrument.RadarAltimeter,    65f },
        };
        public static readonly Dictionary<ScienceInstrument, int> InstrumentSciencePoints = new()
        {
            { ScienceInstrument.WideAngleCamera,   800  },
            { ScienceInstrument.NarrowAngleCamera, 1200 },
            { ScienceInstrument.UVSpectrometer,    1800 },
            { ScienceInstrument.InfraredMapper,    2200 },
            { ScienceInstrument.Magnetometer,      1000 },
            { ScienceInstrument.ParticleDetector,  1400 },
            { ScienceInstrument.RadioScience,      600  },
            { ScienceInstrument.RadarAltimeter,    1600 },
        };

        // Distance from Sun in AU — for solar degradation
        public static readonly Dictionary<string, float> PlanetAU = new()
        {
            { "mercury", 0.39f },
            { "venus",   0.72f },
            { "mars",    1.52f },
            { "jupiter", 5.20f },
            { "saturn",  9.54f },
            { "uranus",  19.19f },
            { "neptune", 30.07f },
        };

        // Minimum delta-V required to reach each planet (m/s)
        public static readonly Dictionary<string, float> MinDeltaV = new()
        {
            { "mercury", 10900f },
            { "venus",   6500f  },
            { "mars",    5600f  },
            { "jupiter", 9100f  },
            { "saturn",  10400f },
            { "uranus",  15900f },
            { "neptune", 16000f },
        };

        // ── Objetivo de misión ────────────────────────────────────────────────
        // Orbit insertion burn at arrival (real values: Juno JOI 542 m/s, Cassini SOI 626 m/s,
        // MRO MOI ~1,000 m/s, MESSENGER MOI ~860 m/s; Uranus/Neptune from NASA mission studies)
        public static readonly Dictionary<string, float> OrbitInsertionDV = new()
        {
            { "mercury", 900f  },
            { "venus",   1000f },
            { "mars",    1000f },
            { "jupiter", 600f  },
            { "saturn",  650f  },
            { "uranus",  1500f },
            { "neptune", 1800f },
        };

        // Descent from orbit to the surface; gas giants have no surface to land on.
        // Mars/Venus are cheap thanks to aerobraking and parachutes; Mercury is all rocket.
        public static readonly Dictionary<string, float> LandingDV = new()
        {
            { "mercury", 3000f },
            { "venus",   600f  },
            { "mars",    900f  },
        };

        public const float LanderMassKg = 250f;
        public const float LanderCostM  = 150f;

        // Longer observation = more science: an orbiter maps for years, a lander touches the ground
        public static float ObjectiveScienceMult(string objective) => objective switch
        {
            "orbit"   => 1.5f,
            "landing" => 2.0f,
            _         => 1.0f,
        };

        // Real missions that flew each profile — shown in the debrief
        public static string RealMission(string planet, string objective) => (planet, objective) switch
        {
            ("mercury", "flyby")   => "Mariner 10 (1974)",
            ("mercury", "orbit")   => "MESSENGER (2011)",
            ("mercury", "landing") => "ninguna todavía: serías la primera",
            ("venus",   "flyby")   => "Mariner 2 (1962)",
            ("venus",   "orbit")   => "Magellan (1990)",
            ("venus",   "landing") => "Venera 13 (1982)",
            ("mars",    "flyby")   => "Mariner 4 (1965)",
            ("mars",    "orbit")   => "Mars Reconnaissance Orbiter (2006)",
            ("mars",    "landing") => "Perseverance (2021)",
            ("jupiter", "flyby")   => "Pioneer 10 (1973)",
            ("jupiter", "orbit")   => "Juno (2016)",
            ("saturn",  "flyby")   => "Voyager 1 (1980)",
            ("saturn",  "orbit")   => "Cassini (2004)",
            ("uranus",  "flyby")   => "Voyager 2 (1986)",
            ("uranus",  "orbit")   => "Uranus Orbiter and Probe (propuesta NASA)",
            ("neptune", "flyby")   => "Voyager 2 (1989)",
            ("neptune", "orbit")   => "Neptune Odyssey (propuesta NASA)",
            _                      => "ninguna",
        };

        // ── Comunicaciones (datos que cada instrumento debe enviar a la Tierra) ─
        public static readonly Dictionary<ScienceInstrument, float> InstrumentDataMb = new()
        {
            { ScienceInstrument.WideAngleCamera,   40f },
            { ScienceInstrument.NarrowAngleCamera, 60f },
            { ScienceInstrument.UVSpectrometer,    30f },
            { ScienceInstrument.InfraredMapper,    80f },
            { ScienceInstrument.Magnetometer,      5f  },
            { ScienceInstrument.ParticleDetector,  10f },
            { ScienceInstrument.RadioScience,      2f  },
            { ScienceInstrument.RadarAltimeter,    50f },
        };

        // Game-time downlink. Real rates span 12,000× (0.5 kbps → 6 Mbps), which would make
        // low gain unplayable; the order and the pain are kept, the ratio is compressed.
        public static readonly Dictionary<AntennaType, float> GameDownlinkMbps = new()
        {
            { AntennaType.LowGain,    1.5f },
            { AntennaType.MediumGain, 6f   },
            { AntennaType.HighGain,   30f  },
        };

        // Light-time in game seconds: 1 real minute of mean one-way delay = 0.2 s
        public static readonly Dictionary<string, float> CommDelayGameS = new()
        {
            { "mercury", 1.6f  },
            { "venus",   1.6f  },
            { "mars",    2.5f  },
            { "jupiter", 8.7f  },
            { "saturn",  15.2f },
            { "uranus",  32.8f },
            { "neptune", 50.5f },
        };

        // One-way comm delay string per planet
        public static readonly Dictionary<string, string> CommDelayLabel = new()
        {
            { "mercury", "3-13 min"   },
            { "venus",   "2-14 min"   },
            { "mars",    "3-22 min"   },
            { "jupiter", "35-52 min"  },
            { "saturn",  "68-84 min"  },
            { "uranus",  "156-172 min"},
            { "neptune", "245-260 min"},
        };

        // ── Computed properties ────────────────────────────────────────────────

        public bool IsLanding => Objective == "landing";
        public bool IsOrbit   => Objective == "orbit";

        public float DryMassKg =>
            SpacecraftBusKg
            + (IsLanding ? LanderMassKg : 0f)
            + EngineMassKg[Engine]
            + PowerMassKg[Power]
            + AntennaMassKg[Antenna]
            + Instruments.Sum(i => InstrumentMassKg[i]);

        public float WetMassKg  => DryMassKg + FuelMassKg;
        public float MassFraction => WetMassKg > 0 ? FuelMassKg / WetMassKg : 0f;

        // Tsiolkovsky rocket equation: ΔV = Isp × g₀ × ln(m_wet / m_dry)
        public float DeltaVms =>
            FuelMassKg > 0 && DryMassKg > 0
                ? EngineIspS[Engine] * 9.80665f * Mathf.Log(WetMassKg / DryMassKg)
                : 0f;

        // Inverse-square solar degradation: efficiency = (1 AU / distance_AU)²
        public float SolarEfficiency()
        {
            float au = PlanetAU.TryGetValue(TargetPlanetId, out var v) ? v : 1f;
            return 1f / (au * au);
        }

        // Effective power after distance degradation (solar only)
        public float EffectivePowerW =>
            Power == PowerSystem.Solar
                ? PowerOutputW[Power] * SolarEfficiency()
                : PowerOutputW[Power];

        public float PowerUsedW =>
            BusPowerW + Instruments.Sum(i => InstrumentPowerW[i]);

        public float TotalCostM =>
            EngineCostM[Engine]
            + PowerCostM[Power]
            + AntennaCostM[Antenna]
            + LaunchCostM[LaunchVeh]
            + Instruments.Sum(i => InstrumentCostM[i])
            + (IsLanding ? LanderCostM : 0f)
            + FuelMassKg * 0.001f;

        // ΔV breakdown: cruise to the planet + orbit insertion + descent
        public float CruiseDeltaV    => MinDeltaV.TryGetValue(TargetPlanetId, out var d) ? d : 5600f;
        public float InsertionDeltaV => IsOrbit || IsLanding
            ? (OrbitInsertionDV.TryGetValue(TargetPlanetId, out var o) ? o : 1000f) : 0f;
        public float LandingDeltaV   => IsLanding
            ? (LandingDV.TryGetValue(TargetPlanetId, out var l) ? l : 0f) : 0f;
        public float RequiredDeltaV  => CruiseDeltaV + InsertionDeltaV + LandingDeltaV;

        public bool  CanLandOnTarget   => LandingDV.ContainsKey(TargetPlanetId ?? "");
        public bool  IsObjectiveOk     => !IsLanding || CanLandOnTarget;
        public float ScienceMultiplier => ObjectiveScienceMult(Objective);
        public float TotalDataMb       => Instruments.Sum(i => InstrumentDataMb[i]);

        public int MaxSciencePoints =>
            Instruments.Sum(i => InstrumentSciencePoints[i]);

        public bool IsBudgetOk  => TotalCostM  <= BudgetLimitM;
        public bool IsMassOk    => WetMassKg   <= LaunchCapacityKg[LaunchVeh];
        public bool IsPowerOk   => PowerUsedW  <= EffectivePowerW;
        public bool HasDeltaV   => DeltaVms    >= RequiredDeltaV;
        public bool IsSolarViable => Power != PowerSystem.Solar || SolarEfficiency() >= 0.05f;
        public bool IsValid     => IsBudgetOk && IsMassOk && IsPowerOk && HasDeltaV && IsObjectiveOk;

        // Maps real fuelMassKg to game fuel units
        public float GameFuelMax => Mathf.Clamp(FuelMassKg / 1200f * 500f, 100f, 2000f);

        public static MissionDesign DefaultFor(string planetId) => new()
        {
            TargetPlanetId = planetId,
            Objective      = "flyby",
            Engine         = EngineType.Chemical,
            FuelMassKg     = 4500f,   // enough ΔV for Mars with the chemical engine
            Instruments    = new() { ScienceInstrument.WideAngleCamera },
            Power          = PowerSystem.Solar,
            Antenna        = AntennaType.MediumGain,
            LaunchVeh      = LaunchVehicle.FalconHeavy,
        };
    }
}
