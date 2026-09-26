// Navigator 2.0 — P2PChatSystem.cs
// Chat P2P offline via SendaBridge (Iroh + Dilithium-5)
// Mismo protocolo que Flutter ship_sync_service.dart — interoperable
// Guacalita Inc. — SAL / Claude

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Navigator.Network
{
    public enum MessageType { Text, Reaction, System }

    [Serializable]
    public class ChatMessage
    {
        public string      From;
        public string      Text;
        public MessageType Type;
        public long        Timestamp;
    }

    public class P2PChatSystem : MonoBehaviour
    {
        public const int MaxHistory = 20;

        public UnityEvent<ChatMessage> OnMessageReceived  = new();
        public UnityEvent<ChatMessage> OnSystemEvent      = new();
        public UnityEvent<string>      OnNewMessageNotify = new();

        public List<ChatMessage> History { get; } = new();

        public static readonly string[] QuickReactions = { "🔥", "👀", "🚀", "💥" };

        private SendaBridge _senda;
        private string      _myNodeId;
        private string      _myName = "Cadete";

        void Awake()
        {
            _senda = GetComponent<SendaBridge>();
            if (_senda != null)
            {
                _myNodeId = _senda.NodeId;
                _senda.OnMessage.AddListener(OnNavMessage);
            }
        }

        public void SetPlayerName(string name) => _myName = name;

        // ── Enviar ────────────────────────────────────────────────────────────

        public void SendText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            text = text[..Math.Min(100, text.Length)];
            var msg = Build(MessageType.Text, _myName, text);
            BroadcastChat(msg);
            AddToHistory(msg);
        }

        public void SendReaction(string emoji)
        {
            var msg = Build(MessageType.Reaction, _myName, emoji);
            BroadcastChat(msg);
            AddToHistory(msg);
        }

        // ── Eventos del sistema (siempre visibles) ────────────────────────────

        public void BroadcastTakedown(string attacker, string victim)   => BroadcastSystem($"🔥 TAKEDOWN! {attacker} destruyó a {victim}");
        public void BroadcastBurnout(string player)                     => BroadcastSystem($"⚡ {player} activó BURNOUT MODE");
        public void BroadcastArrival(string player, string planet, int pos)
        {
            string medal = pos == 1 ? "🏆" : pos == 2 ? "🥈" : "🥉";
            BroadcastSystem($"{medal} {player} llegó a {planet} — #{pos}");
        }
        public void BroadcastFuelEmpty(string player) => BroadcastSystem($"💀 {player} quedó sin combustible");
        public void BroadcastPowerup(string player, string pod) => BroadcastSystem($"🛡️ {player} recogió {pod}");

        // Wrappers de un argumento — usados desde PowerupSystem y ScienceOpsUI
        public void PowerupBroadcast(string podType) => BroadcastPowerup(_myName, podType);
        public void ArrivalBroadcast(int position)   => BroadcastArrival(_myName, "destino", position);

        // ── Recibir mensajes Senda ─────────────────────────────────────────────

        private void OnNavMessage(NavMsg nav)
        {
            if (nav.t != NavMsgType.chatMsg.ToString() &&
                nav.t != NavMsgType.shipEvent.ToString()) return;

            string text = ExtractString(nav.pl_json, "text");
            if (string.IsNullOrEmpty(text)) return;   // laser / scan events carry no chat text
            string from = ExtractString(nav.pl_json, "name");
            if (string.IsNullOrEmpty(from)) from = nav.from[..Math.Min(8, nav.from.Length)];
            bool   isSystem = nav.t == NavMsgType.shipEvent.ToString();

            var msg = Build(isSystem ? MessageType.System : MessageType.Text, from, text);
            AddToHistory(msg);
            OnMessageReceived.Invoke(msg);
            OnNewMessageNotify.Invoke(from);
            if (isSystem) OnSystemEvent.Invoke(msg);
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void BroadcastChat(ChatMessage msg)
        {
            string json = $"{{\"name\":\"{msg.From}\",\"text\":\"{msg.Text}\"}}";
            _senda?.Broadcast(NavMsg.Build(NavMsgType.chatMsg, _myNodeId, json));
        }

        private void BroadcastSystem(string text)
        {
            var msg = Build(MessageType.System, "SISTEMA", text);
            string json = $"{{\"name\":\"SISTEMA\",\"text\":\"{text}\"}}";
            _senda?.Broadcast(NavMsg.Build(NavMsgType.shipEvent, _myNodeId, json));
            AddToHistory(msg);
            OnSystemEvent.Invoke(msg);
        }

        private void AddToHistory(ChatMessage msg)
        {
            if (History.Count >= MaxHistory) History.RemoveAt(0);
            History.Add(msg);
        }

        private static ChatMessage Build(MessageType type, string from, string text) => new()
        {
            From      = from,
            Text      = text,
            Type      = type,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };

        private static string ExtractString(string json, string key)
        {
            string search = $"\"{key}\":\"";
            int s = json.IndexOf(search);
            if (s < 0) return "";
            s += search.Length;
            int e = json.IndexOf("\"", s);
            return e > s ? json[s..e] : "";
        }
    }
}
