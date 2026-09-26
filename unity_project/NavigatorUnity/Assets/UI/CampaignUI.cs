// Navigator 2.0 — CampaignUI.cs
// World map ("game home"): Earth at the bottom, eight worlds up a winding path to Neptune.
// Completed worlds glow gold, the next open world pulses, locked ones stay dark. Tap a world to
// see its briefing and launch it. Finishing a world for the first time lights it up with a burst.
// Guacalita Inc. — SAL / Claude

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Navigator.Core;
using Navigator.Game;

namespace Navigator.UI
{
    [System.Serializable]
    public class MapNode
    {
        public Button   Button;
        public Image    Planet;   // coloured disc
        public Image    Glow;     // halo: gold when complete, pulsing blue when it's next
        public TMP_Text Name;
        public TMP_Text Stars;
    }

    public class CampaignUI : MonoBehaviour
    {
        public GameObject    Panel;
        public Button        OpenButton;     // lobby "CAMPAIGN"
        public Button        BackButton;
        public TMP_Text      TotalLabel;
        public RectTransform PathLayer;      // lines between worlds are drawn here, behind the nodes
        public RectTransform Home;           // Earth
        public MapNode[]     Nodes;

        [Header("Briefing card")]
        public GameObject Detail;
        public TMP_Text   DetailTitle;
        public TMP_Text   DetailBody;
        public Button     LaunchButton;
        public Button     CloseDetailButton;

        static readonly Color Locked    = new(0.25f, 0.28f, 0.36f, 1f);
        static readonly Color LineDark  = new(1f, 1f, 1f, 0.12f);
        static readonly Color LineLit   = new(1f, 0.84f, 0.3f, 0.9f);

        private readonly List<Image> _lines = new();
        private int   _selected = -1;
        private int   _celebrate = -1;
        private float _celebrateT;
        private bool  _linesDirty = true;
        private Vector2 _lastSize;

        void Awake()
        {
            OpenButton?.onClick.AddListener(Show);
            BackButton?.onClick.AddListener(Hide);
            CloseDetailButton?.onClick.AddListener(() => Select(-1));
            LaunchButton?.onClick.AddListener(Launch);
            for (int i = 0; i < Nodes.Length; i++)
            {
                int idx = i;
                Nodes[i]?.Button?.onClick.AddListener(() => Select(idx));
            }
            if (Panel != null) Panel.SetActive(false);
        }

        // Back from a mission that lit up a new world (the scene reloads to the lobby): show the map
        void Start()
        {
            if (Campaign.JustCompleted >= 0) Show();
            // In class, the teacher opening a world refreshes the map live
            if (ClassroomSession.Instance != null) ClassroomSession.Instance.Changed += Refresh;
        }

        void OnDestroy()
        {
            if (ClassroomSession.Instance != null) ClassroomSession.Instance.Changed -= Refresh;
        }

        void OnEnable()  => Loc.Changed += Refresh;
        void OnDisable() => Loc.Changed -= Refresh;

        public void Show()
        {
            if (Panel != null) Panel.SetActive(true);
            _linesDirty = true;
            Select(-1);
            Refresh();

            // Just came back from lighting up a world: celebrate it
            if (Campaign.JustCompleted >= 0)
            {
                _celebrate  = Campaign.JustCompleted;
                _celebrateT = 0f;
                Campaign.JustCompleted = -1;
                AudioFX.Play(Sfx.Go);
                CosmicSkyFX.MeteorShower(5f);
            }
        }

        public void Hide()
        {
            if (Panel != null) Panel.SetActive(false);
        }

        public void Refresh()
        {
            if (TotalLabel != null)
            {
                TotalLabel.text = Loc.F("camp.total", Campaign.TotalStars(), Campaign.Missions.Length * 3);
                if (Campaign.IsCompleted(Campaign.Missions.Length - 1))
                    TotalLabel.text += "  <color=#FFD633>" + Loc.T("camp.final") + "</color>";
            }

            for (int i = 0; i < Nodes.Length && i < Campaign.Missions.Length; i++)
            {
                var node = Nodes[i];
                if (node == null) continue;
                var m    = Campaign.Missions[i];
                bool open = Campaign.IsUnlocked(i);
                bool done = Campaign.IsCompleted(i);

                if (node.Planet != null) node.Planet.color = open || done ? PlanetColor(m.Planet) : Locked;
                if (node.Name  != null)
                {
                    node.Name.text  = $"{i + 1}. {Loc.T($"camp.{m.Key}.name")}";
                    node.Name.color = open || done ? UIStyle.Text : UIStyle.Muted;
                }
                if (node.Stars != null)
                {
                    node.Stars.text  = done || open ? Campaign.StarString(Campaign.Stars(i)) : "";
                    node.Stars.color = UIStyle.Gold;
                }
            }
            _linesDirty = true;
            if (_selected >= 0) Select(_selected);
        }

        void Select(int index)
        {
            _selected = index;
            if (Detail == null) return;
            Detail.SetActive(index >= 0);
            if (index < 0) return;

            var m = Campaign.Missions[index];
            bool open = Campaign.IsUnlocked(index);
            if (DetailTitle != null) DetailTitle.text = Loc.F("camp.missionN", index + 1, Loc.T($"camp.{m.Key}.name"));
            if (DetailBody != null)
            {
                string state = open ? "" : "\n<color=#FF9A4D>" +
                               Loc.T(Campaign.Gate != null ? "camp.teacherw" : "camp.lockedw") + "</color>";
                DetailBody.text =
                    $"<color=#8FA3C7>{Loc.Planet(m.Planet)} · {Loc.Objective(m.Objective)} · ${m.BudgetM:N0}M</color>\n" +
                    Loc.T($"camp.{m.Key}.brief") + "\n" +
                    $"<size=85%>{Loc.F("camp.goals", m.StarGoals[0], m.StarGoals[1], m.StarGoals[2])}</size>" + state;
            }
            if (LaunchButton != null) LaunchButton.interactable = open;
        }

        void Launch()
        {
            if (_selected < 0 || !Campaign.IsUnlocked(_selected)) return;
            int idx = _selected;
            Hide();
            GameManager.Instance?.StartCampaignMission(idx);
        }

        void Update()
        {
            if (Panel == null || !Panel.activeSelf) return;
            if (PathLayer != null && PathLayer.rect.size != _lastSize) { _lastSize = PathLayer.rect.size; _linesDirty = true; }
            if (_linesDirty) DrawPath();

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
            int next = NextOpenWorld();
            for (int i = 0; i < Nodes.Length && i < Campaign.Missions.Length; i++)
            {
                var node = Nodes[i];
                if (node?.Glow == null) continue;
                bool done = Campaign.IsCompleted(i);
                Color c = done ? new Color(1f, 0.84f, 0.3f, 0.55f)
                        : i == next ? new Color(0.3f, 0.85f, 1f, 0.25f + 0.45f * pulse)
                        : new Color(0f, 0f, 0f, 0f);
                node.Glow.color = c;

                float scale = i == next ? 1f + 0.06f * pulse : 1f;
                if (i == _celebrate)
                {
                    // World lights up: a big bounce and a bright flash that settles into gold
                    _celebrateT += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(_celebrateT / 1.6f);
                    scale = 1f + 0.6f * Mathf.Sin(k * Mathf.PI) * (1f - k * 0.5f);
                    node.Glow.color = Color.Lerp(Color.white, c, k);
                    if (k >= 1f) _celebrate = -1;
                }
                node.Button.transform.localScale = Vector3.one * scale;
            }
        }

        int NextOpenWorld()
        {
            for (int i = 0; i < Campaign.Missions.Length; i++)
                if (!Campaign.IsCompleted(i)) return Campaign.IsUnlocked(i) ? i : -1;
            return -1;
        }

        // Straight segments Earth → 1 → 2 … drawn once the layout knows real positions
        void DrawPath()
        {
            if (PathLayer == null || Home == null) return;
            Canvas.ForceUpdateCanvases();
            _linesDirty = false;

            var points = new List<Vector2> { Center(Home) };
            foreach (var n in Nodes) if (n?.Button != null) points.Add(Center((RectTransform)n.Button.transform));

            while (_lines.Count < points.Count - 1)
            {
                var go = new GameObject("Segment", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(PathLayer, false);
                var img = go.GetComponent<Image>();
                img.raycastTarget = false;
                _lines.Add(img);
            }

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 a = points[i], b = points[i + 1];
                var rt = _lines[i].rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = a;
                rt.sizeDelta = new Vector2(Vector2.Distance(a, b), 10f);
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
                // A segment is lit once the world it leads FROM is done (Earth counts as done)
                bool lit = i == 0 || Campaign.IsCompleted(i - 1);
                _lines[i].color = lit ? LineLit : LineDark;
            }
        }

        // Position of a node's centre in PathLayer's local space
        Vector2 Center(RectTransform rt)
        {
            Vector3 world = rt.TransformPoint(rt.rect.center);
            return PathLayer.InverseTransformPoint(world);
        }

        public static Color PlanetColor(string id) => id switch
        {
            "mercury" => new Color(0.72f, 0.68f, 0.62f),
            "venus"   => new Color(0.95f, 0.82f, 0.5f),
            "earth"   => new Color(0.3f, 0.6f, 1f),
            "mars"    => new Color(0.9f, 0.42f, 0.25f),
            "jupiter" => new Color(0.88f, 0.7f, 0.5f),
            "saturn"  => new Color(0.95f, 0.86f, 0.6f),
            "uranus"  => new Color(0.6f, 0.9f, 0.95f),
            "neptune" => new Color(0.35f, 0.5f, 1f),
            _         => Color.gray,
        };
    }
}
