import 'package:flutter/material.dart';

// Joystick táctil personalizado — sin dependencias externas
class _JoystickPainter extends CustomPainter {
  final Offset center;
  final Offset stick;
  final double radius;
  final Color color;

  _JoystickPainter({
    required this.center,
    required this.stick,
    required this.radius,
    required this.color,
  });

  @override
  void paint(Canvas canvas, Size size) {
    final bgPaint = Paint()
      ..color = color.withValues(alpha: 0.15)
      ..style = PaintingStyle.fill;
    final borderPaint = Paint()
      ..color = color.withValues(alpha: 0.4)
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1.5;
    final stickPaint = Paint()
      ..color = color.withValues(alpha: 0.7)
      ..style = PaintingStyle.fill;

    canvas.drawCircle(center, radius, bgPaint);
    canvas.drawCircle(center, radius, borderPaint);
    canvas.drawCircle(stick, radius * 0.35, stickPaint);
  }

  @override
  bool shouldRepaint(_JoystickPainter o) =>
      o.stick != stick || o.center != center;
}

class _Joystick extends StatefulWidget {
  final void Function(double dx, double dy) onMove;
  final Color color;
  final double size;

  const _Joystick({
    required this.onMove,
    this.color = Colors.white,
    this.size = 110,
  });

  @override
  State<_Joystick> createState() => _JoystickState();
}

class _JoystickState extends State<_Joystick> {
  Offset _stick = Offset.zero;

  @override
  Widget build(BuildContext context) {
    final r = widget.size / 2;
    final center = Offset(r, r);

    return GestureDetector(
      onPanStart: (d) {
        setState(() { _stick = Offset.zero; });
      },
      onPanUpdate: (d) {
        final raw = Offset(
          (d.localPosition.dx - r).clamp(-r, r),
          (d.localPosition.dy - r).clamp(-r, r),
        );
        final mag = raw.distance;
        final maxR = r * 0.6;
        final clamped = mag > maxR ? raw / mag * maxR : raw;
        setState(() => _stick = clamped);
        widget.onMove(clamped.dx / maxR, clamped.dy / maxR);
      },
      onPanEnd: (_) {
        setState(() { _stick = Offset.zero; });
        widget.onMove(0, 0);
      },
      onPanCancel: () {
        setState(() { _stick = Offset.zero; });
        widget.onMove(0, 0);
      },
      child: SizedBox(
        width: widget.size,
        height: widget.size,
        child: CustomPaint(
          painter: _JoystickPainter(
            center: center,
            stick:  center + _stick,
            radius: r * 0.75,
            color:  widget.color,
          ),
        ),
      ),
    );
  }
}

// ──────────────────────────────────────────────────────────────────────────
// FlightController — overlay de controles sobre la vista cockpit
// ──────────────────────────────────────────────────────────────────────────
class FlightController extends StatefulWidget {
  final void Function(double thrust) onThrust;
  final void Function(double yaw, double pitch) onSteer;
  final void Function(double lateral) onStrafe;
  final void Function(bool active) onBoost;

  const FlightController({
    super.key,
    required this.onThrust,
    required this.onSteer,
    required this.onStrafe,
    required this.onBoost,
  });

  @override
  State<FlightController> createState() => _FlightControllerState();
}

class _FlightControllerState extends State<FlightController> {
  bool _boosting = false;
  double _thrustLevel = 0;

  void _onLeftJoystick(double dx, double dy) {
    // Joystick izquierdo: strafe lateral (X) + aceleración (Y invertida)
    widget.onStrafe(dx);
    final thrust = (-dy).clamp(0.0, 1.0);
    setState(() => _thrustLevel = thrust);
    widget.onThrust(thrust);
  }

  void _onRightJoystick(double dx, double dy) {
    // Joystick derecho: steer yaw/pitch
    widget.onSteer(dx, dy);
  }

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Stack(
        children: [
          // Joystick izquierdo — aceleración y strafe
          Positioned(
            left: 20, bottom: 30,
            child: _Joystick(
              onMove: _onLeftJoystick,
              color: const Color(0xFF00E5FF),
              size: 120,
            ),
          ),

          // Joystick derecho — dirección
          Positioned(
            right: 20, bottom: 30,
            child: _Joystick(
              onMove: _onRightJoystick,
              color: const Color(0xFFFF6B35),
              size: 120,
            ),
          ),

          // Botón BOOST
          Positioned(
            right: 155, bottom: 50,
            child: GestureDetector(
              onTapDown: (_) {
                setState(() => _boosting = true);
                widget.onBoost(true);
              },
              onTapUp: (_) {
                setState(() => _boosting = false);
                widget.onBoost(false);
              },
              onTapCancel: () {
                setState(() => _boosting = false);
                widget.onBoost(false);
              },
              child: AnimatedContainer(
                duration: const Duration(milliseconds: 100),
                width: 56, height: 56,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: _boosting
                      ? const Color(0xFFFF6B35)
                      : const Color(0xFFFF6B35).withValues(alpha: 0.3),
                  border: Border.all(
                    color: const Color(0xFFFF6B35).withValues(alpha: 0.8),
                    width: 2,
                  ),
                  boxShadow: _boosting
                      ? [BoxShadow(
                          color: const Color(0xFFFF6B35).withValues(alpha: 0.6),
                          blurRadius: 16, spreadRadius: 4,
                        )]
                      : null,
                ),
                child: const Center(
                  child: Text('BOOST',
                    style: TextStyle(
                      color: Colors.white,
                      fontSize: 10,
                      fontWeight: FontWeight.bold,
                      letterSpacing: 1,
                    ),
                  ),
                ),
              ),
            ),
          ),

          // Indicador de empuje
          Positioned(
            left: 155, bottom: 50,
            child: _ThrustIndicator(level: _thrustLevel),
          ),
        ],
      ),
    );
  }
}

class _ThrustIndicator extends StatelessWidget {
  final double level;  // 0-1
  const _ThrustIndicator({required this.level});

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 8, height: 80,
      child: Column(
        mainAxisAlignment: MainAxisAlignment.end,
        children: [
          Flexible(
            child: FractionallySizedBox(
              heightFactor: level.clamp(0.0, 1.0),
              child: Container(
                width: 8,
                decoration: BoxDecoration(
                  borderRadius: BorderRadius.circular(4),
                  gradient: LinearGradient(
                    begin: Alignment.bottomCenter,
                    end: Alignment.topCenter,
                    colors: [
                      const Color(0xFF00E5FF),
                      const Color(0xFF00E5FF).withValues(alpha: 0.3),
                    ],
                  ),
                ),
              ),
            ),
          ),
          Container(
            height: 2, width: 8,
            color: const Color(0xFF00E5FF).withValues(alpha: 0.5),
          ),
        ],
      ),
    );
  }
}
