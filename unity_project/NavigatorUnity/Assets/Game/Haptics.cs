// Navigator 2.0 — Haptics.cs
// The phone vibrates on heavy hits and incoming lasers: you feel the crash, not only see it.
// Rate-limited so a chain of impacts doesn't buzz nonstop. Can be switched off (saved).
// Guacalita Inc. — SAL / Claude

using UnityEngine;

namespace Navigator.Game
{
    public static class Haptics
    {
        const string Pref = "navigator.haptics";
        static float _last = -10f;

        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(Pref, 1) == 1;
            set { PlayerPrefs.SetInt(Pref, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static void Buzz()
        {
            if (!Enabled || Time.unscaledTime - _last < 0.5f) return;
            _last = Time.unscaledTime;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
