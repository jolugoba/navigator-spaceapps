class PlanetInfo {
  final String id;
  final String name;      // English default name
  final String nasaId;    // NASA Horizons body ID
  final double radiusKm;
  final String colorHex;
  final String modelAsset;

  const PlanetInfo({
    required this.id,
    required this.name,
    required this.nasaId,
    required this.radiusKm,
    required this.colorHex,
    required this.modelAsset,
  });
}

const List<PlanetInfo> kPlanets = [
  PlanetInfo(
    id: 'mercury', name: 'Mercury', nasaId: '199',
    radiusKm: 2439.7, colorHex: '#A0785A',
    modelAsset: 'assets/models/planets/mercury.glb',
  ),
  PlanetInfo(
    id: 'venus', name: 'Venus', nasaId: '299',
    radiusKm: 6051.8, colorHex: '#E8C47A',
    modelAsset: 'assets/models/planets/venus.glb',
  ),
  PlanetInfo(
    id: 'mars', name: 'Mars', nasaId: '499',
    radiusKm: 3389.5, colorHex: '#C1440E',
    modelAsset: 'assets/models/planets/mars.glb',
  ),
  PlanetInfo(
    id: 'jupiter', name: 'Jupiter', nasaId: '599',
    radiusKm: 71492.0, colorHex: '#C88B3A',
    modelAsset: 'assets/models/planets/jupiter.glb',
  ),
  PlanetInfo(
    id: 'saturn', name: 'Saturn', nasaId: '699',
    radiusKm: 60268.0, colorHex: '#E4D191',
    modelAsset: 'assets/models/planets/saturn.glb',
  ),
  PlanetInfo(
    id: 'uranus', name: 'Uranus', nasaId: '799',
    radiusKm: 25559.0, colorHex: '#7FDEEA',
    modelAsset: 'assets/models/planets/uranus.glb',
  ),
  PlanetInfo(
    id: 'neptune', name: 'Neptune', nasaId: '899',
    radiusKm: 24764.0, colorHex: '#3F54BA',
    modelAsset: 'assets/models/planets/neptune.glb',
  ),
];

PlanetInfo? planetById(String id) {
  try { return kPlanets.firstWhere((p) => p.id == id); }
  catch (_) { return null; }
}
