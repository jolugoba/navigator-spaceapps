import 'package:flutter/material.dart';
import 'package:qr_flutter/qr_flutter.dart';
import 'package:senda_p2p_chat/senda_p2p_chat.dart';

import '../network/lobby_service.dart';
import '../models/player_info.dart';
import '../l10n/app_localizations.dart';

class LobbyScreen extends StatefulWidget {
  final LobbyService lobby;
  final PlayerInfo localPlayer;
  final void Function(List<SendaContact> contacts) onStartGame;

  const LobbyScreen({
    super.key,
    required this.lobby,
    required this.localPlayer,
    required this.onStartGame,
  });

  @override
  State<LobbyScreen> createState() => _LobbyScreenState();
}

class _LobbyScreenState extends State<LobbyScreen> {
  final _nodeIdController = TextEditingController();
  bool _connecting = false;
  String? _connectError;

  @override
  void dispose() {
    _nodeIdController.dispose();
    super.dispose();
  }

  Future<void> _connectManual() async {
    final id = _nodeIdController.text.trim();
    if (id.isEmpty) return;
    setState(() { _connecting = true; _connectError = null; });
    final ok = await widget.lobby.connectToPlayer(id);
    if (!mounted) return;
    final s = AppLocalizations.of(context);
    setState(() {
      _connecting = false;
      _connectError = ok ? null : s.lobbyErrorConnect;
    });
    if (ok) _nodeIdController.clear();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF010408),
      body: SafeArea(
        child: ListenableBuilder(
          listenable: widget.lobby,
          builder: (context, _) {
            final s = AppLocalizations.of(context);
            if (!widget.lobby.ready) {
              return Center(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    const CircularProgressIndicator(color: Color(0xFF00E5FF)),
                    const SizedBox(height: 16),
                    Text(s.lobbyConnecting,
                      style: const TextStyle(color: Colors.white54)),
                  ],
                ),
              );
            }

            if (widget.lobby.error != null) {
              return Center(
                child: Text('Error: ${widget.lobby.error}',
                  style: const TextStyle(color: Colors.redAccent)),
              );
            }

            return SingleChildScrollView(
              padding: const EdgeInsets.all(24),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.center,
                children: [
                  // Header
                  const Text('NAVIGATOR',
                    style: TextStyle(
                      color: Color(0xFF00E5FF),
                      fontSize: 28,
                      fontWeight: FontWeight.w900,
                      letterSpacing: 8,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text('LOBBY — ${widget.localPlayer.displayName}',
                    style: const TextStyle(color: Colors.white38, fontSize: 12,
                      letterSpacing: 3),
                  ),
                  const SizedBox(height: 32),

                  // QR de este jugador
                  Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: Colors.white,
                      borderRadius: BorderRadius.circular(16),
                    ),
                    child: QrImageView(
                      data: widget.lobby.nodeId,
                      size: 200,
                      backgroundColor: Colors.white,
                    ),
                  ),
                  const SizedBox(height: 12),
                  Text(s.lobbyShareQr,
                    style: const TextStyle(color: Colors.white38, fontSize: 12)),
                  const SizedBox(height: 8),
                  SelectableText(
                    widget.lobby.nodeId,
                    style: const TextStyle(
                      color: Colors.white24, fontSize: 9, letterSpacing: 1,
                      fontFamily: 'monospace',
                    ),
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 32),

                  _ConnectInput(
                    controller: _nodeIdController,
                    loading: _connecting,
                    error: _connectError,
                    onConnect: _connectManual,
                    s: s,
                  ),
                  const SizedBox(height: 28),

                  _ConnectedPlayers(contacts: widget.lobby.contacts, s: s),
                  const SizedBox(height: 32),

                  // Botón lanzar
                  SizedBox(
                    width: double.infinity,
                    child: ElevatedButton(
                      onPressed: () =>
                          widget.onStartGame(widget.lobby.contacts),
                      style: ElevatedButton.styleFrom(
                        backgroundColor: const Color(0xFF00E5FF),
                        foregroundColor: Colors.black,
                        padding: const EdgeInsets.symmetric(vertical: 18),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12)),
                      ),
                      child: Text(
                        widget.lobby.contacts.isEmpty
                            ? s.lobbyPlaySolo
                            : s.lobbyLaunchWith(widget.lobby.contacts.length + 1),
                        style: const TextStyle(
                          fontWeight: FontWeight.w900,
                          letterSpacing: 3,
                          fontSize: 14,
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            );
          },
        ),
      ),
    );
  }
}

class _ConnectInput extends StatelessWidget {
  final TextEditingController controller;
  final bool loading;
  final String? error;
  final VoidCallback onConnect;
  final AppLocalizations s;

  const _ConnectInput({
    required this.controller,
    required this.loading,
    required this.error,
    required this.onConnect,
    required this.s,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(s.lobbyConnectWith,
          style: const TextStyle(color: Colors.white38, fontSize: 10, letterSpacing: 2)),
        const SizedBox(height: 8),
        Row(
          children: [
            Expanded(
              child: TextField(
                controller: controller,
                style: const TextStyle(color: Colors.white, fontSize: 12),
                decoration: InputDecoration(
                  hintText: s.lobbyNodeIdHint,
                  hintStyle: TextStyle(color: Colors.white24, fontSize: 12),
                  filled: true,
                  fillColor: Colors.white.withValues(alpha: 0.06),
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(10),
                    borderSide: BorderSide(color: Colors.white24),
                  ),
                  enabledBorder: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(10),
                    borderSide: const BorderSide(color: Colors.white24),
                  ),
                  contentPadding: const EdgeInsets.symmetric(
                    horizontal: 14, vertical: 12),
                ),
              ),
            ),
            const SizedBox(width: 10),
            SizedBox(
              height: 48,
              child: loading
                  ? const Center(
                      child: SizedBox(
                        width: 24, height: 24,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          color: Color(0xFF00E5FF),
                        ),
                      ),
                    )
                  : ElevatedButton(
                      onPressed: onConnect,
                      style: ElevatedButton.styleFrom(
                        backgroundColor: const Color(0xFF00E5FF).withValues(alpha: 0.2),
                        foregroundColor: const Color(0xFF00E5FF),
                        side: const BorderSide(color: Color(0xFF00E5FF)),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(10)),
                      ),
                      child: Text(s.btnLink, style: const TextStyle(
                        fontSize: 11, fontWeight: FontWeight.bold,
                        letterSpacing: 2,
                      )),
                    ),
            ),
          ],
        ),
        if (error != null)
          Padding(
            padding: const EdgeInsets.only(top: 6),
            child: Text(error!,
              style: const TextStyle(color: Colors.redAccent, fontSize: 11)),
          ),
      ],
    );
  }
}

class _ConnectedPlayers extends StatelessWidget {
  final List<SendaContact> contacts;
  final AppLocalizations s;
  const _ConnectedPlayers({required this.contacts, required this.s});

  @override
  Widget build(BuildContext context) {
    if (contacts.isEmpty) {
      return Container(
        width: double.infinity,
        padding: const EdgeInsets.symmetric(vertical: 20),
        decoration: BoxDecoration(
          color: Colors.white.withValues(alpha: 0.03),
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: Colors.white12),
        ),
        child: Column(
          children: [
            const Icon(Icons.wifi_off, color: Colors.white24, size: 28),
            const SizedBox(height: 8),
            Text(s.lobbyNoPilots,
              style: const TextStyle(color: Colors.white24, fontSize: 12)),
            const SizedBox(height: 4),
            Text(s.lobbyNoPilotsSub,
              style: const TextStyle(color: Colors.white12, fontSize: 10)),
          ],
        ),
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('${contacts.length} PILOT${contacts.length != 1 ? 'S' : ''} CONNECTED',
          style: const TextStyle(color: Colors.white38, fontSize: 10, letterSpacing: 2)),
        const SizedBox(height: 8),
        ...contacts.map((c) {
          final short = c.nodeId.length > 16
              ? '${c.nodeId.substring(0, 8)}...${c.nodeId.substring(c.nodeId.length - 4)}'
              : c.nodeId;
          return Container(
            margin: const EdgeInsets.only(bottom: 6),
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
            decoration: BoxDecoration(
              color: Colors.orange.withValues(alpha: 0.08),
              borderRadius: BorderRadius.circular(10),
              border: Border.all(color: Colors.orange.withValues(alpha: 0.3)),
            ),
            child: Row(
              children: [
                Container(
                  width: 8, height: 8,
                  decoration: const BoxDecoration(
                    color: Colors.orangeAccent, shape: BoxShape.circle,
                  ),
                ),
                const SizedBox(width: 10),
                Text(short, style: const TextStyle(
                  color: Colors.white70, fontSize: 12, fontFamily: 'monospace',
                )),
                const Spacer(),
                const Icon(Icons.lock_outline, color: Colors.green, size: 12),
                const SizedBox(width: 4),
                const Text('PQC', style: TextStyle(
                  color: Colors.green, fontSize: 9, letterSpacing: 1,
                )),
              ],
            ),
          );
        }),
      ],
    );
  }
}
