// Navigator 2.0 — ClassroomSession.cs
// Teacher mode over the same P2P network as multiplayer (Senda, QR to connect).
//
//   Teacher: shows their QR, sees every student, their world and last result, launches questions
//            about what the class just learned, and decides who moves on to the next world.
//            Can also switch the lasers off for the whole class.
//   Student: scanning the teacher's QR joins the class. From then on the world map only opens
//            the worlds the teacher has approved; mission results and quiz answers go to the teacher.
//
// Messages (NavMsgType.classroom, JSON "op"):
//   teacher → all:     hello{name,weapons} · quiz{q} · quizend · weapons{on} · bye
//   teacher → student: phase{phase}
//   student → teacher: join{name,phase,stars} · result{mission,score,stars,arrived} · answer{q,a}
// Guacalita Inc. — SAL / Claude

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Navigator.Core;
using Navigator.Game;
using Navigator.Network;

namespace Navigator.UI
{
    public class ClassroomSession : MonoBehaviour
    {
        public enum Role { None, Teacher, Student }

        public class Student
        {
            public string Id, Name;
            public int    Phase;          // highest world index approved (0 = first world)
            public int    Stars;
            public string LastResult = "";
            public int    Answer = -1;    // for the current question
        }

        public static ClassroomSession Instance { get; private set; }

        public Role   Mode { get; private set; }
        public string TeacherId   { get; private set; }
        public string TeacherName { get; private set; }
        public int    ApprovedPhase { get; private set; }   // student side
        public int    QuizId { get; private set; } = -1;
        public bool   Weapons { get; private set; } = true;
        public readonly Dictionary<string, Student> Students = new();

        public event Action         Changed;       // refresh dashboards / map
        public event Action<int>    QuizStarted;   // question index
        public event Action         QuizEnded;
        public event Action<string> Toast;         // short message for the student

        private SendaBridge Senda => GameManager.Instance != null ? GameManager.Instance.Senda : null;
        private float _helloTimer;
        private int   _nextQuestion;

        void Awake() => Instance = this;

        void Start()
        {
            if (Senda == null) return;
            Senda.OnMessage.AddListener(OnMessage);
            Senda.OnPeerConnected.AddListener(_ => { if (Mode == Role.Teacher) SendHello(); });
            Senda.OnPeerDisconnected.AddListener(id => { if (Students.Remove(id)) Changed?.Invoke(); });
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (Mode == Role.Student) Campaign.Gate = null;
        }

        void Update()
        {
            // Late joiners: the teacher repeats hello every few seconds
            if (Mode != Role.Teacher) return;
            _helloTimer -= Time.unscaledDeltaTime;
            if (_helloTimer <= 0f) SendHello();
        }

        // ── Teacher ───────────────────────────────────────────────────────────
        public void BecomeTeacher()
        {
            if (Mode == Role.Student) LeaveClass();
            Mode = Role.Teacher;
            Campaign.Gate = null;
            SendHello();
            Changed?.Invoke();
        }

        public void StopTeaching()
        {
            if (Mode != Role.Teacher) return;
            Broadcast("{\"op\":\"bye\"}");
            Mode = Role.None;
            Students.Clear();
            QuizId = -1;
            Changed?.Invoke();
        }

        void SendHello()
        {
            _helloTimer = 3f;
            string name = GameManager.Instance != null ? GameManager.Instance.PlayerName : "Teacher";
            Broadcast($"{{\"op\":\"hello\",\"name\":\"{Esc(name)}\",\"weapons\":{(Weapons ? 1 : 0)}}}");
        }

        public void Approve(string studentId)
        {
            if (!Students.TryGetValue(studentId, out var st)) return;
            st.Phase = Mathf.Min(st.Phase + 1, Campaign.Missions.Length - 1);
            SendTo(studentId, $"{{\"op\":\"phase\",\"phase\":{st.Phase}}}");
            Changed?.Invoke();
        }

        public int ApproveCorrect()
        {
            if (QuizId < 0) return 0;
            int n = 0;
            foreach (var st in new List<Student>(Students.Values))
                if (st.Answer == Quiz.Bank[QuizId].Correct) { Approve(st.Id); n++; }
            return n;
        }

        public void ApproveAll()
        {
            foreach (var id in new List<string>(Students.Keys)) Approve(id);
        }

        // Next question for the world most of the class is on
        public void AskNext()
        {
            int world = ClassWorld();
            var pool = Quiz.ForWorld(world);
            if (pool.Count == 0) pool = Quiz.ForWorld(0);
            QuizId = pool[_nextQuestion++ % pool.Count];
            foreach (var st in Students.Values) st.Answer = -1;
            Broadcast($"{{\"op\":\"quiz\",\"q\":{QuizId}}}");
            Changed?.Invoke();
        }

        public void EndQuiz()
        {
            Broadcast("{\"op\":\"quizend\"}");
            QuizId = -1;
            Changed?.Invoke();
        }

        public void SetWeapons(bool on)
        {
            Weapons = on;
            Broadcast($"{{\"op\":\"weapons\",\"on\":{(on ? 1 : 0)}}}");
            Changed?.Invoke();
        }

        public int ClassWorld()
        {
            if (Students.Count == 0) return 0;
            var counts = new Dictionary<int, int>();
            int best = 0, bestCount = -1;
            foreach (var st in Students.Values)
            {
                counts[st.Phase] = counts.TryGetValue(st.Phase, out var c) ? c + 1 : 1;
                if (counts[st.Phase] > bestCount) { bestCount = counts[st.Phase]; best = st.Phase; }
            }
            return best;
        }

        public (int answered, int correct) QuizStats()
        {
            int a = 0, c = 0;
            if (QuizId < 0) return (0, 0);
            foreach (var st in Students.Values)
            {
                if (st.Answer < 0) continue;
                a++;
                if (st.Answer == Quiz.Bank[QuizId].Correct) c++;
            }
            return (a, c);
        }

        // ── Student ───────────────────────────────────────────────────────────
        void JoinClass(string teacherId, string teacherName)
        {
            bool first = Mode != Role.Student || TeacherId != teacherId;
            Mode = Role.Student;
            TeacherId = teacherId;
            TeacherName = teacherName;
            // The teacher decides: only approved worlds open
            Campaign.Gate = i => i <= ApprovedPhase;
            if (first)
            {
                SendJoin();
                Toast?.Invoke(Loc.F("class.joined", teacherName));
            }
            Changed?.Invoke();
        }

        public void LeaveClass()
        {
            Mode = Role.None;
            TeacherId = null;
            Campaign.Gate = null;
            ShipTools.WeaponsAllowed = true;
            Changed?.Invoke();
        }

        void SendJoin()
        {
            string name = GameManager.Instance != null ? GameManager.Instance.PlayerName : "Student";
            SendTo(TeacherId, $"{{\"op\":\"join\",\"name\":\"{Esc(name)}\",\"phase\":{ApprovedPhase},\"stars\":{Campaign.TotalStars()}}}");
        }

        public void Answer(int choice)
        {
            if (Mode != Role.Student || QuizId < 0) return;
            SendTo(TeacherId, $"{{\"op\":\"answer\",\"q\":{QuizId},\"a\":{choice}}}");
        }

        // Called by GameManager at the end of every campaign mission
        public void ReportResult(int missionIndex, int score, int stars, bool arrived)
        {
            if (Mode != Role.Student) return;
            SendTo(TeacherId, $"{{\"op\":\"result\",\"mission\":{missionIndex},\"score\":{score},\"stars\":{stars},\"arrived\":{(arrived ? 1 : 0)}}}");
        }

        // ── Network ───────────────────────────────────────────────────────────
        void OnMessage(NavMsg msg)
        {
            if (msg.t != nameof(NavMsgType.classroom) || string.IsNullOrEmpty(msg.from)) return;
            string json = msg.pl_json ?? "";
            switch (Str(json, "op"))
            {
                // → student
                case "hello":
                    if (Mode == Role.Teacher) break;   // two teachers: ignore each other
                    if (Mode == Role.Student && TeacherId != msg.from) break;
                    JoinClass(msg.from, Str(json, "name") ?? "Teacher");
                    ApplyWeapons(Num(json, "weapons") > 0);
                    break;
                case "phase":
                    if (Mode != Role.Student || msg.from != TeacherId) break;
                    int phase = Mathf.Clamp(Num(json, "phase"), 0, Campaign.Missions.Length - 1);
                    if (phase > ApprovedPhase)
                    {
                        ApprovedPhase = phase;
                        Toast?.Invoke(Loc.F("class.unlocked", phase + 1, Loc.T($"camp.{Campaign.Missions[phase].Key}.name")));
                        AudioFX.Play(Sfx.Go);
                    }
                    Changed?.Invoke();
                    break;
                case "quiz":
                    if (Mode != Role.Student || msg.from != TeacherId) break;
                    int q = Num(json, "q");
                    if (q < 0 || q >= Quiz.Bank.Length) break;
                    QuizId = q;
                    QuizStarted?.Invoke(q);
                    break;
                case "quizend":
                    if (Mode != Role.Student || msg.from != TeacherId) break;
                    QuizId = -1;
                    QuizEnded?.Invoke();
                    break;
                case "weapons":
                    if (Mode == Role.Student && msg.from == TeacherId) ApplyWeapons(Num(json, "on") > 0);
                    break;
                case "bye":
                    if (Mode == Role.Student && msg.from == TeacherId)
                    {
                        Toast?.Invoke(Loc.T("class.ended"));
                        LeaveClass();
                    }
                    break;

                // → teacher
                case "join":
                    if (Mode != Role.Teacher) break;
                    var st = GetStudent(msg.from);
                    st.Name  = Str(json, "name") ?? st.Name;
                    st.Stars = Num(json, "stars");
                    // A student rejoining keeps what the teacher already approved
                    st.Phase = Mathf.Max(st.Phase, Num(json, "phase"));
                    Changed?.Invoke();
                    break;
                case "result":
                    if (Mode != Role.Teacher) break;
                    var sr = GetStudent(msg.from);
                    int m = Num(json, "mission");
                    int stars = Num(json, "stars");
                    sr.LastResult = Loc.F("class.result", m + 1, Num(json, "score"), Campaign.StarString(stars),
                                          Num(json, "arrived") > 0 ? "" : " · " + Loc.T("class.notarrived"));
                    Changed?.Invoke();
                    break;
                case "answer":
                    if (Mode != Role.Teacher || Num(json, "q") != QuizId) break;
                    GetStudent(msg.from).Answer = Num(json, "a");
                    Changed?.Invoke();
                    break;
            }
        }

        void ApplyWeapons(bool on)
        {
            Weapons = on;
            ShipTools.WeaponsAllowed = on;
        }

        Student GetStudent(string id)
        {
            if (!Students.TryGetValue(id, out var st))
            {
                st = new Student { Id = id, Name = id.Length > 8 ? id[..8] : id };
                Students[id] = st;
            }
            return st;
        }

        void Broadcast(string json)
        {
            var s = Senda;
            if (s == null || !s.Initialized) return;
            s.Broadcast(NavMsg.Build(NavMsgType.classroom, s.NodeId, json));
        }

        void SendTo(string id, string json)
        {
            var s = Senda;
            if (s == null || !s.Initialized || string.IsNullOrEmpty(id)) return;
            s.Send(id, NavMsg.Build(NavMsgType.classroom, s.NodeId, json));
        }

        static string Esc(string s) => (s ?? "").Replace("\\", "").Replace("\"", "'");

        static string Str(string json, string key)
        {
            string k = $"\"{key}\":\"";
            int i = json.IndexOf(k, StringComparison.Ordinal);
            if (i < 0) return null;
            i += k.Length;
            int e = json.IndexOf('"', i);
            return e >= i ? json[i..e] : null;
        }

        static int Num(string json, string key)
        {
            string k = $"\"{key}\":";
            int i = json.IndexOf(k, StringComparison.Ordinal);
            if (i < 0) return 0;
            i += k.Length;
            int e = json.IndexOfAny(new[] { ',', '}' }, i);
            string v = e > i ? json[i..e].Trim() : "";
            return int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 0;
        }
    }
}
