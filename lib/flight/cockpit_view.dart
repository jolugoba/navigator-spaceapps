import 'dart:async';
import 'dart:math' as math;
import 'dart:ui' as ui;

import 'package:camera/camera.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:geolocator/geolocator.dart';
import 'package:permission_handler/permission_handler.dart';

import '../core/components.dart';
import '../core/space_world.dart';
import '../core/vec3.dart';
import '../models/mission_design.dart';
import '../models/ship_state.dart';
import '../models/player_info.dart';
import '../models/planet_info.dart';
import '../nasa/horizons_service.dart';
import '../network/ship_sync_service.dart';
import '../screens/science_ops_screen.dart';
import '../ui/hud_overlay.dart';
import '../ui/chat_overlay.dart';
import '../ui/mission_result_screen.dart';
import 'gyro_service.dart';
import 'flight_controller.dart';
import 'physics_engine.dart';

// ─────────────────────────────────────────────────────────────────────────────
// Stars
// ─────────────────────────────────────────────────────────────────────────────

class _Star {
  final Vec3 dir;
  final double size;
  final double brightness;
  final int colorBias; // 0=white 1=blue-white 2=yellow-white
  const _Star(this.dir, this.size, this.brightness, this.colorBias);
}

List<_Star> _generateStars(int count) {
  final rng = math.Random(42);
  return List.generate(count, (_) {
    final theta = rng.nextDouble() * math.pi * 2;
    final phi = math.acos(1 - 2 * rng.nextDouble());
    return _Star(
      Vec3(math.sin(phi) * math.cos(theta), math.cos(phi),
          math.sin(phi) * math.sin(theta)),
      0.4 + rng.nextDouble() * 2.2,
      0.2 + rng.nextDouble() * 0.8,
      rng.nextInt(3),
    );
  });
}

// ─────────────────────────────────────────────────────────────────────────────
// Nebula definitions
// ─────────────────────────────────────────────────────────────────────────────

class _NebulaDef {
  final double dx, dy, dz;
  final Color color;
  final double spread;
  const _NebulaDef(this.dx, this.dy, this.dz, this.color, this.spread);
}

const _nebulae = [
  _NebulaDef( 0.35,  0.60,  0.72, Color(0xFF5B0EA6), 0.70),
  _NebulaDef(-0.68,  0.18,  0.71, Color(0xFF0D7377), 0.60),
  _NebulaDef( 0.52, -0.38,  0.76, Color(0xFF8B2500), 0.55),
  _NebulaDef(-0.38, -0.58,  0.72, Color(0xFF6B0052), 0.65),
  _NebulaDef( 0.82,  0.28, -0.50, Color(0xFF00004F), 0.50),
];

// ─────────────────────────────────────────────────────────────────────────────
// Exhaust particle system
// ─────────────────────────────────────────────────────────────────────────────

class _Particle {
  Offset pos;
  Offset vel;
  double life; // 1.0=fresh → 0.0=dead
  final double size;
  final bool boost;

  _Particle({
    required this.pos,
    required this.vel,
    required this.size,
    this.boost = false,
  }) : life = 1.0;
}

class _ParticleSystem {
  final _particles = <_Particle>[];
  final _rng = math.Random();

  // Call every physics frame when thrusting
  void emit(Offset leftEngine, Offset rightEngine,
      {required bool boosting, required double thrust}) {
    final count = boosting ? 6 : (thrust * 4).round().clamp(0, 4);
    for (var i = 0; i < count; i++) {
      for (final origin in [leftEngine, rightEngine]) {
        _particles.add(_Particle(
          pos: origin + Offset(
            (_rng.nextDouble() - 0.5) * 10,
            (_rng.nextDouble() - 0.5) * 6,
          ),
          vel: Offset(
            (_rng.nextDouble() - 0.5) * 30,
            60 + _rng.nextDouble() * (boosting ? 180 : 100),
          ),
          size: boosting
              ? 4 + _rng.nextDouble() * 7
              : 2.5 + _rng.nextDouble() * 4,
          boost: boosting,
        ));
      }
    }
  }

  void tick(double dt) {
    for (final p in _particles) {
      p.pos += p.vel * dt;
      p.vel = Offset(p.vel.dx * 0.92, p.vel.dy * 1.05);
      p.life -= dt * (p.boost ? 2.0 : 2.8);
    }
    _particles.removeWhere((p) => p.life <= 0);
    if (_particles.length > 300) {
      _particles.removeRange(0, _particles.length - 300);
    }
  }

  List<_Particle> get all => _particles;
}

// ─────────────────────────────────────────────────────────────────────────────
// Shooting star
// ─────────────────────────────────────────────────────────────────────────────

class _ShootingStar {
  final Offset from;
  final Offset to;
  double progress; // 0 → 1
  final double speed;
  _ShootingStar({required this.from, required this.to, required this.speed})
      : progress = 0.0;
  double get life => (1.0 - progress).clamp(0.0, 1.0);
}

// ─────────────────────────────────────────────────────────────────────────────
// Space dust (depth-parallax particles for movement feel)
// ─────────────────────────────────────────────────────────────────────────────

class _DustParticle {
  Offset pos;
  double life; // 1 → 0
  final double size;
  _DustParticle({required this.pos, required this.size}) : life = 1.0;
}

class _DustSystem {
  final _dust = <_DustParticle>[];
  final _rng = math.Random(99);

  void tick(double dt, double speed, double thrust, Size screen) {
    if (screen.isEmpty) return;
    final cx = screen.width / 2, cy = screen.height / 2;
    final intensity = ((speed / 80.0) + thrust * 0.5).clamp(0.0, 1.0);
    final count = (intensity * 5).round();
    for (var i = 0; i < count; i++) {
      final a = _rng.nextDouble() * math.pi * 2;
      final r = 10 + _rng.nextDouble() * (screen.shortestSide * 0.35);
      _dust.add(_DustParticle(
        pos: Offset(cx + math.cos(a) * r, cy + math.sin(a) * r),
        size: 0.8 + _rng.nextDouble() * 1.6,
      ));
    }
    final expand = 1.0 + dt * intensity * 3.5;
    for (final d in _dust) {
      final dx = d.pos.dx - cx, dy = d.pos.dy - cy;
      d.pos = Offset(cx + dx * expand, cy + dy * expand);
      d.life -= dt * (1.8 + intensity * 2.0);
    }
    _dust.removeWhere((d) => d.life <= 0 ||
        d.pos.dx < -60 || d.pos.dx > screen.width + 60 ||
        d.pos.dy < -60 || d.pos.dy > screen.height + 60);
    if (_dust.length > 200) _dust.removeRange(0, _dust.length - 200);
  }

  List<_DustParticle> get all => _dust;
}

// ─────────────────────────────────────────────────────────────────────────────
// Explosion system
// ─────────────────────────────────────────────────────────────────────────────

class _ExplosionParticle {
  Offset offset; // screen-space offset from explosion center
  Offset vel;
  double life;
  final Color color;
  final double size;
  _ExplosionParticle(
      {required this.offset,
      required this.vel,
      required this.life,
      required this.color,
      required this.size});
}

class _Explosion {
  final Vec3 worldPos;
  final List<_ExplosionParticle> particles;
  final bool big;
  double age; // grows over time, used for shockwave ring
  bool get done => age > 2.5 && particles.every((p) => p.life <= 0);
  _Explosion({required this.worldPos, required this.particles, required this.big})
      : age = 0;
}

List<_Explosion> _buildExplosion(Vec3 worldPos, {required bool big}) {
  final rng = math.Random();
  final count = big ? 70 : 35;
  const colors = [
    Color(0xFFFF8800), Color(0xFFFFDD00), Color(0xFFFF4400),
    Color(0xFFFFFFAA), Color(0xFFFF6600),
  ];
  final particles = List.generate(count, (_) {
    final angle = rng.nextDouble() * math.pi * 2;
    final speed = big
        ? 90.0 + rng.nextDouble() * 320
        : 50.0 + rng.nextDouble() * 180;
    return _ExplosionParticle(
      offset: Offset.zero,
      vel: Offset(math.cos(angle) * speed, math.sin(angle) * speed),
      life: 0.6 + rng.nextDouble() * 0.4,
      color: colors[rng.nextInt(colors.length)],
      size: big ? 3.0 + rng.nextDouble() * 8 : 2.0 + rng.nextDouble() * 5,
    );
  });
  return [_Explosion(worldPos: worldPos, particles: particles, big: big)];
}

// ─────────────────────────────────────────────────────────────────────────────
// Planet texture cache
//
// Place textures in assets/textures/<planetId>.jpg (or .png).
// Download free 2K textures from:
//   https://www.solarsystemscope.com/textures/
// File names should match kPlanets IDs:
//   mercury.jpg, venus.jpg, mars.jpg, jupiter.jpg,
//   saturn.jpg, uranus.jpg, neptune.jpg
//
// If a file is missing the painter falls back to procedural rendering.
// ─────────────────────────────────────────────────────────────────────────────

class _TextureCache {
  final _cache = <String, ui.Image?>{};
  bool _started = false;

  Future<void> loadAll(Iterable<String> planetIds,
      VoidCallback onLoaded) async {
    if (_started) return;
    _started = true;
    for (final pid in planetIds) {
      try {
        final data = await rootBundle.load('assets/textures/$pid.jpg');
        final codec = await ui.instantiateImageCodec(
          data.buffer.asUint8List(),
          targetWidth: 512,
          targetHeight: 256,
        );
        final frame = await codec.getNextFrame();
        _cache[pid] = frame.image;
      } catch (_) {
        try {
          final data = await rootBundle.load('assets/textures/$pid.png');
          final codec = await ui.instantiateImageCodec(
            data.buffer.asUint8List(),
            targetWidth: 512,
            targetHeight: 256,
          );
          final frame = await codec.getNextFrame();
          _cache[pid] = frame.image;
        } catch (_) {
          _cache[pid] = null; // not available — use procedural
        }
      }
      onLoaded();
    }
  }

  ui.Image? operator [](String pid) => _cache[pid];
  bool isReady(String pid) => _cache.containsKey(pid);
}

// ─────────────────────────────────────────────────────────────────────────────
// Universe CustomPainter
// ─────────────────────────────────────────────────────────────────────────────

class _UniversePainter extends CustomPainter {
  final double shipYaw, shipPitch;
  final Vec3 shipPos;
  final double speed;
  final bool boosting;
  final Vec3? planetPos;
  final Color planetColor;
  final String? planetId;
  final List<_Star> stars;
  final List<Vec3> remotePosns;
  final List<_Particle> particles;
  final List<_ShootingStar> shootingStars;
  final List<_DustParticle> dust;
  final List<_Explosion> explosions;
  final ui.Image? planetTexture;
  final double fuelFraction;
  final int lives;
  final bool invincible;
  final double lowFuelFlash; // 0→1 oscillating when fuel low
  final double proximityDist; // distance to nearest remote ship

  const _UniversePainter({
    required this.shipYaw,
    required this.shipPitch,
    required this.shipPos,
    required this.speed,
    required this.boosting,
    required this.stars,
    required this.remotePosns,
    required this.particles,
    required this.shootingStars,
    required this.dust,
    required this.explosions,
    required this.fuelFraction,
    required this.lives,
    required this.invincible,
    required this.lowFuelFlash,
    required this.proximityDist,
    this.planetPos,
    this.planetColor = const Color(0xFF00E5FF),
    this.planetId,
    this.planetTexture,
  });

  Vec3 _fwd() {
    final cp = math.cos(shipPitch), sp = math.sin(shipPitch);
    final cy = math.cos(shipYaw), sy = math.sin(shipYaw);
    return Vec3(-sy * cp, sp, -cy * cp);
  }

  Vec3 _rgt() {
    final cy = math.cos(shipYaw), sy = math.sin(shipYaw);
    return Vec3(cy, 0.0, -sy);
  }

  Offset? _project(Vec3 normDir, double cx, double cy, double focal,
      Vec3 fwd, Vec3 rgt, Vec3 up) {
    final lz = normDir.dot(fwd);
    if (lz <= 0.02) return null;
    return Offset(
      normDir.dot(rgt) / lz * focal + cx,
      -normDir.dot(up) / lz * focal + cy,
    );
  }

  @override
  void paint(Canvas canvas, Size size) {
    final cx = size.width / 2;
    final cy = size.height / 2;
    final focal = size.width * 0.95;
    final fwd = _fwd();
    final rgt = _rgt();
    final up = rgt.cross(fwd).normalized();

    _paintNebulae(canvas, size, cx, cy, focal, fwd, rgt, up);
    _paintSun(canvas, cx, cy, focal, fwd, rgt, up);
    _paintStars(canvas, size, cx, cy, focal, fwd, rgt, up);
    _paintShootingStars(canvas);
    if (boosting || speed > 25) _paintBoostStreaks(canvas, cx, cy, speed);
    _paintDust(canvas);
    if (planetPos != null) {
      _paintPlanet(canvas, size, cx, cy, focal, fwd, rgt, up);
    }
    for (final pos in remotePosns) {
      _paintRemoteShip(canvas, pos, cx, cy, focal, fwd, rgt, up);
    }
    _paintExplosions(canvas, cx, cy, focal, fwd, rgt, up);
    _paintCockpitFrame(canvas, size, cx, cy);
    _paintExhaustParticles(canvas, size);
    if (fuelFraction < 0.15) _paintLowFuelWarning(canvas, size);
    if (invincible) _paintHitFlash(canvas, size);
  }

  // ── Nebulae ─────────────────────────────────────────────────────────────────

  void _paintNebulae(Canvas canvas, Size size, double cx, double cy,
      double focal, Vec3 fwd, Vec3 rgt, Vec3 up) {
    for (final n in _nebulae) {
      final dir = Vec3(n.dx, n.dy, n.dz).normalized();
      final lz = dir.dot(fwd);
      if (lz <= 0.1) continue;
      final lx = dir.dot(rgt);
      final ly = dir.dot(up);
      final sx = lx / lz * focal + cx;
      final sy = -ly / lz * focal + cy;
      final blobR = size.shortestSide * n.spread;
      for (var i = 3; i >= 1; i--) {
        canvas.drawCircle(
          Offset(sx, sy),
          blobR * i * 0.55,
          Paint()
            ..color = n.color.withValues(alpha: 0.025 * i)
            ..maskFilter =
                MaskFilter.blur(BlurStyle.normal, blobR * i * 0.3),
        );
      }
    }
  }

  // ── Stars ────────────────────────────────────────────────────────────────────

  void _paintStars(Canvas canvas, Size size, double cx, double cy,
      double focal, Vec3 fwd, Vec3 rgt, Vec3 up) {
    for (final s in stars) {
      final scr = _project(s.dir, cx, cy, focal, fwd, rgt, up);
      if (scr == null) continue;
      if (scr.dx < -4 ||
          scr.dx > size.width + 4 ||
          scr.dy < -4 ||
          scr.dy > size.height + 4) { continue; }
      final a = s.brightness;
      Color c;
      switch (s.colorBias) {
        case 1:  c = Color.fromARGB((a * 255).round(), 180, 205, 255); break;
        case 2:  c = Color.fromARGB((a * 255).round(), 255, 245, 180); break;
        default: c = Color.fromARGB((a * 255).round(), 230, 238, 255);
      }
      if (boosting) {
        // Hyperspace elongation: streak each star outward from screen center
        final fromC = scr - Offset(cx, cy);
        final dist = fromC.distance;
        if (dist > 4) {
          final norm = fromC / dist;
          final len = (dist * 0.10 + 6).clamp(3.0, 55.0);
          canvas.drawLine(
              scr, scr + norm * len,
              Paint()
                ..color = c.withValues(alpha: a * 0.75)
                ..strokeWidth = s.size * 0.45 + 0.3
                ..strokeCap = StrokeCap.round);
        }
      }
      canvas.drawCircle(scr, s.size, Paint()..color = c);
      if (s.size > 1.8 && a > 0.65) {
        final sp = Paint()
          ..color = c.withValues(alpha: a * 0.45)
          ..strokeWidth = 0.6;
        canvas.drawLine(scr - Offset(s.size * 2.2, 0),
            scr + Offset(s.size * 2.2, 0), sp);
        canvas.drawLine(scr - Offset(0, s.size * 2.2),
            scr + Offset(0, s.size * 2.2), sp);
      }
    }
  }

  // ── Boost streaks ────────────────────────────────────────────────────────────

  void _paintBoostStreaks(Canvas canvas, double cx, double cy, double spd) {
    final intensity = boosting ? 0.65 : (spd / 250).clamp(0.0, 0.5);
    final rng = math.Random(17);
    for (var i = 0; i < 32; i++) {
      final angle = rng.nextDouble() * math.pi * 2;
      final base = 40 + rng.nextDouble() * 160;
      final len = (18 + rng.nextDouble() * 100) * intensity;
      canvas.drawLine(
        Offset(cx + math.cos(angle) * base, cy + math.sin(angle) * base),
        Offset(cx + math.cos(angle) * (base + len),
            cy + math.sin(angle) * (base + len)),
        Paint()
          ..color = Colors.cyanAccent.withValues(alpha: intensity * 0.55)
          ..strokeWidth = 0.9,
      );
    }
  }

  // ── Sun ───────────────────────────────────────────────────────────────────────

  void _paintSun(Canvas canvas, double cx, double cy, double focal,
      Vec3 fwd, Vec3 rgt, Vec3 up) {
    final raw = Vec3(0.55, 0.25, 0.80);
    final dir = raw * (1.0 / raw.length);
    final scr = _project(dir, cx, cy, focal, fwd, rgt, up);
    if (scr == null) return;

    // Outer corona
    canvas.drawCircle(scr, 140,
        Paint()
          ..color = const Color(0xFFFFDD44).withValues(alpha: 0.03)
          ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 70));
    canvas.drawCircle(scr, 70,
        Paint()
          ..color = const Color(0xFFFFEE88).withValues(alpha: 0.08)
          ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 35));
    canvas.drawCircle(scr, 28,
        Paint()
          ..color = const Color(0xFFFFFFAA).withValues(alpha: 0.50)
          ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 14));
    // Bright core
    canvas.drawCircle(scr, 7, Paint()..color = const Color(0xFFFFFFEE));
    canvas.drawCircle(scr, 4, Paint()..color = Colors.white);

    // 4-point sparkle
    for (final a in [0.0, math.pi / 4]) {
      final alpha = a == 0 ? 0.65 : 0.30;
      final sp = Paint()
        ..color = Colors.white.withValues(alpha: alpha)
        ..strokeWidth = 0.9;
      final len = a == 0 ? 24.0 : 16.0;
      canvas.drawLine(scr - Offset(math.cos(a) * len, math.sin(a) * len),
          scr + Offset(math.cos(a) * len, math.sin(a) * len), sp);
      canvas.drawLine(
          scr - Offset(-math.sin(a) * len, math.cos(a) * len),
          scr + Offset(-math.sin(a) * len, math.cos(a) * len), sp);
    }

    // Lens flares — along line from sun toward screen center
    final toCenter = Offset(cx, cy) - scr;
    final totalDist = toCenter.distance;
    if (totalDist < 1) return;
    final lensDir = toCenter / totalDist;
    const flares = [
      (0.28, 32.0, 0.10, Color(0xFF88BBFF)),
      (0.45, 14.0, 0.14, Color(0xFFFFCC88)),
      (0.60, 48.0, 0.06, Color(0xFF6688FF)),
      (0.78, 10.0, 0.18, Color(0xFFFFEEAA)),
      (1.05, 22.0, 0.08, Color(0xFFAADDFF)),
    ];
    for (final f in flares) {
      final fp = scr + lensDir * (totalDist * f.$1);
      canvas.drawCircle(
          fp, f.$2,
          Paint()
            ..color = f.$4.withValues(alpha: f.$3)
            ..style = PaintingStyle.stroke
            ..strokeWidth = 1.8);
    }
  }

  // ── Shooting stars ───────────────────────────────────────────────────────────

  void _paintShootingStars(Canvas canvas) {
    for (final ss in shootingStars) {
      if (ss.progress <= 0) continue;
      final head = Offset.lerp(ss.from, ss.to, ss.progress)!;
      final tailT = (ss.progress - 0.25).clamp(0.0, 1.0);
      final tail = Offset.lerp(ss.from, ss.to, tailT)!;
      final a = ss.life;
      canvas.drawLine(
          tail, head,
          Paint()
            ..color = Colors.white.withValues(alpha: a * 0.85)
            ..strokeWidth = 1.6
            ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 2));
      canvas.drawCircle(head, 2.2,
          Paint()..color = Colors.white.withValues(alpha: a * 0.95));
    }
  }

  // ── Space dust (movement depth) ──────────────────────────────────────────────

  void _paintDust(Canvas canvas) {
    for (final d in dust) {
      final a = (d.life * 0.55).clamp(0.0, 0.55);
      canvas.drawCircle(d.pos, d.size,
          Paint()..color = Colors.white.withValues(alpha: a));
    }
  }

  // ── Exhaust particles ────────────────────────────────────────────────────────

  void _paintExhaustParticles(Canvas canvas, Size size) {
    // Sputter effect: skip some particles when fuel < 15%
    final lowFuel = fuelFraction < 0.15;
    final sputterRng = math.Random(42);
    for (final p in particles) {
      // Low fuel: engine sputters — randomly skip ~40% of particles
      if (lowFuel && sputterRng.nextDouble() < 0.40) continue;
      final a = p.life.clamp(0.0, 1.0);
      // Low fuel → orange/red exhaust instead of cyan
      final Color startCol, endCol;
      if (fuelFraction < 0.05) {
        startCol = Colors.red; endCol = Colors.deepOrange;
      } else if (fuelFraction < 0.15) {
        startCol = Colors.orange; endCol = Colors.red;
      } else {
        startCol = p.boost ? Colors.white : Colors.cyanAccent;
        endCol = p.boost ? Colors.cyanAccent : const Color(0xFF0033AA);
      }
      final t = 1.0 - a;
      final col = Color.lerp(startCol, endCol, t)!;
      // Glow
      canvas.drawCircle(
        p.pos,
        p.size * a * 2.5,
        Paint()
          ..color = col.withValues(alpha: a * 0.3)
          ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 6),
      );
      // Core
      canvas.drawCircle(
        p.pos,
        p.size * a,
        Paint()..color = col.withValues(alpha: a * 0.85),
      );
    }
  }

  // ── Planet ───────────────────────────────────────────────────────────────────

  void _paintPlanet(Canvas canvas, Size size, double cx, double cy,
      double focal, Vec3 fwd, Vec3 rgt, Vec3 up) {
    final rel = planetPos! - shipPos;
    final dist = rel.length;
    if (dist < 1) return;
    final scr = _project(rel * (1.0 / dist), cx, cy, focal, fwd, rgt, up);
    if (scr == null) return;

    const visualR = 150.0;
    final r = (visualR / dist) * focal;
    final pid = planetId ?? '';

    // Outer atmosphere glow
    canvas.drawCircle(
      scr,
      r * 1.8,
      Paint()
        ..color = planetColor.withValues(alpha: 0.06)
        ..maskFilter =
            MaskFilter.blur(BlurStyle.normal, (r * 0.7).clamp(4, 90)),
    );
    canvas.drawCircle(
      scr,
      r * 1.25,
      Paint()
        ..color = planetColor.withValues(alpha: 0.18)
        ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 12),
    );

    // Saturn/Uranus rings — back half first
    final bool hasRings = pid == 'saturn' || pid == 'uranus';
    if (hasRings) _paintRings(canvas, scr, r, pid == 'uranus', false);

    // Planet body (texture or procedural)
    _paintPlanetBody(canvas, scr, r, pid);

    // Saturn/Uranus rings — front half
    if (hasRings) _paintRings(canvas, scr, r, pid == 'uranus', true);

    // Distance label
    if (r < 130 && dist < 990) {
      final tp = TextPainter(
        text: TextSpan(
          text: '${dist.round()} u',
          style: TextStyle(
              color: planetColor.withValues(alpha: 0.75),
              fontSize: 10,
              letterSpacing: 1.2),
        ),
        textDirection: TextDirection.ltr,
      )..layout();
      tp.paint(
          canvas, Offset(scr.dx - tp.width / 2, scr.dy + r + 8));
    }
  }

  void _paintPlanetBody(Canvas canvas, Offset pos, double r, String pid) {
    canvas.save();
    canvas.clipPath(Path()..addOval(Rect.fromCircle(center: pos, radius: r)));

    if (planetTexture != null) {
      // ── Texture path ──────────────────────────────────────────────────
      final src = Rect.fromLTWH(0, 0, planetTexture!.width.toDouble(),
          planetTexture!.height.toDouble());
      canvas.drawImageRect(
          planetTexture!, src, Rect.fromCircle(center: pos, radius: r),
          Paint()..filterQuality = FilterQuality.medium);
    } else {
      // ── Procedural path ───────────────────────────────────────────────
      final Color hi, mid, dark;
      switch (pid) {
        case 'mercury': hi = const Color(0xFFB8B8B8); mid = const Color(0xFF888888); dark = const Color(0xFF444444); break;
        case 'venus':   hi = const Color(0xFFFFE599); mid = const Color(0xFFE8A44B); dark = const Color(0xFF9E5B10); break;
        case 'mars':    hi = const Color(0xFFFF9988); mid = const Color(0xFFCC4422); dark = const Color(0xFF661100); break;
        case 'jupiter': hi = const Color(0xFFE8C89A); mid = const Color(0xFFC98C50); dark = const Color(0xFF7A3D12); break;
        case 'saturn':  hi = const Color(0xFFEEDD99); mid = const Color(0xFFCCAA55); dark = const Color(0xFF886622); break;
        case 'uranus':  hi = const Color(0xFFAAEEDD); mid = const Color(0xFF55CCBB); dark = const Color(0xFF1A7A6E); break;
        case 'neptune': hi = const Color(0xFF6688DD); mid = const Color(0xFF2244AA); dark = const Color(0xFF081050); break;
        default:
          hi = Color.lerp(Colors.white, planetColor, 0.4)!;
          mid = planetColor;
          dark = _darken(planetColor, 0.38);
      }
      canvas.drawRect(
        Rect.fromCircle(center: pos, radius: r),
        Paint()
          ..shader = RadialGradient(
            center: const Alignment(-0.35, -0.45),
            colors: [hi, mid, dark],
            stops: const [0.0, 0.55, 1.0],
          ).createShader(Rect.fromCircle(center: pos, radius: r)),
      );

      // Surface details
      if (r > 10) {
        switch (pid) {
          case 'jupiter': _paintJupiterBands(canvas, pos, r); break;
          case 'saturn':  _paintSaturnBands(canvas, pos, r);  break;
          case 'mars':    _paintMarsSurface(canvas, pos, r);  break;
          case 'mercury': _paintCraters(canvas, pos, r);      break;
          case 'venus':   _paintVenusClouds(canvas, pos, r);  break;
          case 'neptune': _paintNeptuneDarkSpot(canvas, pos, r); break;
          default: break;
        }
      }
    }

    // Lighting overlay — applies to both texture and procedural
    canvas.drawRect(
      Rect.fromCircle(center: pos, radius: r),
      Paint()
        ..shader = RadialGradient(
          center: const Alignment(-0.35, -0.45),
          colors: [
            Colors.white.withValues(alpha: 0.22),
            Colors.transparent,
            Colors.black.withValues(alpha: 0.50),
          ],
          stops: const [0.0, 0.45, 1.0],
        ).createShader(Rect.fromCircle(center: pos, radius: r)),
    );

    canvas.restore();
  }

  // ── Jupiter bands + Great Red Spot ──────────────────────────────────────────

  void _paintJupiterBands(Canvas canvas, Offset pos, double r) {
    const bands = [
      (0.00, 0.12, Color(0xFF8B5E2A)),
      (0.12, 0.25, Color(0xFFD4A86A)),
      (0.25, 0.40, Color(0xFF7A3D12)),
      (0.40, 0.55, Color(0xFFCC9955)),
      (0.55, 0.68, Color(0xFF6B3311)),
      (0.68, 0.80, Color(0xFFBB8844)),
      (0.80, 1.00, Color(0xFF8B5E2A)),
    ];
    for (final b in bands) {
      final y0 = pos.dy - r + r * 2 * b.$1;
      final y1 = pos.dy - r + r * 2 * b.$2;
      canvas.drawRect(
        Rect.fromLTRB(pos.dx - r, y0, pos.dx + r, y1),
        Paint()..color = b.$3.withValues(alpha: 0.62),
      );
    }
    if (r > 25) {
      final spot = Rect.fromCenter(
        center: Offset(pos.dx + r * 0.2, pos.dy + r * 0.15),
        width: r * 0.46, height: r * 0.22);
      canvas.drawOval(spot,
          Paint()..color = const Color(0xFFCC2200).withValues(alpha: 0.82));
      canvas.drawOval(spot,
          Paint()
            ..color = const Color(0xFFFF6644).withValues(alpha: 0.4)
            ..style = PaintingStyle.stroke
            ..strokeWidth = (r * 0.04).clamp(1, 4));
    }
  }

  void _paintSaturnBands(Canvas canvas, Offset pos, double r) {
    const bands = [
      (0.00, 0.20, Color(0xFFBB9944)),
      (0.20, 0.40, Color(0xFFDDCC77)),
      (0.40, 0.60, Color(0xFFC8A840)),
      (0.60, 0.80, Color(0xFFEEDD99)),
      (0.80, 1.00, Color(0xFFBB9944)),
    ];
    for (final b in bands) {
      final y0 = pos.dy - r + r * 2 * b.$1;
      final y1 = pos.dy - r + r * 2 * b.$2;
      canvas.drawRect(
        Rect.fromLTRB(pos.dx - r, y0, pos.dx + r, y1),
        Paint()..color = b.$3.withValues(alpha: 0.48),
      );
    }
  }

  void _paintRings(Canvas canvas, Offset pos, double r,
      bool isUranus, bool front) {
    final defs = isUranus
        ? [(1.50, 0.28, 0.35, const Color(0xFF99BBCC)),
           (1.70, 0.28, 0.22, const Color(0xFF77AABB)),
           (1.90, 0.28, 0.15, const Color(0xFF99CCDD))]
        : [(1.45, 0.28, 0.55, const Color(0xFFD4AA55)),
           (1.65, 0.28, 0.40, const Color(0xFFC89933)),
           (1.82, 0.28, 0.28, const Color(0xFFBB8822)),
           (2.05, 0.28, 0.18, const Color(0xFFAA7711)),
           (2.22, 0.28, 0.11, const Color(0xFF997700))];

    for (final d in defs) {
      final rw = r * d.$1, rh = rw * d.$2;
      final thick = (r * 0.045).clamp(1.5, 9.0);
      final rect = Rect.fromCenter(center: pos, width: rw * 2, height: rh * 2);
      final clip = front
          ? Rect.fromLTRB(pos.dx - rw * 1.1, pos.dy, pos.dx + rw * 1.1, 1e6)
          : Rect.fromLTRB(pos.dx - rw * 1.1, -1e6, pos.dx + rw * 1.1, pos.dy);
      canvas.save();
      canvas.clipRect(clip);
      canvas.drawOval(
        rect,
        Paint()
          ..color = d.$4.withValues(alpha: d.$3)
          ..style = PaintingStyle.stroke
          ..strokeWidth = thick,
      );
      canvas.restore();
    }
  }

  void _paintMarsSurface(Canvas canvas, Offset pos, double r) {
    canvas.drawOval(
      Rect.fromCenter(
          center: Offset(pos.dx - r * 0.2, pos.dy + r * 0.1),
          width: r * 0.70, height: r * 0.40),
      Paint()..color = const Color(0xFF441100).withValues(alpha: 0.45),
    );
    canvas.drawOval(
      Rect.fromCenter(
          center: Offset(pos.dx + r * 0.3, pos.dy - r * 0.2),
          width: r * 0.50, height: r * 0.30),
      Paint()..color = const Color(0xFF551500).withValues(alpha: 0.35),
    );
    // Polar cap
    canvas.drawOval(
      Rect.fromCenter(
          center: Offset(pos.dx, pos.dy - r * 0.78),
          width: r * 0.70, height: r * 0.22),
      Paint()..color = Colors.white.withValues(alpha: 0.78),
    );
  }

  void _paintCraters(Canvas canvas, Offset pos, double r) {
    const defs = [( 0.25, -0.35, 0.14), (-0.30, 0.20, 0.10),
                   ( 0.45,  0.40, 0.08), (-0.15, -0.50, 0.07),
                   ( 0.10,  0.55, 0.09)];
    for (final d in defs) {
      final cp = Offset(pos.dx + d.$1 * r, pos.dy + d.$2 * r);
      final cr = d.$3 * r;
      canvas.drawCircle(cp, cr,
          Paint()..color = Colors.black.withValues(alpha: 0.28));
      canvas.drawCircle(cp, cr,
          Paint()
            ..color = Colors.white.withValues(alpha: 0.15)
            ..style = PaintingStyle.stroke
            ..strokeWidth = (cr * 0.2).clamp(0.5, 2));
    }
  }

  void _paintVenusClouds(Canvas canvas, Offset pos, double r) {
    final path = Path()
      ..moveTo(pos.dx - r, pos.dy - r * 0.1)
      ..quadraticBezierTo(
          pos.dx, pos.dy - r * 0.4, pos.dx + r, pos.dy + r * 0.1);
    canvas.drawPath(
      path,
      Paint()
        ..color = Colors.white.withValues(alpha: 0.22)
        ..style = PaintingStyle.stroke
        ..strokeWidth = r * 0.20
        ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 7),
    );
  }

  void _paintNeptuneDarkSpot(Canvas canvas, Offset pos, double r) {
    if (r < 15) return;
    canvas.drawOval(
      Rect.fromCenter(
          center: Offset(pos.dx - r * 0.2, pos.dy - r * 0.1),
          width: r * 0.36, height: r * 0.22),
      Paint()..color = Colors.black.withValues(alpha: 0.35),
    );
  }

  // ── Remote ships (detailed fighter silhouette) ───────────────────────────────

  void _paintRemoteShip(Canvas canvas, Vec3 pos, double cx, double cy,
      double focal, Vec3 fwd, Vec3 rgt, Vec3 up) {
    final rel = pos - shipPos;
    final dist = rel.length;
    if (dist < 0.5) return;
    final norm = rel * (1.0 / dist);
    final scr = _project(norm, cx, cy, focal, fwd, rgt, up);
    if (scr == null) return;

    final sc = (60.0 / dist).clamp(0.3, 5.0);
    final sx = scr.dx, sy = scr.dy;
    const col = Color(0xFFFF6B35);

    // Engine glow
    canvas.drawCircle(scr, 12 * sc,
        Paint()
          ..color = col.withValues(alpha: 0.20)
          ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 8));

    // Fighter body
    final body = Path()
      ..moveTo(sx,           sy - 11 * sc)
      ..lineTo(sx + 7 * sc,  sy +  3 * sc)
      ..lineTo(sx + 3 * sc,  sy +  1 * sc)
      ..lineTo(sx + 2 * sc,  sy +  7 * sc)
      ..lineTo(sx,           sy +  4 * sc)
      ..lineTo(sx - 2 * sc,  sy +  7 * sc)
      ..lineTo(sx - 3 * sc,  sy +  1 * sc)
      ..lineTo(sx - 7 * sc,  sy +  3 * sc)
      ..close();
    canvas.drawPath(body, Paint()..color = col);
    canvas.drawPath(body,
        Paint()
          ..color = Colors.white.withValues(alpha: 0.28)
          ..style = PaintingStyle.stroke
          ..strokeWidth = 0.7);

    if (sc > 0.8) {
      canvas.drawOval(
        Rect.fromCenter(
            center: Offset(sx, sy - 4 * sc), width: 3 * sc, height: 4 * sc),
        Paint()..color = Colors.cyanAccent.withValues(alpha: 0.75),
      );
    }
    // Engine glows
    canvas.drawCircle(Offset(sx + 2 * sc, sy + 7 * sc), 2.5 * sc,
        Paint()..color = Colors.cyanAccent.withValues(alpha: 0.8));
    canvas.drawCircle(Offset(sx - 2 * sc, sy + 7 * sc), 2.5 * sc,
        Paint()..color = Colors.cyanAccent.withValues(alpha: 0.8));
  }

  // ── Explosions ───────────────────────────────────────────────────────────────

  void _paintExplosions(Canvas canvas, double cx, double cy, double focal,
      Vec3 fwd, Vec3 rgt, Vec3 up) {
    for (final e in explosions) {
      final rel = e.worldPos - shipPos;
      final dist = rel.length;
      if (dist < 0.5) continue;
      final scr = _project(rel * (1.0 / dist), cx, cy, focal, fwd, rgt, up);
      if (scr == null) continue;

      // Shockwave expanding ring
      final ringR = e.age * (e.big ? 140.0 : 70.0);
      if (ringR > 2) {
        canvas.drawCircle(
            scr, ringR,
            Paint()
              ..color = const Color(0xFFFF8800).withValues(
                  alpha: (0.6 - e.age * 0.28).clamp(0, 0.6))
              ..style = PaintingStyle.stroke
              ..strokeWidth = (e.big ? 6.0 : 3.5) * (1 - e.age * 0.4).clamp(0.1, 1));
      }

      // Flash core
      if (e.age < 0.25) {
        final flashR = (e.big ? 80.0 : 40.0) * (1 - e.age * 4).clamp(0, 1);
        canvas.drawCircle(
            scr, flashR,
            Paint()
              ..color = Colors.white.withValues(alpha: (0.9 - e.age * 4).clamp(0, 0.9))
              ..maskFilter = MaskFilter.blur(BlurStyle.normal, flashR * 0.5));
      }

      // Particles
      for (final p in e.particles) {
        final a = p.life.clamp(0.0, 1.0);
        final pos = scr + p.offset;
        canvas.drawCircle(
            pos, p.size * a,
            Paint()
              ..color = p.color.withValues(alpha: a * 0.9)
              ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 3));
        canvas.drawCircle(pos, p.size * a * 0.5,
            Paint()..color = Colors.white.withValues(alpha: a * 0.6));
      }
    }
  }

  // ── Low-fuel edge warning ────────────────────────────────────────────────────

  void _paintLowFuelWarning(Canvas canvas, Size size) {
    final a = (lowFuelFlash * 0.55).clamp(0.0, 0.55);
    final critical = fuelFraction < 0.05;
    final col = critical ? Colors.red : Colors.orange;
    // Red/orange vignette on screen edges
    canvas.drawRect(
        Rect.fromLTWH(0, 0, size.width, size.height),
        Paint()
          ..shader = RadialGradient(
            center: Alignment.center,
            radius: 0.85,
            colors: [Colors.transparent, col.withValues(alpha: a)],
          ).createShader(Rect.fromLTWH(0, 0, size.width, size.height)));
    // Warning text
    final label = critical ? 'COMBUSTIBLE AGOTADO' : 'COMBUSTIBLE CRÍTICO';
    final tp = TextPainter(
      text: TextSpan(
          text: label,
          style: TextStyle(
              color: col.withValues(alpha: (lowFuelFlash * 0.95).clamp(0, 0.95)),
              fontSize: 13,
              fontWeight: FontWeight.bold,
              letterSpacing: 2.5)),
      textDirection: TextDirection.ltr,
    )..layout();
    tp.paint(canvas,
        Offset((size.width - tp.width) / 2, size.height * 0.20));
  }

  // ── Hit flash (invincibility indicator) ─────────────────────────────────────

  void _paintHitFlash(Canvas canvas, Size size) {
    canvas.drawRect(
        Rect.fromLTWH(0, 0, size.width, size.height),
        Paint()..color = Colors.red.withValues(alpha: 0.12));
    // Red border
    canvas.drawRect(
        Rect.fromLTWH(0, 0, size.width, size.height),
        Paint()
          ..color = Colors.red.withValues(alpha: 0.45)
          ..style = PaintingStyle.stroke
          ..strokeWidth = 6);
  }

  // ── Cockpit frame ─────────────────────────────────────────────────────────────

  void _paintCockpitFrame(Canvas canvas, Size size, double cx, double cy) {
    final w = size.width, h = size.height;

    // Bottom instrument bar gradient
    canvas.drawRect(
      Rect.fromLTRB(0, h * 0.78, w, h),
      Paint()
        ..shader = LinearGradient(
          begin: Alignment.topCenter,
          end: Alignment.bottomCenter,
          colors: [
            Colors.transparent,
            Colors.black.withValues(alpha: 0.60),
            Colors.black.withValues(alpha: 0.88),
          ],
        ).createShader(Rect.fromLTRB(0, h * 0.78, w, h)),
    );

    final line = Paint()
      ..color = Colors.cyanAccent.withValues(alpha: 0.20)
      ..strokeWidth = 1;
    canvas.drawLine(Offset(0, h * 0.78), Offset(w, h * 0.78), line);

    // Ship nose silhouette — visible at bottom centre
    _paintShipNose(canvas, w, h);

    // Corner brackets
    _bracket(canvas, Offset(16, 16), 1, 1);
    _bracket(canvas, Offset(w - 16, 16), -1, 1);
    _bracket(canvas, Offset(16, h - 16), 1, -1);
    _bracket(canvas, Offset(w - 16, h - 16), -1, -1);

    // Lives indicator (top-right area)
    _paintLives(canvas, w);

    // Proximity / rendezvous alert
    if (proximityDist < 50.0) _paintProximityAlert(canvas, w, h);

    // Instrument panel
    _paintSpeedGauge(canvas, w, h);
    _paintFuelGauge(canvas, w, h);
    _paintGForceBar(canvas, w, h);

    // Center reticle (slightly above center)
    final mc = Offset(w / 2, h * 0.36);
    final cp = Paint()
      ..color = Colors.cyanAccent.withValues(alpha: 0.38)
      ..strokeWidth = 1.2;
    const len = 12.0, gap = 6.0;
    canvas.drawLine(mc - Offset(len + gap, 0), mc - Offset(gap, 0), cp);
    canvas.drawLine(mc + Offset(gap, 0),       mc + Offset(len + gap, 0), cp);
    canvas.drawLine(mc - Offset(0, len + gap), mc - Offset(0, gap), cp);
    canvas.drawLine(mc + Offset(0, gap),       mc + Offset(0, len + gap), cp);
    canvas.drawCircle(mc, 4,
        Paint()
          ..color = Colors.cyanAccent.withValues(alpha: 0.40)
          ..style = PaintingStyle.stroke
          ..strokeWidth = 1);
  }

  void _paintShipNose(Canvas canvas, double w, double h) {
    // Dark fuselage body at bottom edge
    final bodyPath = Path()
      ..moveTo(w * 0.28, h)
      ..lineTo(w * 0.35, h * 0.84)
      ..quadraticBezierTo(w * 0.50, h * 0.79, w * 0.65, h * 0.84)
      ..lineTo(w * 0.72, h)
      ..close();
    canvas.drawPath(
        bodyPath,
        Paint()
          ..color = const Color(0xFF0A1628)
          ..style = PaintingStyle.fill);
    canvas.drawPath(
        bodyPath,
        Paint()
          ..color = Colors.cyanAccent.withValues(alpha: 0.22)
          ..style = PaintingStyle.stroke
          ..strokeWidth = 1.2);

    // Wing strakes — left and right
    for (final side in [-1.0, 1.0]) {
      final wx = w / 2 + side * w * 0.20;
      final tip = Offset(w / 2 + side * w * 0.48, h);
      final wing = Path()
        ..moveTo(wx, h * 0.84)
        ..lineTo(tip.dx, tip.dy)
        ..lineTo(wx + side * w * 0.04, h)
        ..close();
      canvas.drawPath(
          wing,
          Paint()
            ..color = const Color(0xFF060E1E)
            ..style = PaintingStyle.fill);
      canvas.drawPath(
          wing,
          Paint()
            ..color = Colors.cyanAccent.withValues(alpha: 0.15)
            ..style = PaintingStyle.stroke
            ..strokeWidth = 1.0);
    }

    // Engine glow dots at rear
    for (final x in [w * 0.43, w * 0.57]) {
      canvas.drawCircle(
          Offset(x, h * 0.875),
          5,
          Paint()
            ..color = Colors.cyanAccent.withValues(alpha: 0.70)
            ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 5));
      canvas.drawCircle(Offset(x, h * 0.875), 2.5,
          Paint()..color = Colors.white.withValues(alpha: 0.90));
    }

    // Cockpit glass highlight arc
    canvas.drawArc(
      Rect.fromCenter(
          center: Offset(w / 2, h * 0.66), width: w * 0.30, height: h * 0.22),
      math.pi * 1.1,
      math.pi * 0.8,
      false,
      Paint()
        ..color = Colors.cyanAccent.withValues(alpha: 0.16)
        ..strokeWidth = 1.8
        ..style = PaintingStyle.stroke,
    );
  }

  void _bracket(Canvas canvas, Offset o, double sx, double sy) {
    final p = Paint()
      ..color = Colors.cyanAccent.withValues(alpha: 0.35)
      ..strokeWidth = 1.5;
    canvas.drawLine(o, o + Offset(18 * sx, 0), p);
    canvas.drawLine(o, o + Offset(0, 18 * sy), p);
  }

  // ── Lives (top-right) ────────────────────────────────────────────────────────

  void _paintLives(Canvas canvas, double w) {
    const maxLives = 3;
    const r = 6.0, gap = 16.0;
    final startX = w - 28.0;
    for (var i = 0; i < maxLives; i++) {
      final cx = startX - i * gap;
      final alive = i < lives;
      canvas.drawCircle(
          Offset(cx, 36),
          r,
          Paint()
            ..color = alive
                ? const Color(0xFF44FF88).withValues(alpha: 0.90)
                : Colors.white.withValues(alpha: 0.20));
      if (alive) {
        canvas.drawCircle(
            Offset(cx, 36), r,
            Paint()
              ..color = const Color(0xFF44FF88).withValues(alpha: 0.35)
              ..style = PaintingStyle.stroke
              ..strokeWidth = 1
              ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 4));
      }
    }
    final lp = TextPainter(
      text: const TextSpan(
          text: 'VIDAS',
          style: TextStyle(color: Color(0x6644FF88), fontSize: 7)),
      textDirection: TextDirection.ltr,
    )..layout();
    lp.paint(canvas, Offset(startX - (maxLives - 1) * gap - lp.width / 2 + 4, 44));
  }

  // ── Proximity / rendezvous alert ──────────────────────────────────────────────

  void _paintProximityAlert(Canvas canvas, double w, double h) {
    final isClose = proximityDist < 14.0;
    final col = isClose ? Colors.red : Colors.cyanAccent;
    final label = isClose ? '⚠ COLISIÓN INMINENTE' : '◈ NAVE DETECTADA — ${proximityDist.round()} u';
    final tp = TextPainter(
      text: TextSpan(
          text: label,
          style: TextStyle(
              color: col,
              fontSize: 11,
              fontWeight: FontWeight.bold,
              letterSpacing: 1.8)),
      textDirection: TextDirection.ltr,
    )..layout();
    final rx = (w - tp.width) / 2, ry = h * 0.13;
    canvas.drawRRect(
        RRect.fromRectAndRadius(
            Rect.fromLTWH(rx - 10, ry - 5, tp.width + 20, tp.height + 10),
            const Radius.circular(4)),
        Paint()..color = col.withValues(alpha: 0.12));
    canvas.drawRRect(
        RRect.fromRectAndRadius(
            Rect.fromLTWH(rx - 10, ry - 5, tp.width + 20, tp.height + 10),
            const Radius.circular(4)),
        Paint()
          ..color = col.withValues(alpha: 0.50)
          ..style = PaintingStyle.stroke
          ..strokeWidth = 1);
    tp.paint(canvas, Offset(rx, ry));
  }

  // ── Speed gauge (arc, bottom-left) ───────────────────────────────────────────

  void _paintSpeedGauge(Canvas canvas, double w, double h) {
    final center = Offset(w * 0.14, h * 0.905);
    const r = 26.0;
    const startA = math.pi * 0.75, sweepFull = math.pi * 1.5;
    final frac = (speed / 300.0).clamp(0.0, 1.0);
    final color = Color.lerp(Colors.cyanAccent, Colors.orangeAccent, frac)!;

    // Track
    canvas.drawArc(Rect.fromCircle(center: center, radius: r), startA, sweepFull,
        false, Paint()..color = Colors.white.withValues(alpha: 0.10)..strokeWidth = 3..style = PaintingStyle.stroke);
    // Fill
    if (frac > 0) {
      canvas.drawArc(Rect.fromCircle(center: center, radius: r), startA, sweepFull * frac,
          false, Paint()..color = color.withValues(alpha: 0.85)..strokeWidth = 3..style = PaintingStyle.stroke);
    }
    // Value text
    final tp = TextPainter(
      text: TextSpan(
        text: '${speed.round()}',
        style: TextStyle(color: color, fontSize: 10, fontWeight: FontWeight.bold),
      ),
      textDirection: TextDirection.ltr,
    )..layout();
    tp.paint(canvas, center - Offset(tp.width / 2, tp.height / 2));
    // Label
    final lp = TextPainter(
      text: const TextSpan(text: 'SPD', style: TextStyle(color: Color(0x8800E5FF), fontSize: 7)),
      textDirection: TextDirection.ltr,
    )..layout();
    lp.paint(canvas, Offset(center.dx - lp.width / 2, center.dy + 14));
  }

  // ── Fuel gauge (arc, bottom-right) ───────────────────────────────────────────

  void _paintFuelGauge(Canvas canvas, double w, double h) {
    final center = Offset(w * 0.86, h * 0.905);
    const r = 26.0;
    const startA = math.pi * 0.75, sweepFull = math.pi * 1.5;
    final color = fuelFraction > 0.3
        ? const Color(0xFF44FF88)
        : const Color(0xFFFF4444);

    canvas.drawArc(Rect.fromCircle(center: center, radius: r), startA, sweepFull,
        false, Paint()..color = Colors.white.withValues(alpha: 0.10)..strokeWidth = 3..style = PaintingStyle.stroke);
    if (fuelFraction > 0) {
      canvas.drawArc(Rect.fromCircle(center: center, radius: r), startA, sweepFull * fuelFraction,
          false, Paint()..color = color.withValues(alpha: 0.85)..strokeWidth = 3..style = PaintingStyle.stroke);
    }
    final pct = (fuelFraction * 100).round();
    final tp = TextPainter(
      text: TextSpan(text: '$pct%', style: TextStyle(color: color, fontSize: 10, fontWeight: FontWeight.bold)),
      textDirection: TextDirection.ltr,
    )..layout();
    tp.paint(canvas, center - Offset(tp.width / 2, tp.height / 2));
    final lp = TextPainter(
      text: const TextSpan(text: 'FUEL', style: TextStyle(color: Color(0x8844FF88), fontSize: 7)),
      textDirection: TextDirection.ltr,
    )..layout();
    lp.paint(canvas, Offset(center.dx - lp.width / 2, center.dy + 14));
  }

  // ── G-force / thrust bar (centre-bottom) ─────────────────────────────────────

  void _paintGForceBar(Canvas canvas, double w, double h) {
    final barW = w * 0.20, barH = 5.0;
    final left = (w - barW) / 2, top = h * 0.882;
    // Background
    canvas.drawRRect(
        RRect.fromRectAndRadius(Rect.fromLTWH(left, top, barW, barH), const Radius.circular(3)),
        Paint()..color = Colors.white.withValues(alpha: 0.10));
    // Thrust fill
    if (boosting) {
      canvas.drawRRect(
          RRect.fromRectAndRadius(Rect.fromLTWH(left, top, barW, barH), const Radius.circular(3)),
          Paint()
            ..shader = LinearGradient(colors: [Colors.cyanAccent, Colors.white])
                .createShader(Rect.fromLTWH(left, top, barW, barH)));
      // BOOST label
      final bp = TextPainter(
        text: const TextSpan(text: 'BOOST', style: TextStyle(color: Colors.cyanAccent, fontSize: 8, letterSpacing: 2, fontWeight: FontWeight.bold)),
        textDirection: TextDirection.ltr,
      )..layout();
      bp.paint(canvas, Offset((w - bp.width) / 2, top - 14));
    } else {
      final frac = (speed / 300.0).clamp(0.0, 1.0);
      if (frac > 0) {
        canvas.drawRRect(
            RRect.fromRectAndRadius(Rect.fromLTWH(left, top, barW * frac, barH), const Radius.circular(3)),
            Paint()..color = Colors.cyanAccent.withValues(alpha: 0.70));
      }
    }
  }

  // ── Helpers ──────────────────────────────────────────────────────────────────

  Color _darken(Color c, double f) => Color.fromRGBO(
        (c.r * 255 * f).round().clamp(0, 255),
        (c.g * 255 * f).round().clamp(0, 255),
        (c.b * 255 * f).round().clamp(0, 255),
        1.0,
      );

  @override
  bool shouldRepaint(covariant _UniversePainter old) => true;
}

// ─────────────────────────────────────────────────────────────────────────────
// CockpitView
// ─────────────────────────────────────────────────────────────────────────────

class CockpitView extends StatefulWidget {
  final SpaceWorld world;
  final EntityId localShipId;
  final PhysicsEngine physics;
  final GyroService gyro;
  final ShipSyncService? sync;
  final PlayerInfo localPlayer;
  final MissionDesign? missionDesign;
  final VoidCallback onExit;

  const CockpitView({
    super.key,
    required this.world,
    required this.localShipId,
    required this.physics,
    required this.gyro,
    required this.localPlayer,
    required this.onExit,
    this.sync,
    this.missionDesign,
  });

  @override
  State<CockpitView> createState() => _CockpitViewState();
}

class _CockpitViewState extends State<CockpitView> {
  // Camera
  CameraController? _camera;
  bool _camReady = false;

  // Visual
  final _stars = _generateStars(300);
  final _textureCache = _TextureCache();
  final _particles = _ParticleSystem();
  final _dust = _DustSystem();
  final _shootingStars = <_ShootingStar>[];
  final _ssRng = math.Random();
  Size _screenSize = Size.zero;

  // Explosions & collisions
  final _explosions = <_Explosion>[];
  final _expRng = math.Random(55);
  int _lives = 3;
  bool _invincible = false;
  double _invincibleTimer = 0;
  double _shakeX = 0, _shakeY = 0, _shakePower = 0;
  double _lowFuelFlash = 0;
  double _proximityDist = double.infinity;

  // Timers
  Timer? _tickTimer;
  Timer? _shootingStarTimer;

  // Inputs
  double _thrustMain = 0;
  double _steerYaw = 0;
  double _steerPitch = 0;
  double _strafeLateral = 0;
  bool _boosting = false;

  // HUD state
  ShipState _localState = ShipState.initial('local');
  double _fuelFraction = 1.0;
  final Map<String, ShipState> _remoteStates = {};

  // Mission
  bool _missionEnded = false;
  bool _showScienceOps = false;
  MissionResult? _missionResult;
  final Map<String, int> _remoteScores = {};

  // Chat
  bool _showChat = false;

  // Burnout system
  double _burnoutBar = 0.0;          // 0.0 – 1.0
  bool _burnoutActive = false;
  int _burnoutTicksLeft = 0;         // 10s × 60fps = 600 ticks
  int _nearMissCombo = 0;            // consecutive near-misses
  double _nearMissCooldown = 0.0;    // seconds since last near-miss
  static const _burnoutDurationTicks = 600;
  static const _nearMissWindow = 3.0; // seconds before combo resets

  // Planet
  Vec3? _planetWorldPos;
  Color? _planetColor;
  String? _planetName;
  String? _planetId;

  final HorizonsService _horizons = HorizonsService();

  @override
  void initState() {
    super.initState();
    _initCamera();
    widget.gyro.addListener(_onGyro);

    _tickTimer = Timer.periodic(
      const Duration(milliseconds: 16),
      (_) => _physicsStep(),
    );

    widget.sync?.onRemoteUpdate.listen((states) {
      if (mounted) setState(() => _remoteStates.addAll(states));
    });
    widget.sync?.onRemoteScore.listen((scores) {
      if (mounted) setState(() => _remoteScores.addAll(scores));
    });

    final mission = widget.world.get<MissionTarget>(widget.localShipId);
    if (mission != null) {
      _planetWorldPos = widget.physics.planetWorldPosition(mission.planetId);
      final planet =
          kPlanets.where((p) => p.id == mission.planetId).firstOrNull;
      if (planet != null) {
        _planetColor = _hexColor(planet.colorHex);
        _planetName = planet.name;
        _planetId = planet.id;
      }
    }

    // Load planet textures from assets/textures/<planetId>.jpg
    // If files don't exist the cache falls back silently to procedural rendering.
    _textureCache
        .loadAll(kPlanets.map((p) => p.id), () {
          if (mounted) setState(() {});
        })
        .ignore();

    // Shooting stars — spawn one every 5s with jitter
    _shootingStarTimer = Timer.periodic(
      const Duration(seconds: 5),
      (_) => _spawnShootingStar(),
    );
    Future.delayed(const Duration(milliseconds: 800), _spawnShootingStar);

    _initNasa();
  }

  void _spawnShootingStar() {
    if (!mounted || _screenSize.isEmpty) return;
    final w = _screenSize.width, h = _screenSize.height * 0.72;
    final startX = w * (0.05 + _ssRng.nextDouble() * 0.90);
    final startY = h * (0.04 + _ssRng.nextDouble() * 0.70);
    final angle = (_ssRng.nextDouble() - 0.3) * math.pi / 4 + math.pi / 6;
    final len = 80 + _ssRng.nextDouble() * 140;
    setState(() {
      _shootingStars.add(_ShootingStar(
        from: Offset(startX, startY),
        to: Offset(startX + math.cos(angle) * len,
            startY + math.sin(angle) * len),
        speed: 1.8 + _ssRng.nextDouble() * 2.5,
      ));
    });
  }

  // ── Collision & explosion ────────────────────────────────────────────────────

  void _onCollision(Vec3 remotePos) {
    if (_invincible || _missionEnded) return;
    _invincible = true;
    _invincibleTimer = 3.0;
    _shakePower = 20.0;
    _lives = (_lives - 1).clamp(0, 3);

    // Explosion at remote ship world position
    setState(() {
      _explosions.addAll(_buildExplosion(remotePos, big: _lives == 0));
    });

    if (_lives <= 0) {
      // Self-destruct explosion at own position
      Future.delayed(const Duration(milliseconds: 300), () {
        if (mounted) {
          setState(() {
            _explosions.addAll(
                _buildExplosion(_localState.position, big: true));
          });
          Future.delayed(const Duration(milliseconds: 800),
              () => _endMission(success: false));
        }
      });
    }
  }

  Future<void> _initCamera() async {
    try {
      final status = await Permission.camera.request();
      if (status.isDenied || status.isPermanentlyDenied) return;
      final cams = await availableCameras();
      if (!mounted || cams.isEmpty) return;
      final ctrl = CameraController(
        cams.first, ResolutionPreset.medium,
        enableAudio: false,
        imageFormatGroup: ImageFormatGroup.jpeg,
      );
      await ctrl.initialize();
      if (mounted) setState(() { _camera = ctrl; _camReady = true; });
    } catch (_) {}
  }

  Color _hexColor(String hex) {
    try {
      return Color(int.parse('FF${hex.replaceAll('#', '')}', radix: 16));
    } catch (_) {
      return const Color(0xFF00E5FF);
    }
  }

  Future<void> _initNasa() async {
    final mission = widget.world.get<MissionTarget>(widget.localShipId);
    if (mission == null) return;
    final planet =
        kPlanets.where((p) => p.id == mission.planetId).firstOrNull;
    if (planet == null) return;

    try {
      LocationPermission perm = await Geolocator.checkPermission();
      if (perm == LocationPermission.denied) {
        perm = await Geolocator.requestPermission();
      }
      double lat = 4.7110, lon = -74.0721;
      if (perm == LocationPermission.whileInUse ||
          perm == LocationPermission.always) {
        try {
          final pos = await Geolocator.getCurrentPosition(
            locationSettings: const LocationSettings(
              accuracy: LocationAccuracy.low,
              timeLimit: Duration(seconds: 8),
            ),
          );
          lat = pos.latitude;
          lon = pos.longitude;
        } catch (_) {}
      }
      final horizonsPos = await _horizons.getPlanetPosition(
        nasaBodyId: planet.nasaId,
        planetId: planet.id,
        lat: lat,
        lon: lon,
      );
      if (horizonsPos != null && mounted) {
        final azRad = horizonsPos.azimuthDeg * math.pi / 180;
        final altRad = horizonsPos.altitudeDeg * math.pi / 180;
        final dir = Vec3(
          math.sin(azRad) * math.cos(altRad),
          math.sin(altRad),
          -math.cos(azRad) * math.cos(altRad),
        ).normalized();
        widget.physics.setPlanetPosition(planet.id, dir * 1000.0);
        if (mounted) setState(() => _planetWorldPos = dir * 1000.0);
      }
    } catch (_) {}
  }

  void _onGyro(GyroData data) {}

  void _physicsStep() {
    if (_missionEnded) return;
    const dt = 0.016;

    widget.physics.applyInput(
      shipId: widget.localShipId,
      thrustMain: _thrustMain,
      steerYaw: _steerYaw,
      steerPitch: _steerPitch,
      strafeLateral: _strafeLateral,
      boost: _boosting,
    );
    widget.physics.tick(dt);

    // Emit exhaust particles when thrusting
    if (_screenSize != Size.zero &&
        (_thrustMain > 0.05 || _boosting)) {
      final cx = _screenSize.width / 2;
      final ey = _screenSize.height * 0.77;
      final spread = _screenSize.width * 0.07;
      _particles.emit(
        Offset(cx - spread, ey),
        Offset(cx + spread, ey),
        boosting: _boosting,
        thrust: _thrustMain,
      );
    }
    _particles.tick(dt);

    // Dust movement parallax
    _dust.tick(dt, _localState.velocity.length, _thrustMain, _screenSize);

    // Shooting stars advance
    for (final ss in _shootingStars) {
      ss.progress = (ss.progress + dt * ss.speed).clamp(0.0, 1.0);
    }
    _shootingStars.removeWhere((ss) => ss.progress >= 1.0);

    // Explosions tick
    for (final e in _explosions) {
      e.age += dt;
      for (final p in e.particles) {
        p.offset += p.vel * dt;
        p.vel = Offset(p.vel.dx * 0.94, p.vel.dy * 0.94);
        p.life -= dt * 1.4;
      }
      e.particles.removeWhere((p) => p.life <= 0);
    }
    _explosions.removeWhere((e) => e.done);

    // Screen shake decay
    if (_shakePower > 0) {
      _shakePower = (_shakePower - dt * 40).clamp(0.0, 100.0);
      _shakeX = (_expRng.nextDouble() - 0.5) * _shakePower;
      _shakeY = (_expRng.nextDouble() - 0.5) * _shakePower;
    } else {
      _shakeX = 0; _shakeY = 0;
    }

    // Invincibility countdown
    if (_invincible) {
      _invincibleTimer -= dt;
      if (_invincibleTimer <= 0) _invincible = false;
    }

    // Low-fuel flash oscillation
    if (_fuelFraction < 0.15) {
      _lowFuelFlash = (math.sin(
              DateTime.now().millisecondsSinceEpoch.toDouble() / 280.0) +
          1) /
          2;
    } else {
      _lowFuelFlash = 0;
    }

    // Collision detection & proximity tracking
    double nearestDist = double.infinity;
    for (final remote in _remoteStates.values) {
      final d = (_localState.position - remote.position).length;
      if (d < nearestDist) nearestDist = d;
      if (d < 12.0 && !_invincible && !_missionEnded) {
        _onCollision(remote.position);
        break;
      }
    }
    _proximityDist = nearestDist;

    final t = widget.world.get<ShipTransform>(widget.localShipId);
    final v = widget.world.get<ShipVelocity>(widget.localShipId);
    final m = widget.world.get<MissionTarget>(widget.localShipId);
    final fuel = widget.world.get<ShipFuel>(widget.localShipId);
    final scoreComp = widget.world.get<MissionScore>(widget.localShipId);
    if (t == null || v == null) return;

    final newState = ShipState(
      nodeId: widget.localPlayer.nodeId,
      position: t.position,
      velocity: v.linear,
      yaw: t.yaw,
      pitch: t.pitch,
      roll: t.roll,
      throttle: _thrustMain,
      boosting: _boosting,
      missionTarget: m?.planetId,
      distanceToTarget: m?.distanceUnits ?? 0,
      score: scoreComp?.compute(fuel?.fraction ?? 1.0) ?? 0,
      timestamp: DateTime.now().millisecondsSinceEpoch,
    );

    // ── Burnout system ─────────────────────────────────────────────────────────
    _nearMissCooldown += dt;
    if (_nearMissCooldown > _nearMissWindow) _nearMissCombo = 0;

    if (_burnoutActive) {
      _burnoutTicksLeft--;
      if (_burnoutTicksLeft <= 0) {
        _burnoutActive = false;
        _burnoutBar = 0.0;
      }
    } else {
      // Near-miss with any remote ship (12–22u = danger zone without collision)
      for (final remote in _remoteStates.values) {
        final d = (_localState.position - remote.position).length;
        if (d > 12.0 && d < 22.0) {
          _nearMissCombo++;
          _nearMissCooldown = 0;
          final comboMult = _nearMissCombo >= 10
              ? 3.0
              : _nearMissCombo >= 5
                  ? 2.0
                  : _nearMissCombo >= 3
                      ? 1.5
                      : 1.0;
          _burnoutBar = (_burnoutBar + 0.12 * comboMult).clamp(0.0, 1.0);
          break;
        }
      }
      // Low fuel danger (< 5%) continuously fills bar
      if (_fuelFraction < 0.05) {
        _burnoutBar = (_burnoutBar + 0.05 * dt).clamp(0.0, 1.0);
      }
      // Boost flying fills bar slowly
      if (_boosting) {
        _burnoutBar = (_burnoutBar + 0.008 * dt).clamp(0.0, 1.0);
      }
      // Bar decays slowly when not doing anything risky
      if (!_boosting && _fuelFraction >= 0.05) {
        _burnoutBar = (_burnoutBar - 0.02 * dt).clamp(0.0, 1.0);
      }
      // Trigger BURNOUT MODE when full
      if (_burnoutBar >= 1.0) {
        _burnoutActive = true;
        _burnoutTicksLeft = _burnoutDurationTicks;
        _nearMissCombo = 0;
      }
    }
    // ── End burnout system ──────────────────────────────────────────────────────

    if (mounted) {
      setState(() {
        _localState = newState;
        _fuelFraction = fuel?.fraction ?? 1.0;
      });
    }
    widget.sync?.broadcast(newState);

    if (m != null && m.arrivedAt != null && !_showScienceOps) {
      if (widget.missionDesign != null) {
        _tickTimer?.cancel();
        if (mounted) setState(() => _showScienceOps = true);
      } else {
        _endMission(success: true);
      }
    } else if (fuel != null && fuel.isEmpty && m != null) {
      _endMission(success: false);
    }
  }

  void _endMissionWithScience(int sciencePoints) {
    if (_missionEnded) return;
    _missionEnded = true;
    _tickTimer?.cancel();
    final m = widget.world.get<MissionTarget>(widget.localShipId);
    final fuel = widget.world.get<ShipFuel>(widget.localShipId);
    final score = widget.world.get<MissionScore>(widget.localShipId);
    final fuelFrac = fuel?.fraction ?? 0.0;
    final baseScore =
        score != null ? score.compute(fuelFrac) : 0;
    final planet = m != null
        ? kPlanets.where((p) => p.id == m.planetId).firstOrNull
        : null;
    widget.sync?.broadcastMissionEnd(baseScore + sciencePoints);
    setState(() {
      _showScienceOps = false;
      _missionResult = MissionResult(
        success: true,
        score: baseScore + sciencePoints,
        fuelFraction: fuelFrac,
        elapsed: score?.elapsed ?? Duration.zero,
        boostSeconds: score?.boostSecondsUsed ?? 0.0,
        arrivalDistance: m?.distanceUnits ?? 0,
        planetId: planet?.id,
      );
    });
  }

  void _endMission({required bool success}) {
    if (_missionEnded) return;
    _missionEnded = true;
    _tickTimer?.cancel();
    final m = widget.world.get<MissionTarget>(widget.localShipId);
    final fuel = widget.world.get<ShipFuel>(widget.localShipId);
    final score = widget.world.get<MissionScore>(widget.localShipId);
    final fuelFrac = fuel?.fraction ?? 0.0;
    final finalScore =
        success && score != null ? score.compute(fuelFrac) : 0;
    final planet = m != null
        ? kPlanets.where((p) => p.id == m.planetId).firstOrNull
        : null;
    widget.sync?.broadcastMissionEnd(finalScore);
    setState(() => _missionResult = MissionResult(
          success: success,
          score: finalScore,
          fuelFraction: fuelFrac,
          elapsed: score?.elapsed ?? Duration.zero,
          boostSeconds: score?.boostSecondsUsed ?? 0.0,
          arrivalDistance: m?.distanceUnits ?? 9999.0,
          planetId: planet?.id,
        ));
  }

  @override
  void dispose() {
    _tickTimer?.cancel();
    _shootingStarTimer?.cancel();
    _camera?.dispose();
    widget.gyro.removeListener(_onGyro);
    widget.gyro.disable();
    super.dispose();
  }

  void _openChat() => setState(() => _showChat = !_showChat);

  @override
  Widget build(BuildContext context) {
    _screenSize = MediaQuery.sizeOf(context);
    final remotePosns = _remoteStates.values.map((s) => s.position).toList();

    return Scaffold(
      backgroundColor: Colors.black,
      body: Transform.translate(
        offset: Offset(_shakeX, _shakeY),
        child: Stack(
        fit: StackFit.expand,
        children: [
          // 1. AR camera background
          if (_camReady && _camera != null)
            SizedBox.expand(
              child: FittedBox(
                fit: BoxFit.cover,
                child: SizedBox(
                  width: _camera!.value.previewSize?.height ?? 640,
                  height: _camera!.value.previewSize?.width ?? 480,
                  child: CameraPreview(_camera!),
                ),
              ),
            )
          else
            Container(color: const Color(0xFF000510)),

          // 2. Deep-space overlay
          Container(
            decoration: BoxDecoration(
              gradient: RadialGradient(
                center: Alignment.center,
                radius: 1.4,
                colors: [
                  Colors.black.withValues(alpha: 0.60),
                  Colors.black.withValues(alpha: 0.90),
                ],
              ),
            ),
          ),

          // 3. Universe (nebulae + stars + planet + ships + cockpit)
          CustomPaint(
            painter: _UniversePainter(
              shipYaw: _localState.yaw,
              shipPitch: _localState.pitch,
              shipPos: _localState.position,
              speed: _localState.velocity.length,
              boosting: _boosting,
              planetPos: _planetWorldPos,
              planetColor: _planetColor ?? const Color(0xFF00E5FF),
              planetId: _planetId,
              stars: _stars,
              remotePosns: remotePosns,
              particles: List.unmodifiable(_particles.all),
              shootingStars: List.unmodifiable(_shootingStars),
              dust: List.unmodifiable(_dust.all),
              explosions: List.unmodifiable(_explosions),
              fuelFraction: _fuelFraction,
              lives: _lives,
              invincible: _invincible,
              lowFuelFlash: _lowFuelFlash,
              proximityDist: _proximityDist,
              planetTexture: _planetId != null
                  ? _textureCache[_planetId!]
                  : null,
            ),
            child: const SizedBox.expand(),
          ),

          // 4. Flight controls
          if (!_missionEnded)
            FlightController(
              onThrust: (v) => setState(() => _thrustMain = v),
              onSteer: (y, p) => setState(() {
                _steerYaw = y;
                _steerPitch = p;
              }),
              onStrafe: (v) => setState(() => _strafeLateral = v),
              onBoost: (b) => setState(() => _boosting = b),
            ),

          // 5. HUD
          if (_missionResult == null)
            HudOverlay(
              localState: _localState,
              remoteStates: _remoteStates,
              fuelFraction: _fuelFraction,
              onChat: _openChat,
              onExit: widget.onExit,
              targetPlanetPos: _planetWorldPos,
              targetPlanetColor: _planetColor,
              targetPlanetName: _planetName,
            ),

          // 6. Mission result
          if (_missionResult != null)
            MissionResultOverlay(
              result: _missionResult!,
              remoteScores: _remoteScores,
              onNewMission: widget.onExit,
              onMainMenu: widget.onExit,
            ),

          // 7. Chat
          if (_showChat && _missionResult == null)
            ChatOverlay(
              sync: widget.sync,
              onClose: () => setState(() => _showChat = false),
            ),

          // 8. Science operations overlay on planet arrival
          if (_showScienceOps && widget.missionDesign != null)
            ScienceOpsScreen(
              design: widget.missionDesign!,
              planetId: _planetId ?? widget.missionDesign!.targetPlanetId,
              onComplete: _endMissionWithScience,
            ),

          // 9. Fuel transfer button when close to another ship
          if (_proximityDist < 30.0 && _fuelFraction > 0.10 && _missionResult == null)
            Positioned(
              bottom: 120,
              left: 0, right: 0,
              child: Center(
                child: GestureDetector(
                  onTap: () {
                    widget.sync?.broadcast(_localState);
                  },
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 8),
                    decoration: BoxDecoration(
                      color: Colors.cyanAccent.withValues(alpha: 0.12),
                      border: Border.all(color: Colors.cyanAccent.withValues(alpha: 0.60), width: 1),
                      borderRadius: BorderRadius.circular(6),
                    ),
                    child: const Text(
                      '⚡ TRANSFERIR COMBUSTIBLE',
                      style: TextStyle(
                        color: Colors.cyanAccent,
                        fontSize: 12,
                        fontWeight: FontWeight.bold,
                        letterSpacing: 1.8,
                      ),
                    ),
                  ),
                ),
              ),
            ),
        ],
        ),
      ),
    );
  }
}
