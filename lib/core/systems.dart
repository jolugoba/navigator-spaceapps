import 'dart:math' as math;
import 'components.dart';
import 'space_world.dart';
import 'vec3.dart';

abstract class System {
  void tick(SpaceWorld world, double dt);
}

// Drena combustible y corta thrust si está vacío
class FuelSystem extends System {
  static const double mainDrainRate  = 3.0;
  static const double boostDrainRate = 20.0;

  @override
  void tick(SpaceWorld world, double dt) {
    for (final id in world.query<ShipFuel>()) {
      final fuel   = world.get<ShipFuel>(id)!;
      final thrust = world.get<ShipThrust>(id);
      final score  = world.get<MissionScore>(id);

      if (fuel.isEmpty) {
        if (thrust != null) {
          thrust.main    = 0;
          thrust.lateral = 0;
          thrust.boost   = false;
        }
        continue;
      }
      if (thrust == null) continue;

      final thrusting = thrust.main.abs() > 0.01 || thrust.lateral.abs() > 0.01;
      final drain = (thrusting ? thrust.main.abs() * mainDrainRate : 0.0)
                  + (thrust.boost ? boostDrainRate : 0.0);
      fuel.current = (fuel.current - drain * dt).clamp(0.0, fuel.max);

      if (score != null) {
        if (thrust.boost) score.boostSecondsUsed += dt;
        score.totalDeltaV += thrust.main.abs() * dt;
      }
    }
  }
}

// Aplica thrust → velocidad, integra posición, gravedad planetaria
class FlightPhysicsSystem extends System {
  static const double maxSpeed        = 80.0;
  static const double boostMultiplier = 3.0;
  static const double drag            = 0.02;
  static const double thrustForce     = 30.0;

  double thrustMultiplier = 1.0;
  final Map<String, double> planetGravityMu = {};
  Map<String, Vec3> Function()? getPlanetPositions;

  @override
  void tick(SpaceWorld world, double dt) {
    for (final id in world.query<ShipTransform>()) {
      final transform = world.get<ShipTransform>(id)!;
      final vel  = world.get<ShipVelocity>(id);
      final thr  = world.get<ShipThrust>(id);
      if (vel == null) continue;

      if (thr != null) {
        final forward = transform.forward;
        final right   = transform.right;
        final boost   = thr.boost ? boostMultiplier : 1.0;

        vel.linear = vel.linear +
            forward * (thr.main * thrustForce * thrustMultiplier * boost * dt);

        if (thr.lateral.abs() > 0.01) {
          vel.linear = vel.linear + right * (thr.lateral * thrustForce * 0.4 * dt);
        }

        final speed = vel.linear.length;
        if (speed > 0.01) {
          vel.linear = vel.linear * (1.0 - drag * dt);
        }

        final currentSpeed = vel.linear.length;
        final cap = thr.boost ? maxSpeed * boostMultiplier : maxSpeed;
        if (currentSpeed > cap) {
          vel.linear = vel.linear.normalized() * cap;
        }
      }

      // Planetary gravity — F = μ/r² toward planet (independent of thrust)
      if (getPlanetPositions != null && planetGravityMu.isNotEmpty) {
        final positions = getPlanetPositions!();
        for (final entry in planetGravityMu.entries) {
          final ppos = positions[entry.key];
          if (ppos == null) continue;
          final delta = ppos - transform.position;
          final r2 = delta.dot(delta);
          if (r2 < 100.0) continue; // avoid singularity
          final forceMag = entry.value / r2;
          vel.linear = vel.linear + delta.normalized() * (forceMag * dt);
        }
      }

      transform.position = transform.position + vel.linear * dt;
    }
  }
}

// Aplica joystick angular → yaw/pitch de la nave
class SteeringSystem extends System {
  @override
  void tick(SpaceWorld world, double dt) {
    for (final id in world.query<ShipTransform>()) {
      final transform = world.get<ShipTransform>(id)!;
      final vel = world.get<ShipVelocity>(id);
      if (vel == null) continue;

      const angularSpeed = 1.8;
      transform.yaw   += vel.angularYaw   * angularSpeed * dt;
      transform.pitch += vel.angularPitch * angularSpeed * dt;

      transform.pitch = transform.pitch.clamp(
        -math.pi / 2 + 0.05, math.pi / 2 - 0.05);
    }
  }
}

// Actualiza distancia al planeta objetivo usando posiciones 3D reales
class MissionTrackingSystem extends System {
  // Posiciones de planetas en coordenadas de juego (actualizadas desde NASA o desde main)
  final Map<String, Vec3> planetPositions;
  // Fallback: distancias escalares (legacy)
  final Map<String, double> planetDistances;

  MissionTrackingSystem({
    Map<String, Vec3>?   planetPositions,
    Map<String, double>? planetDistances,
  })  : planetPositions = planetPositions ?? {},
        planetDistances = planetDistances ?? {};

  @override
  void tick(SpaceWorld world, double dt) {
    for (final id in world.query<MissionTarget>()) {
      final mission   = world.get<MissionTarget>(id)!;
      final transform = world.get<ShipTransform>(id);
      if (transform == null) continue;

      final pos = planetPositions[mission.planetId];
      if (pos != null) {
        final delta = pos - transform.position;
        mission.distanceUnits = delta.length;
      } else {
        final targetDist = planetDistances[mission.planetId] ?? 1000.0;
        mission.distanceUnits = (targetDist - transform.position.length).abs();
      }

      if (mission.arrivedAt == null && mission.distanceUnits < 10.0) {
        mission.arrivedAt = DateTime.now();
      }
    }
  }
}

// Colisiones básicas entre naves
class ShipCollisionSystem extends System {
  static const double collisionRadius = 2.0;
  final void Function(EntityId a, EntityId b)? onCollision;

  ShipCollisionSystem({this.onCollision});

  @override
  void tick(SpaceWorld world, double dt) {
    final ships = world.query<ShipTransform>().toList();
    for (int i = 0; i < ships.length; i++) {
      for (int j = i + 1; j < ships.length; j++) {
        final tA = world.get<ShipTransform>(ships[i])!;
        final tB = world.get<ShipTransform>(ships[j])!;
        final dist = (tA.position - tB.position).length;
        if (dist < collisionRadius * 2) {
          onCollision?.call(ships[i], ships[j]);
          final vA = world.get<ShipVelocity>(ships[i]);
          final vB = world.get<ShipVelocity>(ships[j]);
          if (vA != null && vB != null) {
            final tmp = vA.linear;
            vA.linear = vB.linear * 0.5;
            vB.linear = tmp * 0.5;
          }
        }
      }
    }
  }
}
