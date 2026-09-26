// Navigator — the link between this Flutter app and the embedded Unity game
//
// Flutter → Unity: sendToUnity("FlutterBridge", method, arg)   (Assets/UI/FlutterBridge.cs)
// Unity → Flutter: JSON strings {"type":"ready" | "mission" | "exit", ...}

import 'dart:convert';
import 'dart:io';

import 'package:flutter/services.dart';
import 'package:flutter_embed_unity/flutter_embed_unity.dart';

class UnityHost {
  static const _channel = MethodChannel('navigator/host');
  static const bridgeObject = 'FlutterBridge';

  /// Unity loads once per app run and then stays in memory (paused when hidden).
  static bool sceneReady = false;

  /// Whether this build links the exported Unity library (android/unityLibrary).
  static Future<bool> hasUnity() async {
    if (Platform.isIOS) return true; // iOS is always built with the Unity framework in Xcode
    try {
      return await _channel.invokeMethod<bool>('hasUnity') ?? false;
    } on PlatformException {
      return false;
    } on MissingPluginException {
      return false;
    }
  }

  static void applySettings({required String languageCode, required String playerName}) {
    sendToUnity(bridgeObject, 'SetLanguage', languageCode);
    sendToUnity(bridgeObject, 'SetPlayerName', playerName);
  }

  /// Parses one message from Unity; returns null for anything that isn't a JSON object.
  static Map<String, dynamic>? parse(String message) {
    try {
      final decoded = jsonDecode(message);
      return decoded is Map<String, dynamic> ? decoded : null;
    } catch (_) {
      return null;
    }
  }
}
