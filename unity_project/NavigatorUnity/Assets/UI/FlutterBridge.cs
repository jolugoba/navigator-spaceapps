// Navigator 2.0 — FlutterBridge.cs
// Talks to the Flutter app that hosts Unity (flutter_app/, package flutter_embed_unity).
//   Flutter → Unity: sendToUnity("FlutterBridge", "<method>", "<arg>")
//       SetLanguage("en" | "es") · SetPlayerName("<name>")
//   Unity → Flutter: one JSON object per message
//       {"type":"ready"} · {"type":"mission",...} · {"type":"exit"}
// Running as a normal app (Editor, standalone APK) every call is a harmless no-op.
// Guacalita Inc. — SAL / Claude

using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using Navigator.Core;

namespace Navigator.UI
{
    public class FlutterBridge : MonoBehaviour
    {
        public const string ObjectName = "FlutterBridge";   // Flutter addresses this GameObject by name

        static bool? _embedded;

        // True only when Unity runs inside the Flutter app
        public static bool IsEmbedded
        {
            get
            {
                if (_embedded.HasValue) return _embedded.Value;
#if UNITY_EDITOR
                _embedded = false;
#elif UNITY_ANDROID
                // The Flutter plugin's Kotlin class only exists inside the Flutter app
                try
                {
                    using var cls = new AndroidJavaClass("com.learntoflutter.flutter_embed_unity_android.messaging.SendToFlutter");
                    _embedded = cls.GetRawClass() != System.IntPtr.Zero;
                }
                catch { _embedded = false; }
#elif UNITY_IOS
                _embedded = true;   // iOS builds of Navigator are only shipped through the Flutter app
#else
                _embedded = false;
#endif
                return _embedded.Value;
            }
        }

        void Awake() => gameObject.name = ObjectName;

        void Start() => Send("{\"type\":\"ready\"}");

        void Update()
        {
            // Android back / Esc on the main screen goes back to the Flutter menu
            var kb = Keyboard.current;
            bool back = kb != null && kb.escapeKey.wasPressedThisFrame;
            if (back && IsEmbedded && GameManager.Instance != null && GameManager.Instance.State == GameState.Lobby)
                Send("{\"type\":\"exit\"}");
        }

        // ── Flutter → Unity ───────────────────────────────────────────────────
        public void SetLanguage(string code)
        {
            Loc.Set(code != null && code.Trim().ToLowerInvariant().StartsWith("es") ? Lang.Spanish : Lang.English);
        }

        public void SetPlayerName(string name)
        {
            if (GameManager.Instance != null) GameManager.Instance.PlayerName = name;
        }

        // ── Unity → Flutter ───────────────────────────────────────────────────
        public static void Send(string json)
        {
            if (!IsEmbedded) { Debug.Log("[FlutterBridge] (no Flutter host) " + json); return; }
            try { SendToFlutter.Send(json); }
            catch (System.Exception e) { Debug.LogWarning("[FlutterBridge] " + e.Message); }
        }

        public static void ReportMission(MissionDesign mission, MissionOutcome outcome, int score, int arrivalPos, int stars)
        {
            if (mission == null) return;
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder("{\"type\":\"mission\"");
            sb.Append(",\"planet\":\"").Append(Escape(mission.TargetPlanetId)).Append('"');
            sb.Append(",\"objective\":\"").Append(Escape(mission.Objective)).Append('"');
            sb.Append(",\"score\":").Append(score.ToString(inv));
            sb.Append(",\"arrived\":").Append(arrivalPos > 0 ? "true" : "false");
            sb.Append(",\"position\":").Append(arrivalPos.ToString(inv));
            sb.Append(",\"stars\":").Append(stars.ToString(inv));
            sb.Append(",\"costM\":").Append(mission.TotalCostM.ToString("F0", inv));
            if (outcome != null)
            {
                sb.Append(",\"timeS\":").Append(outcome.FlightTimeS.ToString("F1", inv));
                sb.Append(",\"fuel\":").Append(outcome.FuelAtArrival.ToString("F3", inv));
                sb.Append(",\"science\":").Append(outcome.PointsReceived.ToString(inv));
                if (!string.IsNullOrEmpty(outcome.FailReason))
                    sb.Append(",\"fail\":\"").Append(Escape(Loc.T(outcome.FailReason))).Append('"');
            }
            sb.Append(",\"ts\":").Append(System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(inv));
            sb.Append('}');
            Send(sb.ToString());
        }

        static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length + 8);
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"':  sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n");  break;
                    case '\r': break;
                    default:
                        if (c < 0x20) sb.Append(' ');
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
