// Navigator — results that the Unity game reports back ({"type":"mission",...}), kept on the phone

import 'dart:convert';

import 'package:shared_preferences/shared_preferences.dart';

class MissionRecord {
  MissionRecord({
    required this.planet,
    required this.objective,
    required this.score,
    required this.arrived,
    required this.position,
    required this.stars,
    required this.costM,
    required this.timeS,
    required this.fuel,
    required this.science,
    required this.fail,
    required this.when,
  });

  final String planet;
  final String objective;
  final int score;
  final bool arrived;
  final int position;
  final int stars; // -1 = free play
  final double costM;
  final double timeS;
  final double fuel;
  final int science;
  final String? fail;
  final DateTime when;

  static int _int(Object? v) => v is num ? v.toInt() : int.tryParse('$v') ?? 0;
  static double _double(Object? v) => v is num ? v.toDouble() : double.tryParse('$v') ?? 0;

  factory MissionRecord.fromJson(Map<String, dynamic> j) => MissionRecord(
        planet: '${j['planet'] ?? ''}',
        objective: '${j['objective'] ?? ''}',
        score: _int(j['score']),
        arrived: j['arrived'] == true,
        position: _int(j['position']),
        stars: j.containsKey('stars') ? _int(j['stars']) : -1,
        costM: _double(j['costM']),
        timeS: _double(j['timeS']),
        fuel: _double(j['fuel']),
        science: _int(j['science']),
        fail: j['fail'] as String?,
        when: DateTime.fromMillisecondsSinceEpoch(
            j['ts'] is num ? (j['ts'] as num).toInt() : DateTime.now().millisecondsSinceEpoch),
      );

  Map<String, dynamic> toJson() => {
        'planet': planet,
        'objective': objective,
        'score': score,
        'arrived': arrived,
        'position': position,
        'stars': stars,
        'costM': costM,
        'timeS': timeS,
        'fuel': fuel,
        'science': science,
        if (fail != null) 'fail': fail,
        'ts': when.millisecondsSinceEpoch,
      };
}

class MissionLog {
  static const _key = 'navigator.missions';
  static const maxRecords = 50;

  static Future<List<MissionRecord>> load() async {
    final prefs = await SharedPreferences.getInstance();
    final raw = prefs.getStringList(_key) ?? const [];
    final out = <MissionRecord>[];
    for (final s in raw) {
      try {
        out.add(MissionRecord.fromJson(jsonDecode(s) as Map<String, dynamic>));
      } catch (_) {
        // skip a corrupt entry instead of losing the whole log
      }
    }
    return out;
  }

  static Future<void> add(MissionRecord r) async {
    final prefs = await SharedPreferences.getInstance();
    final raw = prefs.getStringList(_key) ?? <String>[];
    raw.insert(0, jsonEncode(r.toJson()));
    if (raw.length > maxRecords) raw.removeRange(maxRecords, raw.length);
    await prefs.setStringList(_key, raw);
  }

  static Future<void> clear() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_key);
  }
}
