import 'package:flutter/material.dart';
import '../models/planet_info.dart';
import '../l10n/app_localizations.dart';

class MissionSelectScreen extends StatefulWidget {
  final void Function(PlanetInfo planet) onMissionSelected;
  final VoidCallback onFreeMode;

  const MissionSelectScreen({
    super.key,
    required this.onMissionSelected,
    required this.onFreeMode,
  });

  @override
  State<MissionSelectScreen> createState() => _MissionSelectScreenState();
}

class _MissionSelectScreenState extends State<MissionSelectScreen> {
  PlanetInfo? _selected;

  @override
  Widget build(BuildContext context) {
    final s = AppLocalizations.of(context);
    return Scaffold(
      backgroundColor: const Color(0xFF010408),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(s.selectTitle,
                style: const TextStyle(
                  color: Color(0xFF00E5FF), fontSize: 18,
                  fontWeight: FontWeight.w900, letterSpacing: 6)),
              const SizedBox(height: 4),
              Text(s.selectSubtitle,
                style: const TextStyle(color: Colors.white38, fontSize: 12)),
              const SizedBox(height: 24),

              Expanded(
                child: GridView.builder(
                  gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                    crossAxisCount: 2,
                    crossAxisSpacing: 12,
                    mainAxisSpacing: 12,
                    childAspectRatio: 1.3,
                  ),
                  itemCount: kPlanets.length,
                  itemBuilder: (ctx, i) {
                    final planet   = kPlanets[i];
                    final selected = _selected?.id == planet.id;
                    final color    = Color(int.parse(
                        'FF${planet.colorHex.replaceAll('#', '')}', radix: 16));
                    return GestureDetector(
                      onTap: () => setState(() => _selected = planet),
                      child: AnimatedContainer(
                        duration: const Duration(milliseconds: 150),
                        decoration: BoxDecoration(
                          color: selected
                              ? color.withValues(alpha: 0.2)
                              : Colors.white.withValues(alpha: 0.04),
                          borderRadius: BorderRadius.circular(14),
                          border: Border.all(
                            color: selected ? color : Colors.white12,
                            width: selected ? 2 : 1,
                          ),
                        ),
                        padding: const EdgeInsets.all(14),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Row(children: [
                              Container(
                                width: 12, height: 12,
                                decoration: BoxDecoration(
                                  shape: BoxShape.circle, color: color)),
                              const SizedBox(width: 8),
                              Text(s.planetName(planet.id),
                                style: const TextStyle(
                                  color: Colors.white,
                                  fontWeight: FontWeight.bold, fontSize: 14)),
                            ]),
                            Text(s.planetDescription(planet.id),
                              style: const TextStyle(
                                color: Colors.white38, fontSize: 9, height: 1.4),
                              maxLines: 2, overflow: TextOverflow.ellipsis),
                          ],
                        ),
                      ),
                    );
                  },
                ),
              ),

              // ── Cuadro educativo (aparece al seleccionar) ────
              if (_selected != null) ...[
                const SizedBox(height: 12),
                _FactsCard(
                  key: ValueKey(_selected!.id),
                  label: s.eduFactsLabel,
                  facts: s.planetFacts(_selected!.id),
                  color: Color(int.parse(
                      'FF${_selected!.colorHex.replaceAll('#', '')}', radix: 16)),
                ),
              ],

              const SizedBox(height: 12),

              Row(children: [
                Expanded(
                  child: OutlinedButton(
                    onPressed: widget.onFreeMode,
                    style: OutlinedButton.styleFrom(
                      foregroundColor: Colors.white54,
                      side: const BorderSide(color: Colors.white24),
                      padding: const EdgeInsets.symmetric(vertical: 16),
                      shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12)),
                    ),
                    child: Text(s.btnFreeMode,
                      style: const TextStyle(letterSpacing: 2, fontSize: 12)),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: ElevatedButton(
                    onPressed: _selected == null
                        ? null
                        : () => widget.onMissionSelected(_selected!),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: const Color(0xFF00E5FF),
                      foregroundColor: Colors.black,
                      disabledBackgroundColor: Colors.white12,
                      padding: const EdgeInsets.symmetric(vertical: 16),
                      shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12)),
                    ),
                    child: Text(s.btnLaunch,
                      style: const TextStyle(
                        fontWeight: FontWeight.w900, letterSpacing: 3, fontSize: 14)),
                  ),
                ),
              ]),
            ],
          ),
        ),
      ),
    );
  }
}

// ── Tarjeta de datos educativos ────────────────────────────────────────────
class _FactsCard extends StatelessWidget {
  final String label;
  final List<String> facts;
  final Color color;

  const _FactsCard({
    super.key,
    required this.label,
    required this.facts,
    required this.color,
  });

  @override
  Widget build(BuildContext context) {
    return AnimatedSwitcher(
      duration: const Duration(milliseconds: 250),
      child: Container(
        width: double.infinity,
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(
          color: color.withValues(alpha: 0.06),
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: color.withValues(alpha: 0.25)),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(children: [
              Icon(Icons.lightbulb_outline, color: color, size: 13),
              const SizedBox(width: 6),
              Text(label, style: TextStyle(
                color: color, fontSize: 9,
                fontWeight: FontWeight.w800, letterSpacing: 2)),
            ]),
            const SizedBox(height: 8),
            ...facts.map((f) => Padding(
              padding: const EdgeInsets.only(bottom: 4),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('• ', style: TextStyle(color: color, fontSize: 11)),
                  Expanded(child: Text(f,
                    style: const TextStyle(
                      color: Colors.white60, fontSize: 11, height: 1.4))),
                ],
              ),
            )),
          ],
        ),
      ),
    );
  }
}
