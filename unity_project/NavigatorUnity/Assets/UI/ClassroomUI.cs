// Navigator 2.0 — ClassroomUI.cs
// Screens for teacher mode (logic lives in ClassroomSession):
//   · Teacher dashboard: class QR, one row per student (name, world, last result, answer, PASS),
//     question round controls, "pass everyone who got it right", lasers on/off.
//   · Student quiz: the question the teacher launched, four answers, instant feedback.
//   · Toast: short notices ("You joined the class", "Your teacher opened world 3").
// Guacalita Inc. — SAL / Claude

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Navigator.Core;

namespace Navigator.UI
{
    public class ClassroomUI : MonoBehaviour
    {
        [Header("Teacher dashboard")]
        public GameObject    TeacherPanel;
        public Button        OpenTeacherButton;   // lobby "TEACHER"
        public Button        CloseTeacherButton;
        public RawImage      ClassQR;
        public TMP_Text      ClassTitle;
        public TMP_Text      QuestionLabel;
        public TMP_Text      QuestionStats;
        public Button        AskButton;
        public TMP_Text      AskLabel;
        public Button        PassCorrectButton;
        public Button        PassAllButton;
        public Button        WeaponsButton;
        public TMP_Text      WeaponsLabel;
        public RectTransform StudentList;         // ScrollRect content

        [Header("Student quiz")]
        public GameObject QuizPanel;
        public TMP_Text   QuizQuestion;
        public Button[]   QuizOptions;
        public TMP_Text   QuizFeedback;
        public Button     QuizCloseButton;

        [Header("Toast")]
        public GameObject ToastBox;
        public TMP_Text   Toast;

        private ClassroomSession _session;
        private readonly List<GameObject> _rows = new();
        private float _toastTimer;
        private int   _answered = -1;

        void Start()
        {
            _session = ClassroomSession.Instance;
            OpenTeacherButton?.onClick.AddListener(OpenTeacher);
            CloseTeacherButton?.onClick.AddListener(CloseTeacher);
            AskButton?.onClick.AddListener(() =>
            {
                if (_session == null) return;
                if (_session.QuizId >= 0) _session.EndQuiz(); else _session.AskNext();
            });
            PassCorrectButton?.onClick.AddListener(() => _session?.ApproveCorrect());
            PassAllButton?.onClick.AddListener(() => _session?.ApproveAll());
            WeaponsButton?.onClick.AddListener(() => _session?.SetWeapons(!_session.Weapons));
            QuizCloseButton?.onClick.AddListener(() => QuizPanel?.SetActive(false));
            for (int i = 0; i < QuizOptions.Length; i++)
            {
                int idx = i;
                QuizOptions[i]?.onClick.AddListener(() => AnswerQuiz(idx));
            }

            if (TeacherPanel != null) TeacherPanel.SetActive(false);
            if (QuizPanel != null) QuizPanel.SetActive(false);
            if (ToastBox != null) ToastBox.SetActive(false);

            if (_session != null)
            {
                _session.Changed     += Refresh;
                _session.QuizStarted += ShowQuiz;
                _session.QuizEnded   += OnQuizEnded;
                _session.Toast       += ShowToast;
            }
            Loc.Changed += Refresh;
        }

        void OnDestroy()
        {
            if (_session != null)
            {
                _session.Changed     -= Refresh;
                _session.QuizStarted -= ShowQuiz;
                _session.QuizEnded   -= OnQuizEnded;
                _session.Toast       -= ShowToast;
            }
            Loc.Changed -= Refresh;
        }

        void Update()
        {
            if (_toastTimer > 0f)
            {
                _toastTimer -= Time.unscaledDeltaTime;
                if (_toastTimer <= 0f && ToastBox != null) ToastBox.SetActive(false);
            }
        }

        // ── Teacher ───────────────────────────────────────────────────────────
        void OpenTeacher()
        {
            if (_session == null) return;
            _session.BecomeTeacher();
            var senda = GameManager.Instance != null ? GameManager.Instance.Senda : null;
            if (ClassQR != null && senda != null && !string.IsNullOrEmpty(senda.NodeId))
                ClassQR.texture = LobbyManager.GenerateQR(senda.NodeId, 512);
            if (TeacherPanel != null) TeacherPanel.SetActive(true);
            Refresh();
        }

        void CloseTeacher()
        {
            _session?.StopTeaching();
            if (TeacherPanel != null) TeacherPanel.SetActive(false);
        }

        void Refresh()
        {
            if (_session == null || TeacherPanel == null || !TeacherPanel.activeSelf) return;

            if (ClassTitle != null) ClassTitle.text = Loc.F("class.title", _session.Students.Count);
            bool asking = _session.QuizId >= 0;
            if (AskLabel != null) AskLabel.text = Loc.T(asking ? "class.endq" : "class.ask");
            if (WeaponsLabel != null) WeaponsLabel.text = Loc.T(_session.Weapons ? "class.weapons.on" : "class.weapons.off");
            if (PassCorrectButton != null) PassCorrectButton.interactable = asking;

            if (QuestionLabel != null)
            {
                if (asking)
                {
                    var q = Quiz.Bank[_session.QuizId];
                    QuestionLabel.text = $"<b>{q.Q}</b>\n<color=#4DE680>{Loc.F("class.answer", q.Option(q.Correct))}</color>";
                }
                else QuestionLabel.text = Loc.F("class.noq", _session.ClassWorld() + 1);
            }
            if (QuestionStats != null)
            {
                var (answered, correct) = _session.QuizStats();
                QuestionStats.text = asking ? Loc.F("class.stats", answered, _session.Students.Count, correct) : "";
            }
            RebuildRows();
        }

        void RebuildRows()
        {
            if (StudentList == null) return;
            foreach (var r in _rows) Destroy(r);
            _rows.Clear();

            if (_session.Students.Count == 0)
            {
                _rows.Add(Row(Loc.T("class.empty"), null, null));
                return;
            }
            bool asking = _session.QuizId >= 0;
            int correct = asking ? Quiz.Bank[_session.QuizId].Correct : -1;
            foreach (var st in _session.Students.Values)
            {
                string world = Loc.F("class.world", st.Phase + 1, Loc.T($"camp.{Campaign.Missions[st.Phase].Key}.name"));
                string answer = !asking ? "" :
                                st.Answer < 0 ? $"  <color=#8FA3C7>{Loc.T("class.waiting")}</color>" :
                                st.Answer == correct ? $"  <color=#4DE680>{Loc.T("class.right")}</color>"
                                                     : $"  <color=#FF6B5B>{Loc.T("class.wrong")}</color>";
                string text = $"<b>{st.Name}</b>{answer}\n<size=80%><color=#8FA3C7>{world}" +
                              (string.IsNullOrEmpty(st.LastResult) ? "" : " · " + st.LastResult) + "</color></size>";
                string id = st.Id;
                bool atEnd = st.Phase >= Campaign.Missions.Length - 1;
                _rows.Add(Row(text, atEnd ? null : Loc.T("class.pass"), atEnd ? null : () => _session.Approve(id)));
            }
        }

        // One student row, created at runtime
        GameObject Row(string text, string buttonLabel, UnityEngine.Events.UnityAction onClick)
        {
            var row = new GameObject("Student", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            row.transform.SetParent(StudentList, false);
            row.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.05f);
            row.GetComponent<LayoutElement>().preferredHeight = 110f;

            var label = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            label.transform.SetParent(row.transform, false);
            var lr = label.rectTransform;
            lr.anchorMin = new Vector2(0f, 0f); lr.anchorMax = new Vector2(buttonLabel != null ? 0.7f : 1f, 1f);
            lr.offsetMin = new Vector2(20f, 6f); lr.offsetMax = new Vector2(-10f, -6f);
            label.text = text;
            label.fontSize = 30;
            label.enableAutoSizing = true; label.fontSizeMin = 18; label.fontSizeMax = 30;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = UIStyle.Text;
            label.raycastTarget = false;

            if (buttonLabel != null)
            {
                var b = new GameObject("Pass", typeof(RectTransform), typeof(Image), typeof(Button));
                b.transform.SetParent(row.transform, false);
                var br = (RectTransform)b.transform;
                br.anchorMin = new Vector2(0.72f, 0.18f); br.anchorMax = new Vector2(0.97f, 0.82f);
                br.offsetMin = br.offsetMax = Vector2.zero;
                b.GetComponent<Image>().color = UIStyle.Good;
                b.GetComponent<Button>().onClick.AddListener(onClick);
                var bl = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
                bl.transform.SetParent(b.transform, false);
                bl.rectTransform.anchorMin = Vector2.zero; bl.rectTransform.anchorMax = Vector2.one;
                bl.rectTransform.offsetMin = bl.rectTransform.offsetMax = Vector2.zero;
                bl.text = buttonLabel;
                bl.fontSize = 26; bl.fontStyle = FontStyles.Bold;
                bl.alignment = TextAlignmentOptions.Center;
                bl.color = UIStyle.Ink;
                bl.raycastTarget = false;
            }
            return row;
        }

        // ── Student ───────────────────────────────────────────────────────────
        void ShowQuiz(int q)
        {
            if (QuizPanel == null) return;
            var question = Quiz.Bank[q];
            _answered = -1;
            QuizPanel.SetActive(true);
            if (QuizQuestion != null) QuizQuestion.text = question.Q;
            for (int i = 0; i < QuizOptions.Length; i++)
            {
                if (QuizOptions[i] == null) continue;
                QuizOptions[i].interactable = true;
                QuizOptions[i].GetComponent<Image>().color = UIStyle.ButtonIdle;
                var label = QuizOptions[i].GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = question.Option(i);
            }
            if (QuizFeedback != null) QuizFeedback.text = Loc.F("class.from", _session != null ? _session.TeacherName : "");
            if (QuizCloseButton != null) QuizCloseButton.gameObject.SetActive(false);
            Navigator.Game.AudioFX.Play(Navigator.Game.Sfx.Alert, 0.6f);
        }

        void AnswerQuiz(int choice)
        {
            if (_session == null || _session.QuizId < 0 || _answered >= 0) return;
            _answered = choice;
            _session.Answer(choice);
            var q = Quiz.Bank[_session.QuizId];
            bool right = choice == q.Correct;
            for (int i = 0; i < QuizOptions.Length; i++)
            {
                if (QuizOptions[i] == null) continue;
                QuizOptions[i].interactable = false;
                var img = QuizOptions[i].GetComponent<Image>();
                if (i == q.Correct) img.color = UIStyle.Good;
                else if (i == choice) img.color = UIStyle.Bad;
            }
            if (QuizFeedback != null)
                QuizFeedback.text = right ? $"<color=#4DE680>{Loc.T("class.right.you")}</color>"
                                          : $"<color=#FF6B5B>{Loc.F("class.wrong.you", q.Option(q.Correct))}</color>";
            if (QuizCloseButton != null) QuizCloseButton.gameObject.SetActive(true);
            Navigator.Game.AudioFX.Play(right ? Navigator.Game.Sfx.Pickup : Navigator.Game.Sfx.Hit, 0.6f);
        }

        void OnQuizEnded()
        {
            if (QuizPanel != null && _answered < 0) QuizPanel.SetActive(false);
        }

        void ShowToast(string text)
        {
            if (Toast == null) return;
            Toast.text = text;
            if (ToastBox != null) ToastBox.SetActive(true);
            _toastTimer = 4f;
        }
    }
}
