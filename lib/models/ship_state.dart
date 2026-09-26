import '../core/vec3.dart';

class ShipState {
  final String nodeId;
  final Vec3 position;
  final Vec3 velocity;
  final double yaw;          // radians — horizontal heading
  final double pitch;        // radians — vertical heading
  final double roll;         // radians
  final double throttle;     // 0.0 - 1.0
  final bool boosting;
  final String? missionTarget;  // planet id or null
  final double distanceToTarget; // game units
  final int score;
  final Int64 timestamp;   // ms since epoch

  const ShipState({
    required this.nodeId,
    required this.position,
    required this.velocity,
    this.yaw = 0,
    this.pitch = 0,
    this.roll = 0,
    this.throttle = 0,
    this.boosting = false,
    this.missionTarget,
    this.distanceToTarget = 0,
    this.score = 0,
    required this.timestamp,
  });

  ShipState copyWith({
    Vec3? position,
    Vec3? velocity,
    double? yaw,
    double? pitch,
    double? roll,
    double? throttle,
    bool? boosting,
    String? missionTarget,
    double? distanceToTarget,
    int? score,
  }) => ShipState(
    nodeId: nodeId,
    position: position ?? this.position,
    velocity: velocity ?? this.velocity,
    yaw: yaw ?? this.yaw,
    pitch: pitch ?? this.pitch,
    roll: roll ?? this.roll,
    throttle: throttle ?? this.throttle,
    boosting: boosting ?? this.boosting,
    missionTarget: missionTarget ?? this.missionTarget,
    distanceToTarget: distanceToTarget ?? this.distanceToTarget,
    score: score ?? this.score,
    timestamp: DateTime.now().millisecondsSinceEpoch,
  );

  Map<String, dynamic> toJson() => {
    't': 'ship',
    'id': nodeId,
    'px': position.x, 'py': position.y, 'pz': position.z,
    'vx': velocity.x, 'vy': velocity.y, 'vz': velocity.z,
    'yaw': yaw, 'pitch': pitch, 'roll': roll,
    'thr': throttle,
    'bst': boosting,
    'mis': missionTarget,
    'dst': distanceToTarget,
    'scr': score,
    'ts': timestamp,
  };

  factory ShipState.fromJson(Map<String, dynamic> j) => ShipState(
    nodeId: j['id'] as String,
    position: Vec3(
      (j['px'] as num).toDouble(),
      (j['py'] as num).toDouble(),
      (j['pz'] as num).toDouble(),
    ),
    velocity: Vec3(
      (j['vx'] as num).toDouble(),
      (j['vy'] as num).toDouble(),
      (j['vz'] as num).toDouble(),
    ),
    yaw: (j['yaw'] as num).toDouble(),
    pitch: (j['pitch'] as num).toDouble(),
    roll: (j['roll'] as num? ?? 0).toDouble(),
    throttle: (j['thr'] as num? ?? 0).toDouble(),
    boosting: j['bst'] as bool? ?? false,
    missionTarget: j['mis'] as String?,
    distanceToTarget: (j['dst'] as num? ?? 0).toDouble(),
    score: j['scr'] as int? ?? 0,
    timestamp: j['ts'] as int,
  );

  factory ShipState.initial(String nodeId) => ShipState(
    nodeId: nodeId,
    position: Vec3.zero(),
    velocity: Vec3.zero(),
    timestamp: DateTime.now().millisecondsSinceEpoch,
  );
}

typedef Int64 = int;
