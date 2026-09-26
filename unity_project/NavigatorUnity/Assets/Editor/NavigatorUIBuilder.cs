// Navigator 2.0 — NavigatorUIBuilder.cs
// Construye TODA la interfaz (Lobby, Diseño, Cuenta atrás, HUD, Ciencia, Scoreboard)
// y conecta cada campo del Inspector. Menú: Navigator → Build UI
// (Navigator → Setup Scene también lo ejecuta)
// Guacalita Inc. — SAL / Claude

#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using TMPro;
using Navigator.Core;
using Navigator.Network;
using Navigator.UI;

namespace Navigator.Editor
{
    public static class NavigatorUIBuilder
    {
        const string RootName          = "NavigatorUI";
        const string GeneratedDir      = "Assets/UI/Generated";
        const string ArrowSpritePath   = GeneratedDir + "/HudArrow.png";
        const string InstrumentPrefab  = "Assets/Prefabs/UI/InstrumentItem.prefab";

        static readonly string[] LegacyUIObjects =
            { "CockpitHUD", "MissionDesignUI", "ScoreboardUI", "ScienceOpsUI", "LobbyManager" };

        [MenuItem("Navigator/Build UI")]
        public static void BuildMenu()
        {
            var gm = Object.FindFirstObjectByType<GameManager>();
            if (gm == null)
            {
                EditorUtility.DisplayDialog("Navigator", "Primero ejecuta Navigator → Setup Scene.", "OK");
                return;
            }
            if (Build(gm) != null)
                EditorUtility.DisplayDialog("Navigator", "UI construida y conectada.\nGuarda la escena (Ctrl+S).", "OK");
        }

        public static GameObject Build(GameManager gm)
        {
            if (!EnsureTMPResources()) return null;

            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);
            foreach (var n in LegacyUIObjects)
            {
                var legacy = GameObject.Find(n);
                if (legacy != null) Object.DestroyImmediate(legacy);
            }
            EnsureEventSystem();

            // ── Canvas raíz: los controladores de UI viven aquí (siempre activo) ─
            var root = new GameObject(RootName, typeof(RectTransform), typeof(Canvas),
                                      typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0.5f;

            var safe = UIKit.Stretch(root.transform, "SafeArea");
            safe.gameObject.AddComponent<SafeAreaFitter>();

            var lobby   = root.AddComponent<LobbyManager>();
            var design  = root.AddComponent<MissionDesignUI>();
            var hud     = root.AddComponent<CockpitHUD>();
            var science = root.AddComponent<ScienceOpsUI>();
            var board   = root.AddComponent<ScoreboardUI>();
            var events  = root.AddComponent<MissionEventsUI>();
            var tutor   = root.AddComponent<TutorialUI>();

            // Panels that fill the whole screen (incl. behind the notch) get their own backdrop
            gm.LobbyPanel         = BuildLobby(root.transform, safe, lobby, gm);
            gm.MissionDesignPanel = BuildMissionDesign(root.transform, safe, design);
            gm.CountdownPanel     = BuildCountdown(safe, gm);
            gm.HUDPanel           = BuildHUD(safe, hud);
            gm.ScienceOpsPanel    = BuildScienceOps(root.transform, safe, science);
            gm.ScoreboardPanel    = BuildScoreboard(root.transform, safe, board);

            // Overlays go last so they draw above every panel
            BuildEvents(root.transform, events);
            BuildTutorial(root.transform, tutor, gm.MissionDesignPanel);
            BuildCampaign(root.transform, root.AddComponent<CampaignUI>());
            gm.Events   = events;
            gm.Tutorial = tutor;

            gm.Lobby           = lobby;
            gm.MissionDesignUI = design;
            gm.HUD             = hud;
            gm.ScienceOpsUI    = science;
            gm.ScoreboardUI    = board;

            // Only the lobby is visible when the scene opens
            gm.MissionDesignPanel.SetActive(false);
            gm.CountdownPanel.SetActive(false);
            gm.HUDPanel.SetActive(false);
            gm.ScienceOpsPanel.SetActive(false);
            gm.ScoreboardPanel.SetActive(false);

            EditorUtility.SetDirty(gm);
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("[Navigator] UI construida.");
            return root;
        }

        // ══════════════════════════════════════════════════════════════════════
        // LOBBY
        // ══════════════════════════════════════════════════════════════════════

        static GameObject BuildLobby(Transform root, RectTransform safe, LobbyManager lobby, GameManager gm)
        {
            var panel = UIKit.Panel(root, safe, "LobbyPanel", UIStyle.Backdrop);
            var p = panel.content;

            UIKit.Text(p, "Title", "NAVIGATOR", 110, new Vector4(0.05f, 0.87f, 0.95f, 0.96f),
                       TextAlignmentOptions.Center, UIStyle.Accent, FontStyles.Bold);
            UIKit.Text(p, "Subtitle", "@lobby.subtitle", 32,
                       new Vector4(0.05f, 0.83f, 0.95f, 0.87f), TextAlignmentOptions.Center, UIStyle.Muted);

            var card = UIKit.Card(p, "QRCard", new Vector4(0.08f, 0.47f, 0.92f, 0.81f), UIStyle.Card);
            lobby.MyNameLabel = UIKit.TextTop(card, "MyName", "@player.default", 40, 0f, 1f, 24, 56,
                                              TextAlignmentOptions.Center, UIStyle.Text, FontStyles.Bold);
            UIKit.TextTop(card, "Hint", "@lobby.qrhint", 28, 0f, 1f, 80, 40,
                          TextAlignmentOptions.Center, UIStyle.Muted);

            var qrArea = UIKit.Rect(card, "QRArea", new Vector4(0.18f, 0.16f, 0.82f, 0.78f));
            var qrBg = UIKit.Image(qrArea, "QRBackground", Vector4.zero, Color.white);
            UIKit.Fill(qrBg.rectTransform);
            qrBg.gameObject.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            var qr = UIKit.Raw(qrBg.rectTransform, "MyQR", new Vector4(0.04f, 0.04f, 0.96f, 0.96f));
            lobby.MyQRImage = qr;

            // The scan preview covers the QR while scanning
            var preview = UIKit.Raw(qrArea, "ScanPreview", new Vector4(0f, 0f, 1f, 1f));
            preview.gameObject.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            lobby.ScanPreview = preview;

            lobby.MyNodeIdLabel = UIKit.Text(card, "NodeId", "Starting P2P network…", 26,
                                             new Vector4(0.05f, 0.03f, 0.95f, 0.14f),
                                             TextAlignmentOptions.Center, UIStyle.Muted);

            lobby.ScanButton = UIKit.Button(p, "ScanButton", "Scan QR", new Vector4(0.08f, 0.395f, 0.92f, 0.455f),
                                            UIStyle.ButtonIdle, UIStyle.Text, 40);
            lobby.ScanStatusText = UIKit.Text(p, "ScanStatus", "", 28, new Vector4(0.08f, 0.355f, 0.92f, 0.39f),
                                              TextAlignmentOptions.Center, UIStyle.Muted);
            lobby.ConnectedPlayersText = UIKit.Text(p, "Connected", "", 30, new Vector4(0.08f, 0.27f, 0.92f, 0.35f),
                                                    TextAlignmentOptions.Center, UIStyle.Text);
            lobby.StartGameButton = UIKit.Button(p, "StartGameButton", "PLAY SOLO",
                                                 new Vector4(0.08f, 0.07f, 0.92f, 0.17f),
                                                 UIStyle.Accent, UIStyle.Ink, 52, FontStyles.Bold);
            lobby.StartGameLabel = lobby.StartGameButton.GetComponentInChildren<TMP_Text>();
            _campaignButton = UIKit.Button(p, "CampaignButton", "@camp.button", new Vector4(0.08f, 0.18f, 0.92f, 0.26f),
                                           UIStyle.Gold, UIStyle.Ink, 40, FontStyles.Bold);

            lobby.LanguageButton = UIKit.Button(p, "LanguageButton", "@lang.button", new Vector4(0.7f, 0.962f, 0.97f, 0.996f),
                                                UIStyle.ButtonIdle, UIStyle.Accent, 26, FontStyles.Bold);

            lobby.Senda = gm.Senda;
            lobby.Sync  = gm.SyncService;
            return panel.go;
        }

        // ══════════════════════════════════════════════════════════════════════
        // DISEÑO DE MISIÓN
        // ══════════════════════════════════════════════════════════════════════

        static GameObject BuildMissionDesign(Transform root, RectTransform safe, MissionDesignUI ui)
        {
            var panel = UIKit.Panel(root, safe, "MissionDesignPanel", UIStyle.Backdrop);
            var p = panel.content;
            ui.Panel = panel.go;

            UIKit.Text(p, "Title", "@design.title", 54, new Vector4(0.05f, 0.955f, 0.85f, 0.995f),
                       TextAlignmentOptions.Left, UIStyle.Accent, FontStyles.Bold);
            ui.BriefLabel = UIKit.Text(p, "Brief", "", 26, new Vector4(0.05f, 0.91f, 0.95f, 0.955f),
                                       TextAlignmentOptions.TopLeft, UIStyle.Muted);

            var content = UIKit.Scroll(p, "Scroll", new Vector4(0f, 0.25f, 1f, 0.905f));

            // Destino
            var s = UIKit.Section(content, "@design.destination", 450);
            string[] planets = { "@planet.mercury", "@planet.venus", "@planet.mars", "@planet.jupiter", "@planet.saturn", "@planet.uranus", "@planet.neptune" };
            ui.PlanetButtons = new Button[planets.Length];
            for (int i = 0; i < planets.Length; i++)
            {
                int row = i < 4 ? 0 : 1, col = i < 4 ? i : i - 4, cols = row == 0 ? 4 : 3;
                ui.PlanetButtons[i] = UIKit.ButtonTop(s, $"Planet_{i}", planets[i],
                    col / (float)cols, (col + 1) / (float)cols, 78 + row * 104, 94, 32);
            }
            ui.PlanetLabel    = UIKit.TextTop(s, "PlanetLabel", "", 32, 0f, 0.5f, 292, 44, TextAlignmentOptions.Left, UIStyle.Text, FontStyles.Bold);
            ui.CommDelayLabel = UIKit.TextTop(s, "CommDelay", "", 30, 0.5f, 1f, 292, 44, TextAlignmentOptions.Right, UIStyle.Muted);
            ui.PlanetScienceLabel = UIKit.TextTop(s, "PlanetScience", "", 26, 0f, 1f, 340, 70, TextAlignmentOptions.TopLeft, UIStyle.Accent);

            // Objetivo
            s = UIKit.Section(content, "@design.objective", 250);
            ui.BtnFlyby   = UIKit.ButtonTop(s, "Flyby",   "@objective.flyby", 0f,     1f / 3, 78, 90, 32);
            ui.BtnOrbit   = UIKit.ButtonTop(s, "Orbit",   "@objective.orbit",     1f / 3, 2f / 3, 78, 90, 32);
            ui.BtnLanding = UIKit.ButtonTop(s, "Landing", "@objective.landing", 2f / 3, 1f,     78, 90, 32);
            ui.ObjectiveInfoLabel = UIKit.TextTop(s, "ObjectiveInfo", "", 28, 0f, 1f, 180, 44, TextAlignmentOptions.Left, UIStyle.Muted);

            // Motor
            s = UIKit.Section(content, "@design.engine", 330);
            ui.BtnChemical = UIKit.ButtonTop(s, "Chemical", "@engine.Chemical", 0f,     1f / 3, 78, 90, 32);
            ui.BtnIon      = UIKit.ButtonTop(s, "Ion",      "@engine.Ion",  1f / 3, 2f / 3, 78, 90, 32);
            ui.BtnNuclear  = UIKit.ButtonTop(s, "Nuclear",  "@engine.Nuclear", 2f / 3, 1f,     78, 90, 32);
            ui.EngineFuelLabel = UIKit.TextTop(s, "EngineFuel", "", 30, 0f, 1f, 182, 44, TextAlignmentOptions.Left, UIStyle.Text);
            ui.EngineNasaLabel = UIKit.TextTop(s, "EngineNasa", "", 26, 0f, 1f, 228, 84, TextAlignmentOptions.TopLeft, UIStyle.Muted);

            // Combustible
            s = UIKit.Section(content, "@design.fuel", 190);
            ui.FuelSlider     = UIKit.SliderTop(s, "FuelSlider", 0f, 0.68f, 96, 56);
            ui.FuelValueLabel = UIKit.TextTop(s, "FuelValue", "", 40, 0.7f, 1f, 86, 72, TextAlignmentOptions.Right, UIStyle.Text, FontStyles.Bold);

            // ΔV
            s = UIKit.Section(content, "@design.deltav", 260);
            ui.DeltaVLabel       = UIKit.TextTop(s, "DeltaV", "", 56, 0f, 0.6f, 66, 76, TextAlignmentOptions.Left, UIStyle.Text, FontStyles.Bold);
            ui.DeltaVStatusLabel = UIKit.TextTop(s, "DeltaVStatus", "", 36, 0.6f, 1f, 76, 60, TextAlignmentOptions.Right, UIStyle.Good, FontStyles.Bold);
            ui.DeltaVBar         = UIKit.BarTop(s, "DeltaVBar", 0f, 1f, 152, 26, UIStyle.Good);
            ui.DeltaVRequiredLabel = UIKit.TextTop(s, "DeltaVRequired", "", 30, 0f, 1f, 192, 44, TextAlignmentOptions.Left, UIStyle.Muted);

            // Energía
            s = UIKit.Section(content, "@design.power", 380);
            ui.BtnSolar          = UIKit.ButtonTop(s, "Solar",   "Solar",   0f,     1f / 3, 78, 90, 32);
            ui.BtnRTG            = UIKit.ButtonTop(s, "RTG",     "RTG",     1f / 3, 2f / 3, 78, 90, 32);
            ui.BtnNuclearReactor = UIKit.ButtonTop(s, "Reactor", "@power.NuclearReactor", 2f / 3, 1f,     78, 90, 32);
            ui.PowerOutputLabel = UIKit.TextTop(s, "PowerOutput", "", 30, 0f,   0.5f, 182, 44, TextAlignmentOptions.Left,  UIStyle.Text);
            ui.PowerUsedLabel   = UIKit.TextTop(s, "PowerUsed",   "", 30, 0.5f, 1f,   182, 44, TextAlignmentOptions.Right, UIStyle.Text);
            ui.PowerNasaLabel   = UIKit.TextTop(s, "PowerNasa",   "", 26, 0f,   1f,   228, 80, TextAlignmentOptions.TopLeft, UIStyle.Muted);
            var warn = UIKit.ImageTop(s, "SolarWarning", 0f, 1f, 314, 50, new Color(UIStyle.Warn.r, UIStyle.Warn.g, UIStyle.Warn.b, 0.2f));
            UIKit.Text(warn.rectTransform, "Text", "@design.solarwarn", 26,
                       new Vector4(0.03f, 0f, 0.97f, 1f), TextAlignmentOptions.Center, UIStyle.Warn);
            ui.SolarWarning = warn.gameObject;

            // Antena
            s = UIKit.Section(content, "@design.antenna", 240);
            ui.BtnLowGain    = UIKit.ButtonTop(s, "Low",    "@antenna.LowGain",  0f,     1f / 3, 78, 90, 32);
            ui.BtnMediumGain = UIKit.ButtonTop(s, "Medium", "@antenna.MediumGain", 1f / 3, 2f / 3, 78, 90, 32);
            ui.BtnHighGain   = UIKit.ButtonTop(s, "High",   "@antenna.HighGain",  2f / 3, 1f,     78, 90, 32);
            ui.AntennaDataRateLabel = UIKit.TextTop(s, "DataRate", "", 30, 0f, 1f, 182, 44, TextAlignmentOptions.Left, UIStyle.Text);

            // Lanzador
            s = UIKit.Section(content, "@design.launcher", 240);
            ui.BtnFalcon9     = UIKit.ButtonTop(s, "Falcon9",     "Falcon 9", 0f,    0.25f, 78, 90, 28);
            ui.BtnFalconHeavy = UIKit.ButtonTop(s, "FalconHeavy", "F. Heavy", 0.25f, 0.5f,  78, 90, 28);
            ui.BtnSLS         = UIKit.ButtonTop(s, "SLS",         "SLS",      0.5f,  0.75f, 78, 90, 28);
            ui.BtnStarship    = UIKit.ButtonTop(s, "Starship",    "Starship", 0.75f, 1f,    78, 90, 28);
            ui.LaunchCapacityLabel = UIKit.TextTop(s, "Capacity", "", 30, 0f, 1f, 182, 44, TextAlignmentOptions.Left, UIStyle.Text);

            // Instrumentos
            var instruments = (ScienceInstrument[])System.Enum.GetValues(typeof(ScienceInstrument));
            int rows = (instruments.Length + 1) / 2;
            s = UIKit.Section(content, "@design.instruments", 78 + rows * 100 + 70);
            ui.InstrumentToggles = new Toggle[instruments.Length];
            for (int i = 0; i < instruments.Length; i++)
            {
                var inst = instruments[i];
                string label = $"{ScienceOpsUI.InstrumentDisplayName(inst)}\n" +
                               $"<size=70%><color=#8FA3C7>{MissionDesign.InstrumentMassKg[inst]:F0} kg · " +
                               $"{MissionDesign.InstrumentPowerW[inst]:F0} W · {MissionDesign.InstrumentSciencePoints[inst]} pts</color></size>";
                int col = i % 2, row = i / 2;
                ui.InstrumentToggles[i] = UIKit.ToggleTop(s, $"Inst_{inst}", label, col * 0.5f, col * 0.5f + 0.5f,
                                                          78 + row * 100, 90);
            }
            ui.InstrumentMassLabel    = UIKit.TextTop(s, "InstMass",    "", 30, 0f,   0.5f, 78 + rows * 100 + 10, 44, TextAlignmentOptions.Left,  UIStyle.Text);
            ui.InstrumentScienceLabel = UIKit.TextTop(s, "InstScience", "", 30, 0.5f, 1f,   78 + rows * 100 + 10, 44, TextAlignmentOptions.Right, UIStyle.Accent);

            // Resumen + lanzar
            var footer = UIKit.Card(p, "Footer", new Vector4(0f, 0f, 1f, 0.245f), UIStyle.Card, rounded: false);
            ui.TotalMassLabel    = UIKit.TextTop(footer, "TotalMass", "", 30, 0.04f, 0.48f, 20, 40, TextAlignmentOptions.Left,  UIStyle.Text);
            ui.TotalCostLabel    = UIKit.TextTop(footer, "TotalCost", "", 30, 0.52f, 0.96f, 20, 40, TextAlignmentOptions.Right, UIStyle.Text);
            ui.MassBar           = UIKit.BarTop(footer, "MassBar",   0.04f, 0.48f, 66, 16, UIStyle.Accent);
            ui.BudgetBar         = UIKit.BarTop(footer, "BudgetBar", 0.52f, 0.96f, 66, 16, UIStyle.Gold);
            ui.MassFractionLabel = UIKit.TextTop(footer, "MassFraction", "", 28, 0.04f, 0.96f, 92, 40, TextAlignmentOptions.Left, UIStyle.Muted);
            ui.ErrorLabel        = UIKit.TextTop(footer, "Error", "", 30, 0.04f, 0.96f, 136, 46, TextAlignmentOptions.Center, UIStyle.Bad, FontStyles.Bold);
            ui.LaunchButton      = UIKit.ButtonTop(footer, "LaunchButton", "@design.launchbtn", 0.04f, 0.96f, 196, 124, 50,
                                                   UIStyle.Accent, UIStyle.Ink, FontStyles.Bold, inset: 0f);
            return panel.go;
        }

        // ══════════════════════════════════════════════════════════════════════
        // EVENTOS DE OPERACIÓN (crucero)
        // ══════════════════════════════════════════════════════════════════════

        // A banner under the HUD's top bar: nothing is dimmed or blocked and the flight keeps
        // running, so a decision never hands the rivals free time
        static void BuildEvents(Transform root, MissionEventsUI ev)
        {
            var go = UIKit.Stretch(root, "EventPanel").gameObject;
            var safe = UIKit.Stretch(go.transform, "Content");
            safe.gameObject.AddComponent<SafeAreaFitter>();
            var card = UIKit.Card(UIKit.Column(safe), "Banner", new Vector4(0.03f, 0.655f, 0.97f, 0.84f), UIStyle.Card);
            card.GetComponent<Image>().raycastTarget = true;   // taps on the banner don't thrust

            UIKit.Text(card, "Tag", "@ev.header", 22, new Vector4(0.04f, 0.83f, 0.66f, 0.97f), TextAlignmentOptions.Left, UIStyle.Warn, FontStyles.Bold);
            ev.TimerLabel = UIKit.Text(card, "Timer", "", 22, new Vector4(0.6f, 0.83f, 0.96f, 0.97f), TextAlignmentOptions.Right, UIStyle.Muted);
            ev.TitleLabel = UIKit.Text(card, "Title", "", 40, new Vector4(0.04f, 0.64f, 0.96f, 0.83f), TextAlignmentOptions.Left, UIStyle.Text, FontStyles.Bold);
            ev.BodyLabel  = UIKit.Text(card, "Body", "", 26, new Vector4(0.04f, 0.37f, 0.96f, 0.64f), TextAlignmentOptions.TopLeft, UIStyle.Text);
            ev.OptionA = UIKit.Button(card, "OptionA", "A", new Vector4(0.03f, 0.07f, 0.495f, 0.36f), UIStyle.Accent, UIStyle.Ink, 26, FontStyles.Bold);
            ev.OptionB = UIKit.Button(card, "OptionB", "B", new Vector4(0.505f, 0.07f, 0.97f, 0.36f), UIStyle.ButtonIdle, UIStyle.Text, 26, FontStyles.Bold);
            ev.OptionALabel = ev.OptionA.GetComponentInChildren<TMP_Text>();
            ev.OptionBLabel = ev.OptionB.GetComponentInChildren<TMP_Text>();
            ev.OptionALabel.textWrappingMode = TextWrappingModes.Normal;
            ev.OptionBLabel.textWrappingMode = TextWrappingModes.Normal;
            ev.TimerBar = UIKit.Bar(card, "TimerBar", new Vector4(0.03f, 0.01f, 0.97f, 0.045f), UIStyle.Warn);

            ev.Panel = go;
            go.transform.SetAsLastSibling();
            go.SetActive(false);
        }

        // ══════════════════════════════════════════════════════════════════════
        // CAMPAÑA · PROGRAMA NASA
        // ══════════════════════════════════════════════════════════════════════

        static Button _campaignButton;   // created with the lobby, wired to the campaign screen

        static void BuildCampaign(Transform root, CampaignUI camp)
        {
            var go = UIKit.Stretch(root, "CampaignPanel").gameObject;
            var bg = go.AddComponent<Image>();
            bg.color = UIStyle.Backdrop;
            bg.raycastTarget = true;
            var safe = UIKit.Stretch(go.transform, "Content");
            safe.gameObject.AddComponent<SafeAreaFitter>();
            var p = UIKit.Column(safe);

            UIKit.Text(p, "Title", "@camp.title", 64, new Vector4(0.05f, 0.9f, 0.95f, 0.97f), TextAlignmentOptions.Left, UIStyle.Gold, FontStyles.Bold);
            UIKit.Text(p, "Subtitle", "@camp.subtitle", 28, new Vector4(0.05f, 0.85f, 0.95f, 0.9f), TextAlignmentOptions.TopLeft, UIStyle.Muted);
            camp.TotalLabel = UIKit.Text(p, "Total", "", 30, new Vector4(0.05f, 0.81f, 0.95f, 0.85f), TextAlignmentOptions.Left, UIStyle.Text, FontStyles.Bold);

            int n = Campaign.Missions.Length;
            camp.MissionButtons = new Button[n];
            float top = 0.8f, bottom = 0.15f, h = (top - bottom) / n;
            for (int i = 0; i < n; i++)
            {
                float y1 = top - i * h, y0 = y1 - h + 0.012f;
                var b = UIKit.Button(p, $"Mission_{i}", "", new Vector4(0.05f, y0, 0.95f, y1), UIStyle.Card, UIStyle.Text, 32);
                var label = b.GetComponentInChildren<TMP_Text>();
                label.alignment = TextAlignmentOptions.Left;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.rectTransform.offsetMin = new Vector2(24, 0);
                camp.MissionButtons[i] = b;
            }
            camp.BackButton = UIKit.Button(p, "Back", "@camp.back", new Vector4(0.05f, 0.04f, 0.95f, 0.12f), UIStyle.ButtonIdle, UIStyle.Text, 36);
            camp.OpenButton = _campaignButton;
            camp.Panel = go;
            go.transform.SetAsLastSibling();
            go.SetActive(false);
        }

        // ══════════════════════════════════════════════════════════════════════
        // TUTORIAL DE LA PRIMERA MISIÓN
        // ══════════════════════════════════════════════════════════════════════

        static void BuildTutorial(Transform root, TutorialUI tut, GameObject designPanel)
        {
            var go = UIKit.Stretch(root, "TutorialPanel").gameObject;
            var dim = go.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.7f);
            dim.raycastTarget = true;
            var card = UIKit.Card(UIKit.Column(go.transform), "Card", new Vector4(0.05f, 0.2f, 0.95f, 0.8f), UIStyle.Card);

            UIKit.Text(card, "Tag", "@tut.tag", 26, new Vector4(0.05f, 0.91f, 0.7f, 0.97f), TextAlignmentOptions.Left, UIStyle.Accent, FontStyles.Bold);
            tut.CounterLabel = UIKit.Text(card, "Counter", "1 / 8", 26, new Vector4(0.7f, 0.91f, 0.95f, 0.97f), TextAlignmentOptions.Right, UIStyle.Muted);
            tut.TitleLabel   = UIKit.Text(card, "Title", "", 50, new Vector4(0.05f, 0.79f, 0.95f, 0.9f), TextAlignmentOptions.Left, UIStyle.Text, FontStyles.Bold);
            tut.BodyLabel    = UIKit.Text(card, "Body", "", 32, new Vector4(0.05f, 0.2f, 0.95f, 0.78f), TextAlignmentOptions.TopLeft, UIStyle.Text);
            tut.BackButton = UIKit.Button(card, "Back", "@tut.back", new Vector4(0.04f, 0.04f, 0.3f, 0.15f), UIStyle.ButtonIdle, UIStyle.Text, 30);
            tut.SkipButton = UIKit.Button(card, "Skip", "@tut.skip", new Vector4(0.32f, 0.04f, 0.52f, 0.15f), UIStyle.ButtonIdle, UIStyle.Muted, 28);
            tut.NextButton = UIKit.Button(card, "Next", "@tut.next", new Vector4(0.54f, 0.04f, 0.96f, 0.15f), UIStyle.Accent, UIStyle.Ink, 32, FontStyles.Bold);
            tut.NextLabel  = tut.NextButton.GetComponentInChildren<TMP_Text>();
            // NextLabel changes with the step: drop the fixed-key binding
            Object.DestroyImmediate(tut.NextLabel.GetComponent<LocalizedText>());

            // "?" on the mission design header reopens the briefing
            var content = designPanel.transform.Find("Content/Column");
            tut.HelpButton = UIKit.Button(content != null ? content : designPanel.transform, "HelpButton", "?",
                                          new Vector4(0.86f, 0.935f, 0.96f, 0.99f), UIStyle.ButtonIdle, UIStyle.Accent, 44, FontStyles.Bold);

            tut.Panel = go;
            go.transform.SetAsLastSibling();
            go.SetActive(false);
        }

        // ══════════════════════════════════════════════════════════════════════
        // CUENTA ATRÁS
        // ══════════════════════════════════════════════════════════════════════

        static GameObject BuildCountdown(RectTransform safe, GameManager gm)
        {
            var go = UIKit.Stretch(safe, "CountdownPanel").gameObject;
            var dim = UIKit.Image(go.transform, "Dim", new Vector4(0f, 0f, 1f, 1f), new Color(0f, 0f, 0f, 0.35f));
            dim.raycastTarget = false;
            gm.CountdownLabel = UIKit.Text(go.transform, "Count", "3", 240, new Vector4(0f, 0.45f, 1f, 0.65f),
                                           TextAlignmentOptions.Center, UIStyle.Accent, FontStyles.Bold);
            gm.CountdownSubLabel = UIKit.Text(go.transform, "Sub", "", 36, new Vector4(0.08f, 0.33f, 0.92f, 0.44f),
                                              TextAlignmentOptions.Center, UIStyle.Text);
            return go;
        }

        // ══════════════════════════════════════════════════════════════════════
        // HUD EN VUELO — nada intercepta toques: toda la pantalla es el acelerador
        // ══════════════════════════════════════════════════════════════════════

        static GameObject BuildHUD(RectTransform safe, CockpitHUD hud)
        {
            var go = UIKit.Stretch(safe, "HUDPanel").gameObject;
            var p = go.transform;

            hud.DamageIndicatorDamaged  = UIKit.Image(p, "DamageOverlay",   new Vector4(0, 0, 1, 1), new Color(1f, 0.55f, 0.1f, 0.12f)).gameObject;
            hud.DamageIndicatorCritical = UIKit.Image(p, "CriticalOverlay", new Vector4(0, 0, 1, 1), new Color(1f, 0.15f, 0.15f, 0.22f)).gameObject;
            var vignette = VignetteSprite();
            foreach (var o in new[] { hud.DamageIndicatorDamaged, hud.DamageIndicatorCritical })
            {
                var img = o.GetComponent<Image>();
                img.sprite = vignette;
                img.type   = UnityEngine.UI.Image.Type.Simple;
            }
            hud.DamageIndicatorDamaged.SetActive(false);
            hud.DamageIndicatorCritical.SetActive(false);
            hud.Vignette = UIKit.Image(p, "Vignette", new Vector4(0, 0, 1, 1), new Color(1f, 0.5f, 0.1f, 0f));
            hud.Vignette.sprite = vignette;
            hud.Vignette.type   = UnityEngine.UI.Image.Type.Simple;

            // Barra superior: posición · distancia · tiempo
            var top = UIKit.Card(p, "TopBar", new Vector4(0.03f, 0.905f, 0.97f, 0.985f), UIStyle.HudCard);
            hud.RacePositionLabel = UIKit.Text(top, "Position", "#1", 64, new Vector4(0.04f, 0f, 0.26f, 1f),
                                               TextAlignmentOptions.Left, UIStyle.Gold, FontStyles.Bold);
            hud.DistanceLabel = UIKit.Text(top, "Distance", "", 44, new Vector4(0.26f, 0f, 0.74f, 1f),
                                           TextAlignmentOptions.Center, UIStyle.Text, FontStyles.Bold);
            hud.TimeLabel = UIKit.Text(top, "Time", "", 44, new Vector4(0.74f, 0f, 0.96f, 1f),
                                       TextAlignmentOptions.Right, UIStyle.Text);
            hud.ProgressBar = UIKit.Bar(p, "Progress", new Vector4(0.03f, 0.89f, 0.97f, 0.898f), UIStyle.Accent);
            hud.CommDelayLabel = UIKit.Text(p, "CommDelay", "", 28, new Vector4(0.03f, 0.85f, 0.97f, 0.885f),
                                            TextAlignmentOptions.Center, UIStyle.Muted);

            // Flecha al planeta: gira alrededor del centro de la pantalla
            hud.ArrowToPlanet = UIKit.Image(p, "ArrowToPlanet", new Vector4(0.5f, 0.5f, 0.5f, 0.5f), UIStyle.Accent);
            hud.ArrowToPlanet.rectTransform.sizeDelta = new Vector2(560, 560);
            hud.ArrowToPlanet.sprite = ArrowSprite();
            hud.ArrowToPlanet.type   = UnityEngine.UI.Image.Type.Simple;

            // Avisos centrales
            hud.BurnoutModeIndicator = UIKit.Text(p, "BurnoutMode", "¡BURNOUT!", 110, new Vector4(0.05f, 0.62f, 0.95f, 0.72f),
                                                  TextAlignmentOptions.Center, UIStyle.Burnout, FontStyles.Bold | FontStyles.Italic).gameObject;
            hud.BurnoutModeIndicator.SetActive(false);
            hud.NearMissComboLabel = UIKit.Text(p, "NearMiss", "NEAR MISS", 56, new Vector4(0.05f, 0.56f, 0.95f, 0.62f),
                                                TextAlignmentOptions.Center, UIStyle.Gold, FontStyles.Bold);
            hud.NearMissComboObj = hud.NearMissComboLabel.gameObject;
            hud.NearMissComboObj.SetActive(false);
            hud.LowFuelWarning = UIKit.Text(p, "LowFuel", "LOW FUEL", 44, new Vector4(0.05f, 0.3f, 0.95f, 0.34f),
                                            TextAlignmentOptions.Center, UIStyle.Warn, FontStyles.Bold).gameObject;
            hud.LowFuelWarning.SetActive(false);

            hud.ImpactLabel = UIKit.Text(p, "Impact", "", 50, new Vector4(0.04f, 0.47f, 0.96f, 0.54f),
                                         TextAlignmentOptions.Center, UIStyle.Warn, FontStyles.Bold);

            UIKit.Text(p, "ControlsHint", "@hud.controls", 26,
                       new Vector4(0.03f, 0.265f, 0.97f, 0.29f), TextAlignmentOptions.Center, UIStyle.Muted);

            // Burnout + casco
            UIKit.Text(p, "BurnoutTitle", "BURNOUT", 28, new Vector4(0.03f, 0.235f, 0.5f, 0.26f),
                       TextAlignmentOptions.Left, UIStyle.Burnout, FontStyles.Bold);
            hud.LivesLabel = UIKit.Text(p, "Lives", "HULL 3/3", 28, new Vector4(0.5f, 0.235f, 0.97f, 0.26f),
                                        TextAlignmentOptions.Right, UIStyle.Text, FontStyles.Bold);
            hud.BurnoutBar = UIKit.Bar(p, "BurnoutBar", new Vector4(0.03f, 0.215f, 0.97f, 0.232f), UIStyle.Burnout);

            // Velocidad y combustible
            var speed = UIKit.Card(p, "SpeedCard", new Vector4(0.03f, 0.03f, 0.485f, 0.2f), UIStyle.HudCard);
            UIKit.Text(speed, "Title", "@hud.speed", 26, new Vector4(0.08f, 0.7f, 0.92f, 0.92f), TextAlignmentOptions.Left, UIStyle.Muted, FontStyles.Bold);
            hud.SpeedLabel = UIKit.Text(speed, "Value", "0 m/s", 60, new Vector4(0.08f, 0.28f, 0.92f, 0.72f),
                                        TextAlignmentOptions.Left, UIStyle.Text, FontStyles.Bold);
            hud.SpeedGaugeFill = UIKit.Bar(speed, "Gauge", new Vector4(0.08f, 0.12f, 0.92f, 0.2f), UIStyle.Accent);

            var fuel = UIKit.Card(p, "FuelCard", new Vector4(0.515f, 0.03f, 0.97f, 0.2f), UIStyle.HudCard);
            UIKit.Text(fuel, "Title", "@hud.fuel", 26, new Vector4(0.08f, 0.7f, 0.92f, 0.92f), TextAlignmentOptions.Left, UIStyle.Muted, FontStyles.Bold);
            hud.FuelLabel = UIKit.Text(fuel, "Value", "100%", 60, new Vector4(0.08f, 0.28f, 0.92f, 0.72f),
                                       TextAlignmentOptions.Left, UIStyle.Text, FontStyles.Bold);
            hud.FuelBar = UIKit.Bar(fuel, "Bar", new Vector4(0.08f, 0.12f, 0.92f, 0.2f), UIStyle.Good);

            foreach (var g in go.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
            return go;
        }

        // ══════════════════════════════════════════════════════════════════════
        // CIENCIA EN EL PLANETA
        // ══════════════════════════════════════════════════════════════════════

        static GameObject BuildScienceOps(Transform root, RectTransform safe, ScienceOpsUI ui)
        {
            var panel = UIKit.Panel(root, safe, "ScienceOpsPanel", UIStyle.Backdrop);
            var p = panel.content;
            ui.Panel = panel.go;

            ui.PlanetNameLabel = UIKit.Text(p, "Planet", "MARS", 90, new Vector4(0.05f, 0.9f, 0.95f, 0.98f),
                                            TextAlignmentOptions.Left, UIStyle.Accent, FontStyles.Bold);
            ui.ArrivalPositionLabel  = UIKit.Text(p, "Arrival", "", 34, new Vector4(0.05f, 0.865f, 0.6f, 0.9f), TextAlignmentOptions.Left, UIStyle.Gold, FontStyles.Bold);
            ui.MissionObjectiveLabel = UIKit.Text(p, "Objective", "", 30, new Vector4(0.6f, 0.865f, 0.95f, 0.9f), TextAlignmentOptions.Right, UIStyle.Text);
            ui.CommDelayLabel        = UIKit.Text(p, "CommDelay", "", 28, new Vector4(0.05f, 0.835f, 0.95f, 0.865f), TextAlignmentOptions.Left, UIStyle.Muted);

            var info = UIKit.Card(p, "PlanetInfo", new Vector4(0.05f, 0.755f, 0.95f, 0.83f), UIStyle.Card);
            ui.GravityLabel    = UIKit.Text(info, "Gravity",    "", 32, new Vector4(0.04f, 0.52f, 0.96f, 0.92f), TextAlignmentOptions.Left, UIStyle.Text);
            ui.AtmosphereLabel = UIKit.Text(info, "Atmosphere", "", 28, new Vector4(0.04f, 0.08f, 0.96f, 0.5f),  TextAlignmentOptions.Left, UIStyle.Muted);

            ui.DownlinkLabel = UIKit.Text(p, "Downlink", "", 28, new Vector4(0.05f, 0.72f, 0.95f, 0.75f), TextAlignmentOptions.Left, UIStyle.Muted);
            ui.WindowLabel   = UIKit.Text(p, "Window",   "", 36, new Vector4(0.05f, 0.685f, 0.95f, 0.72f), TextAlignmentOptions.Left, UIStyle.Text, FontStyles.Bold);

            var power = UIKit.Card(p, "Power", new Vector4(0.05f, 0.605f, 0.95f, 0.68f), UIStyle.Card);
            UIKit.Text(power, "Title", "@sci.power", 28, new Vector4(0.04f, 0.55f, 0.4f, 0.92f), TextAlignmentOptions.Left, UIStyle.Muted, FontStyles.Bold);
            ui.PowerLabel       = UIKit.Text(power, "Value",  "", 32, new Vector4(0.4f, 0.55f, 0.75f, 0.92f), TextAlignmentOptions.Center, UIStyle.Text);
            ui.PowerStatusLabel = UIKit.Text(power, "Status", "", 32, new Vector4(0.75f, 0.55f, 0.96f, 0.92f), TextAlignmentOptions.Right, UIStyle.Good, FontStyles.Bold);
            ui.PowerBar         = UIKit.Bar(power, "Bar", new Vector4(0.04f, 0.18f, 0.96f, 0.36f), UIStyle.Accent);

            UIKit.Text(p, "InstTitle", "@sci.pipeline", 28, new Vector4(0.05f, 0.575f, 0.95f, 0.6f),
                       TextAlignmentOptions.Left, UIStyle.Muted, FontStyles.Bold);
            var list = UIKit.Rect(p, "Instruments", new Vector4(0.05f, 0.3f, 0.95f, 0.57f));
            var vlg = list.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12;
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            vlg.childControlWidth  = true; vlg.childForceExpandWidth  = true;
            ui.InstrumentsContainer = list;
            ui.InstrumentItemPrefab = InstrumentItem();

            var score = UIKit.Card(p, "Score", new Vector4(0.05f, 0.16f, 0.95f, 0.29f), UIStyle.Card);
            ui.SciencePointsLabel = UIKit.Text(score, "Science", "", 32, new Vector4(0.04f, 0.62f, 0.5f, 0.92f), TextAlignmentOptions.Left, UIStyle.Accent, FontStyles.Bold);
            ui.TotalScoreLabel    = UIKit.Text(score, "Total",   "", 40, new Vector4(0.5f, 0.62f, 0.96f, 0.92f), TextAlignmentOptions.Right, UIStyle.Gold, FontStyles.Bold);
            ui.ScoreBar           = UIKit.Bar(score, "ScoreBar", new Vector4(0.04f, 0.46f, 0.96f, 0.56f), UIStyle.Gold);
            ui.FuelLabel          = UIKit.Text(score, "Fuel", "", 28, new Vector4(0.04f, 0.08f, 0.3f, 0.38f), TextAlignmentOptions.Left, UIStyle.Muted);
            ui.FuelBar            = UIKit.Bar(score, "FuelBar", new Vector4(0.32f, 0.17f, 0.96f, 0.29f), UIStyle.Good);

            ui.CompleteMissionBtn = UIKit.Button(p, "CompleteMission", "Operations in progress…",
                                                 new Vector4(0.05f, 0.03f, 0.95f, 0.135f),
                                                 UIStyle.Accent, UIStyle.Ink, 44, FontStyles.Bold);
            ui.CompleteBtnLabel = ui.CompleteMissionBtn.GetComponentInChildren<TMP_Text>();
            return panel.go;
        }

        static GameObject InstrumentItem()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(InstrumentPrefab));
            var item = new GameObject("InstrumentItem", typeof(RectTransform));
            var le = item.AddComponent<LayoutElement>();
            le.preferredHeight = 88;
            le.minHeight       = 60;
            var bg = item.AddComponent<Image>();
            UIKit.Rounded(bg, UIStyle.Card);

            UIKit.Text(item.transform, "Label",    "Instrument", 30, new Vector4(0.04f, 0.4f, 0.64f, 0.95f), TextAlignmentOptions.Left,  UIStyle.Text);
            UIKit.Text(item.transform, "PtsLabel", "0 / 0",       28, new Vector4(0.64f, 0.4f, 0.96f, 0.95f), TextAlignmentOptions.Right, UIStyle.Accent);
            UIKit.Image(item.transform, "BarTrack", new Vector4(0.04f, 0.14f, 0.96f, 0.3f), UIStyle.BarTrack);
            var fill = UIKit.Image(item.transform, "Bar", new Vector4(0.04f, 0.14f, 0.96f, 0.3f), UIStyle.Accent);
            UIKit.MakeFilled(fill);
            fill.fillAmount = 0f;
            foreach (var g in item.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;

            var prefab = PrefabUtility.SaveAsPrefabAsset(item, InstrumentPrefab);
            Object.DestroyImmediate(item);
            return prefab;
        }

        // ══════════════════════════════════════════════════════════════════════
        // SCOREBOARD
        // ══════════════════════════════════════════════════════════════════════

        static GameObject BuildScoreboard(Transform root, RectTransform safe, ScoreboardUI ui)
        {
            var panel = UIKit.Panel(root, safe, "ScoreboardPanel", UIStyle.Backdrop);
            var p = panel.content;
            ui.Panel = panel.go;

            ui.WinnerLabel    = UIKit.Text(p, "Winner", "", 100, new Vector4(0.03f, 0.9f, 0.97f, 0.975f), TextAlignmentOptions.Center, UIStyle.Gold, FontStyles.Bold);
            ui.WinnerSubLabel = UIKit.Text(p, "WinnerSub", "", 32, new Vector4(0.05f, 0.86f, 0.95f, 0.9f), TextAlignmentOptions.Center, UIStyle.Text);

            var local = UIKit.Card(p, "Local", new Vector4(0.05f, 0.69f, 0.95f, 0.855f), UIStyle.Card);
            ui.LocalPlayerName   = UIKit.Text(local, "Name",  "You", 40, new Vector4(0.05f, 0.8f, 0.55f, 0.95f), TextAlignmentOptions.Left, UIStyle.Text, FontStyles.Bold);
            ui.LocalScoreLabel   = UIKit.Text(local, "Score", "0", 96, new Vector4(0.45f, 0.62f, 0.95f, 0.97f), TextAlignmentOptions.Right, UIStyle.Accent, FontStyles.Bold);
            ui.LocalArrivalLabel = UIKit.Text(local, "Arrival", "", 34, new Vector4(0.05f, 0.64f, 0.55f, 0.8f), TextAlignmentOptions.Left, UIStyle.Gold, FontStyles.Bold);
            ui.LocalScoreBar     = UIKit.Bar(local, "Bar", new Vector4(0.05f, 0.52f, 0.95f, 0.58f), UIStyle.Accent);
            ui.LocalScienceLabel  = UIKit.Text(local, "Science",   "", 32, new Vector4(0.05f, 0.33f, 0.95f, 0.47f), TextAlignmentOptions.Left, UIStyle.Text);
            ui.LocalFuelLabel     = UIKit.Text(local, "Fuel",      "", 32, new Vector4(0.05f, 0.18f, 0.95f, 0.32f), TextAlignmentOptions.Left, UIStyle.Text);
            ui.LocalTakedownLabel = UIKit.Text(local, "Takedowns", "", 32, new Vector4(0.05f, 0.03f, 0.95f, 0.17f), TextAlignmentOptions.Left, UIStyle.Text);

            var remote = UIKit.Card(p, "Remote", new Vector4(0.05f, 0.535f, 0.95f, 0.68f), UIStyle.Card);
            ui.RemoteBlock        = remote.gameObject;
            ui.RemotePlayerName   = UIKit.Text(remote, "Name", "", 30, new Vector4(0.04f, 0.78f, 0.6f, 0.96f), TextAlignmentOptions.Left, UIStyle.Accent, FontStyles.Bold);
            ui.RemoteScoreLabel   = UIKit.Text(remote, "Score", "-", 64, new Vector4(0.6f, 0.55f, 0.96f, 0.96f), TextAlignmentOptions.Right, UIStyle.Gold, FontStyles.Bold);
            ui.RemoteArrivalLabel = UIKit.Text(remote, "Status", "", 26, new Vector4(0.04f, 0.14f, 0.96f, 0.78f), TextAlignmentOptions.TopLeft, UIStyle.Text);
            ui.RemoteScoreBar     = UIKit.Bar(remote, "Bar", new Vector4(0.04f, 0.04f, 0.96f, 0.1f), UIStyle.Gold);

            var debrief = UIKit.Card(p, "Debrief", new Vector4(0.05f, 0.245f, 0.95f, 0.525f), UIStyle.Card);
            ui.DebriefLabel = UIKit.Text(debrief, "Text", "", 27, new Vector4(0.04f, 0.03f, 0.96f, 0.97f),
                                         TextAlignmentOptions.TopLeft, UIStyle.Text);
            ui.DebriefLabel.fontSizeMin = 16f;

            ui.PlayAgainButton = UIKit.Button(p, "PlayAgain", "@sb.again", new Vector4(0.05f, 0.13f, 0.49f, 0.23f),
                                              UIStyle.ButtonIdle, UIStyle.Text, 32, FontStyles.Bold);
            ui.NextMissionButton = UIKit.Button(p, "NextMission", "@sb.next", new Vector4(0.51f, 0.13f, 0.95f, 0.23f),
                                                UIStyle.Accent, UIStyle.Ink, 40, FontStyles.Bold);
            ui.NextMissionButton.gameObject.SetActive(false);
            ui.MainMenuButton  = UIKit.Button(p, "MainMenu", "@sb.menu", new Vector4(0.05f, 0.03f, 0.95f, 0.11f),
                                              UIStyle.ButtonIdle, UIStyle.Text, 38);
            return panel.go;
        }

        // ══════════════════════════════════════════════════════════════════════
        // Infra
        // ══════════════════════════════════════════════════════════════════════

        static bool EnsureTMPResources()
        {
            var settings = Resources.Load<TMP_Settings>("TMP Settings");
            if (settings != null && TMP_Settings.defaultFontAsset != null) return true;

            EditorUtility.DisplayDialog("Navigator — TextMeshPro",
                "Faltan los recursos esenciales de TextMeshPro.\n\n" +
                "Se abrirá el importador: pulsa \"Import TMP Essentials\" y luego vuelve a ejecutar " +
                "Navigator → Setup Scene.", "OK");
            EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
            return false;
        }

        static void EnsureEventSystem()
        {
            var es = Object.FindFirstObjectByType<EventSystem>();
            if (es == null) es = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            var legacy = es.GetComponent<StandaloneInputModule>();
            if (legacy != null) Object.DestroyImmediate(legacy);
            if (es.GetComponent<InputSystemUIInputModule>() == null)
                es.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        // A soft arrowhead near the top of a transparent square: rotating the square
        // around the screen center moves the arrow along a ring.
        static Sprite ArrowSprite()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(ArrowSpritePath);
            if (existing != null) return existing;

            Directory.CreateDirectory(GeneratedDir);
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            Vector2 apex = new(128, 250), left = new(88, 196), right = new(168, 196), notch = new(128, 212);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var pt = new Vector2(x + 0.5f, y + 0.5f);
                bool inside = InTriangle(pt, apex, left, notch) || InTriangle(pt, apex, notch, right);
                px[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(ArrowSpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(ArrowSpritePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(ArrowSpritePath);
            importer.textureType         = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled       = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(ArrowSpritePath);
        }

        // Transparent centre, soft coloured edges: tinted per effect (Burnout, damage)
        static Sprite VignetteSprite()
        {
            const string path = GeneratedDir + "/Vignette.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            Directory.CreateDirectory(GeneratedDir);
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy) / Mathf.Sqrt(2f);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, r));
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType         = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled       = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Cross(p, a, b), d2 = Cross(p, b, c), d3 = Cross(p, c, a);
            bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(neg && pos);
        }

        static float Cross(Vector2 p, Vector2 a, Vector2 b) => (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // UIKit — helpers de construcción. Anchors como Vector4(xMin, yMin, xMax, yMax)
    // normalizados; las funciones *Top colocan por píxeles desde el borde superior.
    // ══════════════════════════════════════════════════════════════════════════

    static class UIKit
    {
        static Sprite _rounded, _knob, _check;
        static Sprite RoundedSprite => _rounded ??= AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        static Sprite KnobSprite    => _knob    ??= AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        static Sprite CheckSprite   => _check   ??= AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");

        public struct PanelParts { public GameObject go; public RectTransform content; }

        // Full-screen backdrop (under the notch too) with content inside the safe area
        public static PanelParts Panel(Transform root, RectTransform safe, string name, Color backdrop)
        {
            var go = Stretch(root, name).gameObject;
            go.transform.SetSiblingIndex(0);
            var bg = go.AddComponent<Image>();
            bg.color = backdrop;
            bg.raycastTarget = true;   // blocks taps from reaching the flight controls

            var content = Stretch(go.transform, "Content");
            content.gameObject.AddComponent<SafeAreaFitter>();
            return new PanelParts { go = go, content = Column(content) };
        }

        public static RectTransform Stretch(Transform parent, string name) => Rect(parent, name, new Vector4(0, 0, 1, 1));

        // Phone-shaped centred column so layouts never squash in landscape windows
        public static RectTransform Column(Transform parent)
        {
            var col = Stretch(parent, "Column");
            col.gameObject.AddComponent<PortraitColumn>();
            return col;
        }

        public static RectTransform Rect(Transform parent, string name, Vector4 a)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(a.x, a.y);
            rt.anchorMax = new Vector2(a.z, a.w);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        // Pixel placement from the top edge of `parent`, horizontally by normalized x0..x1
        public static RectTransform RectTop(Transform parent, string name, float x0, float x1, float top, float height, float inset = 6f)
        {
            var rt = Rect(parent, name, new Vector4(x0, 1f, x1, 1f));
            rt.offsetMin = new Vector2(inset, -top - height);
            rt.offsetMax = new Vector2(-inset, -top);
            return rt;
        }

        public static void Fill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        public static void Rounded(Image img, Color c)
        {
            img.sprite = RoundedSprite;
            img.type   = UnityEngine.UI.Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 0.35f;   // bigger corner radius at 1080p
            img.color  = c;
        }

        public static Image Image(Transform parent, string name, Vector4 a, Color c)
        {
            var img = Rect(parent, name, a).gameObject.AddComponent<Image>();
            Rounded(img, c);
            img.raycastTarget = false;
            return img;
        }

        public static Image ImageTop(Transform parent, string name, float x0, float x1, float top, float h, Color c)
        {
            var img = RectTop(parent, name, x0, x1, top, h).gameObject.AddComponent<Image>();
            Rounded(img, c);
            img.raycastTarget = false;
            return img;
        }

        public static RectTransform Card(Transform parent, string name, Vector4 a, Color c, bool rounded = true)
        {
            var img = Image(parent, name, a, c);
            if (!rounded) { img.sprite = null; img.type = UnityEngine.UI.Image.Type.Simple; }
            return img.rectTransform;
        }

        public static RawImage Raw(Transform parent, string name, Vector4 a)
        {
            var raw = Rect(parent, name, a).gameObject.AddComponent<RawImage>();
            raw.raycastTarget = false;
            return raw;
        }

        static TMP_Text Style(TextMeshProUGUI t, string text, float size, TextAlignmentOptions align, Color c, FontStyles style)
        {
            // "@key" = localized fixed text that follows the language toggle
            if (!string.IsNullOrEmpty(text) && text[0] == '@')
            {
                string key = text.Substring(1);
                t.gameObject.AddComponent<LocalizedText>().Key = key;
                text = Loc.T(key);
            }
            t.text      = text;
            t.fontSize  = size;
            t.enableAutoSizing = true;
            t.fontSizeMax = size;
            t.fontSizeMin = Mathf.Max(14f, size * 0.55f);
            t.alignment = align;
            t.color     = c;
            t.fontStyle = style;
            t.richText  = true;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode     = TextOverflowModes.Truncate;
            t.raycastTarget    = false;
            return t;
        }

        public static TMP_Text Text(Transform parent, string name, string text, float size, Vector4 a,
                                    TextAlignmentOptions align, Color c, FontStyles style = FontStyles.Normal)
        {
            var t = Rect(parent, name, a).gameObject.AddComponent<TextMeshProUGUI>();
            return Style(t, text, size, align, c, style);
        }

        public static TMP_Text TextTop(Transform parent, string name, string text, float size, float x0, float x1,
                                       float top, float h, TextAlignmentOptions align, Color c,
                                       FontStyles style = FontStyles.Normal)
        {
            var t = RectTop(parent, name, x0, x1, top, h).gameObject.AddComponent<TextMeshProUGUI>();
            return Style(t, text, size, align, c, style);
        }

        static Button MakeButton(RectTransform rt, string label, Color bg, Color fg, float size, FontStyles style)
        {
            var img = rt.gameObject.AddComponent<Image>();
            Rounded(img, bg);
            img.raycastTarget = true;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var cb = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            cb.pressedColor     = new Color(0.75f, 0.75f, 0.75f, 1f);
            cb.selectedColor    = Color.white;
            cb.disabledColor    = new Color(1f, 1f, 1f, 0.35f);
            cb.fadeDuration     = 0.08f;
            btn.colors = cb;
            var t = Text(rt, "Label", label, size, new Vector4(0.04f, 0f, 0.96f, 1f), TextAlignmentOptions.Center, fg, style);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            return btn;
        }

        public static Button Button(Transform parent, string name, string label, Vector4 a, Color bg, Color fg,
                                    float size, FontStyles style = FontStyles.Normal)
            => MakeButton(Rect(parent, name, a), label, bg, fg, size, style);

        public static Button ButtonTop(Transform parent, string name, string label, float x0, float x1,
                                       float top, float h, float size)
            => MakeButton(RectTop(parent, name, x0, x1, top, h), label, UIStyle.ButtonIdle, UIStyle.Text, size, FontStyles.Normal);

        public static Button ButtonTop(Transform parent, string name, string label, float x0, float x1,
                                       float top, float h, float size, Color bg, Color fg, FontStyles style, float inset)
            => MakeButton(RectTop(parent, name, x0, x1, top, h, inset), label, bg, fg, size, style);

        public static void MakeFilled(Image img)
        {
            img.type       = UnityEngine.UI.Image.Type.Filled;
            img.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            img.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
            img.fillAmount = 0.5f;
        }

        // Returns the fill image; the track sits behind it
        public static Image Bar(Transform parent, string name, Vector4 a, Color fill)
        {
            var track = Image(parent, name, a, UIStyle.BarTrack);
            var f = Image(track.rectTransform, "Fill", new Vector4(0, 0, 1, 1), fill);
            MakeFilled(f);
            return f;
        }

        public static Image BarTop(Transform parent, string name, float x0, float x1, float top, float h, Color fill)
        {
            var track = ImageTop(parent, name, x0, x1, top, h, UIStyle.BarTrack);
            var f = Image(track.rectTransform, "Fill", new Vector4(0, 0, 1, 1), fill);
            MakeFilled(f);
            return f;
        }

        public static Slider SliderTop(Transform parent, string name, float x0, float x1, float top, float h)
        {
            var rt = RectTop(parent, name, x0, x1, top, h);
            var slider = rt.gameObject.AddComponent<Slider>();

            var bg = Image(rt, "Background", new Vector4(0f, 0.35f, 1f, 0.65f), UIStyle.BarTrack);
            var fillArea = Rect(rt, "Fill Area", new Vector4(0f, 0.35f, 1f, 0.65f));
            fillArea.offsetMin = new Vector2(10, 0); fillArea.offsetMax = new Vector2(-10, 0);
            var fill = Image(fillArea, "Fill", new Vector4(0f, 0f, 0f, 1f), UIStyle.Accent);
            fill.rectTransform.sizeDelta = new Vector2(10, 0);

            var handleArea = Rect(rt, "Handle Slide Area", new Vector4(0f, 0f, 1f, 1f));
            handleArea.offsetMin = new Vector2(10, 0); handleArea.offsetMax = new Vector2(-10, 0);
            var handle = Image(handleArea, "Handle", new Vector4(0f, 0f, 0f, 1f), Color.white);
            handle.sprite = KnobSprite;
            handle.type   = UnityEngine.UI.Image.Type.Simple;
            handle.preserveAspect = true;
            handle.rectTransform.sizeDelta = new Vector2(h, 0);
            handle.raycastTarget = true;
            bg.raycastTarget = true;

            slider.fillRect      = fill.rectTransform;
            slider.handleRect    = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction     = Slider.Direction.LeftToRight;
            return slider;
        }

        public static Toggle ToggleTop(Transform parent, string name, string label, float x0, float x1, float top, float h)
        {
            var rt = RectTop(parent, name, x0, x1, top, h);
            var bg = rt.gameObject.AddComponent<Image>();
            Rounded(bg, UIStyle.ButtonIdle);
            bg.raycastTarget = true;
            var toggle = rt.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = bg;

            var box = Image(rt, "Box", Vector4.zero, UIStyle.BarTrack);
            box.rectTransform.anchorMin = box.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            box.rectTransform.sizeDelta = new Vector2(48, 48);
            box.rectTransform.anchoredPosition = new Vector2(40, 0);
            var check = Image(box.rectTransform, "Checkmark", new Vector4(0.1f, 0.1f, 0.9f, 0.9f), UIStyle.Accent);
            check.sprite = CheckSprite;
            check.type   = UnityEngine.UI.Image.Type.Simple;
            toggle.graphic = check;

            var t = Text(rt, "Label", label, 28, new Vector4(0f, 0.04f, 0.97f, 0.96f), TextAlignmentOptions.Left, UIStyle.Text);
            t.rectTransform.offsetMin = new Vector2(80, 0);
            toggle.isOn = false;
            return toggle;
        }

        // Scrollable vertical list; returns the content transform
        public static RectTransform Scroll(Transform parent, string name, Vector4 a)
        {
            var rt = Rect(parent, name, a);
            var scroll = rt.gameObject.AddComponent<ScrollRect>();
            var viewport = Stretch(rt, "Viewport");
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);   // catches drags on empty space

            var content = Rect(viewport, "Content", new Vector4(0f, 1f, 1f, 1f));
            content.pivot = new Vector2(0.5f, 1f);
            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(32, 32, 16, 32);
            vlg.spacing = 20;
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            vlg.childControlWidth  = true; vlg.childForceExpandWidth  = true;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport   = viewport;
            scroll.content    = content;
            scroll.horizontal = false;
            scroll.movementType      = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            return content;
        }

        public static RectTransform Section(RectTransform content, string title, float height)
        {
            var card = Image(content, title.TrimStart('@'), Vector4.zero, UIStyle.Card);
            var le = card.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight       = height;
            var inner = Stretch(card.rectTransform, "Inner");
            inner.offsetMin = new Vector2(20, 16);
            inner.offsetMax = new Vector2(-20, -16);
            TextTop(inner, "Title", title, 28, 0f, 1f, 4, 50, TextAlignmentOptions.Left, UIStyle.Accent, FontStyles.Bold);
            return inner;
        }
    }
}
#endif
