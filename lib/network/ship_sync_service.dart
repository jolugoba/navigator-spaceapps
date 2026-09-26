import 'dart:async';
import 'dart:convert';
import 'package:senda_p2p_chat/senda_p2p_chat.dart';

import '../models/ship_state.dart';
import '../models/player_info.dart';
import 'nav_protocol.dart';

class ShipSyncService {
  final SendaP2PClient _p2p;
  final PlayerInfo localPlayer;

  final List<SendaContact> _knownContacts = [];

  Timer? _broadcastTimer;
  ShipState? _lastBroadcast;

  final _remoteController =
      StreamController<Map<String, ShipState>>.broadcast();
  Stream<Map<String, ShipState>> get onRemoteUpdate => _remoteController.stream;

  final _scoreController =
      StreamController<Map<String, int>>.broadcast();
  Stream<Map<String, int>> get onRemoteScore => _scoreController.stream;

  final Map<String, ShipState> _remoteShips = {};
  Map<String, ShipState> get remoteShips => Map.unmodifiable(_remoteShips);

  final Map<String, int> _remoteScores = {};
  Map<String, int> get remoteScores => Map.unmodifiable(_remoteScores);

  final List<void Function(String nodeId, String text)> _chatListeners = [];

  ShipSyncService({required SendaP2PClient p2p, required this.localPlayer})
      : _p2p = p2p {
    _p2p.onMessage.listen(_onMessage);
    _p2p.onPeerConnected.listen(_onPeerConnected);
    _p2p.onPeerDisconnected.listen(_onPeerDisconnected);
  }

  void startBroadcast() {
    _broadcastTimer = Timer.periodic(
      const Duration(milliseconds: 50),
      (_) {
        if (_lastBroadcast != null) _sendToAll(_lastBroadcast!.toJson());
      },
    );
  }

  void broadcast(ShipState state) {
    _lastBroadcast = state;
  }

  void broadcastMissionEnd(int score) {
    final msg = NavMessage(
      type:       NavMsgType.missionEnd,
      fromNodeId: localPlayer.nodeId,
      payload:    {'score': score},
      ts:         DateTime.now(),
    );
    _sendToAll(msg.toJson());
  }

  Future<void> sendChat(String text) async {
    final msg = NavMessage(
      type:       NavMsgType.chatMsg,
      fromNodeId: localPlayer.nodeId,
      payload:    {'text': text},
      ts:         DateTime.now(),
    );
    _sendToAll(msg.toJson());
  }

  void addChatListener(void Function(String nodeId, String text) l) =>
      _chatListeners.add(l);
  void removeChatListener(void Function(String nodeId, String text) l) =>
      _chatListeners.remove(l);

  Future<void> _sendToAll(Map<String, dynamic> data) async {
    final encoded = jsonEncode(data);
    for (final contact in _knownContacts) {
      try { await _p2p.send(contact, encoded); } catch (_) {}
    }
  }

  void _onMessage(SendaMessage msg) {
    try {
      final data = jsonDecode(msg.text) as Map<String, dynamic>;
      final nav  = NavMessage.fromJson(data);

      switch (nav.type) {
        case NavMsgType.shipState:
          final state = ShipState.fromJson(nav.payload);
          _remoteShips[state.nodeId] = state;
          _remoteController.add(Map.from(_remoteShips));

        case NavMsgType.chatMsg:
          final text = nav.payload['text'] as String? ?? '';
          for (final l in _chatListeners) { l(nav.fromNodeId, text); }

        case NavMsgType.missionEnd:
          final score = nav.payload['score'] as int? ?? 0;
          _remoteScores[nav.fromNodeId] = score;
          _scoreController.add(Map.from(_remoteScores));

        case NavMsgType.lobbyInfo:
          break;

        default:
          break;
      }
    } catch (_) {}
  }

  void _onPeerConnected(SendaContact contact) {
    if (!_knownContacts.any((c) => c.nodeId == contact.nodeId)) {
      _knownContacts.add(contact);
    }
    final info = NavMessage(
      type:       NavMsgType.lobbyInfo,
      fromNodeId: localPlayer.nodeId,
      payload:    localPlayer.toJson(),
      ts:         DateTime.now(),
    );
    try { _p2p.send(contact, jsonEncode(info.toJson())); } catch (_) {}
  }

  void _onPeerDisconnected(String nodeId) {
    _knownContacts.removeWhere((c) => c.nodeId == nodeId);
    _remoteShips.remove(nodeId);
    _remoteController.add(Map.from(_remoteShips));
  }

  void dispose() {
    _broadcastTimer?.cancel();
    _remoteController.close();
    _scoreController.close();
  }
}
