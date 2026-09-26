import 'dart:async';
import 'dart:math' as math;
import 'package:flutter/material.dart';
import '../models/mission_design.dart';
import '../models/planet_info.dart';

class ScienceOpsScreen extends StatefulWidget {
  final MissionDesign design;
  final String planetId;
  final void Function(int sciencePoints) onComplete;

  const ScienceOpsScreen({
    super.key,
    required this.design,
    required this.planetId,
    required this.onComplete,
  });

  @override
  State<ScienceOpsScreen> createState() => _ScienceOpsScreenState();
}

class _ScienceOpsScreenState extends State<ScienceOpsScreen> {
  late final Map<ScienceInstrument, bool> _active;
  late final Map<ScienceInstrument, double> _progress;
  Timer? _tick;
  double _txProgress = 0.0;
  bool _transmitting = false;
  bool _txDone = false;

  // One-way light delay: AU × 8.317 min/AU (speed of light)
  static const _planetAU = <String, double>{
    'mercury': 0.61, 'venus': 0.28, 'mars': 0.52,
    'jupiter': 5.20, 'saturn': 9.54, 'uranus': 19.19, 'neptune': 30.07,
  };

  double get _commDelayMin =>
      (_planetAU[widget.planetId] ?? 1.0) * 8.317;

  double get _powerUsed {
    double p = MissionDesign.busPowerW;
    for (final e in _active.entries) {
      if (e.value) p += instrumentPowerW[e.key]!;
    }
    return p;
  }

  bool get _hasPower => _powerUsed <= widget.design.effectivePowerW;

  int get _scienceCollected {
    int pts = 0;
    for (final i in widget.design.instruments) {
      if ((_progress[i] ?? 0) >= 1.0) pts += instrumentSciencePoints[i]!;
    }
    return pts;
  }

  @override
  void initState() {
    super.initState();
    _active = {for (final i in widget.design.instruments) i: false};
    _progress = {for (final i in widget.design.instruments) i: 0.0};
    _tick = Timer.periodic(const Duration(milliseconds: 50), _onTick);
  }

  void _onTick(Timer _) {
    if (!mounted) return;
    const dt = 0.05;
    bool changed = false;
    for (final i in widget.design.instruments) {
      if (_active[i] == true && _hasPower && _progress[i]! < 1.0) {
        _progress[i] = (_progress[i]! + dt / 25.0).clamp(0.0, 1.0);
        changed = true;
      }
    }
    if (_transmitting) {
      final rateKbps = antennaDataRateKbps[widget.design.antenna]!;
      // Scale total so low-gain antenna still finishes in reasonable time
      final totalMb = widget.design.scienceDataMB.clamp(0.01, 100.0);
      final mbPerSec = rateKbps / (8.0 * 1000.0);
      _txProgress = (_txProgress + mbPerSec * dt / totalMb).clamp(0.0, 1.0);
      if (_txProgress >= 1.0) {
        _transmitting = false;
        _txDone = true;
      }
      changed = true;
    }
    if (changed) setState(() {});
  }

  void _toggleInstrument(ScienceInstrument i) {
    final isActive = _active[i] ?? false;
    if (!isActive) {
      final wouldUse = _powerUsed + instrumentPowerW[i]!;
      if (wouldUse > widget.design.effectivePowerW) return;
    }
    setState(() => _active[i] = !isActive);
  }

  @override
  void dispose() {
    _tick?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final planetInfo =
        kPlanets.where((p) => p.id == widget.planetId).firstOrNull;
    final planetName = planetInfo?.name ?? widget.planetId.toUpperCase();
    Color planetColor;
    try {
      final hex = (planetInfo?.colorHex ?? '00E5FF').replaceAll('#', '');
      planetColor = Color(int.parse('FF$hex', radix: 16));
    } catch (_) {
      planetColor = const Color(0xFF00E5FF);
    }

    final delayMin = _commDelayMin;
    final delayStr = delayMin >= 60
        ? '${(delayMin / 60).floor()}h ${(delayMin % 60).round()}m'
        : '${delayMin.toStringAsFixed(1)} min';

    final powerFrac =
        (_powerUsed / widget.design.effectivePowerW).clamp(0.0, 1.2);
    final powerOk = powerFrac <= 1.0;
    final sciPts = _scienceCollected;

    return Material(
      color: const Color(0xFF010408),
      child: Stack(
        fit: StackFit.expand,
        children: [
          // Starfield background
          CustomPaint(
              painter: _SciStarBgPainter(), child: const SizedBox.expand()),

          // Planet glow top-right
          Positioned(
            right: -80,
            top: -80,
            child: Container(
              width: 360,
              height: 360,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                gradient: RadialGradient(colors: [
                  planetColor.withValues(alpha: 0.20),
                  planetColor.withValues(alpha: 0.05),
                  Colors.transparent,
                ]),
              ),
            ),
          ),

          SafeArea(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // ── Header ────────────────────────────────────────────────────
                Container(
                  padding: const EdgeInsets.fromLTRB(16, 12, 16, 10),
                  decoration: BoxDecoration(
                    border: Border(
                      bottom: BorderSide(
                          color: planetColor.withValues(alpha: 0.30), width: 1),
                    ),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Icon(Icons.satellite_alt_rounded,
                              color: planetColor, size: 16),
                          const SizedBox(width: 8),
                          Text(
                            'OPERACIONES CIENTÍFICAS — $planetName',
                            style: TextStyle(
                              color: planetColor,
                              fontSize: 12,
                              fontWeight: FontWeight.bold,
                              letterSpacing: 1.8,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 6),
                      Row(
                        children: [
                          _Label('RETARDO', delayStr, Colors.white38),
                          const SizedBox(width: 20),
                          _Label('ANTENA',
                              antennaLabel[widget.design.antenna]!,
                              Colors.white38),
                          const SizedBox(width: 20),
                          _Label(
                            'DATOS',
                            _formatRate(
                                antennaDataRateKbps[widget.design.antenna]!),
                            Colors.white38,
                          ),
                          const SizedBox(width: 20),
                          _Label('OBJETIVO',
                              widget.design.objective.toUpperCase(),
                              Colors.white38),
                        ],
                      ),
                    ],
                  ),
                ),

                // ── Power bar ─────────────────────────────────────────────────
                Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                  child: Row(
                    children: [
                      const Text('POTENCIA  ',
                          style: TextStyle(
                              color: Colors.white38,
                              fontSize: 9,
                              letterSpacing: 1.5)),
                      Expanded(
                        child: ClipRRect(
                          borderRadius: BorderRadius.circular(3),
                          child: LinearProgressIndicator(
                            value: powerFrac.clamp(0.0, 1.0),
                            minHeight: 6,
                            backgroundColor: Colors.white10,
                            color: powerOk
                                ? const Color(0xFF44FF88)
                                : Colors.redAccent,
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Text(
                        '${_powerUsed.round()} / ${widget.design.effectivePowerW.round()} W',
                        style: TextStyle(
                          color: powerOk
                              ? const Color(0xFF44FF88)
                              : Colors.redAccent,
                          fontSize: 9,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ],
                  ),
                ),

                const Divider(color: Colors.white10, height: 1),

                // ── Instrument list ────────────────────────────────────────────
                Expanded(
                  child: ListView(
                    padding: const EdgeInsets.fromLTRB(12, 8, 12, 8),
                    children:
                        widget.design.instruments.map((i) {
                      final prog = _progress[i] ?? 0.0;
                      final act = _active[i] ?? false;
                      final done = prog >= 1.0;
                      final pts = instrumentSciencePoints[i]!;
                      final powerW = instrumentPowerW[i]!;
                      final canActivate =
                          act || _powerUsed + powerW <= widget.design.effectivePowerW;

                      return Container(
                        margin: const EdgeInsets.only(bottom: 8),
                        padding: const EdgeInsets.all(10),
                        decoration: BoxDecoration(
                          color: done
                              ? const Color(0xFF001A0A)
                              : act
                                  ? const Color(0xFF001525)
                                  : const Color(0xFF0A0E14),
                          border: Border.all(
                            color: done
                                ? const Color(0xFF44FF88).withValues(alpha: 0.40)
                                : act
                                    ? Colors.cyanAccent.withValues(alpha: 0.30)
                                    : Colors.white.withValues(alpha: 0.07),
                          ),
                          borderRadius: BorderRadius.circular(6),
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              children: [
                                GestureDetector(
                                  onTap: done
                                      ? null
                                      : (canActivate
                                          ? () => _toggleInstrument(i)
                                          : null),
                                  child: AnimatedContainer(
                                    duration: const Duration(milliseconds: 200),
                                    width: 28,
                                    height: 16,
                                    decoration: BoxDecoration(
                                      color: done
                                          ? const Color(0xFF44FF88)
                                              .withValues(alpha: 0.20)
                                          : act
                                              ? Colors.cyanAccent
                                                  .withValues(alpha: 0.25)
                                              : Colors.white
                                                  .withValues(alpha: 0.08),
                                      borderRadius: BorderRadius.circular(8),
                                      border: Border.all(
                                        color: done
                                            ? const Color(0xFF44FF88)
                                            : act
                                                ? Colors.cyanAccent
                                                : Colors.white24,
                                      ),
                                    ),
                                    child: Center(
                                      child: Text(
                                        done
                                            ? '✓'
                                            : act
                                                ? '●'
                                                : '○',
                                        style: TextStyle(
                                          fontSize: 9,
                                          color: done
                                              ? const Color(0xFF44FF88)
                                              : act
                                                  ? Colors.cyanAccent
                                                  : Colors.white38,
                                        ),
                                      ),
                                    ),
                                  ),
                                ),
                                const SizedBox(width: 8),
                                Expanded(
                                  child: Text(
                                    instrumentLabel[i]!,
                                    style: TextStyle(
                                      color: done
                                          ? const Color(0xFF44FF88)
                                          : act
                                              ? Colors.white
                                              : Colors.white70,
                                      fontSize: 11,
                                      fontWeight: FontWeight.w600,
                                    ),
                                  ),
                                ),
                                Container(
                                  padding: const EdgeInsets.symmetric(
                                      horizontal: 6, vertical: 2),
                                  decoration: BoxDecoration(
                                    color: done
                                        ? const Color(0xFF44FF88)
                                            .withValues(alpha: 0.15)
                                        : Colors.white.withValues(alpha: 0.06),
                                    borderRadius: BorderRadius.circular(4),
                                  ),
                                  child: Text(
                                    '+$pts pts',
                                    style: TextStyle(
                                      color: done
                                          ? const Color(0xFF44FF88)
                                          : Colors.white38,
                                      fontSize: 9,
                                      fontWeight: FontWeight.bold,
                                    ),
                                  ),
                                ),
                                const SizedBox(width: 6),
                                Text(
                                  '${powerW.round()}W',
                                  style: const TextStyle(
                                      color: Colors.white24, fontSize: 9),
                                ),
                              ],
                            ),
                            const SizedBox(height: 6),
                            ClipRRect(
                              borderRadius: BorderRadius.circular(3),
                              child: LinearProgressIndicator(
                                value: prog,
                                minHeight: 4,
                                backgroundColor: Colors.white10,
                                color: done
                                    ? const Color(0xFF44FF88)
                                    : act
                                        ? Colors.cyanAccent
                                        : Colors.white24,
                              ),
                            ),
                            if (act && !done && !canActivate)
                              Padding(
                                padding: const EdgeInsets.only(top: 4),
                                child: Text(
                                  'Potencia insuficiente',
                                  style: TextStyle(
                                    color: Colors.redAccent.withValues(alpha: 0.70),
                                    fontSize: 8,
                                  ),
                                ),
                              ),
                          ],
                        ),
                      );
                    }).toList(),
                  ),
                ),

                // ── Transmission progress ──────────────────────────────────────
                if (_transmitting || _txDone)
                  Container(
                    margin: const EdgeInsets.fromLTRB(12, 0, 12, 6),
                    padding: const EdgeInsets.all(10),
                    decoration: BoxDecoration(
                      color: const Color(0xFF001015),
                      border: Border.all(
                          color: Colors.cyanAccent.withValues(alpha: 0.25)),
                      borderRadius: BorderRadius.circular(6),
                    ),
                    child: Row(
                      children: [
                        Icon(
                          _txDone ? Icons.check_circle_outline : Icons.cell_tower,
                          color: _txDone
                              ? const Color(0xFF44FF88)
                              : Colors.cyanAccent,
                          size: 14,
                        ),
                        const SizedBox(width: 8),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                _txDone
                                    ? 'TRANSMISIÓN COMPLETADA  ✓'
                                    : 'TRANSMITIENDO HACIA TIERRA...',
                                style: TextStyle(
                                  color: _txDone
                                      ? const Color(0xFF44FF88)
                                      : Colors.cyanAccent,
                                  fontSize: 9,
                                  letterSpacing: 1.5,
                                ),
                              ),
                              const SizedBox(height: 4),
                              ClipRRect(
                                borderRadius: BorderRadius.circular(3),
                                child: LinearProgressIndicator(
                                  value: _txProgress,
                                  minHeight: 4,
                                  backgroundColor: Colors.white10,
                                  color: _txDone
                                      ? const Color(0xFF44FF88)
                                      : Colors.cyanAccent,
                                ),
                              ),
                            ],
                          ),
                        ),
                        const SizedBox(width: 8),
                        Text(
                          '${(_txProgress * 100).round()}%',
                          style: const TextStyle(
                              color: Colors.white54, fontSize: 9),
                        ),
                      ],
                    ),
                  ),

                // ── Bottom action bar ─────────────────────────────────────────
                Container(
                  padding: const EdgeInsets.fromLTRB(14, 10, 14, 16),
                  decoration: const BoxDecoration(
                    border: Border(
                        top: BorderSide(color: Color(0xFF1A2A35))),
                  ),
                  child: Row(
                    children: [
                      Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          const Text('CIENCIA',
                              style: TextStyle(
                                  color: Colors.white38,
                                  fontSize: 8,
                                  letterSpacing: 1.5)),
                          RichText(
                            text: TextSpan(children: [
                              TextSpan(
                                text: '$sciPts',
                                style: const TextStyle(
                                  color: Color(0xFF44FF88),
                                  fontSize: 20,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                              TextSpan(
                                text: ' / ${widget.design.maxSciencePoints}',
                                style: const TextStyle(
                                    color: Colors.white38, fontSize: 12),
                              ),
                              const TextSpan(
                                text: ' pts',
                                style: TextStyle(
                                    color: Colors.white24, fontSize: 9),
                              ),
                            ]),
                          ),
                        ],
                      ),
                      const Spacer(),
                      if (sciPts > 0 && !_transmitting && !_txDone)
                        _ActionButton(
                          label: 'TRANSMITIR',
                          icon: Icons.cell_tower,
                          color: Colors.cyanAccent,
                          onTap: () => setState(() {
                            _transmitting = true;
                            _txProgress = 0.0;
                          }),
                        ),
                      if (sciPts > 0) const SizedBox(width: 10),
                      _ActionButton(
                        label: 'COMPLETAR MISIÓN',
                        icon: Icons.rocket_launch_rounded,
                        color: sciPts > 0
                            ? const Color(0xFF44FF88)
                            : Colors.white30,
                        onTap: sciPts > 0
                            ? () => widget.onComplete(sciPts)
                            : null,
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  String _formatRate(double kbps) {
    if (kbps >= 1000) {
      return '${(kbps / 1000).toStringAsFixed(1)} Mbps';
    }
    return '${kbps.toStringAsFixed(1)} kbps';
  }
}

// ── Sub-widgets ───────────────────────────────────────────────────────────────

class _Label extends StatelessWidget {
  final String title, value;
  final Color color;
  const _Label(this.title, this.value, this.color);

  @override
  Widget build(BuildContext context) => Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(title,
              style: TextStyle(color: color, fontSize: 7, letterSpacing: 1.2)),
          Text(value,
              style: const TextStyle(
                  color: Colors.white70,
                  fontSize: 9,
                  fontWeight: FontWeight.w600)),
        ],
      );
}

class _ActionButton extends StatelessWidget {
  final String label;
  final IconData icon;
  final Color color;
  final VoidCallback? onTap;
  const _ActionButton(
      {required this.label,
      required this.icon,
      required this.color,
      this.onTap});

  @override
  Widget build(BuildContext context) => GestureDetector(
        onTap: onTap,
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 200),
          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 9),
          decoration: BoxDecoration(
            color: onTap != null
                ? color.withValues(alpha: 0.12)
                : Colors.white.withValues(alpha: 0.04),
            border: Border.all(
                color: onTap != null ? color.withValues(alpha: 0.60) : Colors.white12),
            borderRadius: BorderRadius.circular(6),
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(icon, color: onTap != null ? color : Colors.white24, size: 14),
              const SizedBox(width: 6),
              Text(
                label,
                style: TextStyle(
                  color: onTap != null ? color : Colors.white24,
                  fontSize: 10,
                  fontWeight: FontWeight.bold,
                  letterSpacing: 1.5,
                ),
              ),
            ],
          ),
        ),
      );
}

class _SciStarBgPainter extends CustomPainter {
  static final _rng = math.Random(77);
  static final _stars =
      List.generate(120, (_) => Offset(_rng.nextDouble(), _rng.nextDouble()));
  static final _sizes = List.generate(120, (_) => 0.5 + _rng.nextDouble() * 1.5);
  static final _alphas =
      List.generate(120, (_) => 0.08 + _rng.nextDouble() * 0.22);

  @override
  void paint(Canvas canvas, Size size) {
    for (var i = 0; i < _stars.length; i++) {
      canvas.drawCircle(
        Offset(_stars[i].dx * size.width, _stars[i].dy * size.height),
        _sizes[i],
        Paint()..color = Colors.white.withValues(alpha: _alphas[i]),
      );
    }
  }

  @override
  bool shouldRepaint(covariant CustomPainter old) => false;
}
