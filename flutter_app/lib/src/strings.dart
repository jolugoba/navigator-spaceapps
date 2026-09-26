// Navigator — texts of the Flutter shell (the game itself is localized inside Unity)

enum AppLang { en, es }

class S {
  S(this.lang);
  final AppLang lang;

  bool get es => lang == AppLang.es;

  String get tagline => es
      ? 'Diseña, lanza y opera una misión espacial real'
      : 'Design, launch and fly a real space mission';
  String get challenge => 'NASA Space Apps Challenge 2026';
  String get pilotName => es ? 'Nombre del piloto' : 'Pilot name';
  String get pilotHint => es ? 'Cadete' : 'Cadet';
  String get launch => es ? 'INICIAR MISIÓN' : 'START MISSION';
  String get language => es ? 'ENGLISH' : 'ESPAÑOL';
  String get missionLog => es ? 'Bitácora de misiones' : 'Mission log';
  String get noMissions => es
      ? 'Aún no hay misiones. ¡Lanza la primera!'
      : 'No missions yet. Launch your first one!';
  String get best => es ? 'Mejor puntaje' : 'Best score';
  String get flown => es ? 'Misiones' : 'Missions';
  String get arrived => es ? 'Llegó' : 'Arrived';
  String get failed => es ? 'Fallida' : 'Failed';
  String get clearLog => es ? 'Borrar bitácora' : 'Clear log';
  String get loading => es ? 'Cargando el motor 3D…' : 'Loading the 3D engine…';
  String get permissionsNote => es
      ? 'La cámara muestra el espacio sobre tu cuarto (AR). La ubicación ubica los planetas reales en tu cielo.'
      : 'The camera shows space over your room (AR). Location places the real planets in your sky.';
  String get noUnityTitle => es ? 'Falta exportar Unity' : 'Unity is not exported yet';
  String get noUnityBody => es
      ? 'Esta compilación no incluye el juego. En Unity abre el menú\n\n'
          'Navigator → Flutter → Export Android → flutter_app\n\n'
          'y luego vuelve a ejecutar:\n\nflutter run'
      : 'This build does not include the game. In Unity open the menu\n\n'
          'Navigator → Flutter → Export Android → flutter_app\n\n'
          'then run again:\n\nflutter run';
  String get back => es ? 'Volver' : 'Back';

  String planet(String id) {
    const en = {
      'mercury': 'Mercury', 'venus': 'Venus', 'mars': 'Mars', 'jupiter': 'Jupiter',
      'saturn': 'Saturn', 'uranus': 'Uranus', 'neptune': 'Neptune',
    };
    const esNames = {
      'mercury': 'Mercurio', 'venus': 'Venus', 'mars': 'Marte', 'jupiter': 'Júpiter',
      'saturn': 'Saturno', 'uranus': 'Urano', 'neptune': 'Neptuno',
    };
    return (es ? esNames : en)[id] ?? id;
  }

  String objective(String id) {
    switch (id) {
      case 'flyby':
        return es ? 'Sobrevuelo' : 'Flyby';
      case 'orbit':
        return es ? 'Órbita' : 'Orbit';
      case 'landing':
        return es ? 'Aterrizaje' : 'Landing';
    }
    return id;
  }
}
