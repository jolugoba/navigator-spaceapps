import 'package:flutter/services.dart';

class GyroData {
  final double azimuth;  // radians — horizontal (yaw del mundo)
  final double pitch;    // radians — inclinación vertical
  final double roll;     // radians — roll

  const GyroData({
    this.azimuth = 0,
    this.pitch   = 0,
    this.roll    = 0,
  });
}

class GyroService {
  static const _channel = MethodChannel('com.guacalita.navigator/gyro');

  GyroData _last = const GyroData();
  GyroData get current => _last;

  final List<void Function(GyroData)> _listeners = [];

  Future<void> enable() async {
    _channel.setMethodCallHandler((call) async {
      if (call.method == 'GYRO_UPDATE') {
        _last = GyroData(
          azimuth: (call.arguments['azimuth'] as num).toDouble(),
          pitch:   (call.arguments['pitch']   as num).toDouble(),
          roll:    (call.arguments['roll']    as num).toDouble(),
        );
        for (final l in _listeners) { l(_last); }
      }
    });
    await _channel.invokeMethod('ENABLE_GYRO');
  }

  Future<void> disable() async {
    await _channel.invokeMethod('DISABLE_GYRO');
    _listeners.clear();
  }

  void addListener(void Function(GyroData) listener) =>
      _listeners.add(listener);

  void removeListener(void Function(GyroData) listener) =>
      _listeners.remove(listener);
}
