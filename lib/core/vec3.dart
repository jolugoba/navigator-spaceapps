import 'dart:math' as math;

class Vec3 {
  double x, y, z;

  Vec3(this.x, this.y, this.z);
  Vec3.zero() : x = 0, y = 0, z = 0;
  Vec3.forward() : x = 0, y = 0, z = -1;
  Vec3.up() : x = 0, y = 1, z = 0;

  Vec3 operator +(Vec3 o) => Vec3(x + o.x, y + o.y, z + o.z);
  Vec3 operator -(Vec3 o) => Vec3(x - o.x, y - o.y, z - o.z);
  Vec3 operator *(double s) => Vec3(x * s, y * s, z * s);

  double get lengthSquared => x * x + y * y + z * z;
  double get length => math.sqrt(lengthSquared);

  Vec3 normalized() {
    final l = length;
    if (l < 1e-10) return Vec3.zero();
    return Vec3(x / l, y / l, z / l);
  }

  double dot(Vec3 o) => x * o.x + y * o.y + z * o.z;

  Vec3 cross(Vec3 o) => Vec3(
    y * o.z - z * o.y,
    z * o.x - x * o.z,
    x * o.y - y * o.x,
  );

  Vec3 copyWith({double? x, double? y, double? z}) =>
      Vec3(x ?? this.x, y ?? this.y, z ?? this.z);

  Map<String, double> toJson() => {'x': x, 'y': y, 'z': z};
  factory Vec3.fromJson(Map<String, dynamic> j) =>
      Vec3((j['x'] as num).toDouble(), (j['y'] as num).toDouble(),
          (j['z'] as num).toDouble());

  @override
  String toString() => 'Vec3($x, $y, $z)';
}

// Rotate a direction vector by euler angles (yaw then pitch)
Vec3 applyEuler(Vec3 dir, double yaw, double pitch) {
  // Yaw around Y
  final cy = math.cos(yaw), sy = math.sin(yaw);
  double x1 = dir.x * cy + dir.z * sy;
  double z1 = -dir.x * sy + dir.z * cy;

  // Pitch around X
  final cp = math.cos(pitch), sp = math.sin(pitch);
  double y2 = dir.y * cp - z1 * sp;
  double z2 = dir.y * sp + z1 * cp;

  return Vec3(x1, y2, z2);
}
