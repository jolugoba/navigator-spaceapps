import 'package:flutter/material.dart';

/// Multi-language support — English (default) + Spanish.
/// Usage: AppLocalizations.of(context).someKey
class AppLocalizations {
  final Locale locale;
  const AppLocalizations(this.locale);

  static AppLocalizations of(BuildContext context) =>
      Localizations.of<AppLocalizations>(context, AppLocalizations)!;

  static const delegate = _AppLocalizationsDelegate();

  static const supportedLocales = [Locale('en'), Locale('es')];

  String _s(String key) =>
      _ui[locale.languageCode]?[key] ?? _ui['en']![key] ?? key;

  // ── Main Menu ──────────────────────────────────────────────────────────
  String get appTitle      => _s('app_title');
  String get appSubtitle   => _s('app_subtitle');
  String get featureFlight => _s('feature_flight');
  String get featureGyro   => _s('feature_gyro');
  String get featureP2p    => _s('feature_p2p');
  String get featureCrypto => _s('feature_crypto');
  String get featureNasa   => _s('feature_nasa');
  String get btnStartMission => _s('btn_start_mission');
  String get credits       => _s('credits');

  // ── Mission Select ─────────────────────────────────────────────────────
  String get selectTitle    => _s('select_title');
  String get selectSubtitle => _s('select_subtitle');
  String get btnFreeMode    => _s('btn_free_mode');
  String get btnLaunch      => _s('btn_launch');
  String get eduFactsLabel  => _s('edu_facts_label');

  // ── Lobby ──────────────────────────────────────────────────────────────
  String get lobbyConnecting   => _s('lobby_connecting');
  String get lobbyShareQr      => _s('lobby_share_qr');
  String get lobbyConnectWith  => _s('lobby_connect_with');
  String get lobbyNodeIdHint   => _s('lobby_node_id_hint');
  String get btnLink           => _s('btn_link');
  String get lobbyNoPilots     => _s('lobby_no_pilots');
  String get lobbyNoPilotsSub  => _s('lobby_no_pilots_sub');
  String get lobbyPlaySolo     => _s('lobby_play_solo');
  String get lobbyErrorConnect => _s('lobby_error_connect');
  String lobbyLaunchWith(int n) =>
      _s('lobby_launch_with').replaceAll('{n}', '$n');

  // ── HUD ────────────────────────────────────────────────────────────────
  String get hudVel      => _s('hud_vel');
  String get hudDest     => _s('hud_dest');
  String get hudDist     => _s('hud_dist');
  String get hudFreeMode => _s('hud_free_mode');
  String get hudFuel     => _s('hud_fuel');
  String get hudRadio    => _s('hud_radio');

  // ── Chat ───────────────────────────────────────────────────────────────
  String get chatTitle      => _s('chat_title');
  String get chatHint       => _s('chat_hint');
  String get chatSolo       => _s('chat_solo');

  // ── Scoreboard ─────────────────────────────────────────────────────────
  String get resultScoreboard => _s('result_scoreboard');
  String get resultYou        => _s('result_you');

  // ── Mission Result ─────────────────────────────────────────────────────
  String get resultComplete    => _s('result_complete');
  String get resultFailed      => _s('result_failed');
  String get resultPoints      => _s('result_points');
  String get resultBase        => _s('result_base');
  String get resultFuelLabel   => _s('result_fuel_label');
  String get resultTimeLabel   => _s('result_time_label');
  String get resultBoostLabel  => _s('result_boost_label');
  String get resultNoFuel      => _s('result_no_fuel');
  String get resultProgress    => _s('result_progress');
  String get btnNewMission     => _s('btn_new_mission');
  String get btnRetry          => _s('btn_retry');
  String get btnMenu           => _s('btn_menu');
  String get statTime          => _s('stat_time');
  String get statFuel          => _s('stat_fuel');
  String get statDist          => _s('stat_dist');

  // ── Planets ────────────────────────────────────────────────────────────
  String planetName(String id)        => _s('planet_$id');
  String planetDescription(String id) => _s('desc_$id');
  List<String> planetFacts(String id) =>
      _pf[locale.languageCode]?[id] ?? _pf['en']?[id] ?? [];

  // ── String tables ──────────────────────────────────────────────────────
  static const _ui = <String, Map<String, String>>{
    'en': {
      'app_title':      'NAVIGATOR',
      'app_subtitle':   'MULTIPLAYER SPACE EXPLORATION',
      'feature_flight': 'First-person free flight',
      'feature_gyro':   'Gyroscope — tilt your head to look around',
      'feature_p2p':    'P2P Multiplayer — no server needed',
      'feature_crypto': 'Post-quantum encryption Kyber-1024',
      'feature_nasa':   'Real NASA Horizons orbital data',
      'btn_start_mission': 'START MISSION',
      'credits':        'NASA SPACE APPS CHALLENGE 2026 · GUACALITA INC.',

      'select_title':    'SELECT MISSION',
      'select_subtitle': 'Choose your destination in the solar system',
      'btn_free_mode':   'FREE MODE',
      'btn_launch':      'LAUNCH',
      'edu_facts_label': 'DID YOU KNOW?',

      'lobby_connecting':    'Initializing P2P network...',
      'lobby_share_qr':      'Share this QR with other pilots',
      'lobby_connect_with':  'CONNECT WITH PILOT',
      'lobby_node_id_hint':  'Other pilot\'s Node ID',
      'btn_link':            'LINK',
      'lobby_no_pilots':     'No pilots connected',
      'lobby_no_pilots_sub': 'Share your QR or enter a pilot\'s node ID',
      'lobby_play_solo':     'PLAY SOLO',
      'lobby_launch_with':   'LAUNCH WITH {n} PILOTS',
      'lobby_error_connect': 'Could not connect. Check the ID.',

      'hud_vel':       'VEL',
      'hud_dest':      'DEST',
      'hud_dist':      'DIST',
      'hud_free_mode': 'FREE',
      'hud_fuel':      'FUEL',
      'hud_radio':     'RADIO',

      'chat_title':   'RADIO COMMS',
      'chat_hint':    'Transmit message...',
      'chat_solo':    'Solo mode — no pilots connected',

      'result_scoreboard': 'PILOTS RANKING',
      'result_you':        'YOU',

      'result_complete':   'MISSION COMPLETE',
      'result_failed':     'MISSION FAILED',
      'result_points':     'POINTS',
      'result_base':       'Mission base',
      'result_fuel_label': 'Fuel',
      'result_time_label': 'Time',
      'result_boost_label':'Boost',
      'result_no_fuel':    'OUT OF FUEL',
      'result_progress':   '% of the way there',
      'btn_new_mission':   'NEW MISSION',
      'btn_retry':         'RETRY',
      'btn_menu':          'MENU',
      'stat_time':         'TIME',
      'stat_fuel':         'FUEL',
      'stat_dist':         'DIST',

      'planet_mercury': 'Mercury',
      'planet_venus':   'Venus',
      'planet_mars':    'Mars',
      'planet_jupiter': 'Jupiter',
      'planet_saturn':  'Saturn',
      'planet_uranus':  'Uranus',
      'planet_neptune': 'Neptune',

      'desc_mercury': 'Closest to the Sun. Temperatures: −180 °C to 430 °C. No atmosphere.',
      'desc_venus':   'Hottest planet. Dense CO₂ atmosphere. Surface at 465 °C.',
      'desc_mars':    'The Red Planet. Home to Olympus Mons, tallest volcano in the solar system.',
      'desc_jupiter': 'Largest planet. Its Great Red Spot storm has raged for 350+ years.',
      'desc_saturn':  'Its rings span 282,000 km but are only 10–100 m thick.',
      'desc_uranus':  'Ice giant that rotates on its side — axial tilt of 98°.',
      'desc_neptune': 'Farthest planet. Winds reach 2,100 km/h — strongest in the solar system.',
    },
    'es': {
      'app_title':      'NAVIGATOR',
      'app_subtitle':   'EXPLORACIÓN ESPACIAL MULTIJUGADOR',
      'feature_flight': 'Vuelo libre en primera persona',
      'feature_gyro':   'Giroscopio — mueve la cabeza para mirar',
      'feature_p2p':    'Multijugador P2P sin servidor',
      'feature_crypto': 'Cifrado post-cuántico Kyber-1024',
      'feature_nasa':   'Datos orbitales reales NASA Horizons',
      'btn_start_mission': 'INICIAR MISIÓN',
      'credits':        'NASA SPACE APPS CHALLENGE 2026 · GUACALITA INC.',

      'select_title':    'SELECCIONAR MISIÓN',
      'select_subtitle': 'Elige tu destino en el sistema solar',
      'btn_free_mode':   'MODO LIBRE',
      'btn_launch':      'LANZAR',
      'edu_facts_label': '¿SABÍAS QUE...?',

      'lobby_connecting':    'Inicializando red P2P...',
      'lobby_share_qr':      'Comparte este QR con otros pilotos',
      'lobby_connect_with':  'CONECTAR CON PILOTO',
      'lobby_node_id_hint':  'Node ID del otro piloto',
      'btn_link':            'LINK',
      'lobby_no_pilots':     'Sin pilotos conectados',
      'lobby_no_pilots_sub': 'Comparte tu QR o ingresa el ID del otro piloto',
      'lobby_play_solo':     'JUGAR SOLO',
      'lobby_launch_with':   'LANZAR CON {n} PILOTOS',
      'lobby_error_connect': 'No se pudo conectar. Verifica el ID.',

      'hud_vel':       'VEL',
      'hud_dest':      'DESTINO',
      'hud_dist':      'DIST',
      'hud_free_mode': 'LIBRE',
      'hud_fuel':      'FUEL',
      'hud_radio':     'RADIO',

      'chat_title':   'RADIO COMMS',
      'chat_hint':    'Transmitir mensaje...',
      'chat_solo':    'Modo solo — sin pilotos conectados',

      'result_scoreboard': 'RANKING DE PILOTOS',
      'result_you':        'TÚ',

      'result_complete':   'MISIÓN COMPLETADA',
      'result_failed':     'MISIÓN FALLIDA',
      'result_points':     'PUNTOS',
      'result_base':       'Base misión',
      'result_fuel_label': 'Combustible',
      'result_time_label': 'Tiempo',
      'result_boost_label':'Boost',
      'result_no_fuel':    'SIN COMBUSTIBLE',
      'result_progress':   '% del camino recorrido',
      'btn_new_mission':   'NUEVA MISIÓN',
      'btn_retry':         'REINTENTAR',
      'btn_menu':          'MENÚ',
      'stat_time':         'TIEMPO',
      'stat_fuel':         'FUEL',
      'stat_dist':         'DIST',

      'planet_mercury': 'Mercurio',
      'planet_venus':   'Venus',
      'planet_mars':    'Marte',
      'planet_jupiter': 'Júpiter',
      'planet_saturn':  'Saturno',
      'planet_uranus':  'Urano',
      'planet_neptune': 'Neptuno',

      'desc_mercury': 'El más cercano al Sol. Temperaturas: −180 °C a 430 °C. Sin atmósfera.',
      'desc_venus':   'El planeta más caliente. Atmósfera densa de CO₂. Superficie a 465 °C.',
      'desc_mars':    'El Planeta Rojo. Hogar del Olympus Mons, el volcán más alto del sistema solar.',
      'desc_jupiter': 'El planeta más grande. La Gran Mancha Roja lleva más de 350 años activa.',
      'desc_saturn':  'Sus anillos abarcan 282,000 km pero tienen solo 10–100 m de grosor.',
      'desc_uranus':  'Gigante de hielo que rota de lado — inclinación axial de 98°.',
      'desc_neptune': 'El más lejano. Sus vientos alcanzan 2,100 km/h — los más fuertes del sistema solar.',
    },
  };

  // ── Educational facts per planet ───────────────────────────────────────
  static const _pf = <String, Map<String, List<String>>>{
    'en': {
      'mercury': [
        'Mercury completes one orbit in just 88 Earth days — the fastest planet.',
        'NASA\'s MESSENGER orbited Mercury for 4 years (2011–2015) and mapped its entire surface.',
      ],
      'venus': [
        'One day on Venus (243 Earth days) is longer than one Venusian year (225 days).',
        'The Magellan probe mapped 98 % of Venus\'s surface using radar in the 1990s.',
      ],
      'mars': [
        'NASA\'s Perseverance rover is actively exploring Mars, collecting samples for future return.',
        'Olympus Mons on Mars is 22 km high — 2.5× taller than Mount Everest.',
      ],
      'jupiter': [
        'Jupiter has 95 known moons. Europa may harbor a liquid ocean beneath its icy shell.',
        'The Great Red Spot is a storm larger than Earth — it has been raging for over 350 years.',
      ],
      'saturn': [
        'Saturn is the least dense planet in the solar system — it would float in water!',
        'NASA\'s Cassini spacecraft orbited Saturn for 13 years, diving between the rings in 2017.',
      ],
      'uranus': [
        'Uranus rotates almost on its side, with an axial tilt of 98°, likely from an ancient collision.',
        'Its 27 known moons are named after Shakespeare characters: Titania, Oberon, Miranda…',
      ],
      'neptune': [
        'Neptune was predicted mathematically 17 years before it was directly observed in 1846.',
        'Triton, Neptune\'s largest moon, orbits in the opposite direction to all other large moons.',
      ],
    },
    'es': {
      'mercury': [
        'Mercurio completa una órbita en solo 88 días terrestres — el planeta más rápido.',
        'La misión MESSENGER de la NASA orbitó Mercurio por 4 años (2011–2015) y mapeó toda su superficie.',
      ],
      'venus': [
        'Un día en Venus (243 días terrestres) es más largo que un año venusiano (225 días).',
        'La sonda Magallanes mapeó el 98 % de la superficie de Venus con radar en los años 90.',
      ],
      'mars': [
        'El rover Perseverance de la NASA explora Marte activamente, recolectando muestras para futura devolución.',
        'Olympus Mons en Marte tiene 22 km de altura — 2,5× más alto que el Everest.',
      ],
      'jupiter': [
        'Júpiter tiene 95 lunas conocidas. Europa podría albergar un océano bajo su corteza helada.',
        'La Gran Mancha Roja es una tormenta más grande que la Tierra — lleva más de 350 años activa.',
      ],
      'saturn': [
        '¡Saturno es el planeta menos denso del sistema solar — flotaría en el agua!',
        'La nave Cassini orbitó Saturno por 13 años, buceando entre sus anillos en 2017.',
      ],
      'uranus': [
        'Urano rota casi de lado con una inclinación axial de 98°, probablemente por una colisión antigua.',
        'Sus 27 lunas conocidas llevan nombres de personajes de Shakespeare: Titania, Oberón, Miranda…',
      ],
      'neptune': [
        'Neptuno fue predicho matemáticamente 17 años antes de ser observado directamente en 1846.',
        'Tritón, la luna más grande de Neptuno, orbita en dirección opuesta a todas las demás.',
      ],
    },
  };
}

class _AppLocalizationsDelegate
    extends LocalizationsDelegate<AppLocalizations> {
  const _AppLocalizationsDelegate();

  @override
  bool isSupported(Locale locale) =>
      ['en', 'es'].contains(locale.languageCode);

  @override
  Future<AppLocalizations> load(Locale locale) async =>
      AppLocalizations(locale);

  @override
  bool shouldReload(_AppLocalizationsDelegate _) => false;
}
