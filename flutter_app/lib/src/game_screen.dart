// Navigator — full-screen Unity game inside Flutter

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_embed_unity/flutter_embed_unity.dart';

import 'mission_log.dart';
import 'strings.dart';
import 'unity_host.dart';

class GameScreen extends StatefulWidget {
  const GameScreen({super.key, required this.strings, required this.playerName});

  final S strings;
  final String playerName;

  @override
  State<GameScreen> createState() => _GameScreenState();
}

class _GameScreenState extends State<GameScreen> {
  bool _loading = !UnityHost.sceneReady;
  int _missionsThisVisit = 0;

  @override
  void initState() {
    super.initState();
    SystemChrome.setEnabledSystemUIMode(SystemUiMode.immersiveSticky);
    // Unity is already loaded from an earlier visit: settings can go straight away
    if (UnityHost.sceneReady) _applySettings();
  }

  @override
  void dispose() {
    SystemChrome.setEnabledSystemUIMode(SystemUiMode.edgeToEdge);
    super.dispose();
  }

  void _applySettings() => UnityHost.applySettings(
        languageCode: widget.strings.es ? 'es' : 'en',
        playerName: widget.playerName,
      );

  Future<void> _onMessage(String message) async {
    final msg = UnityHost.parse(message);
    if (msg == null) return;
    switch (msg['type']) {
      case 'ready':
        UnityHost.sceneReady = true;
        _applySettings();
        if (mounted && _loading) setState(() => _loading = false);
        break;
      case 'mission':
        await MissionLog.add(MissionRecord.fromJson(msg));
        _missionsThisVisit++;
        break;
      case 'exit':
        if (mounted) Navigator.of(context).pop(_missionsThisVisit);
        break;
    }
  }

  @override
  Widget build(BuildContext context) {
    return PopScope(
      canPop: true,
      onPopInvokedWithResult: (didPop, result) {},
      child: Scaffold(
        backgroundColor: Colors.black,
        body: Stack(
          fit: StackFit.expand,
          children: [
            EmbedUnity(onMessageFromUnity: _onMessage),
            if (_loading)
              IgnorePointer(
                child: Container(
                  color: Colors.black,
                  alignment: Alignment.center,
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      const CircularProgressIndicator(color: Color(0xFF33CCFF)),
                      const SizedBox(height: 20),
                      Text(widget.strings.loading,
                          style: const TextStyle(color: Colors.white70, fontSize: 16)),
                    ],
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}

/// Shown when the app was built before Unity was exported into android/unityLibrary.
class NoUnityScreen extends StatelessWidget {
  const NoUnityScreen({super.key, required this.strings});

  final S strings;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(strings.noUnityTitle)),
      body: Padding(
        padding: const EdgeInsets.all(24),
        child: SelectableText(strings.noUnityBody, style: const TextStyle(fontSize: 16, height: 1.4)),
      ),
    );
  }
}
