// Navigator 2.0 — ShipSyncService.cs
// Broadcasts/receives ship state via SendaBridge (Iroh + Dilithium)
// Same JSON protocol as Flutter ship_sync_service.dart — fully interoperable
// Guacalita Inc. — SAL / Claude

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Navigator.Network
{
    public class ShipSyncService : MonoBehaviour
    {
        public UnityEvent<Dictionary<string, ShipStateNet>> OnRemoteUpdate = new();
        public UnityEvent<Dictionary<string, int>>          OnRemoteScore  = new();
        public UnityEvent<string>                           OnRemoteArrived = new();  // nodeId
        public UnityEvent<string, string>                   OnRemoteName    = new();  // nodeId, name
        public UnityEvent<string[]>                         OnPeerList      = new();  // peers a rival knows

        public float SendHz = 20f;

        private SendaBridge _senda;
        private float       _lastSend;
        private readonly Dictionary<string, ShipStateNet> _remoteShips  = new();
        private readonly Dictionary<string, int>          _remoteScores = new();

        // Read lazily: SendaBridge.Init() runs after this Awake
        private string _nodeId => _senda != null ? _senda.NodeId : null;

        void Awake()
        {
            _senda  = GetComponent<SendaBridge>();
            if (_senda != null)
            {
                _senda.OnMessage.AddListener(OnNavMessage);
                _senda.OnPeerDisconnected.AddListener(OnPeerGone);
            }
        }

        // ── Send our ship state to all peers (call every 50ms) ────────────────
        public void Broadcast(ShipStateNet state)
        {
            // Every phone talks to every phone: in a full classroom, send less often so the
            // total traffic stays flat (20 Hz with up to 5 players, 4 Hz floor for big rooms)
            int peers = _senda != null ? _senda.ConnectedPeers.Count : 0;
            float hz  = Mathf.Max(4f, SendHz * Mathf.Min(1f, 4f / Mathf.Max(1, peers)));
            if (Time.unscaledTime - _lastSend < 1f / hz) return;
            _lastSend = Time.unscaledTime;

            string json = $"{{\"nodeId\":\"{state.NodeId}\"," +
                          $"\"px\":{state.PosX:F2},\"py\":{state.PosY:F2},\"pz\":{state.PosZ:F2}," +
                          $"\"vx\":{state.VelX:F2},\"vy\":{state.VelY:F2},\"vz\":{state.VelZ:F2}," +
                          $"\"yaw\":{state.Yaw:F1},\"pitch\":{state.Pitch:F1},\"roll\":{state.Roll:F1}," +
                          $"\"thr\":{state.Throttle:F2},\"boost\":{(state.Boosting?"true":"false")}," +
                          $"\"burn\":{(state.BurnoutActive?"true":"false")}," +
                          $"\"fuel\":{state.FuelFraction:F3},\"lives\":{state.Lives}," +
                          $"\"score\":{state.Score},\"ts\":{state.Timestamp}}}";

            _senda?.Broadcast(NavMsg.Build(NavMsgType.shipState, _nodeId, json));
        }

        // ── Broadcast mission end score ───────────────────────────────────────
        public void BroadcastMissionEnd(string nodeId, int score)
        {
            string json = $"{{\"score\":{score}}}";
            _senda?.Broadcast(NavMsg.Build(NavMsgType.missionEnd, nodeId, json));
        }

        // New round: forget last round's final scores
        public void ClearRound() => _remoteScores.Clear();

        public void BroadcastArrival(int position)
        {
            _senda?.Broadcast(NavMsg.Build(NavMsgType.missionSync, _nodeId,
                $"{{\"arrived\":{position}}}"));
        }

        // A rival who left: remove their ship so no ghost keeps floating in the race
        private void OnPeerGone(string nodeId)
        {
            bool had = _remoteShips.Remove(nodeId);
            _remoteScores.Remove(nodeId);
            if (had) OnRemoteUpdate.Invoke(new Dictionary<string, ShipStateNet>(_remoteShips));
        }

        // ── Receive ───────────────────────────────────────────────────────────
        private void OnNavMessage(NavMsg msg)
        {
            if (string.IsNullOrEmpty(msg.from) || msg.from == _nodeId) return;
            if (!System.Enum.TryParse<NavMsgType>(msg.t, out var type)) return;

            switch (type)
            {
                case NavMsgType.shipState:
                    var state = ParseShipState(msg.pl_json, msg.from);
                    if (state != null)
                    {
                        _remoteShips[msg.from] = state;
                        OnRemoteUpdate.Invoke(new Dictionary<string, ShipStateNet>(_remoteShips));
                    }
                    break;

                case NavMsgType.missionEnd:
                    int score = ParseInt(msg.pl_json, "score");
                    _remoteScores[msg.from] = score;
                    OnRemoteScore.Invoke(new Dictionary<string, int>(_remoteScores));
                    break;

                case NavMsgType.missionSync:
                    if (msg.pl_json.Contains("\"arrived\"")) OnRemoteArrived.Invoke(msg.from);
                    break;

                case NavMsgType.lobbyInfo:
                    string name = ExtractValue(msg.pl_json, "name");
                    if (!string.IsNullOrEmpty(name)) OnRemoteName.Invoke(msg.from, name);
                    // ';'-separated so the simple parser (which stops at ',') keeps it whole
                    string peers = ExtractValue(msg.pl_json, "peers");
                    if (!string.IsNullOrEmpty(peers))
                        OnPeerList.Invoke(peers.Split(';', System.StringSplitOptions.RemoveEmptyEntries));
                    break;
            }
        }

        // ── Simple JSON field parsers (no Newtonsoft dependency) ──────────────
        private static ShipStateNet ParseShipState(string json, string nodeId)
        {
            try { return new ShipStateNet {
                NodeId       = nodeId,
                PosX         = ParseFloat(json, "px"),
                PosY         = ParseFloat(json, "py"),
                PosZ         = ParseFloat(json, "pz"),
                VelX         = ParseFloat(json, "vx"),
                VelY         = ParseFloat(json, "vy"),
                VelZ         = ParseFloat(json, "vz"),
                Yaw          = ParseFloat(json, "yaw"),
                Pitch        = ParseFloat(json, "pitch"),
                Roll         = ParseFloat(json, "roll"),
                Throttle     = ParseFloat(json, "thr"),
                Boosting     = json.Contains("\"boost\":true"),
                BurnoutActive = json.Contains("\"burn\":true"),
                FuelFraction = ParseFloat(json, "fuel"),
                Lives        = ParseInt(json, "lives"),
                Score        = ParseInt(json, "score"),
                Timestamp    = ParseLong(json, "ts"),
            }; } catch { return null; }
        }

        private static float ParseFloat(string json, string key)
        {
            string val = ExtractValue(json, key);
            return float.TryParse(val, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var f) ? f : 0f;
        }
        private static int ParseInt(string json, string key)
        {
            string val = ExtractValue(json, key);
            return int.TryParse(val, out var i) ? i : 0;
        }
        private static long ParseLong(string json, string key)
        {
            string val = ExtractValue(json, key);
            return long.TryParse(val, out var l) ? l : 0L;
        }
        private static string ExtractValue(string json, string key)
        {
            string search = $"\"{key}\":";
            int s = json.IndexOf(search);
            if (s < 0) return "";
            s += search.Length;
            int e = json.IndexOfAny(new[]{',','}'}, s);
            return e > s ? json[s..e].Trim('"') : "";
        }
    }

    // ── ShipStateNet — estado de una nave en red ──────────────────────────────
    // Enviado cada 50ms vía Senda. Idéntico al ShipState de Flutter.

    public class ShipStateNet
    {
        public string NodeId;
        public string DisplayName;
        public float  PosX, PosY, PosZ;
        public float  VelX, VelY, VelZ;
        public float  Yaw, Pitch, Roll;
        public float  Throttle;
        public bool   Boosting;
        public bool   BurnoutActive;
        public float  FuelFraction;
        public int    Lives;
        public int    Score;
        public long   Timestamp;

        public Vector3 Position => new(PosX, PosY, PosZ);
        public Vector3 Velocity => new(VelX, VelY, VelZ);

        public static ShipStateNet FromTransform(
            string nodeId, string displayName,
            Transform t, Rigidbody rb,
            float throttle, bool boosting, bool burnoutActive,
            string targetPlanet, float distAU,
            int lives, float fuelFraction, int score) => new()
        {
            NodeId        = nodeId,
            DisplayName   = displayName,
            PosX          = t.position.x,
            PosY          = t.position.y,
            PosZ          = t.position.z,
            VelX          = rb != null ? rb.linearVelocity.x : 0f,
            VelY          = rb != null ? rb.linearVelocity.y : 0f,
            VelZ          = rb != null ? rb.linearVelocity.z : 0f,
            Yaw           = t.eulerAngles.y,
            Pitch         = t.eulerAngles.x,
            Roll          = t.eulerAngles.z,
            Throttle      = throttle,
            Boosting      = boosting,
            BurnoutActive = burnoutActive,
            FuelFraction  = fuelFraction,
            Lives         = lives,
            Score         = score,
            Timestamp     = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };
    }
}
