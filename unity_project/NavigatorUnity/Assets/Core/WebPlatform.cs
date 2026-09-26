// Navigator 2.0 — WebPlatform.cs
// Browser versions of a few phone features (NavP2P.jslib): clipboard and vibration.
// On other platforms they fall back to Unity's own APIs.
// Guacalita Inc. — SAL / Claude

#if UNITY_WEBGL && !UNITY_EDITOR
#define NAV_WEB
#endif

using System;
using UnityEngine;
#if NAV_WEB
using System.Runtime.InteropServices;
#endif

namespace Navigator.Core
{
    public class WebPlatform : MonoBehaviour
    {
#if NAV_WEB
        [DllImport("__Internal")] static extern void NavWeb_Copy(string text);
        [DllImport("__Internal")] static extern void NavWeb_Paste(string gameObjectName);
        [DllImport("__Internal")] static extern void NavWeb_Vibrate(int ms);
#endif
#if NAV_WEB
        public static bool IsWeb => true;
#else
        public static bool IsWeb => false;
#endif

        public static void Copy(string text)
        {
#if NAV_WEB
            NavWeb_Copy(text ?? "");
#else
            GUIUtility.systemCopyBuffer = text ?? "";
#endif
        }

        // The browser reads the clipboard asynchronously (and may ask the user), so the text comes back later
        public static void Paste(Action<string> done)
        {
#if NAV_WEB
            Receiver.Pending = done;
            NavWeb_Paste(Receiver.gameObject.name);
#else
            done?.Invoke(GUIUtility.systemCopyBuffer);
#endif
        }

        public static void Vibrate(int ms)
        {
#if NAV_WEB
            NavWeb_Vibrate(ms);
#elif (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

#if NAV_WEB
        Action<string> Pending;
        static WebPlatform _receiver;
        static WebPlatform Receiver
        {
            get
            {
                if (_receiver != null) return _receiver;
                var go = new GameObject("NavWebPlatform");
                DontDestroyOnLoad(go);
                return _receiver = go.AddComponent<WebPlatform>();
            }
        }

        public void OnWebPaste(string text)
        {
            var cb = Pending; Pending = null;
            cb?.Invoke(text);
        }
#endif
    }
}
