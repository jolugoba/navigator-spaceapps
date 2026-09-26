import 'dart:math' as math;
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'l10n/app_localizations.dart';

import 'core/components.dart';
import 'core/space_world.dart';
import 'core/vec3.dart';
import 'flight/cockpit_view.dart';
import 'flight/gyro_service.dart';
import 'flight/physics_engine.dart';
import 'models/mission_design.dart';
import 'models/planet_info.dart';
import 'models/player_info.dart';
import 'network/lobby_service.dart';
import 'network/ship_sync_service.dart';
import 'screens/mission_design_screen.dart';
import 'ui/lobby_screen.dart';
import 'ui/main_menu.dart';
import 'ui/mission_select.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  SystemChrome.setPreferredOrientations([
    DeviceOrientation.landscapeLeft,
    DeviceOrientation.landscapeRight,
  ]);
  SystemChrome.setEnabledSystemUIMode(SystemUiMode.immersiveSticky);
  runApp(const NavigatorApp());
}

class NavigatorApp extends StatelessWidget {
  const NavigatorApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'NAVIGATOR',
      debugShowCheckedModeBanner: false,
      localizationsDelegates: const [
        AppLocalizations.delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
      supportedLocales: AppLocalizations.supportedLocales,
      theme: ThemeData(
        brightness: Brightness.dark,
        scaffoldBackgroundColor: const Color(0xFF010408),
        colorScheme: const ColorScheme.dark(
          primary: Color(0xFF00E5FF),
          secondary: Color(0xFFFF6B35),
        ),
        textTheme: const TextTheme(
          bodyMedium: TextStyle(color: Colors.white70),
        ),
      ),
      home: const _NavigatorRoot(),
    );
  }
}

enum _Screen { menu, lobby, missionSelect, missionDesign, flight }

class _NavigatorRoot extends StatefulWidget {
  const _NavigatorRoot();

  @override
  State<_NavigatorRoot> createState() => _NavigatorRootState();
}

class _NavigatorRootState extends State<_NavigatorRoot> {
  _Screen _screen = _Screen.menu;

  LobbyService? _lobby;
  ShipSyncService? _sync;
  SpaceWorld? _world;
  PhysicsEngine? _physics;
  EntityId? _localShipId;
  final _gyro = GyroService();

  PlayerInfo? _localPlayer;
  PlanetInfo? _selectedPlanet;
  MissionDesign? _selectedDesign;

  @override
  void initState() {
    super.initState();
    _loadPlayerName();
  }

  Future<void> _loadPlayerName() async {
    final prefs = await SharedPreferences.getInstance();
    final name  = prefs.getString('playerName') ?? _defaultName();
    if (mounted) {
      setState(() => _localPlayer = PlayerInfo(
        nodeId:      'local',
        displayName: name,
        joinedAt:    DateTime.now(),
      ));
    }
  }

  String _defaultName() {
    const names = ['Orion', 'Vega', 'Sirius', 'Lyra', 'Cygnus', 'Aquila'];
    return names[DateTime.now().millisecond % names.length];
  }

  Future<void> _goLobby() async {
    final lobby = LobbyService();
    await lobby.init();
    if (!mounted) return;
    setState(() {
      _lobby = lobby;
      _localPlayer = PlayerInfo(
        nodeId:      lobby.nodeId,
        displayName: _localPlayer?.displayName ?? _defaultName(),
        joinedAt:    DateTime.now(),
      );
      _screen = _Screen.lobby;
    });
  }

  void _goMissionSelect(List contacts) {
    _sync = ShipSyncService(
      p2p:         _lobby!.client,
      localPlayer: _localPlayer!,
    );
    setState(() => _screen = _Screen.missionSelect);
  }

  // Dirección fija por planeta (normalizada) para posición de juego inicial.
  // Cada planeta está a 1000 unidades de la nave en una dirección característica.
  Vec3 _planetGameDirection(String planetId) {
    const dirs = <String, (double, double, double)>{
      'mercury': ( 0.60,  0.20, -0.78),
      'venus':   (-0.50,  0.30, -0.81),
      'mars':    ( 0.00,  0.00, -1.00),
      'jupiter': ( 0.70, -0.20, -0.69),
      'saturn':  (-0.40,  0.40, -0.82),
      'uranus':  ( 0.30, -0.50, -0.81),
      'neptune': (-0.60,  0.20, -0.78),
    };
    final d = dirs[planetId] ?? (0.0, 0.0, -1.0);
    final v = Vec3(d.$1, d.$2, d.$3);
    final l = math.sqrt(v.x*v.x + v.y*v.y + v.z*v.z);
    return Vec3(v.x / l, v.y / l, v.z / l) * 1000.0;
  }

  void _goDesignScreen(PlanetInfo? planet) {
    setState(() {
      _selectedPlanet = planet;
      _screen = _Screen.missionDesign;
    });
  }

  void _startFlight({PlanetInfo? mission, MissionDesign? design}) {
    final world   = SpaceWorld();
    final physics = PhysicsEngine(world);

    final shipId = world.createEntity();
    world.add<ShipTransform>(shipId, ShipTransform(position: Vec3.zero()));
    world.add<ShipVelocity>(shipId, ShipVelocity());
    world.add<ShipThrust>(shipId, ShipThrust());
    world.add<ShipOwner>(shipId, ShipOwner(
      nodeId:      _localPlayer!.nodeId,
      displayName: _localPlayer!.displayName,
      isLocal:     true,
    ));
    world.add<ShipHealth>(shipId, ShipHealth());

    // Fuel capacity from mission design (or default)
    final fuelMax = design?.gameFuelMax ?? 500.0;
    world.add<ShipFuel>(shipId, ShipFuel(current: fuelMax, max: fuelMax));

    if (mission != null) {
      final planetPos = _planetGameDirection(mission.id);
      physics.setPlanetPosition(mission.id, planetPos);

      world.add<MissionScore>(shipId, MissionScore());
      world.add<MissionTarget>(shipId, MissionTarget(
        planetId:     mission.id,
        distanceUnits: 1000,
      ));
    }

    // Apply engine config + planetary gravity from mission design
    if (design != null) {
      final mult = engineThrustMultiplier[design.engine] ?? 1.0;
      physics.setEngineConfig(thrustMultiplier: mult);
      const muMap = <String, double>{
        'mercury': 190000.0, 'venus':   455000.0, 'mars':    190000.0,
        'jupiter': 1265000.0,'saturn':  535000.0, 'uranus':  445000.0,
        'neptune': 570000.0,
      };
      physics.registerPlanetGravity(
        design.targetPlanetId,
        muMap[design.targetPlanetId] ?? 200000.0,
      );
    }

    _world          = world;
    _physics        = physics;
    _localShipId    = shipId;
    _selectedDesign = design;
    _sync?.startBroadcast();

    setState(() => _screen = _Screen.flight);
  }

  void _exitFlight() {
    _sync?.dispose();
    _world?.dispose();
    setState(() {
      _sync           = null;
      _world          = null;
      _physics        = null;
      _localShipId    = null;
      _selectedDesign = null;
      _screen         = _Screen.menu;
    });
  }

  @override
  void dispose() {
    _gyro.disable();
    _sync?.dispose();
    _world?.dispose();
    _lobby?.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return switch (_screen) {
      _Screen.menu => MainMenuScreen(onPlay: _goLobby),

      _Screen.lobby => LobbyScreen(
          lobby:       _lobby!,
          localPlayer: _localPlayer!,
          onStartGame: _goMissionSelect,
        ),

      _Screen.missionSelect => MissionSelectScreen(
          onMissionSelected: (p) => _goDesignScreen(p),
          onFreeMode:        () => _startFlight(),
        ),

      _Screen.missionDesign => _selectedPlanet != null
          ? MissionDesignScreen(
              planet: _selectedPlanet!,
              onLaunch: (design) =>
                  _startFlight(mission: _selectedPlanet, design: design),
              onBack: () => setState(() => _screen = _Screen.missionSelect),
            )
          : const Center(child: CircularProgressIndicator()),

      _Screen.flight => (_world != null && _localShipId != null)
          ? CockpitView(
              world:         _world!,
              localShipId:   _localShipId!,
              physics:       _physics!,
              gyro:          _gyro,
              localPlayer:   _localPlayer!,
              sync:          _sync,
              missionDesign: _selectedDesign,
              onExit:        _exitFlight,
            )
          : const Center(child: CircularProgressIndicator()),
    };
  }
}
