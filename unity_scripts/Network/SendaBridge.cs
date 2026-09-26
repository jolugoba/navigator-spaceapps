// Navigator 2.0 — SendaBridge.cs
// Wraps the same native Senda library used by the Flutter plugin senda_p2p_chat
// Iroh P2P + Dilithium-5 signatures — same as Navigator Flutter
// P/Invoke to senda_core.dll (Android: libsenda_core.so, iOS: senda_core.a)
//
// Protocol: identical JSON to Flutter NavMessage — Flutter ↔ Unity interoperable
// Guacalita Inc. — SAL / Claude

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace Navigator.Network
{
    // ── Native function signatures (senda_core Rust FFI) ─────────────────────
    // These match the exported functions in senda/native/senda_core/src/ffi.rs
    internal static class SendaFFI
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private const string LIB = "senda_core";
#elif UNITY_IOS && !UNITY_EDITOR
        private const string LIB = "__Internal";
#else
        private const string LIB = "senda_core";  // senda_core.dll on Windows/Editor
#endif

        // Initialize Iroh node — returns node_id as null-terminated UTF-8
        [DllImport(LIB)] internal static extern IntPtr senda_init();

        // Get our node_id string
        [DllImport(LIB)] internal static extern IntPtr senda_node_id();

        // Connect to a remote node by node_id string
        // Returns 1 on success, 0 on failure
        [DllImport(LIB, CharSet = CharSet.Ansi)]
        internal static extern int senda_connect(string remoteNodeId);

        // Send UTF-8 text to a connected node
        // Returns 1 on success
        [DllImport(LIB, CharSet = CharSet.Ansi)]
        internal static extern int senda_send(string remoteNodeId, string message);

        // Broadcast to all connected nodes
        [DllImport(LIB, CharSet = CharSet.Ansi)]
        internal static extern int senda_broadcast(string message);

        // Poll for next received message — returns null if none
        // Caller must free with senda_free_string
        [DllImport(LIB)] internal static extern IntPtr senda_poll_message();

        // Poll for next peer-connected event — returns nodeId or null
        [DllImport(LIB)] internal static extern IntPtr senda_poll_connected();

        // Poll for next peer-disconnected event — returns nodeId or null
        [DllImport(LIB)] internal static extern IntPtr senda_poll_disconnected();

        // Sign a message with Dilithium-5
        [DllImport(LIB, CharSet = CharSet.Ansi)]
        internal static extern IntPtr senda_sign(string message);

        // Verify a Dilithium-5 signature
        [DllImport(LIB, CharSet = CharSet.Ansi)]
        internal static extern int senda_verify(string message, string signature, string senderNodeId);

        // Free a string returned by any senda_* function
        [DllImport(LIB)] internal static extern void senda_free_string(IntPtr ptr);

        // Shutdown node cleanly
        [DllImport(LIB)] internal static extern void senda_shutdown();

        // Helpers
        internal static string PtrToStringUtf8(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return null;
            int len = 0;
            while (Marshal.ReadByte(ptr, len) != 0) len++;
            byte[] bytes = new byte[len];
            Marshal.Copy(ptr, bytes, 0, len);
            return Encoding.UTF8.GetString(bytes);
        }
    }

    // ── SendaBridge MonoBehaviour ─────────────────────────────────────────────

    public class SendaBridge : MonoBehaviour
    {
        // ── Events (same semantic as Flutter SendaP2PClient streams) ──────────
        public UnityEvent<string>      OnPeerConnected    = new();  // remoteNodeId
        public UnityEvent<string>      OnPeerDisconnected = new();  // remoteNodeId
        public UnityEvent<NavMsg>      OnMessage          = new();  // parsed message

        // ── State ──────────────────────────────────────────────────────────────
        public  string NodeId       { get; private set; }
        public  bool   Initialized  { get; private set; }

        private readonly List<string> _connectedPeers = new();
        public  IReadOnlyList<string>  ConnectedPeers => _connectedPeers;

        // ── Init ──────────────────────────────────────────────────────────────

        public bool Init()
        {
            try
            {
                IntPtr ptr = SendaFFI.senda_init();
                NodeId      = SendaFFI.PtrToStringUtf8(ptr);
                SendaFFI.senda_free_string(ptr);
                Initialized = !string.IsNullOrEmpty(NodeId);
                Debug.Log($"[Senda] Initialized. NodeId: {NodeId}");
                return Initialized;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Senda] Init failed: {e.Message}");
                return false;
            }
        }

        // ── Connect to another node (Flutter or Unity) ────────────────────────
        public bool ConnectToNode(string remoteNodeId)
        {
            int ok = SendaFFI.senda_connect(remoteNodeId);
            if (ok == 1)
            {
                if (!_connectedPeers.Contains(remoteNodeId))
                    _connectedPeers.Add(remoteNodeId);
                Debug.Log($"[Senda] Connected to {remoteNodeId}");
            }
            return ok == 1;
        }

        // ── Send NavMessage JSON to all peers ─────────────────────────────────
        public void Broadcast(NavMsg msg)
        {
            string json = msg.ToJson();
            SendaFFI.senda_broadcast(json);
        }

        public void Send(string remoteNodeId, NavMsg msg)
        {
            SendaFFI.senda_send(remoteNodeId, msg.ToJson());
        }

        // ── Poll loop — call every frame (Update) ──────────────────────────────
        void Update()
        {
            PollMessages();
            PollConnected();
            PollDisconnected();
        }

        private void PollMessages()
        {
            IntPtr ptr;
            while ((ptr = SendaFFI.senda_poll_message()) != IntPtr.Zero)
            {
                string json = SendaFFI.PtrToStringUtf8(ptr);
                SendaFFI.senda_free_string(ptr);
                try
                {
                    var msg = NavMsg.FromJson(json);
                    if (msg != null) OnMessage.Invoke(msg);
                }
                catch { /* malformed packet */ }
            }
        }

        private void PollConnected()
        {
            IntPtr ptr;
            while ((ptr = SendaFFI.senda_poll_connected()) != IntPtr.Zero)
            {
                string nodeId = SendaFFI.PtrToStringUtf8(ptr);
                SendaFFI.senda_free_string(ptr);
                if (!_connectedPeers.Contains(nodeId)) _connectedPeers.Add(nodeId);
                OnPeerConnected.Invoke(nodeId);
                Debug.Log($"[Senda] Peer connected: {nodeId}");
            }
        }

        private void PollDisconnected()
        {
            IntPtr ptr;
            while ((ptr = SendaFFI.senda_poll_disconnected()) != IntPtr.Zero)
            {
                string nodeId = SendaFFI.PtrToStringUtf8(ptr);
                SendaFFI.senda_free_string(ptr);
                _connectedPeers.Remove(nodeId);
                OnPeerDisconnected.Invoke(nodeId);
            }
        }

        void OnDestroy() => SendaFFI.senda_shutdown();
    }

    // ── NavMsg — identical protocol to Flutter NavMessage ────────────────────
    // nav_protocol.dart: type, fromNodeId, payload, ts
    // Flutter and Unity parse the same JSON — full interoperability

    [Serializable]
    public class NavMsg
    {
        public string t;         // NavMsgType.name
        public string from;      // fromNodeId
        public string pl_json;   // payload serialized as JSON string
        public long   ts;        // millisecondsSinceEpoch

        public static NavMsg Build(NavMsgType type, string fromNodeId, string payloadJson) => new()
        {
            t        = type.ToString(),
            from     = fromNodeId,
            pl_json  = payloadJson,
            ts       = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };

        public string ToJson() =>
            $"{{\"t\":\"{t}\",\"from\":\"{from}\",\"pl\":{pl_json},\"ts\":{ts}}}";

        public static NavMsg FromJson(string json)
        {
            // Minimal parser — Unity JsonUtility can't handle nested dynamic
            var msg = new NavMsg();
            msg.from = Extract(json, "\"from\":\"", "\"");
            msg.t    = Extract(json, "\"t\":\"",    "\"");
            int plStart = json.IndexOf("\"pl\":") + 5;
            int plEnd   = json.LastIndexOf(",\"ts\":");
            msg.pl_json = plEnd > plStart ? json[plStart..plEnd] : "{}";
            string tsStr = Extract(json, "\"ts\":", "}");
            long.TryParse(tsStr, out msg.ts);
            return msg;
        }

        private static string Extract(string src, string open, string close)
        {
            int s = src.IndexOf(open);
            if (s < 0) return "";
            s += open.Length;
            int e = src.IndexOf(close, s);
            return e > s ? src[s..e] : "";
        }
    }

    public enum NavMsgType
    {
        shipState,      // ShipStateNet — every 50ms
        lobbyInfo,      // PlayerInfo on connect
        chatMsg,        // P2P chat text
        shipEvent,      // arrived, boost, destroyed
        missionSync,    // mission state
        ping,
        missionEnd,     // final score
    }
}
