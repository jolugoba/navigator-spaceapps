// Navigator 2.0 — TutorialUI.cs
// First-mission briefing: the engineering concepts a student needs, one card at a time.
// Shown automatically the first time; the "?" button on the design screen reopens it.
// Guacalita Inc. — SAL / Claude

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Navigator.Core;

namespace Navigator.UI
{
    public class TutorialUI : MonoBehaviour
    {
        const string DoneKey = "navigator.tutorial.done.v2";   // v2: new step about SCAN / FIRE / the map
        public const int Steps = 9;

        public GameObject Panel;
        public TMP_Text   TitleLabel;
        public TMP_Text   BodyLabel;
        public TMP_Text   CounterLabel;
        public Button     NextButton;
        public TMP_Text   NextLabel;
        public Button     BackButton;
        public Button     SkipButton;
        public Button     HelpButton;   // "?" on the mission design screen

        private int _step;

        void Awake()
        {
            NextButton?.onClick.AddListener(Next);
            BackButton?.onClick.AddListener(() => Show(_step - 1));
            SkipButton?.onClick.AddListener(Close);
            HelpButton?.onClick.AddListener(() => Show(0));
            if (Panel != null) Panel.SetActive(false);
        }

        void OnEnable()  => Loc.Changed += Refresh;
        void OnDisable() => Loc.Changed -= Refresh;

        public void ShowIfFirstTime()
        {
            if (PlayerPrefs.GetInt(DoneKey, 0) == 0) Show(0);
        }

        public void Show(int step)
        {
            _step = Mathf.Clamp(step, 0, Steps - 1);
            if (Panel != null) Panel.SetActive(true);
            Refresh();
        }

        void Refresh()
        {
            if (Panel == null || !Panel.activeSelf) return;
            int n = _step + 1;
            if (TitleLabel   != null) TitleLabel.text   = Loc.T($"tut.{n}.title");
            if (BodyLabel    != null) BodyLabel.text    = Loc.T($"tut.{n}.body");
            if (CounterLabel != null) CounterLabel.text = $"{n} / {Steps}";
            if (NextLabel    != null) NextLabel.text    = Loc.T(n == Steps ? "tut.start" : "tut.next");
            if (BackButton   != null) BackButton.interactable = _step > 0;
        }

        void Next()
        {
            if (_step >= Steps - 1) Close();
            else Show(_step + 1);
        }

        void Close()
        {
            PlayerPrefs.SetInt(DoneKey, 1);
            PlayerPrefs.Save();
            if (Panel != null) Panel.SetActive(false);
        }
    }
}
