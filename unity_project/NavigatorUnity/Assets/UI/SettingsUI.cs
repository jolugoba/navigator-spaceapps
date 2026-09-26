// Navigator 2.0 — SettingsUI.cs
// Lobby → SETTINGS: language, sound, vibration and motion effects. Every choice is saved.
// Guacalita Inc. — SAL / Claude

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Navigator.Core;
using Navigator.Game;

namespace Navigator.UI
{
    public class SettingsUI : MonoBehaviour
    {
        public GameObject Panel;
        public Button     OpenButton;
        public Button     BackButton;
        public Button     LanguageButton, SoundButton, VibrationButton, MotionButton;

        void Awake()
        {
            FxPrefs.Apply();
            OpenButton?.onClick.AddListener(() => { if (Panel != null) Panel.SetActive(true); Refresh(); });
            BackButton?.onClick.AddListener(() => { if (Panel != null) Panel.SetActive(false); });
            LanguageButton?.onClick.AddListener(Loc.Toggle);
            SoundButton?.onClick.AddListener(() => { FxPrefs.Sound = !FxPrefs.Sound; Refresh(); AudioFX.Play(Sfx.Beep); });
            VibrationButton?.onClick.AddListener(() => { Haptics.Enabled = !Haptics.Enabled; Refresh(); Haptics.Buzz(); });
            MotionButton?.onClick.AddListener(() => { FxPrefs.Motion = !FxPrefs.Motion; Refresh(); });
            if (Panel != null) Panel.SetActive(false);
        }

        void OnEnable()  => Loc.Changed += Refresh;
        void OnDisable() => Loc.Changed -= Refresh;

        void Refresh()
        {
            Set(LanguageButton, Loc.Current == Lang.Spanish ? "Español" : "English", true);
            Set(SoundButton,     Loc.T(FxPrefs.Sound     ? "set.on" : "set.off"), FxPrefs.Sound);
            Set(VibrationButton, Loc.T(Haptics.Enabled   ? "set.on" : "set.off"), Haptics.Enabled);
            Set(MotionButton,    Loc.T(FxPrefs.Motion    ? "set.on" : "set.off"), FxPrefs.Motion);
        }

        static void Set(Button b, string text, bool on)
        {
            if (b == null) return;
            var t = b.GetComponentInChildren<TMP_Text>();
            if (t != null) { t.text = text; t.color = on ? UIStyle.Ink : UIStyle.Text; }
            b.image.color = on ? UIStyle.Accent : UIStyle.ButtonIdle;
        }
    }
}
