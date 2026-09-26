// Tipos de mensajes P2P entre naves NAVIGATOR
enum NavMsgType {
  shipState,    // Posición/velocidad/rumbo — cada 50ms
  lobbyInfo,    // Presentación al conectarse (nombre, nave)
  chatMsg,      // Mensaje de chat en vuelo
  shipEvent,    // Evento: llegó al planeta, boost, etc.
  missionSync,  // Sincronizar estado de misión
  ping,         // Latencia
  missionEnd,   // Puntuación final al terminar misión
}

class NavMessage {
  final NavMsgType type;
  final String fromNodeId;
  final Map<String, dynamic> payload;
  final DateTime ts;

  const NavMessage({
    required this.type,
    required this.fromNodeId,
    required this.payload,
    required this.ts,
  });

  Map<String, dynamic> toJson() => {
    't': type.name,
    'from': fromNodeId,
    'pl': payload,
    'ts': ts.millisecondsSinceEpoch,
  };

  factory NavMessage.fromJson(Map<String, dynamic> j) {
    final typeStr = j['t'] as String;
    final type = NavMsgType.values.firstWhere(
      (e) => e.name == typeStr,
      orElse: () => NavMsgType.ping,
    );
    return NavMessage(
      type: type,
      fromNodeId: j['from'] as String,
      payload: Map<String, dynamic>.from(j['pl'] as Map),
      ts: DateTime.fromMillisecondsSinceEpoch(j['ts'] as int),
    );
  }
}

// Eventos de nave
class ShipEvent {
  static const String arrivedAtPlanet = 'arrived';
  static const String boosted         = 'boost';
  static const String docked          = 'docked';
  static const String launched        = 'launched';
  static const String destroyed       = 'destroyed';
}
