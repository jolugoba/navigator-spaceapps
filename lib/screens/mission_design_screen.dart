import 'dart:math' as math;
import 'package:flutter/material.dart';

import '../models/mission_design.dart';
import '../models/planet_info.dart';

// ─────────────────────────────────────────────────────────────────────────────
// MissionDesignScreen
// NASA-style mission trade-off designer before launch
// ─────────────────────────────────────────────────────────────────────────────

class MissionDesignScreen extends StatefulWidget {
  final PlanetInfo planet;
  final void Function(MissionDesign design) onLaunch;
  final VoidCallback onBack;

  const MissionDesignScreen({
    super.key,
    required this.planet,
    required this.onLaunch,
    required this.onBack,
  });

  @override
  State<MissionDesignScreen> createState() => _MissionDesignScreenState();
}

class _MissionDesignScreenState extends State<MissionDesignScreen>
    with TickerProviderStateMixin {
  late MissionDesign _design;
  late AnimationController _pulseCtrl;

  @override
  void initState() {
    super.initState();
    _design = MissionDesign.defaultFor(widget.planet.id);
    _pulseCtrl = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1200),
    )..repeat(reverse: true);
  }

  @override
  void dispose() {
    _pulseCtrl.dispose();
    super.dispose();
  }

  void _update(MissionDesign d) => setState(() => _design = d);

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF010408),
      body: Stack(
        children: [
          // Star field background
          const _StarBackground(),

          // Main content
          SafeArea(
            child: Column(
              children: [
                _buildHeader(),
                Expanded(
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      // Left: configuration panels
                      Expanded(
                        flex: 3,
                        child: SingleChildScrollView(
                          padding: const EdgeInsets.fromLTRB(16, 8, 8, 8),
                          child: Column(
                            children: [
                              _buildObjectivePanel(),
                              const SizedBox(height: 10),
                              _buildPropulsionPanel(),
                              const SizedBox(height: 10),
                              _buildPowerPanel(),
                              const SizedBox(height: 10),
                              _buildAntennaPanel(),
                              const SizedBox(height: 10),
                              _buildLaunchVehiclePanel(),
                            ],
                          ),
                        ),
                      ),

                      // Right: instruments + summary
                      Expanded(
                        flex: 2,
                        child: SingleChildScrollView(
                          padding: const EdgeInsets.fromLTRB(8, 8, 16, 8),
                          child: Column(
                            children: [
                              _buildInstrumentsPanel(),
                              const SizedBox(height: 10),
                              _buildSummaryPanel(),
                              const SizedBox(height: 10),
                              _buildLaunchButton(),
                            ],
                          ),
                        ),
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

  // ── Header ──────────────────────────────────────────────────────────────────

  Widget _buildHeader() {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 10),
      decoration: BoxDecoration(
        border: Border(
            bottom: BorderSide(
                color: Colors.cyanAccent.withValues(alpha: 0.20))),
      ),
      child: Row(
        children: [
          GestureDetector(
            onTap: widget.onBack,
            child: const Icon(Icons.arrow_back_ios, color: Colors.cyanAccent, size: 18),
          ),
          const SizedBox(width: 16),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('DISEÑO DE MISIÓN',
                    style: TextStyle(
                        color: Colors.cyanAccent,
                        fontSize: 13,
                        fontWeight: FontWeight.bold,
                        letterSpacing: 3)),
                Text(
                  'DESTINO: ${widget.planet.name.toUpperCase()}',
                  style: TextStyle(
                      color: Colors.white.withValues(alpha: 0.55),
                      fontSize: 10,
                      letterSpacing: 2),
                ),
              ],
            ),
          ),
          // Budget indicator top-right
          _BudgetBadge(design: _design),
        ],
      ),
    );
  }

  // ── Objective ───────────────────────────────────────────────────────────────

  Widget _buildObjectivePanel() {
    return _Panel(
      title: 'OBJETIVO DE MISIÓN',
      icon: Icons.flag_outlined,
      child: Row(
        children: [
          for (final obj in ['flyby', 'orbit', 'landing'])
            Expanded(
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 4),
                child: _SelectChip(
                  label: obj == 'flyby'
                      ? 'SOBREVUELO'
                      : obj == 'orbit'
                          ? 'ÓRBITA'
                          : 'ATERRIZAJE',
                  subtitle: obj == 'flyby'
                      ? 'Menor ΔV'
                      : obj == 'orbit'
                          ? 'Más ciencia'
                          : 'Requiere lander',
                  selected: _design.objective == obj,
                  onTap: () => _update(_design.copyWith(objective: obj)),
                ),
              ),
            ),
        ],
      ),
    );
  }

  // ── Propulsion ──────────────────────────────────────────────────────────────

  Widget _buildPropulsionPanel() {
    return _Panel(
      title: 'PROPULSIÓN',
      icon: Icons.rocket_launch_outlined,
      child: Column(
        children: [
          Row(
            children: [
              for (final e in EngineType.values)
                Expanded(
                  child: Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 3),
                    child: _SelectChip(
                      label: e == EngineType.chemical
                          ? 'QUÍMICO'
                          : e == EngineType.ion
                              ? 'IÓNICO'
                              : 'NUCLEAR',
                      subtitle: '\$${engineCostM[e]!.round()}M',
                      selected: _design.engine == e,
                      onTap: () => _update(_design.copyWith(engine: e)),
                    ),
                  ),
                ),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            engineDesc[_design.engine]!,
            style: TextStyle(
                color: Colors.white.withValues(alpha: 0.55), fontSize: 9),
          ),
          const SizedBox(height: 4),
          // Fuel type
          Text(
            'Combustible: ${engineFuelType[_design.engine]!}',
            style: const TextStyle(color: Colors.orange, fontSize: 8),
          ),
          const SizedBox(height: 2),
          // NASA mission reference
          Text(
            engineNasaRef[_design.engine]!,
            style: TextStyle(
                color: Colors.cyanAccent.withValues(alpha: 0.50), fontSize: 8),
          ),
          const SizedBox(height: 10),
          Row(
            children: [
              Text('PROPELENTE',
                  style: TextStyle(
                      color: Colors.cyanAccent.withValues(alpha: 0.70),
                      fontSize: 9,
                      letterSpacing: 1.5)),
              const Spacer(),
              Text('${_design.fuelMassKg.round()} kg',
                  style: const TextStyle(
                      color: Colors.white, fontSize: 10, fontWeight: FontWeight.bold)),
            ],
          ),
          Slider(
            value: _design.fuelMassKg,
            min: 200,
            max: 8000,
            divisions: 78,
            activeColor: Colors.cyanAccent,
            inactiveColor: Colors.cyanAccent.withValues(alpha: 0.15),
            onChanged: (v) => _update(_design.copyWith(fuelMassKg: v)),
          ),
          _DeltaVBar(design: _design),
        ],
      ),
    );
  }

  // ── Power ────────────────────────────────────────────────────────────────────

  Widget _buildPowerPanel() {
    return _Panel(
      title: 'SISTEMA DE ENERGÍA',
      icon: Icons.bolt_outlined,
      child: Column(
        children: [
          Row(
            children: [
              for (final p in PowerSystem.values)
                Expanded(
                  child: Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 3),
                    child: _SelectChip(
                      label: p == PowerSystem.solar
                          ? 'SOLAR'
                          : p == PowerSystem.rtg
                              ? 'RTG'
                              : 'NUCLEAR',
                      subtitle: '${powerOutputW[p]!.round()}W',
                      selected: _design.power == p,
                      onTap: () => _update(_design.copyWith(power: p)),
                    ),
                  ),
                ),
            ],
          ),
          const SizedBox(height: 6),
          _PowerBar(design: _design),
          // NASA reference per power system
          const SizedBox(height: 4),
          Text(
            powerNasaRef[_design.power]!,
            style: TextStyle(
                color: Colors.cyanAccent.withValues(alpha: 0.50), fontSize: 8),
          ),
          // Solar degradation warning for outer planets
          if (!_design.isSolarViable) ...[
            const SizedBox(height: 6),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 5),
              decoration: BoxDecoration(
                color: Colors.orange.withValues(alpha: 0.10),
                border: Border.all(color: Colors.orange.withValues(alpha: 0.50)),
                borderRadius: BorderRadius.circular(5),
              ),
              child: Text(
                '⚠ Paneles solares inviables en '
                '${_design.targetPlanetId.toUpperCase()} '
                '(${(_design.solarEfficiency() * 100).toStringAsFixed(1)}% eficiencia). '
                'Misiones reales usan RTG.',
                style: const TextStyle(color: Colors.orange, fontSize: 8),
              ),
            ),
          ],
        ],
      ),
    );
  }

  // ── Antenna ──────────────────────────────────────────────────────────────────

  Widget _buildAntennaPanel() {
    return _Panel(
      title: 'COMUNICACIONES',
      icon: Icons.settings_input_antenna_outlined,
      child: Row(
        children: [
          for (final a in AntennaType.values)
            Expanded(
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 3),
                child: _SelectChip(
                  label: a == AntennaType.lowGain
                      ? 'OMNI'
                      : a == AntennaType.mediumGain
                          ? 'MGA'
                          : 'HGA',
                  subtitle: a == AntennaType.lowGain
                      ? '0.5 kbps'
                      : a == AntennaType.mediumGain
                          ? '100 kbps'
                          : '6 Mbps',
                  selected: _design.antenna == a,
                  onTap: () => _update(_design.copyWith(antenna: a)),
                ),
              ),
            ),
        ],
      ),
    );
  }

  // ── Launch Vehicle ───────────────────────────────────────────────────────────

  Widget _buildLaunchVehiclePanel() {
    return _Panel(
      title: 'VEHÍCULO DE LANZAMIENTO',
      icon: Icons.local_fire_department_outlined,
      child: Column(
        children: [
          Row(
            children: [
              for (final v in LaunchVehicle.values)
                Expanded(
                  child: Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 2),
                    child: _SelectChip(
                      label: v == LaunchVehicle.falcon9
                          ? 'F9'
                          : v == LaunchVehicle.falconHeavy
                              ? 'FH'
                              : v == LaunchVehicle.sls
                                  ? 'SLS'
                                  : 'STAR',
                      subtitle: '\$${launchCostM[v]!.round()}M',
                      selected: _design.launchVehicle == v,
                      onTap: () =>
                          _update(_design.copyWith(launchVehicle: v)),
                    ),
                  ),
                ),
            ],
          ),
          const SizedBox(height: 6),
          _MassBar(design: _design),
        ],
      ),
    );
  }

  // ── Science Instruments ──────────────────────────────────────────────────────

  Widget _buildInstrumentsPanel() {
    return _Panel(
      title: 'INSTRUMENTOS CIENTÍFICOS',
      icon: Icons.science_outlined,
      child: Column(
        children: [
          for (final inst in ScienceInstrument.values) ...[
            _InstrumentRow(
              instrument: inst,
              selected: _design.instruments.contains(inst),
              onToggle: (on) {
                final set = Set<ScienceInstrument>.from(_design.instruments);
                if (on) {
                  set.add(inst);
                } else {
                  set.remove(inst);
                }
                _update(_design.copyWith(instruments: set));
              },
            ),
            if (inst != ScienceInstrument.values.last)
              Divider(
                  height: 1,
                  color: Colors.white.withValues(alpha: 0.06)),
          ],
        ],
      ),
    );
  }

  // ── Summary ──────────────────────────────────────────────────────────────────

  Widget _buildSummaryPanel() {
    return _Panel(
      title: 'RESUMEN DE MISIÓN',
      icon: Icons.analytics_outlined,
      child: Column(
        children: [
          _MetricRow(
            label: 'PRESUPUESTO',
            value:
                '\$${_design.totalCostM.round()}M / \$${MissionDesign.budgetLimitM.round()}M',
            fraction: _design.totalCostM / MissionDesign.budgetLimitM,
            ok: _design.isBudgetOk,
          ),
          const SizedBox(height: 6),
          _MetricRow(
            label: 'MASA TOTAL',
            value:
                '${_design.wetMassKg.round()} kg / ${launchCapacityKg[_design.launchVehicle]!.round()} kg',
            fraction:
                _design.wetMassKg / launchCapacityKg[_design.launchVehicle]!,
            ok: _design.isMassOk,
          ),
          const SizedBox(height: 6),
          _MetricRow(
            label: 'ENERGÍA',
            value:
                '${_design.powerUsedW.round()} W / ${_design.effectivePowerW.round()} W',
            fraction: _design.powerUsedW / _design.effectivePowerW,
            ok: _design.isPowerOk,
          ),
          const SizedBox(height: 6),
          _MetricRow(
            label: 'DELTA-V',
            value:
                '${(_design.deltaVms / 1000).toStringAsFixed(1)} km/s',
            fraction: (_design.deltaVms /
                    (MissionDesign.minDeltaV[_design.targetPlanetId] ?? 9000.0))
                .clamp(0.0, 1.0),
            ok: _design.hasSufficientDeltaV,
          ),
          const SizedBox(height: 6),
          _MetricRow(
            label: 'CIENCIA POTENCIAL',
            value: '${_design.maxSciencePoints} pts',
            fraction:
                (_design.maxSciencePoints / 11600.0).clamp(0.0, 1.0),
            ok: _design.maxSciencePoints > 0,
          ),
          const SizedBox(height: 6),
          // Mass fraction — "tiranía de la ecuación del cohete"
          _MetricRow(
            label: 'FRACCIÓN COMBUSTIBLE',
            value: '${(_design.massFraction * 100).toStringAsFixed(0)}% del peso',
            fraction: _design.massFraction,
            ok: true,
          ),
          const SizedBox(height: 6),
          // Communication delay — real speed of light
          _CommDelayRow(planetId: _design.targetPlanetId),
        ],
      ),
    );
  }

  // ── Launch Button ────────────────────────────────────────────────────────────

  Widget _buildLaunchButton() {
    final ok = _design.isValid;
    final error = _design.firstError;
    return Column(
      children: [
        if (error != null)
          Container(
            margin: const EdgeInsets.only(bottom: 8),
            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
            decoration: BoxDecoration(
              color: Colors.red.withValues(alpha: 0.10),
              border: Border.all(
                  color: Colors.red.withValues(alpha: 0.50)),
              borderRadius: BorderRadius.circular(6),
            ),
            child: Text(
              error,
              style: const TextStyle(color: Colors.redAccent, fontSize: 10),
              textAlign: TextAlign.center,
            ),
          ),
        AnimatedBuilder(
          animation: _pulseCtrl,
          builder: (_, __) {
            final glow = ok ? _pulseCtrl.value * 0.35 : 0.0;
            return GestureDetector(
              onTap: ok ? () => widget.onLaunch(_design) : null,
              child: Container(
                width: double.infinity,
                padding: const EdgeInsets.symmetric(vertical: 14),
                decoration: BoxDecoration(
                  color: ok
                      ? const Color(0xFF00E5FF).withValues(alpha: 0.12 + glow)
                      : Colors.white.withValues(alpha: 0.04),
                  border: Border.all(
                    color: ok
                        ? const Color(0xFF00E5FF)
                            .withValues(alpha: 0.70 + glow)
                        : Colors.white.withValues(alpha: 0.15),
                    width: 1.5,
                  ),
                  borderRadius: BorderRadius.circular(8),
                  boxShadow: ok
                      ? [
                          BoxShadow(
                            color: const Color(0xFF00E5FF)
                                .withValues(alpha: glow * 0.8),
                            blurRadius: 20,
                            spreadRadius: 2,
                          )
                        ]
                      : [],
                ),
                child: Text(
                  ok ? '🚀  LANZAR MISIÓN' : 'CORREGIR ERRORES',
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    color: ok ? Colors.cyanAccent : Colors.white30,
                    fontSize: 13,
                    fontWeight: FontWeight.bold,
                    letterSpacing: 2.5,
                  ),
                ),
              ),
            );
          },
        ),
      ],
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Sub-widgets
// ─────────────────────────────────────────────────────────────────────────────

class _Panel extends StatelessWidget {
  final String title;
  final IconData icon;
  final Widget child;

  const _Panel(
      {required this.title, required this.icon, required this.child});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: const Color(0xFF060D18),
        border: Border.all(
            color: Colors.cyanAccent.withValues(alpha: 0.12)),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(icon, color: Colors.cyanAccent.withValues(alpha: 0.70), size: 14),
              const SizedBox(width: 8),
              Text(
                title,
                style: TextStyle(
                    color: Colors.cyanAccent.withValues(alpha: 0.80),
                    fontSize: 9,
                    fontWeight: FontWeight.bold,
                    letterSpacing: 2),
              ),
            ],
          ),
          const SizedBox(height: 10),
          child,
        ],
      ),
    );
  }
}

class _SelectChip extends StatelessWidget {
  final String label;
  final String subtitle;
  final bool selected;
  final VoidCallback onTap;

  const _SelectChip({
    required this.label,
    required this.subtitle,
    required this.selected,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 180),
        padding: const EdgeInsets.symmetric(vertical: 8, horizontal: 4),
        decoration: BoxDecoration(
          color: selected
              ? Colors.cyanAccent.withValues(alpha: 0.14)
              : Colors.white.withValues(alpha: 0.03),
          border: Border.all(
            color: selected
                ? Colors.cyanAccent.withValues(alpha: 0.80)
                : Colors.white.withValues(alpha: 0.12),
          ),
          borderRadius: BorderRadius.circular(6),
        ),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Text(
              label,
              textAlign: TextAlign.center,
              style: TextStyle(
                color: selected ? Colors.cyanAccent : Colors.white60,
                fontSize: 9,
                fontWeight: FontWeight.bold,
                letterSpacing: 0.8,
              ),
            ),
            const SizedBox(height: 2),
            Text(
              subtitle,
              style: TextStyle(
                  color: selected
                      ? Colors.white.withValues(alpha: 0.65)
                      : Colors.white30,
                  fontSize: 8),
            ),
          ],
        ),
      ),
    );
  }
}

class _MetricRow extends StatelessWidget {
  final String label;
  final String value;
  final double fraction;
  final bool ok;

  const _MetricRow({
    required this.label,
    required this.value,
    required this.fraction,
    required this.ok,
  });

  @override
  Widget build(BuildContext context) {
    final col = ok ? Colors.cyanAccent : Colors.redAccent;
    return Column(
      children: [
        Row(
          children: [
            Text(label,
                style: TextStyle(
                    color: Colors.white.withValues(alpha: 0.50),
                    fontSize: 8,
                    letterSpacing: 1.5)),
            const Spacer(),
            Row(children: [
              Icon(ok ? Icons.check_circle_outline : Icons.error_outline,
                  color: col, size: 10),
              const SizedBox(width: 4),
              Text(value,
                  style: TextStyle(
                      color: col,
                      fontSize: 9,
                      fontWeight: FontWeight.bold)),
            ]),
          ],
        ),
        const SizedBox(height: 4),
        ClipRRect(
          borderRadius: BorderRadius.circular(2),
          child: LinearProgressIndicator(
            value: fraction.clamp(0.0, 1.0),
            minHeight: 3,
            backgroundColor: Colors.white.withValues(alpha: 0.08),
            valueColor: AlwaysStoppedAnimation(
                fraction > 1.0
                    ? Colors.red
                    : ok
                        ? col
                        : Colors.orange),
          ),
        ),
      ],
    );
  }
}

class _InstrumentRow extends StatelessWidget {
  final ScienceInstrument instrument;
  final bool selected;
  final ValueChanged<bool> onToggle;

  const _InstrumentRow({
    required this.instrument,
    required this.selected,
    required this.onToggle,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: () => onToggle(!selected),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 5),
        child: Row(
          children: [
            AnimatedContainer(
              duration: const Duration(milliseconds: 160),
              width: 12, height: 12,
              decoration: BoxDecoration(
                color: selected
                    ? Colors.cyanAccent
                    : Colors.transparent,
                border: Border.all(
                    color: selected
                        ? Colors.cyanAccent
                        : Colors.white24),
                borderRadius: BorderRadius.circular(3),
              ),
              child: selected
                  ? const Icon(Icons.check, size: 9, color: Colors.black)
                  : null,
            ),
            const SizedBox(width: 8),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    instrumentLabel[instrument]!,
                    style: TextStyle(
                        color: selected ? Colors.white : Colors.white54,
                        fontSize: 9,
                        fontWeight: FontWeight.bold),
                  ),
                  Text(
                    instrumentDesc[instrument]!,
                    style: TextStyle(
                        color: Colors.white.withValues(alpha: 0.30),
                        fontSize: 8),
                  ),
                ],
              ),
            ),
            const SizedBox(width: 6),
            Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Text(
                  '\$${instrumentCostM[instrument]!.round()}M',
                  style: const TextStyle(color: Colors.orange, fontSize: 8),
                ),
                Text(
                  '${instrumentMassKg[instrument]!.round()}kg',
                  style: const TextStyle(color: Colors.white38, fontSize: 8),
                ),
                Text(
                  '+${instrumentSciencePoints[instrument]!}pt',
                  style: TextStyle(
                      color: selected
                          ? Colors.cyanAccent
                          : Colors.white24,
                      fontSize: 8,
                      fontWeight: FontWeight.bold),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _DeltaVBar extends StatelessWidget {
  final MissionDesign design;
  const _DeltaVBar({required this.design});

  @override
  Widget build(BuildContext context) {
    final needed = MissionDesign.minDeltaV[design.targetPlanetId] ?? 9000.0;
    final have = design.deltaVms;
    final frac = (have / needed).clamp(0.0, 1.5);
    final ok = have >= needed;
    return Column(
      children: [
        Row(
          children: [
            Text('ΔV DISPONIBLE',
                style: TextStyle(
                    color: Colors.white.withValues(alpha: 0.45),
                    fontSize: 8,
                    letterSpacing: 1.5)),
            const Spacer(),
            Text(
              '${(have / 1000).toStringAsFixed(1)} / ${(needed / 1000).toStringAsFixed(1)} km/s',
              style: TextStyle(
                  color: ok ? Colors.cyanAccent : Colors.redAccent,
                  fontSize: 9,
                  fontWeight: FontWeight.bold),
            ),
          ],
        ),
        const SizedBox(height: 4),
        Stack(
          children: [
            ClipRRect(
              borderRadius: BorderRadius.circular(2),
              child: LinearProgressIndicator(
                value: (frac / 1.5).clamp(0, 1),
                minHeight: 5,
                backgroundColor: Colors.white.withValues(alpha: 0.08),
                valueColor:
                    AlwaysStoppedAnimation(ok ? Colors.cyanAccent : Colors.red),
              ),
            ),
            // "Minimum" marker
            Positioned(
              left: MediaQuery.sizeOf(context).width * 0,
              child: Container(),
            ),
          ],
        ),
        if (!ok)
          Padding(
            padding: const EdgeInsets.only(top: 4),
            child: Text(
              '⚠ ΔV insuficiente para ${design.targetPlanetId.toUpperCase()}. Añade más propelente o cambia motor.',
              style: const TextStyle(color: Colors.orange, fontSize: 8),
            ),
          ),
      ],
    );
  }
}

class _PowerBar extends StatelessWidget {
  final MissionDesign design;
  const _PowerBar({required this.design});

  @override
  Widget build(BuildContext context) {
    final used = design.powerUsedW;
    final avail = design.effectivePowerW;
    final frac = used / avail;
    return Column(
      children: [
        Row(
          children: [
            Text('CONSUMO',
                style: TextStyle(
                    color: Colors.white.withValues(alpha: 0.45),
                    fontSize: 8,
                    letterSpacing: 1.5)),
            const Spacer(),
            Text(
              '${used.round()} / ${avail.round()} W',
              style: TextStyle(
                  color: frac <= 1 ? const Color(0xFF44FF88) : Colors.redAccent,
                  fontSize: 9,
                  fontWeight: FontWeight.bold),
            ),
          ],
        ),
        const SizedBox(height: 4),
        ClipRRect(
          borderRadius: BorderRadius.circular(2),
          child: LinearProgressIndicator(
            value: frac.clamp(0.0, 1.0),
            minHeight: 5,
            backgroundColor: Colors.white.withValues(alpha: 0.08),
            valueColor: AlwaysStoppedAnimation(
                frac > 1.0 ? Colors.red : const Color(0xFF44FF88)),
          ),
        ),
      ],
    );
  }
}

class _MassBar extends StatelessWidget {
  final MissionDesign design;
  const _MassBar({required this.design});

  @override
  Widget build(BuildContext context) {
    final wet = design.wetMassKg;
    final cap = launchCapacityKg[design.launchVehicle]!;
    final frac = wet / cap;
    return Column(
      children: [
        Row(
          children: [
            Text('MASA TOTAL',
                style: TextStyle(
                    color: Colors.white.withValues(alpha: 0.45),
                    fontSize: 8,
                    letterSpacing: 1.5)),
            const Spacer(),
            Text(
              '${wet.round()} / ${cap.round()} kg',
              style: TextStyle(
                  color: frac <= 1 ? Colors.cyanAccent : Colors.redAccent,
                  fontSize: 9,
                  fontWeight: FontWeight.bold),
            ),
          ],
        ),
        const SizedBox(height: 4),
        ClipRRect(
          borderRadius: BorderRadius.circular(2),
          child: LinearProgressIndicator(
            value: frac.clamp(0.0, 1.0),
            minHeight: 5,
            backgroundColor: Colors.white.withValues(alpha: 0.08),
            valueColor: AlwaysStoppedAnimation(
                frac > 1.0 ? Colors.red : Colors.cyanAccent),
          ),
        ),
      ],
    );
  }
}

class _BudgetBadge extends StatelessWidget {
  final MissionDesign design;
  const _BudgetBadge({required this.design});

  @override
  Widget build(BuildContext context) {
    final ok = design.isBudgetOk;
    final pct =
        (design.totalCostM / MissionDesign.budgetLimitM * 100).round();
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(
        color: (ok ? Colors.cyanAccent : Colors.red).withValues(alpha: 0.10),
        border: Border.all(
            color: (ok ? Colors.cyanAccent : Colors.red)
                .withValues(alpha: 0.50)),
        borderRadius: BorderRadius.circular(6),
      ),
      child: Text(
        '\$${design.totalCostM.round()}M  ($pct%)',
        style: TextStyle(
          color: ok ? Colors.cyanAccent : Colors.redAccent,
          fontSize: 10,
          fontWeight: FontWeight.bold,
          letterSpacing: 1,
        ),
      ),
    );
  }
}

// ── Comm delay row ────────────────────────────────────────────────────────────

class _CommDelayRow extends StatelessWidget {
  final String planetId;
  const _CommDelayRow({required this.planetId});

  @override
  Widget build(BuildContext context) {
    final delay = planetCommDelayMin[planetId] ?? '?';
    return Row(
      children: [
        Icon(Icons.signal_cellular_alt,
            color: Colors.white.withValues(alpha: 0.40), size: 10),
        const SizedBox(width: 6),
        Text('RETARDO SEÑAL',
            style: TextStyle(
                color: Colors.white.withValues(alpha: 0.50),
                fontSize: 8,
                letterSpacing: 1.5)),
        const Spacer(),
        Text(
          delay,
          style: const TextStyle(
              color: Colors.amberAccent,
              fontSize: 9,
              fontWeight: FontWeight.bold),
        ),
      ],
    );
  }
}

// ── Animated star field background ────────────────────────────────────────────

class _StarBackground extends StatelessWidget {
  const _StarBackground();

  @override
  Widget build(BuildContext context) {
    return CustomPaint(
      painter: _StarBgPainter(),
      child: const SizedBox.expand(),
    );
  }
}

class _StarBgPainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final rng = math.Random(77);
    for (var i = 0; i < 180; i++) {
      final x = rng.nextDouble() * size.width;
      final y = rng.nextDouble() * size.height;
      final r = 0.3 + rng.nextDouble() * 1.2;
      final a = 0.1 + rng.nextDouble() * 0.45;
      canvas.drawCircle(
          Offset(x, y), r, Paint()..color = Colors.white.withValues(alpha: a));
    }
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}
