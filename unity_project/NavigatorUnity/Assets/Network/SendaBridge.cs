// Navigator 2.0 — SendaBridge.cs
// P/Invoke to senda_iroh_ffi (copied as senda_core.dll / libsenda_core.so)
// Matches the actual exported symbols in senda/senda_iroh_ffi/src/lib.rs
// Guacalita Inc. — SAL / Claude

using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace Navigator.Network
{
    // ── Native bindings — match lib.rs exports exactly ───────────────────────
    internal static class SendaFFI
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private const string LIB = "senda_core";   // libsenda_core.so
#elif UNITY_IOS && !UNITY_EDITOR
        private const string LIB = "__Internal";
#else
        private const string LIB = "senda_core";   // senda_core.dll (Windows/Editor)
#endif

        // iroh_init() — 0=OK, negative=error
        [DllImport(LIB)] internal static extern int  iroh_init();

        // iroh_node_id() — returns heap string, free with iroh_free_string
        [DllImport(LIB)] internal static extern IntPtr iroh_node_id();

        // iroh_connect(node_id_cstr) — 0=OK, negative=error
        [DllImport(LIB, CharSet = CharSet.Ansi)]
        internal static extern int iroh_connect(string remoteNodeId);

        // iroh_send(node_id_cstr, data_ptr, data_len) — returns bytes sent or negative
        [DllImport(LIB, CharSet = CharSet.Ansi)]
        internal static extern long iroh_send(string remoteNodeId,
                                              [In] byte[] data, ulong dataLen);

        // iroh_set_message_callback — called from Rust background thread
        [DllImport(LIB)]
        internal static extern void iroh_set_message_callback(OnMessageDelegate cb);

        // iroh_disconnect(node_id_cstr)
        [DllImport(LIB, CharSet = CharSet.Ansi)]
        internal static extern int iroh_disconnect(string remoteNodeId);

        // iroh_shutdown — close endpoint cleanly
        [DllImport(LIB)] internal static extern void iroh_shutdown();

        // iroh_free_string — free strings returned by iroh_node_id
        [DllImport(LIB)] internal static extern void iroh_free_string(IntPtr ptr);

        // Callback delegate — matches OnMessageCallback in lib.rs
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void OnMessageDelegate(
            IntPtr fromNodeCStr, IntPtr dataPtr, ulong dataLen);

        internal static string PtrToStringUtf8(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return null;
            int len = 0;
            while (Marshal.ReadByte(ptr, len) != 0) len++;
            byte[] bytes = new byte[len];
            Marshal.Copy(ptr, bytes, 0, len);
            return Encoding.UTF8.GetString(bytes);
        }

        internal static string PtrBytesToString(IntPtr dataPtr, ulong dataLen)
        {
            if (dataPtr == IntPtr.Zero || dataLen == 0) return null;
            byte[] bytes = new byte[dataLen];
            Marshal.Copy(dataPtr, bytes, 0, (int)dataLen);
            return Encoding.UTF8.GetString(bytes);
        }
    }

    // ── SendaBridge MonoBehaviour ─────────────────────────────────────────────

    public class SendaBridge : MonoBehaviour
    {
        public UnityEvent<string> OnPeerConnected    = new();
        public UnityEvent<string> OnPeerDisconnected = new();
        public UnityEvent<NavMsg> OnMessage          = new();

        public  string NodeId      { get; private set; }
        public  bool   Initialized { get; private set; }

        private readonly List<string>          _connectedPeers = new();
        public  IReadOnlyList<string>           ConnectedPeers => _connectedPeers;

        // Static because IL2CPP reverse P/Invoke only accepts static [MonoPInvokeCallback] methods
        private static readonly ConcurrentQueue<(string from, string json)> _inbox = new();

        // Keep delegate alive to prevent GC collection
        private static SendaFFI.OnMessageDelegate _msgDelegate;

        // ── Init ──────────────────────────────────────────────────────────────
        public bool Init()
        {
            try
            {
                int result = SendaFFI.iroh_init();
                if (result != 0) { Debug.LogError($"[Senda] iroh_init failed: {result}"); return false; }

                IntPtr ptr = SendaFFI.iroh_node_id();
                NodeId = SendaFFI.PtrToStringUtf8(ptr);
                SendaFFI.iroh_free_string(ptr);
                Initialized = !string.IsNullOrEmpty(NodeId);

                // Register message callback (called from Rust background thread)
                _msgDelegate ??= OnNativeMessage;
                SendaFFI.iroh_set_message_callback(_msgDelegate);

                Debug.Log($"[Senda] Init OK. NodeId: {NodeId}");
                return Initialized;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Senda] Init exception: {e.Message}");
                return false;
            }
        }

        // Called from Rust thread — must be thread-safe, no Unity API here
        [AOT.MonoPInvokeCallback(typeof(SendaFFI.OnMessageDelegate))]
        private static void OnNativeMessage(IntPtr fromPtr, IntPtr dataPtr, ulong dataLen)
        {
            string from = SendaFFI.PtrToStringUtf8(fromPtr);
            string json = SendaFFI.PtrBytesToString(dataPtr, dataLen);
            if (json != null) _inbox.Enqueue((from, json));
        }

        // ── Connect ───────────────────────────────────────────────────────────
        public bool ConnectToNode(string remoteNodeId)
        {
            int result = SendaFFI.iroh_connect(remoteNodeId);
            if (result == 0)
            {
                if (!_connectedPeers.Contains(remoteNodeId))
                {
                    _connectedPeers.Add(remoteNodeId);
                    OnPeerConnected.Invoke(remoteNodeId);
                }
                Debug.Log($"[Senda] Connected to {remoteNodeId}");
            }
            return result == 0;
        }

        // ── Send / Broadcast ──────────────────────────────────────────────────
        public void Broadcast(NavMsg msg)
        {
            byte[] data = Encoding.UTF8.GetBytes(msg.ToJson());
            foreach (var peer in _connectedPeers)
                SendaFFI.iroh_send(peer, data, (ulong)data.Length);
        }

        public void Send(string remoteNodeId, NavMsg msg)
        {
            byte[] data = Encoding.UTF8.GetBytes(msg.ToJson());
            SendaFFI.iroh_send(remoteNodeId, data, (ulong)data.Length);
        }

        // ── Update: drain inbox on main thread ────────────────────────────────
        void Update()
        {
            while (_inbox.TryDequeue(out var item))
            {
                // The side that was dialed never calls ConnectToNode, so learn peers from traffic
                if (!string.IsNullOrEmpty(item.from) && item.from != NodeId &&
                    !_connectedPeers.Contains(item.from))
                {
                    _connectedPeers.Add(item.from);
                    OnPeerConnected.Invoke(item.from);
                }

                try
                {
                    var msg = NavMsg.FromJson(item.json);
                    if (msg != null) OnMessage.Invoke(msg);
                }
                catch { /* malformed packet */ }
            }
        }

        void OnDestroy()
        {
            SendaFFI.iroh_set_message_callback(null);
            SendaFFI.iroh_shutdown();
        }
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
