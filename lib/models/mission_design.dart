import 'dart:math' as math;

// ─────────────────────────────────────────────────────────────────────────────
// Enumerations
// ─────────────────────────────────────────────────────────────────────────────

enum EngineType { chemical, ion, nuclear }

enum PowerSystem { solar, rtg, nuclearReactor }

enum AntennaType { lowGain, mediumGain, highGain }

enum LaunchVehicle { falcon9, falconHeavy, sls, starship }

enum ScienceInstrument {
  wideAngleCamera,
  narrowAngleCamera,
  uvSpectrometer,
  infraredMapper,
  magnetometer,
  particleDetector,
  radioScience,
  radarAltimeter,
}

// ─────────────────────────────────────────────────────────────────────────────
// Static lookup tables (const maps)
// ─────────────────────────────────────────────────────────────────────────────

const engineLabel = {
  EngineType.chemical: 'Motor Químico',
  EngineType.ion: 'Motor Iónico',
  EngineType.nuclear: 'Motor Nuclear (NTR)',
};
const engineDesc = {
  EngineType.chemical: 'Alto empuje, bajo Isp. Ideal para planetas cercanos.',
  EngineType.ion: 'Bajo empuje, altísimo Isp. Eficiente en trayectorias largas.',
  EngineType.nuclear: 'Alto empuje y alto Isp. Costoso pero el más capaz.',
};
const engineCostM = {
  EngineType.chemical: 80.0,
  EngineType.ion: 140.0,
  EngineType.nuclear: 320.0,
};
const engineMassKg = {
  EngineType.chemical: 850.0,
  EngineType.ion: 220.0,
  EngineType.nuclear: 1200.0,
};
const engineIspS = {
  EngineType.chemical: 450,
  EngineType.ion: 3000,
  EngineType.nuclear: 900,
};
const engineThrustMultiplier = {
  EngineType.chemical: 1.0,
  EngineType.ion: 0.55,
  EngineType.nuclear: 1.4,
};
const engineFuelType = {
  EngineType.chemical: 'LOX / LH₂ (Hidrógeno + Oxígeno líquido)',
  EngineType.ion: 'Xenón ionizado (gas noble)',
  EngineType.nuclear: 'LH₂ calentado por reactor (NTR)',
};
const engineNasaRef = {
  EngineType.chemical: 'Space Shuttle, SLS — alto empuje, ideal para planetas cercanos.',
  EngineType.ion: 'Sonda Dawn (Vesta + Ceres) — 8× más eficiente en combustible, pero tarda años.',
  EngineType.nuclear: 'Programa NERVA (NASA, probado) — el mejor balance empuje/Isp disponible.',
};

// Distance from Sun in AU per planet — for inverse-square solar degradation
const planetAU = {
  'mercury': 0.39,
  'venus': 0.72,
  'mars': 1.52,
  'jupiter': 5.20,
  'saturn': 9.54,
  'uranus': 19.19,
  'neptune': 30.07,
};

// One-way light travel time in minutes (AU × 8.317 min/AU)
const planetCommDelayMin = {
  'mercury': '3 – 13 min',
  'venus': '2 – 14 min',
  'mars': '3 – 22 min',
  'jupiter': '35 – 52 min',
  'saturn': '68 – 84 min',
  'uranus': '156 – 172 min',
  'neptune': '245 – 260 min',
};

const powerNasaRef = {
  PowerSystem.solar: 'Juno (Júpiter) usa paneles de 72 m². En Neptuno generan solo 0.1% — inviables.',
  PowerSystem.rtg: 'Voyager, Cassini, New Horizons — potencia constante a cualquier distancia.',
  PowerSystem.nuclearReactor: 'Kilopower (NASA) — reactor compacto para misiones a planetas exteriores.',
};

const powerLabel = {
  PowerSystem.solar: 'Paneles Solares',
  PowerSystem.rtg: 'RTG (Radioisótopos)',
  PowerSystem.nuclearReactor: 'Reactor Nuclear',
};
const powerDesc = {
  PowerSystem.solar: 'Baratos. Eficiencia cae con la distancia al Sol.',
  PowerSystem.rtg: 'Confiables. Potencia constante. Usados en Voyager.',
  PowerSystem.nuclearReactor: 'Máxima potencia. Muy pesado y costoso.',
};
const powerCostM = {
  PowerSystem.solar: 40.0,
  PowerSystem.rtg: 110.0,
  PowerSystem.nuclearReactor: 250.0,
};
const powerMassKg = {
  PowerSystem.solar: 180.0,
  PowerSystem.rtg: 55.0,
  PowerSystem.nuclearReactor: 350.0,
};
const powerOutputW = {
  PowerSystem.solar: 280.0,
  PowerSystem.rtg: 500.0,
  PowerSystem.nuclearReactor: 1200.0,
};

const antennaLabel = {
  AntennaType.lowGain: 'Antena Omnidireccional',
  AntennaType.mediumGain: 'Antena Mediana',
  AntennaType.highGain: 'Antena HGA 3.7m',
};
const antennaDesc = {
  AntennaType.lowGain: '0.5 kbps. Funciona sin apuntar. Solo telemetría.',
  AntennaType.mediumGain: '100 kbps. Transmisión de imágenes comprimidas.',
  AntennaType.highGain: '6 Mbps. Transmisión de ciencia en alta resolución.',
};
const antennaCostM = {
  AntennaType.lowGain: 15.0,
  AntennaType.mediumGain: 55.0,
  AntennaType.highGain: 120.0,
};
const antennaMassKg = {
  AntennaType.lowGain: 8.0,
  AntennaType.mediumGain: 35.0,
  AntennaType.highGain: 95.0,
};
const antennaDataRateKbps = {
  AntennaType.lowGain: 0.5,
  AntennaType.mediumGain: 100.0,
  AntennaType.highGain: 6000.0,
};

const launchLabel = {
  LaunchVehicle.falcon9: 'Falcon 9',
  LaunchVehicle.falconHeavy: 'Falcon Heavy',
  LaunchVehicle.sls: 'SLS Block 1B',
  LaunchVehicle.starship: 'Starship',
};
const launchCostM = {
  LaunchVehicle.falcon9: 67.0,
  LaunchVehicle.falconHeavy: 150.0,
  LaunchVehicle.sls: 2000.0,
  LaunchVehicle.starship: 100.0,
};
const launchCapacityKg = {
  LaunchVehicle.falcon9: 3500.0,
  LaunchVehicle.falconHeavy: 8000.0,
  LaunchVehicle.sls: 22000.0,
  LaunchVehicle.starship: 100000.0,
};

const instrumentLabel = {
  ScienceInstrument.wideAngleCamera: 'Cámara Gran Angular',
  ScienceInstrument.narrowAngleCamera: 'Cámara Teleobjetivo',
  ScienceInstrument.uvSpectrometer: 'Espectrómetro UV',
  ScienceInstrument.infraredMapper: 'Mapeador Infrarrojo',
  ScienceInstrument.magnetometer: 'Magnetómetro',
  ScienceInstrument.particleDetector: 'Detector de Partículas',
  ScienceInstrument.radioScience: 'Experimento Radio-Ciencia',
  ScienceInstrument.radarAltimeter: 'Altímetro de Radar',
};
const instrumentDesc = {
  ScienceInstrument.wideAngleCamera: 'Imágenes panorámicas de la superficie.',
  ScienceInstrument.narrowAngleCamera: 'Mapeo de detalle en alta resolución.',
  ScienceInstrument.uvSpectrometer: 'Composición de la atmósfera superior.',
  ScienceInstrument.infraredMapper: 'Temperatura superficial y minerales.',
  ScienceInstrument.magnetometer: 'Campo magnético y magnetosfera.',
  ScienceInstrument.particleDetector: 'Viento solar e iones energéticos.',
  ScienceInstrument.radioScience: 'Gravedad y atmósfera por ocultación.',
  ScienceInstrument.radarAltimeter: 'Topografía de superficie (necesita órbita).',
};
const instrumentCostM = {
  ScienceInstrument.wideAngleCamera: 25.0,
  ScienceInstrument.narrowAngleCamera: 45.0,
  ScienceInstrument.uvSpectrometer: 70.0,
  ScienceInstrument.infraredMapper: 85.0,
  ScienceInstrument.magnetometer: 30.0,
  ScienceInstrument.particleDetector: 55.0,
  ScienceInstrument.radioScience: 20.0,
  ScienceInstrument.radarAltimeter: 65.0,
};
const instrumentMassKg = {
  ScienceInstrument.wideAngleCamera: 12.0,
  ScienceInstrument.narrowAngleCamera: 18.0,
  ScienceInstrument.uvSpectrometer: 28.0,
  ScienceInstrument.infraredMapper: 35.0,
  ScienceInstrument.magnetometer: 8.0,
  ScienceInstrument.particleDetector: 22.0,
  ScienceInstrument.radioScience: 5.0,
  ScienceInstrument.radarAltimeter: 42.0,
};
const instrumentPowerW = {
  ScienceInstrument.wideAngleCamera: 15.0,
  ScienceInstrument.narrowAngleCamera: 25.0,
  ScienceInstrument.uvSpectrometer: 40.0,
  ScienceInstrument.infraredMapper: 60.0,
  ScienceInstrument.magnetometer: 5.0,
  ScienceInstrument.particleDetector: 30.0,
  ScienceInstrument.radioScience: 10.0,
  ScienceInstrument.radarAltimeter: 80.0,
};
const instrumentSciencePoints = {
  ScienceInstrument.wideAngleCamera: 800,
  ScienceInstrument.narrowAngleCamera: 1200,
  ScienceInstrument.uvSpectrometer: 1800,
  ScienceInstrument.infraredMapper: 2200,
  ScienceInstrument.magnetometer: 1000,
  ScienceInstrument.particleDetector: 1400,
  ScienceInstrument.radioScience: 600,
  ScienceInstrument.radarAltimeter: 1600,
};

// ─────────────────────────────────────────────────────────────────────────────
// MissionDesign — immutable data class
// ─────────────────────────────────────────────────────────────────────────────

class MissionDesign {
  static const double budgetLimitM = 1000.0; // $1B
  static const double spacecraftBusKg = 500.0; // structural/avionics dry mass
  static const double busPowerW = 80.0; // always-on bus power

  final String targetPlanetId;
  final String objective; // 'flyby' | 'orbit' | 'landing'
  final EngineType engine;
  final double fuelMassKg;
  final Set<ScienceInstrument> instruments;
  final PowerSystem power;
  final AntennaType antenna;
  final LaunchVehicle launchVehicle;

  const MissionDesign({
    required this.targetPlanetId,
    required this.objective,
    required this.engine,
    required this.fuelMassKg,
    required this.instruments,
    required this.power,
    required this.antenna,
    required this.launchVehicle,
  });

  // ── Mass budget ─────────────────────────────────────────────────────────────

  double get dryMassKg =>
      spacecraftBusKg +
      engineMassKg[engine]! +
      powerMassKg[power]! +
      antennaMassKg[antenna]! +
      instruments.fold(0.0, (s, i) => s + instrumentMassKg[i]!);

  double get wetMassKg => dryMassKg + fuelMassKg;
  bool get isMassOk => wetMassKg <= launchCapacityKg[launchVehicle]!;

  // ── Delta-V (Tsiolkovsky) ────────────────────────────────────────────────────

  double get deltaVms {
    if (fuelMassKg <= 0 || dryMassKg <= 0) return 0;
    return engineIspS[engine]! *
        9.80665 *
        math.log(wetMassKg / dryMassKg);
  }

  // Minimum delta-V required per planet (approximate, Earth departure + arrival)
  static const minDeltaV = {
    'mercury': 10900.0,
    'venus': 6500.0,
    'mars': 5600.0,
    'jupiter': 9100.0,
    'saturn': 10400.0,
    'uranus': 15900.0,
    'neptune': 16000.0,
  };

  bool get hasSufficientDeltaV =>
      deltaVms >= (minDeltaV[targetPlanetId] ?? 5600.0);

  // ── Power budget ────────────────────────────────────────────────────────────

  // Inverse-square solar degradation: efficiency = (1 AU / distance_AU)²
  double solarEfficiency() {
    final au = planetAU[targetPlanetId] ?? 1.0;
    return 1.0 / (au * au);
  }

  // Effective power after distance degradation (solar only; RTG/reactor = constant)
  double get effectivePowerW {
    final nominal = powerOutputW[power]!;
    if (power == PowerSystem.solar) return nominal * solarEfficiency();
    return nominal;
  }

  double get powerUsedW =>
      busPowerW + instruments.fold(0.0, (s, i) => s + instrumentPowerW[i]!);
  bool get isPowerOk => powerUsedW <= effectivePowerW;

  // Solar panels essentially useless below 5% efficiency (Jupiter and beyond)
  bool get isSolarViable =>
      power != PowerSystem.solar || solarEfficiency() >= 0.05;

  // ── Financial budget ────────────────────────────────────────────────────────

  double get totalCostM =>
      engineCostM[engine]! +
      powerCostM[power]! +
      antennaCostM[antenna]! +
      launchCostM[launchVehicle]! +
      instruments.fold(0.0, (s, i) => s + instrumentCostM[i]!) +
      fuelMassKg * 0.001; // $1k/kg propellant

  bool get isBudgetOk => totalCostM <= budgetLimitM;

  // ── Science value ────────────────────────────────────────────────────────────

  int get maxSciencePoints =>
      instruments.fold(0, (s, i) => s + instrumentSciencePoints[i]!);

  // Data volume producible during 30-day encounter (MB)
  double get scienceDataMB {
    final rateKbps = antennaDataRateKbps[antenna]!;
    return rateKbps * 3600 * 24 * 30 / (8 * 1000); // kbps → MB over 30 days
  }

  // ── Validity ────────────────────────────────────────────────────────────────

  bool get isValid => isBudgetOk && isMassOk && isPowerOk;

  String? get firstError {
    if (!isBudgetOk) {
      return 'Presupuesto excedido: \$${totalCostM.round()}M / \$${budgetLimitM.round()}M';
    }
    if (!isMassOk) {
      return 'Masa excede capacidad: ${wetMassKg.round()} kg / ${launchCapacityKg[launchVehicle]!.round()} kg';
    }
    if (!isPowerOk) {
      return 'Consumo eléctrico excesivo: ${powerUsedW.round()} W / ${effectivePowerW.round()} W';
    }
    return null;
  }

  // ── Mass fraction — "the tyranny of the rocket equation" ───────────────────
  // Fraction of total wet mass that is propellant (0.0 – 1.0)
  double get massFraction =>
      wetMassKg > 0 ? fuelMassKg / wetMassKg : 0.0;

  // ── Fuel fraction for in-game ShipFuel ─────────────────────────────────────

  // Maps real fuelMassKg into game fuel units (500 = default max, scaled by mass)
  double get gameFuelMax => (fuelMassKg / 1200.0 * 500.0).clamp(100.0, 2000.0);

  // ── Copy with ───────────────────────────────────────────────────────────────

  MissionDesign copyWith({
    String? targetPlanetId,
    String? objective,
    EngineType? engine,
    double? fuelMassKg,
    Set<ScienceInstrument>? instruments,
    PowerSystem? power,
    AntennaType? antenna,
    LaunchVehicle? launchVehicle,
  }) =>
      MissionDesign(
        targetPlanetId: targetPlanetId ?? this.targetPlanetId,
        objective: objective ?? this.objective,
        engine: engine ?? this.engine,
        fuelMassKg: fuelMassKg ?? this.fuelMassKg,
        instruments: instruments ?? Set.from(this.instruments),
        power: power ?? this.power,
        antenna: antenna ?? this.antenna,
        launchVehicle: launchVehicle ?? this.launchVehicle,
      );

  static MissionDesign defaultFor(String planetId) => MissionDesign(
        targetPlanetId: planetId,
        objective: 'flyby',
        engine: EngineType.chemical,
        fuelMassKg: 1200.0,
        instruments: {ScienceInstrument.wideAngleCamera},
        power: PowerSystem.solar,
        antenna: AntennaType.mediumGain,
        launchVehicle: LaunchVehicle.falconHeavy,
      );
}
