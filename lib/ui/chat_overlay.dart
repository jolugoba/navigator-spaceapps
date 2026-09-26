import 'package:flutter/material.dart';

import '../network/ship_sync_service.dart';
import '../l10n/app_localizations.dart';

class ChatOverlay extends StatefulWidget {
  final ShipSyncService? sync;
  final VoidCallback onClose;

  const ChatOverlay({super.key, required this.sync, required this.onClose});

  @override
  State<ChatOverlay> createState() => _ChatOverlayState();
}

class _ChatOverlayState extends State<ChatOverlay> {
  final _controller     = TextEditingController();
  final _scrollCtrl     = ScrollController();
  final List<_Msg> _msgs = [];

  void _onIncoming(String nodeId, String text) {
    if (!mounted) return;
    setState(() => _msgs.add(_Msg(nodeId: nodeId, text: text, local: false)));
    _scrollBottom();
  }

  @override
  void initState() {
    super.initState();
    widget.sync?.addChatListener(_onIncoming);
  }

  @override
  void dispose() {
    widget.sync?.removeChatListener(_onIncoming);
    _controller.dispose();
    _scrollCtrl.dispose();
    super.dispose();
  }

  Future<void> _send() async {
    final text = _controller.text.trim();
    if (text.isEmpty || widget.sync == null) return;
    _controller.clear();
    await widget.sync!.sendChat(text);
    if (!mounted) return;
    setState(() => _msgs.add(_Msg(nodeId: 'ME', text: text, local: true)));
    _scrollBottom();
  }

  void _scrollBottom() {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (_scrollCtrl.hasClients) {
        _scrollCtrl.animateTo(
          _scrollCtrl.position.maxScrollExtent,
          duration: const Duration(milliseconds: 200),
          curve: Curves.easeOut,
        );
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final s = AppLocalizations.of(context);

    return Positioned(
      bottom: 0, left: 0, right: 0,
      child: Container(
        height: 240,
        decoration: BoxDecoration(
          color: Colors.black.withValues(alpha: 0.88),
          border: Border(
            top: BorderSide(color: const Color(0xFF00E5FF).withValues(alpha: 0.35)),
          ),
        ),
        child: Column(
          children: [
            // ── Header ─────────────────────────────────────────────
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 6),
              child: Row(children: [
                const Icon(Icons.radio, color: Color(0xFF00E5FF), size: 13),
                const SizedBox(width: 6),
                Text(s.chatTitle, style: const TextStyle(
                  color: Color(0xFF00E5FF), fontSize: 10, letterSpacing: 2.5)),
                const Spacer(),
                GestureDetector(
                  onTap: widget.onClose,
                  child: const Icon(Icons.close, color: Colors.white38, size: 16),
                ),
              ]),
            ),
            const Divider(height: 1, color: Colors.white12),

            // ── Messages list ───────────────────────────────────────
            Expanded(
              child: widget.sync == null
                  ? Center(child: Text(s.chatSolo,
                      style: const TextStyle(color: Colors.white24, fontSize: 11)))
                  : _msgs.isEmpty
                      ? const Center(child: Text('—',
                          style: TextStyle(color: Colors.white12, fontSize: 18)))
                      : ListView.builder(
                          controller: _scrollCtrl,
                          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 4),
                          itemCount: _msgs.length,
                          itemBuilder: (_, i) {
                            final m = _msgs[i];
                            final short = m.nodeId.length > 8
                                ? m.nodeId.substring(0, 8)
                                : m.nodeId;
                            return Padding(
                              padding: const EdgeInsets.symmetric(vertical: 2),
                              child: RichText(
                                text: TextSpan(
                                  style: const TextStyle(fontSize: 11, height: 1.4),
                                  children: [
                                    TextSpan(
                                      text: '$short: ',
                                      style: TextStyle(
                                        color: m.local
                                            ? const Color(0xFF00E5FF)
                                            : Colors.orangeAccent,
                                        fontWeight: FontWeight.bold,
                                        letterSpacing: 0.5,
                                      ),
                                    ),
                                    TextSpan(
                                      text: m.text,
                                      style: const TextStyle(color: Colors.white70),
                                    ),
                                  ],
                                ),
                              ),
                            );
                          },
                        ),
            ),

            // ── Input ───────────────────────────────────────────────
            if (widget.sync != null)
              Padding(
                padding: EdgeInsets.only(
                  left: 12, right: 12, bottom:
                      8 + MediaQuery.of(context).viewInsets.bottom),
                child: Row(children: [
                  Expanded(
                    child: TextField(
                      controller: _controller,
                      style: const TextStyle(color: Colors.white, fontSize: 12),
                      decoration: InputDecoration(
                        hintText: s.chatHint,
                        hintStyle: const TextStyle(color: Colors.white24, fontSize: 12),
                        filled: true,
                        fillColor: Colors.white.withValues(alpha: 0.06),
                        contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                        border: OutlineInputBorder(
                          borderRadius: BorderRadius.circular(8),
                          borderSide: const BorderSide(color: Colors.white24),
                        ),
                        enabledBorder: OutlineInputBorder(
                          borderRadius: BorderRadius.circular(8),
                          borderSide: const BorderSide(color: Colors.white24),
                        ),
                        isDense: true,
                      ),
                      onSubmitted: (_) => _send(),
                    ),
                  ),
                  const SizedBox(width: 8),
                  GestureDetector(
                    onTap: _send,
                    child: Container(
                      padding: const EdgeInsets.all(8),
                      decoration: BoxDecoration(
                        color: const Color(0xFF00E5FF).withValues(alpha: 0.15),
                        shape: BoxShape.circle,
                        border: Border.all(
                            color: const Color(0xFF00E5FF).withValues(alpha: 0.5)),
                      ),
                      child: const Icon(Icons.send, color: Color(0xFF00E5FF), size: 14),
                    ),
                  ),
                ]),
              ),
          ],
        ),
      ),
    );
  }
}

class _Msg {
  final String nodeId;
  final String text;
  final bool local;
  const _Msg({required this.nodeId, required this.text, required this.local});
}
