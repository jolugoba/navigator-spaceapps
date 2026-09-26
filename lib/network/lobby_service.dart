import 'package:flutter/material.dart';
import 'package:senda_p2p_chat/senda_p2p_chat.dart';

class LobbyService extends ChangeNotifier {
  final SendaP2PClient _p2p = SendaP2PClient();
  bool _initialized = false;
  String? _error;

  // Lista propia de contactos conectados (Senda no expone getter público)
  final List<SendaContact> _contacts = [];

  String get nodeId    => _initialized ? _p2p.nodeId : '';
  bool   get ready     => _initialized;
  String? get error    => _error;
  List<SendaContact> get contacts => List.unmodifiable(_contacts);

  Future<void> init() async {
    try {
      await _p2p.init();
      _initialized = true;
      _p2p.onPeerConnected.listen((c) {
        if (!_contacts.any((x) => x.nodeId == c.nodeId)) _contacts.add(c);
        notifyListeners();
      });
      _p2p.onPeerDisconnected.listen((nodeId) {
        _contacts.removeWhere((c) => c.nodeId == nodeId);
        notifyListeners();
      });
      notifyListeners();
    } catch (e) {
      _error = e.toString();
      notifyListeners();
    }
  }

  Future<bool> connectToPlayer(String remoteNodeId) async {
    if (!_initialized) return false;
    try {
      final contact = await _p2p.connectToNode(remoteNodeId);
      if (contact != null && !_contacts.any((c) => c.nodeId == contact.nodeId)) {
        _contacts.add(contact);
      }
      notifyListeners();
      return contact != null;
    } catch (_) {
      return false;
    }
  }

  SendaP2PClient get client => _p2p;

  @override
  void dispose() {
    _p2p.dispose();
    super.dispose();
  }
}
