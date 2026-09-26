import 'dart:convert';
import 'package:http/http.dart' as http;

class PlanetPosition {
  final String planetId;
  final double azimuthDeg;
  final double altitudeDeg;
  final double distanceAU;

  const PlanetPosition({
    required this.planetId,
    required this.azimuthDeg,
    required this.altitudeDeg,
    required this.distanceAU,
  });
}

class HorizonsService {
  static const _base = 'https://ssd.jpl.nasa.gov/api/horizons.api';

  final _cache = <String, _CachedPos>{};

  Future<PlanetPosition?> getPlanetPosition({
    required String nasaBodyId,
    required String planetId,
    required double lat,
    required double lon,
  }) async {
    final cacheKey = '$nasaBodyId-${lat.toStringAsFixed(2)}-${lon.toStringAsFixed(2)}';
    final cached = _cache[cacheKey];
    if (cached != null && DateTime.now().difference(cached.fetchedAt).inMinutes < 10) {
      return cached.pos;
    }

    try {
      final now = DateTime.now().toUtc();
      final stop = now.add(const Duration(minutes: 1));
      final startStr = _fmtDate(now);
      final stopStr  = _fmtDate(stop);
      final siteCoord = "'${lon.toStringAsFixed(4)},${lat.toStringAsFixed(4)},0'";

      final uri = Uri.parse(_base).replace(queryParameters: {
        'format':       'json',
        'COMMAND':      "'$nasaBodyId'",
        'OBJ_DATA':     'NO',
        'MAKE_EPHEM':   'YES',
        'EPHEM_TYPE':   'OBSERVER',
        'CENTER':       'coord@399',
        'SITE_COORD':   siteCoord,
        'START_TIME':   "'$startStr'",
        'STOP_TIME':    "'$stopStr'",
        'STEP_SIZE':    "'1m'",
        'QUANTITIES':   "'4,20'",  // Az/Alt + dist
      });

      final res = await http.get(uri).timeout(const Duration(seconds: 10));
      if (res.statusCode != 200) return null;

      final body    = jsonDecode(res.body) as Map<String, dynamic>;
      final result  = body['result'] as String? ?? '';
      final pos     = _parseHorizons(result, planetId);
      if (pos != null) {
        _cache[cacheKey] = _CachedPos(pos, DateTime.now());
      }
      return pos;
    } catch (_) {
      return null;
    }
  }

  PlanetPosition? _parseHorizons(String result, String planetId) {
    // SOE ... EOE section contains ephemeris data lines
    final soeIdx = result.indexOf('\$\$SOE');
    final eoeIdx = result.indexOf('\$\$EOE');
    if (soeIdx < 0 || eoeIdx < 0) return null;

    final block = result.substring(soeIdx + 5, eoeIdx).trim();
    final lines  = block.split('\n').where((l) => l.trim().isNotEmpty).toList();
    if (lines.isEmpty) return null;

    // Typical line format: date  Az  Alt  ...  dist
    final parts = lines.first.trim().split(RegExp(r'\s+'));
    if (parts.length < 5) return null;

    try {
      final az   = double.parse(parts[3]);
      final alt  = double.parse(parts[4]);
      // Distance is usually near the end
      final dist = parts.length > 7 ? double.tryParse(parts[7]) ?? 1.0 : 1.0;
      return PlanetPosition(
        planetId:    planetId,
        azimuthDeg:  az,
        altitudeDeg: alt,
        distanceAU:  dist,
      );
    } catch (_) {
      return null;
    }
  }

  String _fmtDate(DateTime d) =>
      "${d.year}-${d.month.toString().padLeft(2,'0')}-"
      "${d.day.toString().padLeft(2,'0')} "
      "${d.hour.toString().padLeft(2,'0')}:${d.minute.toString().padLeft(2,'0')}";
}

class _CachedPos {
  final PlanetPosition pos;
  final DateTime fetchedAt;
  _CachedPos(this.pos, this.fetchedAt);
}
