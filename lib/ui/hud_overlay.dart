import 'dart:math' as math;
import 'package:flutter/material.dart';
import '../models/ship_state.dart';
import '../core/vec3.dart';

class HudOverlay extends StatelessWidget {
  final ShipState localState;
  final Map<String, ShipState> remoteStates;
  final double fuelFraction;
  final VoidCallback onChat;
  final VoidCallback onExit;
  final Vec3? targetPlanetPos;
  final Color? targetPlanetColor;
  final String? targetPlanetName;

  const HudOverlay({
    super.key,
    required this.localState,
    required this.remoteStates,
    required this.fuelFraction,
    required this.onChat,
    required this.onExit,
    this.targetPlanetPos,
    this.targetPlanetColor,
    this.targetPlanetName,
  });

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Stack(
        children: [
          // ── Top bar ──────────────────────────────────
          Positioned(
            top: 0, left: 0, right: 0,
            child: _TopBar(
              speed:        localState.velocity.length,
              mission:      localState.missionTarget,
              distance:     localState.distanceToTarget,
              fuelFraction: fuelFraction,
              onExit:       onExit,
            ),
          ),

          // ── Indicador de dirección del planeta ───────
          if (targetPlanetPos != null && localState.missionTarget != null)
            _PlanetDirectionIndicator(
              shipPos:     Vec3(
                localState.position.x,
                localState.position.y,
                localState.position.z,
              ),
              shipYaw:     localState.yaw,
              shipPitch:   localState.pitch,
              planetPos:   targetPlanetPos!,
              color:       targetPlanetColor ?? const Color(0xFF00E5FF),
              label:       targetPlanetName ?? '',
              distance:    localState.distanceToTarget,
            ),

          // ── Radar ────────────────────────────────────
          Positioned(
            top: 60, right: 16,
            child: _Radar(
              localState:   localState,
              remoteStates: remoteStates,
            ),
          ),

          // ── Centro — crosshair ───────────────────────
          const Center(child: _Crosshair()),

          // ── Jugadores conectados ─────────────────────
          if (remoteStates.isNotEmpty)
            Positioned(
              top: 200, right: 16,
              child: _PlayerList(remoteStates: remoteStates),
            ),

          // ── Chat button ──────────────────────────────
          Positioned(
            top: 10, right: 16,
            child: GestureDetector(
              onTap: onChat,
              child: Container(
                padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                decoration: BoxDecoration(
                  color: Colors.white.withValues(alpha: 0.08),
                  borderRadius: BorderRadius.circular(20),
                  border: Border.all(color: Colors.white24),
                ),
                child: const Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(Icons.radio, color: Colors.white70, size: 14),
                    SizedBox(width: 4),
                    Text('RADIO', style: TextStyle(
                      color: Colors.white70, fontSize: 10, letterSpacing: 1.5,
                    )),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// ── Planet Direction Indicator ─────────────────────────────────────────────

class _PlanetDirectionIndicator extends StatelessWidget {
  final Vec3 shipPos;
  final double shipYaw;
  final double shipPitch;
  final Vec3 planetPos;
  final Color color;
  final String label;
  final double distance;

  const _PlanetDirectionIndicator({
    required this.shipPos,
    required this.shipYaw,
    required this.shipPitch,
    required this.planetPos,
    required this.color,
    required this.label,
    required this.distance,
  });

  @override
  Widget build(BuildContext context) {
    return Positioned.fill(
      child: IgnorePointer(
        child: LayoutBuilder(
          builder: (_, c) => CustomPaint(
            painter: _PlanetDirectionPainter(
              shipPos:   shipPos,
              shipYaw:   shipYaw,
              shipPitch: shipPitch,
              planetPos: planetPos,
              color:     color,
              label:     label,
              distance:  distance,
            ),
          ),
        ),
      ),
    );
  }
}

class _PlanetDirectionPainter extends CustomPainter {
  final Vec3 shipPos;
  final double shipYaw;
  final double shipPitch;
  final Vec3 planetPos;
  final Color color;
  final String label;
  final double distance;

  const _PlanetDirectionPainter({
    required this.shipPos,
    required this.shipYaw,
    required this.shipPitch,
    required this.planetPos,
    required this.color,
    required this.label,
    required this.distance,
  });

  @override
  void paint(Canvas canvas, Size size) {
    final diff = planetPos - shipPos;
    if (diff.length < 0.1) return;
    final dir = diff.normalized();

    // Vectores base de la nave
    final cp  = math.cos(shipPitch);
    final sp  = math.sin(shipPitch);
    final cy  = math.cos(shipYaw);
    final sy  = math.sin(shipYaw);
    final fwd   = Vec3(-sy * cp,  sp,  -cy * cp);
    final right = Vec3(cy,        0.0, -sy);
    final up    = right.cross(fwd);

    // Proyección a espacio de vista
    final vx = dir.dot(right);
    final vy = dir.dot(up);
    final vz = dir.dot(fwd);

    final cx2   = size.width  / 2;
    final cy2   = size.height / 2;
    final scale = math.min(cx2, cy2) * 0.85;

    double sx, sy2;
    bool   isAhead;

    if (vz > 0.05) {
      sx  = cx2 + (vx / vz) * scale;
      sy2 = cy2 - (vy / vz) * scale;
      isAhead = true;
    } else {
      // Planeta detrás: invertir y proyectar
      sx  = cx2 - vx * scale;
      sy2 = cy2 + vy * scale;
      isAhead = false;
    }

    const margin = 40.0;
    final inBounds = sx > margin && sx < size.width  - margin
                  && sy2 > margin && sy2 < size.height - margin;
    final drawReticle = isAhead && inBounds;

    // Clamp al borde si está fuera
    final Offset finalPos;
    if (drawReticle) {
      finalPos = Offset(sx, sy2);
    } else {
      final dx = sx - cx2;
      final dy = sy2 - cy2;
      if (dx.abs() < 0.01 && dy.abs() < 0.01) {
        finalPos = Offset(cx2, margin);
      } else {
        final scX = dx.abs() > 0.01 ? (cx2  - margin) / dx.abs() : double.infinity;
        final scY = dy.abs() > 0.01 ? (cy2 - margin) / dy.abs() : double.infinity;
        final sc  = math.min(scX, scY);
        finalPos  = Offset(cx2 + dx * sc, cy2 + dy * sc);
      }
    }

    if (drawReticle) {
      _drawReticle(canvas, finalPos, color, label, distance);
    } else {
      final angle = math.atan2(finalPos.dy - cy2, finalPos.dx - cx2);
      _drawEdgeArrow(canvas, finalPos, angle, color, label, distance);
    }
  }

  void _drawReticle(Canvas canvas, Offset pos, Color col,
      String lbl, double dist) {
    const r = 18.0;
    final paint = Paint()
      ..color = col
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1.5;
    final fillPaint = Paint()
      ..color = col.withValues(alpha: 0.10)
      ..style = PaintingStyle.fill;

    // Diamante
    final path = Path()
      ..moveTo(pos.dx,         pos.dy - r)
      ..lineTo(pos.dx + r * 0.6, pos.dy)
      ..lineTo(pos.dx,         pos.dy + r)
      ..lineTo(pos.dx - r * 0.6, pos.dy)
      ..close();
    canvas.drawPath(path, fillPaint);
    canvas.drawPath(path, paint);

    // Etiqueta nombre
    _paintText(canvas, lbl.toUpperCase(), pos.translate(0, -r - 14), col, 9);
    // Distancia
    _paintText(canvas, '${dist.toStringAsFixed(0)} u',
        pos.translate(0, r + 10), col.withValues(alpha: 0.75), 8);
  }

  void _drawEdgeArrow(Canvas canvas, Offset pos, double angle, Color col,
      String lbl, double dist) {
    canvas.save();
    canvas.translate(pos.dx, pos.dy);
    canvas.rotate(angle);
    final paint = Paint()..color = col..style = PaintingStyle.fill;
    // Triángulo flecha
    final path = Path()
      ..moveTo(14,  0)
      ..lineTo(-7, -6)
      ..lineTo(-5,  0)
      ..lineTo(-7,  6)
      ..close();
    canvas.drawPath(path, paint);
    // Círculo base
    canvas.drawCircle(Offset.zero, 4,
        Paint()..color = col.withValues(alpha: 0.3)..style = PaintingStyle.fill);
    canvas.restore();

    // Nombre y distancia fuera del canvas.save
    _paintText(canvas, lbl.toUpperCase(), pos.translate(0, -18), col, 9);
    _paintText(canvas, '${dist.toStringAsFixed(0)} u',
        pos.translate(0, 14), col.withValues(alpha: 0.75), 8);
  }

  void _paintText(Canvas canvas, String text, Offset pos, Color col, double size) {
    final tp = TextPainter(
      text: TextSpan(
        text: text,
        style: TextStyle(
          color: col, fontSize: size, letterSpacing: 1.5,
          fontWeight: FontWeight.bold,
        ),
      ),
      textDirection: TextDirection.ltr,
    )..layout();
    tp.paint(canvas, Offset(pos.dx - tp.width / 2, pos.dy - tp.height / 2));
  }

  @override
  bool shouldRepaint(_PlanetDirectionPainter o) => true;
}

// ── Top Bar ────────────────────────────────────────────────────────────────

class _TopBar extends StatelessWidget {
  final double speed;
  final String? mission;
  final double distance;
  final double fuelFraction;
  final VoidCallback onExit;

  const _TopBar({
    required this.speed,
    required this.mission,
    required this.distance,
    required this.fuelFraction,
    required this.onExit,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      decoration: BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topCenter, end: Alignment.bottomCenter,
          colors: [Colors.black.withValues(alpha: 0.7), Colors.transparent],
        ),
      ),
      child: Row(
        children: [
          GestureDetector(
            onTap: onExit,
            child: Container(
              padding: const EdgeInsets.all(6),
              decoration: BoxDecoration(
                color: Colors.white.withValues(alpha: 0.1),
                borderRadius: BorderRadius.circular(8),
                border: Border.all(color: Colors.white24),
              ),
              child: const Icon(Icons.close, color: Colors.white70, size: 18),
            ),
          ),
          const SizedBox(width: 16),

          _HudValue(label: 'VEL', value: '${speed.toStringAsFixed(0)} u/s'),
          const SizedBox(width: 20),

          if (mission != null) ...[
            _HudValue(
              label: 'DEST',
              value: mission!.toUpperCase(),
              color: const Color(0xFF00E5FF),
            ),
            const SizedBox(width: 20),
            _HudValue(
              label: 'DIST',
              value: '${distance.toStringAsFixed(1)} u',
            ),
          ] else
            const _HudValue(label: 'MODE', value: 'FREE'),

          const Spacer(),
          _FuelGauge(fraction: fuelFraction),
        ],
      ),
    );
  }
}

class _FuelGauge extends StatelessWidget {
  final double fraction;
  const _FuelGauge({required this.fraction});

  Color get _color {
    if (fraction > 0.60) return const Color(0xFF00E5FF);
    if (fraction > 0.30) return const Color(0xFFFFD700);
    return const Color(0xFFFF4444);
  }

  @override
  Widget build(BuildContext context) {
    final pct = (fraction * 100).round();
    final c = _color;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.end,
      mainAxisSize: MainAxisSize.min,
      children: [
        Text('FUEL', style: TextStyle(
          color: c.withValues(alpha: 0.7), fontSize: 8, letterSpacing: 1.5)),
        const SizedBox(height: 3),
        SizedBox(
          width: 70, height: 6,
          child: ClipRRect(
            borderRadius: BorderRadius.circular(3),
            child: Stack(children: [
              Container(color: Colors.white.withValues(alpha: 0.1)),
              FractionallySizedBox(
                widthFactor: fraction.clamp(0.0, 1.0),
                child: Container(
                  decoration: BoxDecoration(
                    color: c,
                    boxShadow: [BoxShadow(
                      color: c.withValues(alpha: 0.5), blurRadius: 4)],
                  ),
                ),
              ),
            ]),
          ),
        ),
        const SizedBox(height: 2),
        Text('$pct%', style: TextStyle(
          color: c, fontSize: 9, fontWeight: FontWeight.w700)),
      ],
    );
  }
}

class _HudValue extends StatelessWidget {
  final String label;
  final String value;
  final Color color;

  const _HudValue({
    required this.label,
    required this.value,
    this.color = Colors.white,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(label, style: TextStyle(
          color: color.withValues(alpha: 0.6), fontSize: 8, letterSpacing: 1.5,
        )),
        Text(value, style: TextStyle(
          color: color, fontSize: 13, fontWeight: FontWeight.w600,
          letterSpacing: 0.5,
        )),
      ],
    );
  }
}

class _Crosshair extends StatelessWidget {
  const _Crosshair();

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 24, height: 24,
      child: CustomPaint(painter: _CrosshairPainter()),
    );
  }
}

class _CrosshairPainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = Colors.white.withValues(alpha: 0.6)
      ..strokeWidth = 1.0
      ..style = PaintingStyle.stroke;

    final c = Offset(size.width / 2, size.height / 2);
    final r = size.width / 2;
    final gap = r * 0.3;
    canvas.drawLine(Offset(c.dx, c.dy - r), Offset(c.dx, c.dy - gap), paint);
    canvas.drawLine(Offset(c.dx, c.dy + gap), Offset(c.dx, c.dy + r), paint);
    canvas.drawLine(Offset(c.dx - r, c.dy), Offset(c.dx - gap, c.dy), paint);
    canvas.drawLine(Offset(c.dx + gap, c.dy), Offset(c.dx + r, c.dy), paint);
    canvas.drawCircle(c, 2, paint..style = PaintingStyle.fill);
  }

  @override
  bool shouldRepaint(_CrosshairPainter _) => false;
}

class _Radar extends StatelessWidget {
  final ShipState localState;
  final Map<String, ShipState> remoteStates;

  const _Radar({required this.localState, required this.remoteStates});

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 80, height: 80,
      child: CustomPaint(
        painter: _RadarPainter(
          local:  localState,
          remote: remoteStates.values.toList(),
        ),
      ),
    );
  }
}

class _RadarPainter extends CustomPainter {
  final ShipState local;
  final List<ShipState> remote;

  _RadarPainter({required this.local, required this.remote});

  @override
  void paint(Canvas canvas, Size size) {
    final c = Offset(size.width / 2, size.height / 2);
    final r = size.width / 2 - 2;
    canvas.drawCircle(c, r,
        Paint()..color = Colors.green.withValues(alpha: 0.08)..style = PaintingStyle.fill);
    final border = Paint()
      ..color = Colors.green.withValues(alpha: 0.4)
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1;
    canvas.drawCircle(c, r, border);
    canvas.drawCircle(c, r * 0.5,
        border..color = Colors.green.withValues(alpha: 0.2));

    canvas.drawCircle(c, 3,
        Paint()..color = Colors.greenAccent..style = PaintingStyle.fill);

    const scale = 0.01;
    final remotePaint = Paint()..color = Colors.orangeAccent..style = PaintingStyle.fill;
    for (final s in remote) {
      final dx = (s.position.x - local.position.x) * scale;
      final dz = (s.position.z - local.position.z) * scale;
      final px = (c.dx + dx).clamp(c.dx - r + 4, c.dx + r - 4);
      final py = (c.dy + dz).clamp(c.dy - r + 4, c.dy + r - 4);
      canvas.drawCircle(Offset(px.toDouble(), py.toDouble()), 3, remotePaint);
    }
  }

  @override
  bool shouldRepaint(_RadarPainter o) => true;
}

class _PlayerList extends StatelessWidget {
  final Map<String, ShipState> remoteStates;
  const _PlayerList({required this.remoteStates});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.end,
      children: remoteStates.entries.take(8).map((e) {
        final id    = e.key;
        final short = id.length > 8 ? id.substring(0, 8) : id;
        return Container(
          margin: const EdgeInsets.only(bottom: 4),
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
          decoration: BoxDecoration(
            color: Colors.orange.withValues(alpha: 0.12),
            borderRadius: BorderRadius.circular(10),
            border: Border.all(color: Colors.orange.withValues(alpha: 0.3)),
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                width: 6, height: 6,
                decoration: const BoxDecoration(
                  color: Colors.orangeAccent, shape: BoxShape.circle,
                ),
              ),
              const SizedBox(width: 6),
              Text(short, style: const TextStyle(
                color: Colors.white70, fontSize: 10, letterSpacing: 0.5,
              )),
            ],
          ),
        );
      }).toList(),
    );
  }
}
