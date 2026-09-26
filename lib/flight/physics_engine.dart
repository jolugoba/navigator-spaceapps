import '../core/components.dart';
import '../core/space_world.dart';
import '../core/systems.dart';
import '../core/vec3.dart';

class PhysicsEngine {
  final SpaceWorld world;
  late final FuelSystem _fuel;
  late final FlightPhysicsSystem _flight;
  late final SteeringSystem _steering;
  late final MissionTrackingSystem _mission;

  PhysicsEngine(this.world) {
    _fuel     = FuelSystem();
    _flight   = FlightPhysicsSystem();
    _steering = SteeringSystem();
    _mission  = MissionTrackingSystem();

    world.addSystem(_fuel);
    world.addSystem(_steering);
    world.addSystem(_flight);
    world.addSystem(_mission);
  }

  // Configura multiplicador de empuje según motor del diseño de misión
  void setEngineConfig({required double thrustMultiplier}) {
    _flight.thrustMultiplier = thrustMultiplier;
  }

  // Registra gravedad planetaria (μ en unidades de juego) y conecta callback
  void registerPlanetGravity(String planetId, double mu) {
    _flight.planetGravityMu[planetId] = mu;
    _flight.getPlanetPositions ??= () => _mission.planetPositions;
  }

  // Establece la posición 3D del planeta en coordenadas de juego
  void setPlanetPosition(String planetId, Vec3 pos) {
    _mission.planetPositions[planetId] = pos;
  }

  // Obtiene la posición 3D del planeta (null si no está definida)
  Vec3? planetWorldPosition(String planetId) =>
      _mission.planetPositions[planetId];

  void updatePlanetDistances(Map<String, double> distances) {
    _mission.planetDistances
      ..clear()
      ..addAll(distances);
  }

  void applyInput({
    required EntityId shipId,
    required double thrustMain,
    required double steerYaw,
    required double steerPitch,
    required double strafeLateral,
    required bool boost,
  }) {
    final thr = world.get<ShipThrust>(shipId);
    final vel = world.get<ShipVelocity>(shipId);
    if (thr == null || vel == null) return;

    thr.main    = thrustMain;
    thr.lateral = strafeLateral;
    thr.boost   = boost;

    vel.angularYaw   = steerYaw;
    vel.angularPitch = steerPitch;
  }

  void tick(double dt) => world.tick(dt);
}
