import 'dart:math' as math;
import 'vec3.dart';

typedef EntityId = int;

abstract class Component {
  const Component();
}

class ShipTransform extends Component {
  Vec3 position;
  double yaw;    // radians — apunta izquierda/derecha
  double pitch;  // radians — apunta arriba/abajo
  double roll;   // radians

  ShipTransform({
    required this.position,
    this.yaw = 0,
    this.pitch = 0,
    this.roll = 0,
  });

  Vec3 get forward {
    final cosP = math.cos(pitch), sinP = math.sin(pitch);
    final cosY = math.cos(yaw),   sinY = math.sin(yaw);
    return Vec3(-sinY * cosP, sinP, -cosY * cosP);
  }

  Vec3 get right {
    final cosY = math.cos(yaw), sinY = math.sin(yaw);
    return Vec3(cosY, 0, -sinY);
  }
}

class ShipVelocity extends Component {
  Vec3 linear;
  double angularYaw;
  double angularPitch;

  ShipVelocity({
    Vec3? linear,
    this.angularYaw = 0,
    this.angularPitch = 0,
  }) : linear = linear ?? Vec3.zero();
}

class ShipThrust extends Component {
  double main;
  double lateral;
  double vertical;
  bool boost;

  ShipThrust({
    this.main = 0,
    this.lateral = 0,
    this.vertical = 0,
    this.boost = false,
  });
}

class ShipOwner extends Component {
  final String nodeId;
  final String displayName;
  final bool isLocal;
  final String shipSkin;

  const ShipOwner({
    required this.nodeId,
    required this.displayName,
    required this.isLocal,
    this.shipSkin = 'explorer',
  });
}

class MissionTarget extends Component {
  final String planetId;
  double distanceUnits;
  DateTime? arrivedAt;

  MissionTarget({required this.planetId, this.distanceUnits = 0});
}

class ShipHealth extends Component {
  double hull;
  ShipHealth({this.hull = 100});
}

class Tag extends Component {
  final String value;
  const Tag(this.value);
}

// ── Combustible ────────────────────────────────────────────────────────────
class ShipFuel extends Component {
  double current;
  final double max;

  ShipFuel({this.current = 500.0, this.max = 500.0});

  double get fraction => (current / max).clamp(0.0, 1.0);
  bool   get isEmpty  => current <= 0;
  int    get percent  => (fraction * 100).round();
}

// ── Puntuación de misión ───────────────────────────────────────────────────
class MissionScore extends Component {
  final DateTime startTime;
  double boostSecondsUsed;
  double totalDeltaV;

  MissionScore()
      : startTime = DateTime.now(),
        boostSecondsUsed = 0,
        totalDeltaV = 0;

  Duration get elapsed => DateTime.now().difference(startTime);

  int compute(double fuelFraction) {
    const base = 5000;
    final fuelBonus    = (fuelFraction * 3000).round();
    final timeBonus    = (2000 - elapsed.inSeconds * 4).clamp(0, 2000).toInt();
    final boostPenalty = (boostSecondsUsed * 30).round().clamp(0, 1500);
    return (base + fuelBonus + timeBonus - boostPenalty).clamp(0, 10000);
  }
}
