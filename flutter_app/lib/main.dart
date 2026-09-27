// Navigator 2.0 — Flutter app that hosts the Unity game (flutter_embed_unity)
// Guacalita Inc. — NASA Space Apps Challenge 2026

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'src/home_screen.dart';
import 'src/strings.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  // The game is played in landscape (twin-stick controls); fixed so it never flips mid-flight
  await SystemChrome.setPreferredOrientations([DeviceOrientation.landscapeLeft]);
  final prefs = await SharedPreferences.getInstance();
  final lang = prefs.getString(NavigatorApp.langKey) == 'es' ? AppLang.es : AppLang.en;
  runApp(NavigatorApp(initialLang: lang));
}

class NavigatorApp extends StatefulWidget {
  const NavigatorApp({super.key, this.initialLang = AppLang.en});

  static const langKey = 'navigator.lang';
  final AppLang initialLang;

  @override
  State<NavigatorApp> createState() => _NavigatorAppState();
}

class _NavigatorAppState extends State<NavigatorApp> {
  late AppLang _lang = widget.initialLang;

  Future<void> _setLang(AppLang lang) async {
    setState(() => _lang = lang);
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(NavigatorApp.langKey, lang == AppLang.es ? 'es' : 'en');
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Navigator',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        brightness: Brightness.dark,
        colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xFF33CCFF), brightness: Brightness.dark),
        useMaterial3: true,
      ),
      home: HomeScreen(lang: _lang, onLanguageChanged: _setLang),
    );
  }
}
