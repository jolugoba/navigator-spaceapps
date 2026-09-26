// Navigator — Flutter home: pilot name, language, launch the Unity mission, mission log

import 'dart:io';

import 'package:flutter/material.dart';
import 'package:permission_handler/permission_handler.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'game_screen.dart';
import 'mission_log.dart';
import 'strings.dart';
import 'unity_host.dart';

const _accent = Color(0xFF33CCFF);
const _gold = Color(0xFFFFC94D);

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key, required this.lang, required this.onLanguageChanged});

  final AppLang lang;
  final ValueChanged<AppLang> onLanguageChanged;

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  static const _nameKey = 'navigator.pilot';
  final _name = TextEditingController();
  List<MissionRecord> _log = const [];
  bool _launching = false;

  S get s => S(widget.lang);

  @override
  void initState() {
    super.initState();
    _restore();
  }

  @override
  void dispose() {
    _name.dispose();
    super.dispose();
  }

  Future<void> _restore() async {
    final prefs = await SharedPreferences.getInstance();
    _name.text = prefs.getString(_nameKey) ?? '';
    final log = await MissionLog.load();
    if (mounted) setState(() => _log = log);
  }

  Future<void> _launch() async {
    if (_launching) return;
    setState(() => _launching = true);
    try {
      final prefs = await SharedPreferences.getInstance();
      final name = _name.text.trim();
      await prefs.setString(_nameKey, name);

      // Android asks at runtime; on iOS Unity triggers the system prompts itself
      if (Platform.isAndroid) {
        await [Permission.camera, Permission.locationWhenInUse].request();
      }

      final hasUnity = await UnityHost.hasUnity();
      if (!mounted) return;
      await Navigator.of(context).push(MaterialPageRoute<void>(
        builder: (_) => hasUnity
            ? GameScreen(strings: s, playerName: name.isEmpty ? s.pilotHint : name)
            : NoUnityScreen(strings: s),
      ));
      final log = await MissionLog.load();
      if (mounted) setState(() => _log = log);
    } finally {
      if (mounted) setState(() => _launching = false);
    }
  }

  Future<void> _clearLog() async {
    await MissionLog.clear();
    if (mounted) setState(() => _log = const []);
  }

  @override
  Widget build(BuildContext context) {
    final best = _log.isEmpty ? 0 : _log.map((r) => r.score).reduce((a, b) => a > b ? a : b);
    return Scaffold(
      body: Container(
        decoration: const BoxDecoration(
          gradient: RadialGradient(
            center: Alignment(0, -0.7),
            radius: 1.3,
            colors: [Color(0xFF1B2A55), Color(0xFF070B18)],
          ),
        ),
        child: SafeArea(
          child: ListView(
            padding: const EdgeInsets.fromLTRB(20, 12, 20, 32),
            children: [
              Align(
                alignment: Alignment.centerRight,
                child: TextButton(
                  onPressed: () => widget.onLanguageChanged(s.es ? AppLang.en : AppLang.es),
                  child: Text(s.language, style: const TextStyle(color: _accent, fontWeight: FontWeight.bold)),
                ),
              ),
              const SizedBox(height: 8),
              const Icon(Icons.rocket_launch, size: 72, color: _accent),
              const SizedBox(height: 12),
              const Text('NAVIGATOR',
                  textAlign: TextAlign.center,
                  style: TextStyle(fontSize: 40, fontWeight: FontWeight.w900, letterSpacing: 6, color: Colors.white)),
              const SizedBox(height: 6),
              Text(s.tagline, textAlign: TextAlign.center, style: const TextStyle(color: Colors.white70, fontSize: 16)),
              const SizedBox(height: 4),
              Text(s.challenge, textAlign: TextAlign.center, style: const TextStyle(color: _gold, fontSize: 13)),
              const SizedBox(height: 28),
              TextField(
                controller: _name,
                maxLength: 16,
                textCapitalization: TextCapitalization.words,
                decoration: InputDecoration(
                  labelText: s.pilotName,
                  hintText: s.pilotHint,
                  prefixIcon: const Icon(Icons.person_outline),
                  border: const OutlineInputBorder(),
                ),
              ),
              const SizedBox(height: 12),
              SizedBox(
                height: 64,
                child: FilledButton.icon(
                  style: FilledButton.styleFrom(
                    backgroundColor: _accent,
                    foregroundColor: const Color(0xFF051020),
                    textStyle: const TextStyle(fontSize: 20, fontWeight: FontWeight.w900, letterSpacing: 2),
                  ),
                  onPressed: _launching ? null : _launch,
                  icon: _launching
                      ? const SizedBox(width: 22, height: 22, child: CircularProgressIndicator(strokeWidth: 3))
                      : const Icon(Icons.play_arrow_rounded, size: 32),
                  label: Text(s.launch),
                ),
              ),
              const SizedBox(height: 10),
              Text(s.permissionsNote,
                  textAlign: TextAlign.center, style: const TextStyle(color: Colors.white54, fontSize: 12)),
              const SizedBox(height: 28),
              Row(
                children: [
                  _Stat(label: s.flown, value: '${_log.length}'),
                  const SizedBox(width: 12),
                  _Stat(label: s.best, value: '$best'),
                ],
              ),
              const SizedBox(height: 20),
              Row(
                children: [
                  Expanded(
                    child: Text(s.missionLog,
                        style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: Colors.white)),
                  ),
                  if (_log.isNotEmpty)
                    TextButton(onPressed: _clearLog, child: Text(s.clearLog)),
                ],
              ),
              const SizedBox(height: 8),
              if (_log.isEmpty)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 24),
                  child: Text(s.noMissions, textAlign: TextAlign.center, style: const TextStyle(color: Colors.white54)),
                )
              else
                for (final r in _log.take(20)) _MissionTile(record: r, s: s),
            ],
          ),
        ),
      ),
    );
  }
}

class _Stat extends StatelessWidget {
  const _Stat({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: Container(
        padding: const EdgeInsets.symmetric(vertical: 14, horizontal: 16),
        decoration: BoxDecoration(
          color: Colors.white.withValues(alpha: 0.06),
          borderRadius: BorderRadius.circular(14),
          border: Border.all(color: Colors.white12),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(label, style: const TextStyle(color: Colors.white54, fontSize: 12)),
            const SizedBox(height: 4),
            Text(value, style: const TextStyle(color: Colors.white, fontSize: 26, fontWeight: FontWeight.bold)),
          ],
        ),
      ),
    );
  }
}

class _MissionTile extends StatelessWidget {
  const _MissionTile({required this.record, required this.s});

  final MissionRecord record;
  final S s;

  @override
  Widget build(BuildContext context) {
    final r = record;
    final stars = r.stars >= 0 ? ' · ${'●' * r.stars}${'○' * (3 - r.stars).clamp(0, 3)}' : '';
    final status = r.arrived ? '${s.arrived} #${r.position}' : (r.fail ?? s.failed);
    final date = '${r.when.day.toString().padLeft(2, '0')}/${r.when.month.toString().padLeft(2, '0')} '
        '${r.when.hour.toString().padLeft(2, '0')}:${r.when.minute.toString().padLeft(2, '0')}';
    return Card(
      color: Colors.white.withValues(alpha: 0.05),
      margin: const EdgeInsets.only(bottom: 8),
      child: ListTile(
        leading: Icon(r.arrived ? Icons.public : Icons.warning_amber_rounded,
            color: r.arrived ? _accent : Colors.orangeAccent),
        title: Text('${s.planet(r.planet)} · ${s.objective(r.objective)}$stars'),
        subtitle: Text('$status · \$${r.costM.toStringAsFixed(0)}M · $date',
            maxLines: 2, overflow: TextOverflow.ellipsis),
        trailing: Text('${r.score}',
            style: const TextStyle(color: _gold, fontSize: 20, fontWeight: FontWeight.bold)),
      ),
    );
  }
}
