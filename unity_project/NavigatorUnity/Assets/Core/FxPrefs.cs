// Navigator 2.0 — FxPrefs.cs
// Player settings that shape the "game feel": sound, vibration and motion effects.
// "Reduce effects" turns off screen shake, HUD jolts and speed streaks (motion sickness,
// photosensitivity) while keeping every gameplay cue.
// Guacalita Inc. — SAL / Claude

using UnityEngine;

namespace Navigator.Core
{
    public static class FxPrefs
    {
        const string SoundKey = "navigator.sound", MotionKey = "navigator.motionfx";

        public static bool Sound
        {
            get => PlayerPrefs.GetInt(SoundKey, 1) == 1;
            set { PlayerPrefs.SetInt(SoundKey, value ? 1 : 0); PlayerPrefs.Save(); Apply(); }
        }

        // Screen shake, HUD jolts, full-screen flashes and speed lines
        // Read once and kept: effects ask every frame
        static bool _motion, _motionRead;
        public static bool Motion
        {
            get
            {
                if (!_motionRead) { _motion = PlayerPrefs.GetInt(MotionKey, 1) == 1; _motionRead = true; }
                return _motion;
            }
            set { _motion = value; _motionRead = true; PlayerPrefs.SetInt(MotionKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static void Apply() => AudioListener.volume = Sound ? 1f : 0f;
    }
}
