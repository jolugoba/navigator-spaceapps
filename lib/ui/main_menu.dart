import 'package:flutter/material.dart';
import 'dart:math' as math;

import '../l10n/app_localizations.dart';

class MainMenuScreen extends StatefulWidget {
  final VoidCallback onPlay;
  const MainMenuScreen({super.key, required this.onPlay});

  @override
  State<MainMenuScreen> createState() => _MainMenuScreenState();
}

class _MainMenuScreenState extends State<MainMenuScreen>
    with SingleTickerProviderStateMixin {
  late AnimationController _pulseCtrl;
  late Animation<double> _pulseAnim;

  @override
  void initState() {
    super.initState();
    _pulseCtrl = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 2000),
    )..repeat(reverse: true);
    _pulseAnim = CurvedAnimation(parent: _pulseCtrl, curve: Curves.easeInOut);
  }

  @override
  void dispose() {
    _pulseCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final s = AppLocalizations.of(context);
    return Scaffold(
      backgroundColor: const Color(0xFF010408),
      body: Stack(
        fit: StackFit.expand,
        children: [
          const _StarField(),
          SafeArea(
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 32),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.center,
                children: [
                  AnimatedBuilder(
                    animation: _pulseAnim,
                    builder: (_, __) => Text(
                      s.appTitle,
                      style: TextStyle(
                        color: Color.lerp(
                          const Color(0xFF00E5FF),
                          const Color(0xFF0080FF),
                          _pulseAnim.value,
                        ),
                        fontSize: 42,
                        fontWeight: FontWeight.w900,
                        letterSpacing: 12,
                        shadows: [
                          Shadow(
                            color: const Color(0xFF00E5FF).withValues(alpha: 0.6),
                            blurRadius: 20 + _pulseAnim.value * 20,
                          ),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    s.appSubtitle,
                    style: const TextStyle(
                        color: Colors.white38, fontSize: 11, letterSpacing: 4),
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 12),
                  Container(
                    height: 1, width: 120,
                    color: const Color(0xFF00E5FF).withValues(alpha: 0.3),
                  ),
                  const SizedBox(height: 60),
                  _FeatureRow(icon: Icons.rocket_launch,   text: s.featureFlight),
                  const SizedBox(height: 12),
                  _FeatureRow(icon: Icons.screen_rotation, text: s.featureGyro),
                  const SizedBox(height: 12),
                  _FeatureRow(icon: Icons.people,          text: s.featureP2p),
                  const SizedBox(height: 12),
                  _FeatureRow(icon: Icons.lock,            text: s.featureCrypto),
                  const SizedBox(height: 12),
                  _FeatureRow(icon: Icons.satellite_alt,   text: s.featureNasa),
                  const SizedBox(height: 60),
                  SizedBox(
                    width: double.infinity,
                    child: ElevatedButton(
                      onPressed: widget.onPlay,
                      style: ElevatedButton.styleFrom(
                        backgroundColor: const Color(0xFF00E5FF),
                        foregroundColor: Colors.black,
                        padding: const EdgeInsets.symmetric(vertical: 20),
                        shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(16)),
                        elevation: 0,
                      ),
                      child: Text(
                        s.btnStartMission,
                        style: const TextStyle(
                          fontWeight: FontWeight.w900, fontSize: 16, letterSpacing: 5),
                      ),
                    ),
                  ),
                  const SizedBox(height: 24),
                  Text(s.credits,
                    style: const TextStyle(
                        color: Colors.white12, fontSize: 9, letterSpacing: 2)),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _FeatureRow extends StatelessWidget {
  final IconData icon;
  final String text;
  const _FeatureRow({required this.icon, required this.text});

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        Icon(icon, color: const Color(0xFF00E5FF).withValues(alpha: 0.7), size: 16),
        const SizedBox(width: 12),
        Text(text, style: const TextStyle(
          color: Colors.white54, fontSize: 13, letterSpacing: 0.5)),
      ],
    );
  }
}

class _StarField extends StatefulWidget {
  const _StarField();
  @override
  State<_StarField> createState() => _StarFieldState();
}

class _StarFieldState extends State<_StarField>
    with SingleTickerProviderStateMixin {
  late AnimationController _ctrl;
  final _rng = math.Random(42);
  late final List<_Star> _stars;

  @override
  void initState() {
    super.initState();
    _stars = List.generate(200, (_) => _Star(
      x: _rng.nextDouble(), y: _rng.nextDouble(),
      r: _rng.nextDouble() * 1.5 + 0.3,
      brightness: _rng.nextDouble(),
      twinkleOffset: _rng.nextDouble() * math.pi * 2,
      twinkleSpeed:  _rng.nextDouble() * 2 + 0.5,
    ));
    _ctrl = AnimationController(
        vsync: this, duration: const Duration(seconds: 6))..repeat();
  }

  @override
  void dispose() { _ctrl.dispose(); super.dispose(); }

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: _ctrl,
      builder: (_, __) => CustomPaint(
        painter: _StarFieldPainter(_stars, _ctrl.value),
        size: Size.infinite,
      ),
    );
  }
}

class _Star {
  final double x, y, r, brightness, twinkleOffset, twinkleSpeed;
  const _Star({
    required this.x, required this.y, required this.r,
    required this.brightness, required this.twinkleOffset,
    required this.twinkleSpeed,
  });
}

class _StarFieldPainter extends CustomPainter {
  final List<_Star> stars;
  final double t;
  _StarFieldPainter(this.stars, this.t);

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()..style = PaintingStyle.fill;
    for (final s in stars) {
      final twinkle =
          (math.sin(t * math.pi * 2 * s.twinkleSpeed + s.twinkleOffset) + 1) / 2;
      final alpha = (s.brightness * 0.7 + twinkle * 0.3).clamp(0.1, 1.0);
      paint.color = Colors.white.withValues(alpha: alpha);
      canvas.drawCircle(
          Offset(s.x * size.width, s.y * size.height), s.r, paint);
    }
  }

  @override
  bool shouldRepaint(_StarFieldPainter o) => o.t != t;
}
