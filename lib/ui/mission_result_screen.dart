import 'dart:async';
import 'dart:math' as math;
import 'package:flutter/material.dart';

import '../l10n/app_localizations.dart';

// ── Datos del resultado ────────────────────────────────────────────────────
class MissionResult {
  final bool success;
  final int score;
  final double fuelFraction;
  final Duration elapsed;
  final double boostSeconds;
  final double arrivalDistance;
  final String? planetId;   // for localized name + educational fact on arrival

  const MissionResult({
    required this.success,
    required this.score,
    required this.fuelFraction,
    required this.elapsed,
    required this.boostSeconds,
    required this.arrivalDistance,
    this.planetId,
  });

  int get fuelBonus    => (fuelFraction * 3000).round();
  int get timeBonus    => (2000 - elapsed.inSeconds * 4).clamp(0, 2000).toInt();
  int get boostPenalty => (boostSeconds * 30).round().clamp(0, 1500);
}

// ── Overlay animado ────────────────────────────────────────────────────────
class MissionResultOverlay extends StatefulWidget {
  final MissionResult result;
  final Map<String, int> remoteScores;
  final VoidCallback onNewMission;
  final VoidCallback onMainMenu;

  const MissionResultOverlay({
    super.key,
    required this.result,
    required this.onNewMission,
    required this.onMainMenu,
    this.remoteScores = const {},
  });

  @override
  State<MissionResultOverlay> createState() => _MissionResultOverlayState();
}

class _MissionResultOverlayState extends State<MissionResultOverlay>
    with SingleTickerProviderStateMixin {
  late AnimationController _ctrl;
  late Animation<double> _slide;
  late Animation<double> _fade;
  int _displayScore = 0;

  @override
  void initState() {
    super.initState();
    _ctrl = AnimationController(
        vsync: this, duration: const Duration(milliseconds: 700));
    _slide = Tween<double>(begin: 80, end: 0)
        .animate(CurvedAnimation(parent: _ctrl, curve: Curves.easeOutCubic));
    _fade = CurvedAnimation(parent: _ctrl, curve: Curves.easeIn);
    _ctrl.forward().then((_) => _animateScore());
  }

  void _animateScore() {
    if (!widget.result.success || widget.result.score == 0) return;
    const steps = 45;
    int step = 0;
    Timer.periodic(const Duration(milliseconds: 25), (t) {
      step++;
      final ease = 1 - math.pow(1 - step / steps, 3).toDouble();
      if (mounted) setState(() => _displayScore = (ease * widget.result.score).round());
      if (step >= steps) t.cancel();
    });
  }

  @override
  void dispose() { _ctrl.dispose(); super.dispose(); }

  @override
  Widget build(BuildContext context) {
    final s      = AppLocalizations.of(context);
    final r      = widget.result;
    final accent = r.success ? const Color(0xFF00E5FF) : const Color(0xFFFF6B35);
    final mins   = r.elapsed.inMinutes;
    final secs   = r.elapsed.inSeconds % 60;
    final fuelPct = (r.fuelFraction * 100).round();

    // Planet name + educational fact (shown only on success with a target)
    final String? localPlanetName = r.planetId != null ? s.planetName(r.planetId!) : null;
    final List<String> arrivalFacts =
        r.planetId != null ? s.planetFacts(r.planetId!) : [];
    final String? arrivalFact =
        arrivalFacts.isNotEmpty ? arrivalFacts[math.Random().nextInt(arrivalFacts.length)] : null;

    return AnimatedBuilder(
      animation: _ctrl,
      builder: (_, child) => Opacity(
        opacity: _fade.value,
        child: Transform.translate(offset: Offset(0, _slide.value), child: child),
      ),
      child: Container(
        color: Colors.black.withValues(alpha: 0.88),
        child: SafeArea(
          child: Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 500),
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(32),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    // ── Badge ─────────────────────────────────────
                    _TitleBadge(
                      text: r.success ? s.resultComplete : s.resultFailed,
                      accent: accent,
                      planetName: localPlanetName,
                    ),
                    const SizedBox(height: 28),

                    if (r.success) ...[
                      // ── Score animado ──────────────────────────
                      _GlowText(
                        text: _displayScore.toString().padLeft(5, '0'),
                        color: accent, fontSize: 52),
                      Text(s.resultPoints, style: const TextStyle(
                        color: Colors.white38, fontSize: 10, letterSpacing: 6)),
                      const SizedBox(height: 24),

                      // ── Desglose ───────────────────────────────
                      _Breakdown(rows: [
                        _BRow(s.resultBase,                             5000,           '+', Colors.white70),
                        _BRow('${s.resultFuelLabel} ($fuelPct%)',        r.fuelBonus,    '+', const Color(0xFF00E5FF)),
                        _BRow('${s.resultTimeLabel} (${mins}m ${secs}s)', r.timeBonus,   '+', Colors.white70),
                        if (r.boostPenalty > 0)
                          _BRow('${s.resultBoostLabel} (${r.boostSeconds.toStringAsFixed(0)}s)',
                                                                         r.boostPenalty, '−', const Color(0xFFFF6B35)),
                      ]),

                      // ── Hecho educativo al llegar ──────────────
                      if (arrivalFact != null) ...[
                        const SizedBox(height: 16),
                        _FactBubble(fact: arrivalFact, accent: accent),
                      ],
                    ] else ...[
                      // ── Fallo ──────────────────────────────────
                      Text(s.resultNoFuel, style: TextStyle(
                        color: accent, fontSize: 18,
                        fontWeight: FontWeight.w900, letterSpacing: 4)),
                      const SizedBox(height: 12),
                      Text(
                        '${((1 - r.arrivalDistance / 1000) * 100).clamp(0, 100).toStringAsFixed(0)}%'
                        ' ${s.resultProgress}',
                        style: const TextStyle(color: Colors.white54, fontSize: 13)),
                    ],

                    const SizedBox(height: 24),
                    _StatsRow(
                      elapsed: r.elapsed,
                      fuelFraction: r.fuelFraction,
                      arrDist: r.arrivalDistance,
                      s: s,
                    ),

                    // ── Ranking multijugador ───────────────────────
                    if (widget.remoteScores.isNotEmpty) ...[
                      const SizedBox(height: 16),
                      _ScoreboardSection(
                        localScore:   r.score,
                        remoteScores: widget.remoteScores,
                        s:            s,
                      ),
                    ],

                    const SizedBox(height: 32),

                    // ── Botones ────────────────────────────────────
                    Row(children: [
                      Expanded(
                        child: OutlinedButton(
                          onPressed: widget.onMainMenu,
                          style: OutlinedButton.styleFrom(
                            foregroundColor: Colors.white54,
                            side: const BorderSide(color: Colors.white24),
                            padding: const EdgeInsets.symmetric(vertical: 16),
                            shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(12)),
                          ),
                          child: Text(s.btnMenu,
                            style: const TextStyle(letterSpacing: 3, fontSize: 12)),
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        flex: 2,
                        child: ElevatedButton(
                          onPressed: widget.onNewMission,
                          style: ElevatedButton.styleFrom(
                            backgroundColor: accent,
                            foregroundColor: Colors.black,
                            padding: const EdgeInsets.symmetric(vertical: 16),
                            shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(12)),
                          ),
                          child: Text(
                            r.success ? s.btnNewMission : s.btnRetry,
                            style: const TextStyle(
                              fontWeight: FontWeight.w900, letterSpacing: 3)),
                        ),
                      ),
                    ]),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

// ── Sub-widgets ────────────────────────────────────────────────────────────

class _TitleBadge extends StatelessWidget {
  final String text;
  final Color accent;
  final String? planetName;
  const _TitleBadge({required this.text, required this.accent, this.planetName});

  @override
  Widget build(BuildContext context) => Column(children: [
    Container(
      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 10),
      decoration: BoxDecoration(
        color: accent.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: accent.withValues(alpha: 0.5)),
      ),
      child: Text(text, style: TextStyle(
        color: accent, fontSize: 14,
        fontWeight: FontWeight.w900, letterSpacing: 4)),
    ),
    if (planetName != null) ...[
      const SizedBox(height: 8),
      Text(planetName!.toUpperCase(),
        style: const TextStyle(color: Colors.white38, fontSize: 10, letterSpacing: 4)),
    ],
  ]);
}

class _GlowText extends StatelessWidget {
  final String text;
  final Color color;
  final double fontSize;
  const _GlowText({required this.text, required this.color, required this.fontSize});

  @override
  Widget build(BuildContext context) => Text(text,
    style: TextStyle(
      color: color, fontSize: fontSize, fontWeight: FontWeight.w900, letterSpacing: 4,
      shadows: [Shadow(color: color.withValues(alpha: 0.5), blurRadius: 24)],
    ));
}

class _BRow {
  final String label;
  final int value;
  final String sign;
  final Color color;
  const _BRow(this.label, this.value, this.sign, this.color);
}

class _Breakdown extends StatelessWidget {
  final List<_BRow> rows;
  const _Breakdown({required this.rows});

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.all(16),
    decoration: BoxDecoration(
      color: Colors.white.withValues(alpha: 0.04),
      borderRadius: BorderRadius.circular(12),
      border: Border.all(color: Colors.white12),
    ),
    child: Column(
      children: rows.map((r) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 5),
        child: Row(children: [
          Expanded(child: Text(r.label,
            style: const TextStyle(color: Colors.white54, fontSize: 12))),
          Text('${r.sign}${r.value}', style: TextStyle(
            color: r.color, fontSize: 13,
            fontWeight: FontWeight.w700, letterSpacing: 1)),
        ]),
      )).toList(),
    ),
  );
}

// Hecho educativo del planeta al llegar
class _FactBubble extends StatelessWidget {
  final String fact;
  final Color accent;
  const _FactBubble({required this.fact, required this.accent});

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.all(12),
    decoration: BoxDecoration(
      color: accent.withValues(alpha: 0.06),
      borderRadius: BorderRadius.circular(10),
      border: Border.all(color: accent.withValues(alpha: 0.25)),
    ),
    child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
      Icon(Icons.lightbulb_outline, color: accent, size: 14),
      const SizedBox(width: 8),
      Expanded(child: Text(fact, style: const TextStyle(
        color: Colors.white60, fontSize: 11, height: 1.5))),
    ]),
  );
}

class _StatsRow extends StatelessWidget {
  final Duration elapsed;
  final double fuelFraction;
  final double arrDist;
  final AppLocalizations s;
  const _StatsRow({
    required this.elapsed, required this.fuelFraction,
    required this.arrDist, required this.s});

  @override
  Widget build(BuildContext context) {
    final mins = elapsed.inMinutes;
    final secs = elapsed.inSeconds % 60;
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 12),
      decoration: BoxDecoration(
        border: Border.symmetric(
            horizontal: BorderSide(color: Colors.white.withValues(alpha: 0.08))),
      ),
      child: Row(mainAxisAlignment: MainAxisAlignment.spaceEvenly, children: [
        _Stat(label: s.statTime, value: '${mins}m ${secs}s'),
        _Stat(label: s.statFuel, value: '${(fuelFraction*100).round()}%'),
        _Stat(label: s.statDist, value: '${arrDist.toStringAsFixed(0)} u'),
      ]),
    );
  }
}

class _Stat extends StatelessWidget {
  final String label;
  final String value;
  const _Stat({required this.label, required this.value});

  @override
  Widget build(BuildContext context) => Column(children: [
    Text(label, style: const TextStyle(
      color: Colors.white38, fontSize: 9, letterSpacing: 2)),
    const SizedBox(height: 3),
    Text(value, style: const TextStyle(
      color: Colors.white, fontSize: 15, fontWeight: FontWeight.w600)),
  ]);
}

// ── Ranking multijugador ───────────────────────────────────────────────────

class _ScoreEntry {
  final String label;
  final int score;
  final bool isLocal;
  const _ScoreEntry({required this.label, required this.score, required this.isLocal});
}

class _ScoreboardSection extends StatelessWidget {
  final int localScore;
  final Map<String, int> remoteScores;
  final AppLocalizations s;

  const _ScoreboardSection({
    required this.localScore,
    required this.remoteScores,
    required this.s,
  });

  @override
  Widget build(BuildContext context) {
    final entries = <_ScoreEntry>[
      _ScoreEntry(label: s.resultYou, score: localScore, isLocal: true),
      ...remoteScores.entries.map((e) {
        final lbl = e.key.length > 8 ? e.key.substring(0, 8) : e.key;
        return _ScoreEntry(label: lbl, score: e.value, isLocal: false);
      }),
    ]..sort((a, b) => b.score.compareTo(a.score));

    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: 0.04),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: Colors.white12),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(s.resultScoreboard,
            style: const TextStyle(
              color: Colors.white38, fontSize: 9, letterSpacing: 3)),
          const SizedBox(height: 10),
          ...entries.asMap().entries.map((e) {
            final rank  = e.key + 1;
            final entry = e.value;
            final rankColor = rank == 1
                ? const Color(0xFFFFD700)
                : rank == 2
                    ? const Color(0xFFCCCCCC)
                    : rank == 3
                        ? const Color(0xFFCD7F32)
                        : Colors.white38;
            return Padding(
              padding: const EdgeInsets.symmetric(vertical: 4),
              child: Row(children: [
                SizedBox(
                  width: 22,
                  child: Text('#$rank', style: TextStyle(
                    color: rankColor, fontSize: 11, fontWeight: FontWeight.bold)),
                ),
                Expanded(
                  child: Text(entry.label,
                    style: TextStyle(
                      color: entry.isLocal ? const Color(0xFF00E5FF) : Colors.white70,
                      fontSize: 12,
                      fontWeight: entry.isLocal ? FontWeight.bold : FontWeight.normal,
                    )),
                ),
                Text('${entry.score}', style: TextStyle(
                  color: rankColor, fontSize: 13, fontWeight: FontWeight.w700,
                  letterSpacing: 1,
                )),
              ]),
            );
          }),
        ],
      ),
    );
  }
}
