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
using Navigator.AR;
using Navigator.Worlds;

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
            // Landscape game: 1080 units tall on every screen, the width grows with the phone
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 1f;

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
            gm.HUDPanel           = BuildHUD(safe, hud, gm.ARShip);
            gm.ScienceOpsPanel    = BuildScienceOps(root.transform, safe, science);
            gm.ScoreboardPanel    = BuildScoreboard(root.transform, safe, board);

            // Overlays go last so they draw above every panel
            BuildEvents(root.transform, events);
            BuildTutorial(root.transform, tutor, gm.MissionDesignPanel);
            BuildCampaign(root.transform, root.AddComponent<CampaignUI>());
            root.AddComponent<ClassroomSession>();
            BuildClassroom(root.transform, root.AddComponent<ClassroomUI>());
            BuildCredits(root.transform, root.AddComponent<CreditsUI>());
            BuildHangar(root.transform, root.AddComponent<HangarUI>());
            BuildSettings(root.transform, safe, root.AddComponent<SettingsUI>());
            gm.SkyFinder = BuildSkyFinder(root.transform, root.AddComponent<SkyFinderUI>(), gm);
            gm.Share     = BuildShare(root.transform, root.AddComponent<ShareChallengeUI>());
            BuildWorldHUD(root.transform, root.AddComponent<WorldHUD>());
            gm.Events   = events;
            gm.Tutorial = tutor;

            // Same sci-fi dressing for the screens built without UIKit.Panel: grid backdrop + intro
            foreach (var name in new[] { "CampaignPanel", "HangarPanel", "CreditsPanel", "ClassroomPanel", "QuizPanel", "TutorialPanel", "SharePanel" })
            {
                var t = root.transform.Find(name);
                if (t != null) UIKit.Dress(t.gameObject);
            }

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

            UIKit.Text(p, "Title", "NAVIGATOR", 110, new Vector4(0.44f, 0.82f, 0.98f, 0.97f),
                       TextAlignmentOptions.Center, UIStyle.Accent, FontStyles.Bold);
            UIKit.Text(p, "Subtitle", "@lobby.subtitle", 30,
                       new Vector4(0.44f, 0.76f, 0.98f, 0.82f), TextAlignmentOptions.Center, UIStyle.Muted);

            var card = UIKit.Card(p, "QRCard", new Vector4(0.02f, 0.15f, 0.41f, 0.84f), UIStyle.Card);
            // Your name: tap to type it (rivals, the scoreboard and the teacher see it)
            lobby.NameInput = UIKit.InputTop(card, "MyName", "@lobby.nameph", 0.12f, 0.88f, 18, 60, 36);
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

            lobby.ScanButton = UIKit.Button(p, "ScanButton", "Scan QR", new Vector4(0.02f, 0.03f, 0.2f, 0.13f),
                                            UIStyle.ButtonIdle, UIStyle.Text, 38);
            lobby.ScanStatusText = UIKit.Text(p, "ScanStatus", "", 28, new Vector4(0.44f, 0.69f, 0.98f, 0.76f),
                                              TextAlignmentOptions.Center, UIStyle.Muted);
            lobby.ConnectedPlayersText = UIKit.Text(p, "Connected", "", 30, new Vector4(0.44f, 0.62f, 0.98f, 0.69f),
                                                    TextAlignmentOptions.Center, UIStyle.Text);
            lobby.StartGameButton = UIKit.Button(p, "StartGameButton", "PLAY SOLO",
                                                 new Vector4(0.46f, 0.43f, 0.96f, 0.6f),
                                                 UIStyle.Accent, UIStyle.Ink, 56, FontStyles.Bold);
            lobby.StartGameLabel = lobby.StartGameButton.GetComponentInChildren<TMP_Text>();
            _campaignButton = UIKit.Button(p, "CampaignButton", "@camp.button", new Vector4(0.46f, 0.26f, 0.62f, 0.4f),
                                           UIStyle.Gold, UIStyle.Ink, 28, FontStyles.Bold);
            lobby.ExploreButton = UIKit.Button(p, "ExploreButton", "@free.button", new Vector4(0.63f, 0.26f, 0.79f, 0.4f),
                                               new Color(0.15f, 0.6f, 0.5f, 1f), UIStyle.Text, 26, FontStyles.Bold);
            lobby.SkyButton = UIKit.Button(p, "SkyButton", "@lobby.sky", new Vector4(0.8f, 0.26f, 0.96f, 0.4f),
                                           new Color(0.35f, 0.25f, 0.7f, 1f), UIStyle.Text, 28, FontStyles.Bold);
            lobby.PasteChallengeButton = UIKit.Button(p, "PasteChallenge", "@lobby.paste", new Vector4(0.21f, 0.03f, 0.41f, 0.13f),
                                                      UIStyle.ButtonIdle, UIStyle.Gold, 30, FontStyles.Bold);

            lobby.LanguageButton = UIKit.Button(p, "LanguageButton", "@lang.button", new Vector4(0.285f, 0.87f, 0.41f, 0.97f),
                                                UIStyle.ButtonIdle, UIStyle.Accent, 26, FontStyles.Bold);
            _teacherButton = UIKit.Button(p, "TeacherButton", "@class.button", new Vector4(0.02f, 0.87f, 0.145f, 0.97f),
                                          UIStyle.ButtonIdle, UIStyle.Gold, 26, FontStyles.Bold);
            _hangarButton = UIKit.Button(p, "HangarButton", "@hangar.button", new Vector4(0.152f, 0.87f, 0.278f, 0.97f),
                                         UIStyle.ButtonIdle, UIStyle.Accent, 26, FontStyles.Bold);
            _creditsButton = UIKit.Button(p, "CreditsButton", "@credits.button", new Vector4(0.55f, 0.04f, 0.74f, 0.12f),
                                          new Color(0f, 0f, 0f, 0f), UIStyle.Muted, 24);
            _settingsButton = UIKit.Button(p, "SettingsButton", "@set.button", new Vector4(0.77f, 0.04f, 0.96f, 0.12f),
                                           new Color(0f, 0f, 0f, 0f), UIStyle.Muted, 24);

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

            UIKit.Text(p, "Title", "@design.title", 54, new Vector4(0.02f, 0.925f, 0.55f, 0.995f),
                       TextAlignmentOptions.Left, UIStyle.Accent, FontStyles.Bold);
            ui.BriefLabel = UIKit.Text(p, "Brief", "", 26, new Vector4(0.02f, 0.855f, 0.63f, 0.925f),
                                       TextAlignmentOptions.TopLeft, UIStyle.Muted);

            var content = UIKit.Scroll(p, "Scroll", new Vector4(0f, 0f, 0.63f, 0.85f));

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

            // Ventana de lanzamiento: la tira muestra el ΔV extra de salir cada día (valles = ventanas)
            s = UIKit.Section(content, "@design.window", 470);
            var chart = UIKit.ImageTop(s, "WindowChart", 0f, 1f, 74, 120, new Color(1f, 1f, 1f, 0.04f));
            ui.WindowChart      = chart.rectTransform;
            ui.WindowSlider     = UIKit.SliderTop(s, "WindowSlider", 0f, 0.62f, 214, 56);
            ui.BestWindowButton = UIKit.ButtonTop(s, "BestWindow", "@design.window.best", 0.65f, 1f, 206, 72, 26);
            ui.WindowLabel      = UIKit.TextTop(s, "WindowInfo", "", 26, 0f, 1f, 292, 170, TextAlignmentOptions.TopLeft, UIStyle.Text);

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
            var footer = UIKit.Card(p, "Footer", new Vector4(0.645f, 0.03f, 0.995f, 0.97f), UIStyle.Card);
            ui.TotalMassLabel    = UIKit.TextTop(footer, "TotalMass", "", 30, 0.04f, 0.48f, 20, 40, TextAlignmentOptions.Left,  UIStyle.Text);
            ui.TotalCostLabel    = UIKit.TextTop(footer, "TotalCost", "", 30, 0.52f, 0.96f, 20, 40, TextAlignmentOptions.Right, UIStyle.Text);
            ui.MassBar           = UIKit.BarTop(footer, "MassBar",   0.04f, 0.48f, 66, 16, UIStyle.Accent);
            ui.BudgetBar         = UIKit.BarTop(footer, "BudgetBar", 0.52f, 0.96f, 66, 16, UIStyle.Gold);
            ui.MassFractionLabel = UIKit.TextTop(footer, "MassFraction", "", 28, 0.04f, 0.96f, 92, 40, TextAlignmentOptions.Left, UIStyle.Muted);
            ui.ErrorLabel        = UIKit.TextTop(footer, "Error", "", 30, 0.04f, 0.96f, 136, 46, TextAlignmentOptions.Center, UIStyle.Bad, FontStyles.Bold);
            ui.ErrorLabel.textWrappingMode = TextWrappingModes.Normal;
            var err = ui.ErrorLabel.rectTransform;   // taller in the side panel: long errors wrap
            err.offsetMin = new Vector2(err.offsetMin.x, -136f - 170f);
            // Launch sits at the bottom of the side panel, right under the thumb
            ui.LaunchButton      = UIKit.Button(footer, "LaunchButton", "@design.launchbtn", new Vector4(0.04f, 0.04f, 0.96f, 0.2f),
                                                UIStyle.Accent, UIStyle.Ink, 50, FontStyles.Bold);
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
            var card = UIKit.Card(UIKit.Column(safe), "Banner", new Vector4(0.2f, 0.52f, 0.8f, 0.86f), UIStyle.Card);
            card.GetComponent<Image>().raycastTarget = true;   // taps on the banner don't thrust
            // Alert look: a warning-coloured frame that breathes, and the banner pops in when it appears
            var alertRim = card.Find("Rim").GetComponent<Image>();   // the card's own frame, recoloured
            alertRim.color = UIStyle.Warn;
            alertRim.gameObject.AddComponent<UIPulse>().Speed = 6f;
            card.gameObject.AddComponent<UIPopIn>().From = 0.8f;
            var stripe = UIKit.Image(card, "AlertStripe", new Vector4(0f, 0.05f, 0.012f, 0.95f), UIStyle.Warn);
            stripe.gameObject.AddComponent<UIPulse>().Speed = 6f;

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
        // ¿DÓNDE ESTÁ ESTA NOCHE? — el cielo real a través de la cámara
        // ══════════════════════════════════════════════════════════════════════

        static SkyFinderUI BuildSkyFinder(Transform root, SkyFinderUI sky, GameManager gm)
        {
            var go = UIKit.Stretch(root, "SkyFinderPanel").gameObject;   // transparent: the camera is the view
            var safe = UIKit.Stretch(go.transform, "Content");
            safe.gameObject.AddComponent<SafeAreaFitter>();
            var p = UIKit.Column(safe);

            var card = UIKit.Card(p, "Info", new Vector4(0.02f, 0.58f, 0.42f, 0.98f), UIStyle.HudCard);
            UIKit.Text(card, "Tag", "@sky.title", 22, new Vector4(0.05f, 0.84f, 0.95f, 0.97f), TextAlignmentOptions.Left, UIStyle.Accent, FontStyles.Bold);
            sky.PlanetLabel = UIKit.Text(card, "Planet", "", 56, new Vector4(0.05f, 0.62f, 0.95f, 0.85f), TextAlignmentOptions.Left, UIStyle.Text, FontStyles.Bold);
            sky.StatusLabel = UIKit.Text(card, "Status", "", 28, new Vector4(0.05f, 0.4f, 0.95f, 0.62f), TextAlignmentOptions.TopLeft, UIStyle.Good, FontStyles.Bold);
            sky.DetailLabel = UIKit.Text(card, "Detail", "", 24, new Vector4(0.05f, 0.03f, 0.95f, 0.4f), TextAlignmentOptions.TopLeft, UIStyle.Text);

            sky.Arrow = UIKit.Image(p, "Arrow", new Vector4(0.5f, 0.5f, 0.5f, 0.5f), UIStyle.Gold);
            sky.Arrow.rectTransform.sizeDelta = new Vector2(420, 420);
            sky.Arrow.sprite = ArrowSprite();
            sky.Arrow.type   = UnityEngine.UI.Image.Type.Simple;

            // Hollow ring (a glyph the font has) so the planet stays visible inside it
            var ring = UIKit.Text(p, "Reticle", "○", 260, new Vector4(0.5f, 0.5f, 0.5f, 0.5f), TextAlignmentOptions.Center, UIStyle.Gold);
            ring.enableAutoSizing = false;
            ring.rectTransform.sizeDelta = new Vector2(280, 280);
            sky.Reticle = ring.rectTransform;

            UIKit.Text(p, "Hint", "@sky.hint", 24, new Vector4(0.05f, 0.15f, 0.95f, 0.2f), TextAlignmentOptions.Center, UIStyle.Muted);

            string[] ids = { "mercury", "venus", "mars", "jupiter", "saturn", "uranus", "neptune" };
            sky.PlanetButtons = new Button[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                float x0 = 0.02f + i * (0.7f / ids.Length), x1 = x0 + 0.7f / ids.Length - 0.006f;
                sky.PlanetButtons[i] = UIKit.Button(p, $"Planet_{ids[i]}", $"@planet.{ids[i]}", new Vector4(x0, 0.03f, x1, 0.13f),
                                                    UIStyle.ButtonIdle, UIStyle.Text, 20, FontStyles.Bold);
            }
            sky.CloseButton = UIKit.Button(p, "Close", "@sky.close", new Vector4(0.74f, 0.03f, 0.98f, 0.13f), UIStyle.Accent, UIStyle.Ink, 36, FontStyles.Bold);

            foreach (var g in go.GetComponentsInChildren<Graphic>(true))
                if (g.GetComponent<Button>() == null) g.raycastTarget = false;   // look around freely

            sky.Horizons     = gm.Compass != null ? gm.Compass.Horizons : null;
            sky.SpaceManager = gm.SpaceManager;
            sky.ARCamera     = gm.Rig != null ? gm.Rig.GetComponent<Camera>() : Camera.main;
            sky.Panel = go;
            go.transform.SetAsLastSibling();
            go.SetActive(false);
            return sky;
        }

        // ══════════════════════════════════════════════════════════════════════
        // RETO COMPARTIBLE
        // ══════════════════════════════════════════════════════════════════════

        static ShareChallengeUI BuildShare(Transform root, ShareChallengeUI share)
        {
            var go = UIKit.Stretch(root, "SharePanel").gameObject;
            var dim = go.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.75f);
            dim.raycastTarget = true;
            var card = UIKit.Card(UIKit.Column(go.transform), "Card", new Vector4(0.1f, 0.05f, 0.9f, 0.95f), UIStyle.Card);

            UIKit.Text(card, "Title", "@share.title", 48, new Vector4(0.5f, 0.84f, 0.97f, 0.96f), TextAlignmentOptions.Left, UIStyle.Gold, FontStyles.Bold);
            share.SummaryLabel = UIKit.Text(card, "Summary", "", 28, new Vector4(0.5f, 0.6f, 0.97f, 0.84f), TextAlignmentOptions.TopLeft, UIStyle.Text, FontStyles.Bold);

            var qrArea = UIKit.Rect(card, "QRArea", new Vector4(0.03f, 0.05f, 0.47f, 0.95f));
            var qrBg = UIKit.Image(qrArea, "QRBackground", Vector4.zero, Color.white);
            UIKit.Fill(qrBg.rectTransform);
            qrBg.gameObject.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            share.QRImage = UIKit.Raw(qrBg.rectTransform, "QR", new Vector4(0.03f, 0.03f, 0.97f, 0.97f));

            UIKit.Text(card, "Hint", "@share.hint", 24, new Vector4(0.5f, 0.46f, 0.97f, 0.6f), TextAlignmentOptions.Center, UIStyle.Muted);
            share.CodeLabel = UIKit.Text(card, "Code", "", 18, new Vector4(0.5f, 0.39f, 0.97f, 0.46f), TextAlignmentOptions.Center, UIStyle.Accent);
            share.CodeLabel.textWrappingMode = TextWrappingModes.NoWrap;

            share.CopyButton  = UIKit.Button(card, "Copy", "@share.copy", new Vector4(0.5f, 0.2f, 0.97f, 0.35f), UIStyle.Gold, UIStyle.Ink, 36, FontStyles.Bold);
            share.CopyLabel   = share.CopyButton.GetComponentInChildren<TMP_Text>();
            Object.DestroyImmediate(share.CopyLabel.GetComponent<LocalizedText>());   // switches to "COPIED!"
            share.CloseButton = UIKit.Button(card, "Close", "@share.close", new Vector4(0.5f, 0.04f, 0.97f, 0.16f), UIStyle.ButtonIdle, UIStyle.Text, 30);

            share.Panel = go;
            go.transform.SetAsLastSibling();
            go.SetActive(false);
            return share;
        }

        // ══════════════════════════════════════════════════════════════════════
        // CAMPAÑA · PROGRAMA NASA
        // ══════════════════════════════════════════════════════════════════════

        static Button _campaignButton;
        static Button _teacherButton;
        static Button _creditsButton;
        static Button _settingsButton;
        static Button _hangarButton;    // created with the lobby, wired to the class dashboard   // created with the lobby, wired to the campaign screen

        // Settings: one row per option, the button shows the current value
        static void BuildSettings(Transform root, RectTransform safe, SettingsUI ui)
        {
            var panel = UIKit.Panel(root, safe, "SettingsPanel", UIStyle.Backdrop);
            var p = panel.content;
            ui.Panel = panel.go;
            ui.OpenButton = _settingsButton;
            UIKit.Text(p, "Title", "@set.title", 60, new Vector4(0.03f, 0.87f, 0.97f, 0.97f), TextAlignmentOptions.Left, UIStyle.Accent, FontStyles.Bold);

            string[] keys = { "set.lang", "set.sound", "set.vibration", "set.motion" };
            var buttons = new Button[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                int col = i % 2, row = i / 2;
                float top = 0.83f - row * 0.31f, x0 = 0.03f + col * 0.48f;
                var card = UIKit.Card(p, "Row_" + keys[i], new Vector4(x0, top - 0.27f, x0 + 0.46f, top), UIStyle.Card);
                UIKit.Text(card, "Label", "@" + keys[i], 34, new Vector4(0.05f, 0.5f, 0.6f, 0.92f), TextAlignmentOptions.Left, UIStyle.Text, FontStyles.Bold);
                UIKit.Text(card, "Hint", "@" + keys[i] + ".hint", 24, new Vector4(0.05f, 0.08f, 0.6f, 0.5f), TextAlignmentOptions.TopLeft, UIStyle.Muted);
                buttons[i] = UIKit.Button(card, "Value", "", new Vector4(0.64f, 0.22f, 0.95f, 0.78f), UIStyle.ButtonIdle, UIStyle.Text, 32, FontStyles.Bold);
            }
            ui.LanguageButton  = buttons[0];
            ui.SoundButton     = buttons[1];
            ui.VibrationButton = buttons[2];
            ui.MotionButton    = buttons[3];
            ui.BackButton = UIKit.Button(p, "Back", "@camp.back", new Vector4(0.3f, 0.04f, 0.7f, 0.17f), UIStyle.Accent, UIStyle.Ink, 40, FontStyles.Bold);
            panel.go.transform.SetAsLastSibling();
            panel.go.SetActive(false);
        }

        // World map: Earth at the bottom, eight worlds zig-zagging up to Neptune
        static void BuildCampaign(Transform root, CampaignUI camp)
        {
            var go = UIKit.Stretch(root, "CampaignPanel").gameObject;
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.03f, 0.08f, 0.94f);
            bg.raycastTarget = true;
            var safe = UIKit.Stretch(go.transform, "Content");
            safe.gameObject.AddComponent<SafeAreaFitter>();
            var p = UIKit.Column(safe);

            UIKit.Text(p, "Title", "@camp.title", 60, new Vector4(0.02f, 0.89f, 0.6f, 0.98f), TextAlignmentOptions.Left, UIStyle.Gold, FontStyles.Bold);
            camp.BackButton = UIKit.Button(p, "Back", "@camp.back", new Vector4(0.82f, 0.895f, 0.98f, 0.975f), UIStyle.ButtonIdle, UIStyle.Text, 28);
            UIKit.Text(p, "Subtitle", "@camp.subtitle", 24, new Vector4(0.02f, 0.83f, 0.66f, 0.89f), TextAlignmentOptions.TopLeft, UIStyle.Muted);
            camp.TotalLabel = UIKit.Text(p, "Total", "", 30, new Vector4(0.66f, 0.83f, 0.98f, 0.89f), TextAlignmentOptions.Right, UIStyle.Text, FontStyles.Bold);

            var map = UIKit.Rect(p, "Map", new Vector4(0.02f, 0.06f, 0.98f, 0.8f));
            camp.PathLayer = UIKit.Stretch(map, "Path");   // first child: drawn behind the worlds

            // Earth: the launch base
            var home = MapPoint(map, "Home", new Vector2(0.035f, 0.5f), 96f);
            var earth = home.gameObject.AddComponent<Image>();
            earth.sprite = UIKit.KnobSprite;
            earth.color  = CampaignUI.PlanetColor("earth");
            earth.raycastTarget = false;
            var homeLabel = UIKit.Text(home, "Label", "@camp.home", 22, new Vector4(0f, 0f, 0f, 0f), TextAlignmentOptions.Left, UIStyle.Accent, FontStyles.Bold);
            homeLabel.rectTransform.anchorMin = homeLabel.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            homeLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            homeLabel.rectTransform.sizeDelta = new Vector2(240f, 40f);
            homeLabel.rectTransform.anchoredPosition = new Vector2(0f, -10f);
            homeLabel.alignment = TextAlignmentOptions.Center;
            camp.Home = home;

            // Zig-zag path from left to right (landscape)
            float[] ys = { 0.3f, 0.72f };
            int n = Campaign.Missions.Length;
            camp.Nodes = new MapNode[n];
            for (int i = 0; i < n; i++)
            {
                float x = Mathf.Lerp(0.14f, 0.95f, n == 1 ? 0f : i / (float)(n - 1));
                var node = new MapNode();
                // Halo first so it's drawn behind the planet disc
                var at = new Vector2(x, ys[i % ys.Length]);
                node.Glow = MapPoint(map, $"Glow_{i + 1}", at, 168f).gameObject.AddComponent<Image>();
                node.Glow.sprite = UIKit.KnobSprite;
                node.Glow.color  = new Color(0f, 0f, 0f, 0f);
                node.Glow.raycastTarget = false;

                var point = MapPoint(map, $"World_{i + 1}", at, 104f);
                node.Planet = point.gameObject.AddComponent<Image>();
                node.Planet.sprite = UIKit.KnobSprite;
                node.Planet.color  = CampaignUI.PlanetColor(Campaign.Missions[i].Planet);
                node.Button = point.gameObject.AddComponent<Button>();
                node.Button.targetGraphic = node.Planet;   // (CampaignUI animates its scale)
                // A slow orbit ring around each world
                var orbit = MapPoint(map, $"Orbit_{i + 1}", at, 150f).gameObject.AddComponent<Image>();
                orbit.sprite = UIKit.RingSprite;
                orbit.color  = new Color(node.Planet.color.r, node.Planet.color.g, node.Planet.color.b, 0.35f);
                orbit.raycastTarget = false;
                orbit.transform.SetSiblingIndex(point.GetSiblingIndex());   // behind the planet
                orbit.gameObject.AddComponent<UISpin>().DegreesPerSecond = (i % 2 == 0 ? 12f : -12f);

                // Name and stars under the world (worlds sit side by side in landscape)
                node.Name = UIKit.Text(point, "Name", "", 24, Vector4.zero, TextAlignmentOptions.Center, UIStyle.Text, FontStyles.Bold);
                var nr = node.Name.rectTransform;
                nr.anchorMin = nr.anchorMax = new Vector2(0.5f, 0f);
                nr.pivot = new Vector2(0.5f, 1f);
                nr.sizeDelta = new Vector2(250f, 40f);
                nr.anchoredPosition = new Vector2(0f, -30f);

                node.Stars = UIKit.Text(point, "Stars", "", 24, Vector4.zero, TextAlignmentOptions.Center, UIStyle.Gold);
                var sr = node.Stars.rectTransform;
                sr.anchorMin = sr.anchorMax = new Vector2(0.5f, 0f);
                sr.pivot = new Vector2(0.5f, 1f);
                sr.sizeDelta = new Vector2(250f, 36f);
                sr.anchoredPosition = new Vector2(0f, -70f);

                camp.Nodes[i] = node;
            }

            // Briefing card for the tapped world
            var card = UIKit.Card(p, "Detail", new Vector4(0.52f, 0.04f, 0.98f, 0.5f), UIStyle.Card);
            camp.Detail = card.gameObject;
            card.GetComponent<Image>().raycastTarget = true;
            camp.DetailTitle = UIKit.Text(card, "Title", "", 34, new Vector4(0.05f, 0.8f, 0.85f, 0.95f), TextAlignmentOptions.Left, UIStyle.Accent, FontStyles.Bold);
            camp.CloseDetailButton = UIKit.Button(card, "Close", "X", new Vector4(0.86f, 0.8f, 0.96f, 0.95f), UIStyle.ButtonIdle, UIStyle.Text, 30, FontStyles.Bold);
            camp.DetailBody = UIKit.Text(card, "Body", "", 26, new Vector4(0.05f, 0.3f, 0.95f, 0.79f), TextAlignmentOptions.TopLeft, UIStyle.Text);
            camp.LaunchButton = UIKit.Button(card, "Launch", "@camp.launch", new Vector4(0.05f, 0.05f, 0.95f, 0.27f), UIStyle.Accent, UIStyle.Text, 34, FontStyles.Bold);
            camp.Detail.SetActive(false);

            camp.OpenButton = _campaignButton;
            camp.Panel = go;
            go.transform.SetAsLastSibling();
            go.SetActive(false);
        }

        // A fixed-size point placed at a normalized position of `parent`, centred on it
        static RectTransform MapPoint(RectTransform parent, string name, Vector2 at, float size)
        {
            var rt = UIKit.Rect(parent, name, new Vector4(at.x, at.y, at.x, at.y));
            rt.sizeDelta = new Vector2(size, size);
            return rt;
        }

        // ══════════════════════════════════════════════════════════════════════
        // HANGAR: elegir nave
        // ══════════════════════════════════════════════════════════════════════

        static void BuildHangar(Transform root, HangarUI ui)
        {
            var go = UIKit.Stretch(root, "HangarPanel").gameObject;
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.03f, 0.08f, 0.97f);
            bg.raycastTarget = true;
            var safe = UIKit.Stretch(go.transform, "Content");
            safe.gameObject.AddComponent<SafeAreaFitter>();
            var p = UIKit.Column(safe);

            UIKit.Text(p, "Title", "@hangar.title", 56, new Vector4(0.02f, 0.89f, 0.6f, 0.98f), TextAlignmentOptions.Left, UIStyle.Gold, FontStyles.Bold);
            ui.BackButton = UIKit.Button(p, "Back", "@camp.back", new Vector4(0.82f, 0.895f, 0.98f, 0.975f), UIStyle.ButtonIdle, UIStyle.Text, 28);

            // Turntable view (square, as wide as the column allows)
            var frame = UIKit.Rect(p, "PreviewFrame", new Vector4(0.02f, 0.03f, 0.55f, 0.88f));
            ui.Preview = UIKit.Raw(frame, "Preview", new Vector4(0f, 0f, 1f, 1f));
            ui.Preview.gameObject.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.FitInParent;

            ui.PrevButton = UIKit.Button(p, "Prev", "<", new Vector4(0.58f, 0.68f, 0.67f, 0.82f), UIStyle.ButtonIdle, UIStyle.Text, 48, FontStyles.Bold);
            ui.CounterLabel = UIKit.Text(p, "Counter", "", 28, new Vector4(0.68f, 0.68f, 0.88f, 0.82f), TextAlignmentOptions.Center, UIStyle.Muted);
            ui.NextButton = UIKit.Button(p, "Next", ">", new Vector4(0.89f, 0.68f, 0.98f, 0.82f), UIStyle.ButtonIdle, UIStyle.Text, 48, FontStyles.Bold);

            ui.NameLabel = UIKit.Text(p, "Name", "", 44, new Vector4(0.58f, 0.56f, 0.98f, 0.66f), TextAlignmentOptions.Center, UIStyle.Accent, FontStyles.Bold);
            ui.DescLabel = UIKit.Text(p, "Desc", "", 28, new Vector4(0.58f, 0.2f, 0.98f, 0.55f), TextAlignmentOptions.Top, UIStyle.Text);
            ui.ChooseButton = UIKit.Button(p, "Choose", "@hangar.choose", new Vector4(0.58f, 0.04f, 0.98f, 0.17f), UIStyle.Accent, UIStyle.Ink, 40, FontStyles.Bold);

            ui.Panel = go;
            ui.OpenButton = _hangarButton;
            go.transform.SetAsLastSibling();
            go.SetActive(false);
        }

        // ══════════════════════════════════════════════════════════════════════
        // CRÉDITOS
        // ══════════════════════════════════════════════════════════════════════

        static void BuildCredits(Transform root, CreditsUI ui)
        {
            var go = UIKit.Stretch(root, "CreditsPanel").gameObject;
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.03f, 0.08f, 0.97f);
            bg.raycastTarget = true;
            var safe = UIKit.Stretch(go.transform, "Content");
            safe.gameObject.AddComponent<SafeAreaFitter>();
            var p = UIKit.Column(safe);

            UIKit.Text(p, "Title", "@credits.title", 56, new Vector4(0.02f, 0.89f, 0.6f, 0.98f), TextAlignmentOptions.Left, UIStyle.Gold, FontStyles.Bold);
            ui.CloseButton = UIKit.Button(p, "Back", "@camp.back", new Vector4(0.82f, 0.895f, 0.98f, 0.975f), UIStyle.ButtonIdle, UIStyle.Text, 28);

            var content = UIKit.Scroll(p, "Scroll", new Vector4(0.02f, 0.02f, 0.98f, 0.88f));
            var body = UIKit.Text(content, "Body", "@credits.body", 26, new Vector4(0f, 1f, 1f, 1f), TextAlignmentOptions.TopLeft, UIStyle.Text);
            body.enableAutoSizing = false;       // grows with the text inside the scroll view
            body.overflowMode = TextOverflowModes.Overflow;

            ui.Panel = go;
            ui.OpenButton = _creditsButton;
            go.transform.SetAsLastSibling();
            go.SetActive(false);
        }

        // ══════════════════════════════════════════════════════════════════════
        // MODO PROFESOR: panel de la clase, pregunta del alumno, avisos
        // ══════════════════════════════════════════════════════════════════════

        static void BuildClassroom(Transform root, ClassroomUI ui)
        {
            // ── Teacher dashboard ─────────────────────────────────────────────
            var go = UIKit.Stretch(root, "ClassroomPanel").gameObject;
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.03f, 0.08f, 0.97f);
            bg.raycastTarget = true;
            var safe = UIKit.Stretch(go.transform, "Content");
            safe.gameObject.AddComponent<SafeAreaFitter>();
            var p = UIKit.Column(safe);

            ui.ClassTitle = UIKit.Text(p, "Title", "", 46, new Vector4(0.02f, 0.89f, 0.6f, 0.98f), TextAlignmentOptions.Left, UIStyle.Gold, FontStyles.Bold);
            ui.CloseTeacherButton = UIKit.Button(p, "Close", "@class.close", new Vector4(0.8f, 0.895f, 0.98f, 0.975f), UIStyle.ButtonIdle, UIStyle.Text, 24, FontStyles.Bold);

            var qrBox = UIKit.Rect(p, "QRBox", new Vector4(0.02f, 0.5f, 0.24f, 0.87f));
            var qrBg = UIKit.Image(qrBox, "QRBackground", Vector4.zero, Color.white);
            UIKit.Fill(qrBg.rectTransform);
            qrBg.gameObject.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            ui.ClassQR = UIKit.Raw(qrBg.rectTransform, "QR", new Vector4(0.04f, 0.04f, 0.96f, 0.96f));
            UIKit.Text(p, "QRHint", "@class.qrhint", 24, new Vector4(0.26f, 0.72f, 0.56f, 0.87f), TextAlignmentOptions.TopLeft, UIStyle.Muted);
            ui.WeaponsButton = UIKit.Button(p, "Weapons", "", new Vector4(0.26f, 0.57f, 0.56f, 0.69f), new Color(0.5f, 0.2f, 0.2f, 1f), UIStyle.Text, 26, FontStyles.Bold);
            ui.WeaponsLabel = ui.WeaponsButton.GetComponentInChildren<TMP_Text>();

            var qCard = UIKit.Card(p, "Question", new Vector4(0.02f, 0.2f, 0.56f, 0.47f), UIStyle.Card);
            ui.QuestionLabel = UIKit.Text(qCard, "Text", "", 28, new Vector4(0.04f, 0.25f, 0.96f, 0.95f), TextAlignmentOptions.TopLeft, UIStyle.Text);
            ui.QuestionStats = UIKit.Text(qCard, "Stats", "", 26, new Vector4(0.04f, 0.04f, 0.96f, 0.24f), TextAlignmentOptions.Left, UIStyle.Accent, FontStyles.Bold);

            ui.AskButton = UIKit.Button(p, "Ask", "", new Vector4(0.02f, 0.105f, 0.56f, 0.185f), UIStyle.Accent, UIStyle.Ink, 32, FontStyles.Bold);
            ui.AskLabel  = ui.AskButton.GetComponentInChildren<TMP_Text>();
            ui.PassCorrectButton = UIKit.Button(p, "PassRight", "@class.passright", new Vector4(0.02f, 0.02f, 0.3f, 0.09f), UIStyle.Good, UIStyle.Ink, 24, FontStyles.Bold);
            ui.PassAllButton     = UIKit.Button(p, "PassAll", "@class.passall", new Vector4(0.31f, 0.02f, 0.56f, 0.09f), UIStyle.ButtonIdle, UIStyle.Text, 24, FontStyles.Bold);

            ui.StudentList = UIKit.Scroll(p, "Students", new Vector4(0.58f, 0.02f, 0.99f, 0.87f));

            ui.TeacherPanel = go;
            ui.OpenTeacherButton = _teacherButton;
            go.SetActive(false);

            // ── Student quiz ──────────────────────────────────────────────────
            var quiz = UIKit.Stretch(root, "QuizPanel").gameObject;
            var dim = quiz.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.75f);
            dim.raycastTarget = true;
            var card = UIKit.Card(UIKit.Column(quiz.transform), "Card", new Vector4(0.12f, 0.05f, 0.88f, 0.95f), UIStyle.Card);
            ui.QuizFeedback = UIKit.Text(card, "From", "", 26, new Vector4(0.05f, 0.9f, 0.95f, 0.97f), TextAlignmentOptions.Left, UIStyle.Accent, FontStyles.Bold);
            ui.QuizQuestion = UIKit.Text(card, "Question", "", 40, new Vector4(0.05f, 0.62f, 0.95f, 0.89f), TextAlignmentOptions.TopLeft, UIStyle.Text, FontStyles.Bold);
            ui.QuizOptions = new Button[4];
            for (int i = 0; i < 4; i++)
            {
                int col = i % 2, row = i / 2;
                float y1 = 0.59f - row * 0.23f, x0 = 0.05f + col * 0.455f;
                var b = UIKit.Button(card, $"Option_{i}", "", new Vector4(x0, y1 - 0.21f, x0 + 0.445f, y1), UIStyle.ButtonIdle, UIStyle.Text, 28);
                var label = b.GetComponentInChildren<TMP_Text>();
                label.textWrappingMode = TextWrappingModes.Normal;
                ui.QuizOptions[i] = b;
            }
            ui.QuizCloseButton = UIKit.Button(card, "OK", "@class.close.quiz", new Vector4(0.35f, 0.03f, 0.65f, 0.12f), UIStyle.Accent, UIStyle.Ink, 30, FontStyles.Bold);
            ui.QuizPanel = quiz;
            quiz.SetActive(false);

            // ── Toast (always on top) ─────────────────────────────────────────
            var box = UIKit.Image(root, "ClassToast", new Vector4(0.2f, 0.86f, 0.8f, 0.94f), new Color(0f, 0f, 0f, 0.75f));
            box.raycastTarget = false;
            UIKit.Rim(box, UIStyle.Gold);
            box.gameObject.AddComponent<UIPopIn>();
            ui.Toast = UIKit.Text(box.transform, "Text", "", 30, new Vector4(0.03f, 0f, 0.97f, 1f), TextAlignmentOptions.Center, UIStyle.Gold, FontStyles.Bold);
            ui.ToastBox = box.gameObject;
            var toast = box;
            box.gameObject.SetActive(false);

            go.transform.SetAsLastSibling();
            quiz.transform.SetAsLastSibling();
            toast.transform.SetAsLastSibling();
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
            var card = UIKit.Card(UIKit.Column(go.transform), "Card", new Vector4(0.12f, 0.05f, 0.88f, 0.95f), UIStyle.Card);

            UIKit.Text(card, "Tag", "@tut.tag", 26, new Vector4(0.05f, 0.91f, 0.7f, 0.97f), TextAlignmentOptions.Left, UIStyle.Accent, FontStyles.Bold);
            tut.CounterLabel = UIKit.Text(card, "Counter", "1 / 9", 26, new Vector4(0.7f, 0.91f, 0.95f, 0.97f), TextAlignmentOptions.Right, UIStyle.Muted);
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
                                          new Vector4(0.565f, 0.925f, 0.625f, 0.995f), UIStyle.ButtonIdle, UIStyle.Accent, 44, FontStyles.Bold);

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

        static GameObject BuildHUD(RectTransform safe, CockpitHUD hud, ARShipController ship)
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
            // Whole-screen flash on hits (red), shields (cyan) and pods; fades in ~0.3 s
            hud.HitFlash = UIKit.Image(p, "HitFlash", new Vector4(0, 0, 1, 1), new Color(1f, 1f, 1f, 0f));
            hud.HitFlash.sprite = vignette;
            hud.HitFlash.type   = UnityEngine.UI.Image.Type.Simple;

            // Landscape HUD (canvas 1080 units tall): information at the top and the upper corners,
            // the two sticks and the action buttons along the bottom, the middle free for the view.

            // Barra superior: posición · distancia · tiempo (centred, fixed width)
            var top = UIKit.Pin(p, "TopBar", new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(1000f, 74f));
            var topImg = top.gameObject.AddComponent<Image>();
            topImg.color = UIStyle.HudCard;
            topImg.sprite = UIKit.FrameSprite;
            topImg.type = UnityEngine.UI.Image.Type.Sliced;
            UIKit.Rim(topImg);
            var topGlitch = top.gameObject.AddComponent<UIGlitch>();
            hud.RacePositionLabel = UIKit.Text(top, "Position", "#1", 46, new Vector4(0.03f, 0f, 0.2f, 1f),
                                               TextAlignmentOptions.Left, UIStyle.Gold, FontStyles.Bold);
            hud.DistanceLabel = UIKit.Text(top, "Distance", "", 32, new Vector4(0.2f, 0f, 0.72f, 1f),
                                           TextAlignmentOptions.Center, UIStyle.Text, FontStyles.Bold);
            hud.TimeLabel = UIKit.Text(top, "Time", "", 32, new Vector4(0.72f, 0f, 0.97f, 1f),
                                       TextAlignmentOptions.Right, UIStyle.Text);
            var progress = UIKit.Pin(p, "Progress", new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(1000f, 8f));
            hud.ProgressBar = UIKit.Bar(progress, "Bar", new Vector4(0f, 0f, 1f, 1f), UIStyle.Accent);
            var comm = UIKit.Pin(p, "CommDelay", new Vector2(0.5f, 1f), new Vector2(0f, -102f), new Vector2(1000f, 34f));
            hud.CommDelayLabel = UIKit.Text(comm, "Text", "", 22, new Vector4(0f, 0f, 1f, 1f),
                                            TextAlignmentOptions.Center, UIStyle.Muted);

            // Flecha al planeta: gira alrededor del centro de la pantalla
            hud.ArrowToPlanet = UIKit.Image(p, "ArrowToPlanet", new Vector4(0.5f, 0.5f, 0.5f, 0.5f), UIStyle.Accent);
            hud.ArrowToPlanet.rectTransform.sizeDelta = new Vector2(260, 260);
            hud.ArrowToPlanet.sprite = ArrowSprite();
            hud.ArrowToPlanet.type   = UnityEngine.UI.Image.Type.Simple;

            // Avisos centrales
            hud.BurnoutModeIndicator = UIKit.Text(p, "BurnoutMode", "¡BURNOUT!", 100, new Vector4(0.2f, 0.64f, 0.8f, 0.77f),
                                                  TextAlignmentOptions.Center, UIStyle.Burnout, FontStyles.Bold | FontStyles.Italic).gameObject;
            hud.BurnoutModeIndicator.SetActive(false);
            hud.NearMissComboLabel = UIKit.Text(p, "NearMiss", "NEAR MISS", 52, new Vector4(0.2f, 0.575f, 0.8f, 0.64f),
                                                TextAlignmentOptions.Center, UIStyle.Gold, FontStyles.Bold);
            hud.NearMissComboObj = hud.NearMissComboLabel.gameObject;
            hud.NearMissComboObj.SetActive(false);
            hud.LowFuelWarning = UIKit.Text(p, "LowFuel", "LOW FUEL", 42, new Vector4(0.25f, 0.27f, 0.75f, 0.32f),
                                            TextAlignmentOptions.Center, UIStyle.Warn, FontStyles.Bold).gameObject;
            hud.LowFuelWarning.AddComponent<UIPulse>().Speed = 8f;
            hud.LowFuelWarning.SetActive(false);

            // Alert banner: dark plate with a coloured glowing frame, pops in with a bounce
            var banner = UIKit.Image(p, "ImpactBanner", new Vector4(0.22f, 0.45f, 0.78f, 0.55f), new Color(0.02f, 0.03f, 0.08f, 0.75f));
            hud.ImpactRim    = UIKit.Rim(banner, UIStyle.Warn);
            hud.ImpactBanner = banner.gameObject;
            banner.gameObject.AddComponent<UIPopIn>();
            var bannerGlitch = banner.gameObject.AddComponent<UIGlitch>();
            hud.ImpactLabel = UIKit.Text(banner.rectTransform, "Impact", "", 42, new Vector4(0.03f, 0.05f, 0.97f, 0.95f),
                                         TextAlignmentOptions.Center, UIStyle.Warn, FontStyles.Bold);

            // Ship status, top-left corner: burnout + hull, speed and fuel
            var stats = UIKit.Pin(p, "ShipStatus", new Vector2(0f, 1f), new Vector2(24f, -12f), new Vector2(440f, 176f));
            // Display parts jolt on hits (not the touch controls: a moving joystick would jerk the aim)
            hud.Glitch = new[] { topGlitch, bannerGlitch, stats.gameObject.AddComponent<UIGlitch>() };

            UIKit.Text(stats, "BurnoutTitle", "BURNOUT", 20, new Vector4(0f, 0.86f, 0.5f, 1f),
                       TextAlignmentOptions.Left, UIStyle.Burnout, FontStyles.Bold);
            hud.LivesLabel = UIKit.Text(stats, "Lives", "HULL 3/3", 20, new Vector4(0.5f, 0.86f, 1f, 1f),
                                        TextAlignmentOptions.Right, UIStyle.Text, FontStyles.Bold);
            hud.BurnoutBar = UIKit.Bar(stats, "BurnoutBar", new Vector4(0f, 0.78f, 1f, 0.83f), UIStyle.Burnout);

            // Velocidad y combustible: compact cards
            var speed = UIKit.Card(stats, "SpeedCard", new Vector4(0f, 0f, 0.49f, 0.72f), UIStyle.HudCard);
            UIKit.Text(speed, "Title", "@hud.speed", 18, new Vector4(0.08f, 0.68f, 0.92f, 0.94f), TextAlignmentOptions.Left, UIStyle.Muted, FontStyles.Bold);
            hud.SpeedLabel = UIKit.Text(speed, "Value", "0 m/s", 40, new Vector4(0.08f, 0.26f, 0.92f, 0.7f),
                                        TextAlignmentOptions.Left, UIStyle.Text, FontStyles.Bold);
            hud.SpeedGaugeFill = UIKit.Bar(speed, "Gauge", new Vector4(0.08f, 0.1f, 0.92f, 0.18f), UIStyle.Accent);

            var fuel = UIKit.Card(stats, "FuelCard", new Vector4(0.51f, 0f, 1f, 0.72f), UIStyle.HudCard);
            UIKit.Text(fuel, "Title", "@hud.fuel", 18, new Vector4(0.08f, 0.68f, 0.92f, 0.94f), TextAlignmentOptions.Left, UIStyle.Muted, FontStyles.Bold);
            hud.FuelLabel = UIKit.Text(fuel, "Value", "100%", 40, new Vector4(0.08f, 0.26f, 0.92f, 0.7f),
                                       TextAlignmentOptions.Left, UIStyle.Text, FontStyles.Bold);
            hud.FuelBar = UIKit.Bar(fuel, "Bar", new Vector4(0.08f, 0.1f, 0.92f, 0.18f), UIStyle.Good);

            // How to fly, a quiet line at the bottom centre
            var hint = UIKit.Pin(p, "ControlsHint", new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(760f, 36f));
            UIKit.Text(hint, "Text", "@hud.controls", 18, new Vector4(0f, 0f, 1f, 1f), TextAlignmentOptions.Center, UIStyle.Muted);

            foreach (var g in go.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;

            // Touch controls come after the loop: they are the only HUD parts that take taps
            BuildTouchControls(p, hud, ship);
            return go;
        }

        // Twin-stick layout, like mobile shooters: LEFT stick moves, RIGHT stick looks around;
        // round action buttons around the right thumb (FIRE the biggest, closest to it)
        static void BuildTouchControls(Transform p, CockpitHUD hud, ARShipController ship)
        {
            var move = Stick(p, "Joystick", new Vector2(0f, 0f), new Vector2(20f, 20f), "@hud.move",
                             new Color(0.4f, 0.85f, 1f, 0.35f), new Color(0.4f, 0.9f, 1f, 0.95f));
            var look = Stick(p, "LookStick", new Vector2(1f, 0f), new Vector2(-20f, 20f), "@hud.look",
                             new Color(1f, 0.78f, 0.35f, 0.35f), new Color(1f, 0.82f, 0.4f, 0.95f));

            // FIRE: big, to the left of the look stick, where the thumb slides to
            var fire = RoundButton(p, "FireButton", "@hud.fire", new Vector2(-440f, 50f), 200f,
                                   new Color(1f, 0.5f, 0.15f, 0.95f), 30);
            hud.FireButton = fire;
            // The laser recharge runs around the button's edge
            hud.FireCooldown = UIKit.Image(fire.transform, "Charge", new Vector4(0f, 0f, 1f, 1f), new Color(1f, 0.92f, 0.55f, 0.95f));
            hud.FireCooldown.sprite = RoundButtonSprite(charge: true);
            hud.FireCooldown.type   = UnityEngine.UI.Image.Type.Filled;
            hud.FireCooldown.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
            hud.FireCooldown.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
            hud.FireCooldown.fillClockwise = true;
            hud.FireCooldown.raycastTarget = false;

            // BOOST (hold): above FIRE · SCAN: above the look stick
            var boost = RoundHoldButton(p, "BoostButton", "@hud.boost", new Vector2(-465f, 280f), 150f,
                                        new Color(1f, 0.22f, 0.3f, 0.95f), 24);
            var scan = RoundButton(p, "ScanButton", "@hud.scan", new Vector2(-150f, 440f), 136f,
                                   new Color(0.2f, 0.95f, 0.5f, 0.95f), 20);
            hud.ScanButton = scan;

            // Reticle that locks onto what the ship points at
            var ret = UIKit.Image(p, "Reticle", new Vector4(0.5f, 0.5f, 0.5f, 0.5f), new Color(1f, 1f, 1f, 0.35f));
            ret.sprite = ReticleSprite();
            ret.type   = UnityEngine.UI.Image.Type.Simple;
            ret.rectTransform.sizeDelta = new Vector2(110f, 110f);
            ret.raycastTarget = false;
            hud.Reticle = ret.rectTransform;
            hud.ReticleLabel = UIKit.Text(ret.rectTransform, "Name", "", 24, Vector4.zero, TextAlignmentOptions.Center, UIStyle.Gold, FontStyles.Bold);
            var rl = hud.ReticleLabel.rectTransform;
            rl.anchorMin = rl.anchorMax = new Vector2(0.5f, 0f);
            rl.pivot = new Vector2(0.5f, 1f);
            rl.sizeDelta = new Vector2(420f, 36f);
            rl.anchoredPosition = new Vector2(0f, -4f);

            // "What is that?" card: top-right, under the gyro button
            var info = UIKit.Card(p, "ScanInfo", new Vector4(1f, 1f, 1f, 1f), new Color(0.04f, 0.08f, 0.16f, 0.85f));
            info.anchorMin = info.anchorMax = info.pivot = new Vector2(1f, 1f);
            info.sizeDelta = new Vector2(600f, 260f);
            info.anchoredPosition = new Vector2(-24f, -86f);
            info.GetComponent<Image>().raycastTarget = false;
            hud.InfoCard  = info.gameObject;
            info.gameObject.AddComponent<UIPopIn>().From = 0.85f;
            hud.InfoTitle = UIKit.Text(info, "Title", "", 32, new Vector4(0.04f, 0.74f, 0.96f, 0.96f), TextAlignmentOptions.Left, UIStyle.Accent, FontStyles.Bold);
            hud.InfoBody  = UIKit.Text(info, "Body", "", 24, new Vector4(0.04f, 0.05f, 0.96f, 0.74f), TextAlignmentOptions.TopLeft, UIStyle.Text);

            // Free flight only: back to the lobby (top-left, under the ship status)
            hud.ExitButton = UIKit.Button(p, "ExitButton", "@free.exit", new Vector4(0f, 1f, 0f, 1f),
                                          new Color(0.12f, 0.16f, 0.27f, 0.8f), UIStyle.Text, 20, FontStyles.Bold);
            var exitRt = (RectTransform)hud.ExitButton.transform;
            exitRt.pivot = new Vector2(0f, 1f);
            exitRt.sizeDelta = new Vector2(170f, 56f);
            exitRt.anchoredPosition = new Vector2(24f, -200f);

            hud.GyroButton = UIKit.Button(p, "GyroButton", "GYRO", new Vector4(1f, 1f, 1f, 1f),
                                          new Color(0.12f, 0.16f, 0.27f, 0.8f), UIStyle.Accent, 20, FontStyles.Bold);
            var gyroRt = (RectTransform)hud.GyroButton.transform;
            gyroRt.pivot = Vector2.one;
            gyroRt.sizeDelta = new Vector2(170f, 56f);
            gyroRt.anchoredPosition = new Vector2(-24f, -12f);
            hud.GyroLabel = hud.GyroButton.GetComponentInChildren<TMP_Text>();

            if (ship != null)
            {
                ship.Stick        = move;
                ship.LookStick    = look;
                ship.ThrustButton = null;   // the left stick is the throttle now
                ship.BoostButton  = boost;
                EditorUtility.SetDirty(ship);
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // MUNDOS: descenso y misión en la superficie (docs/mundos/)
        // Same layout as the flight HUD: sticks and round buttons A/B/C, meters top-left
        // ══════════════════════════════════════════════════════════════════════

        static void BuildWorldHUD(Transform root, WorldHUD w)
        {
            var go = UIKit.Stretch(root, "WorldPanel").gameObject;
            var safe = UIKit.Stretch(go.transform, "Content");
            safe.gameObject.AddComponent<SafeAreaFitter>();
            var p = safe.transform;
            w.Panel = go;

            w.FlashImage = UIKit.Image(go.transform, "Flash", new Vector4(0, 0, 1, 1), new Color(1f, 1f, 1f, 0f));
            w.FlashImage.sprite = VignetteSprite();
            w.FlashImage.type = UnityEngine.UI.Image.Type.Simple;
            w.FlashImage.transform.SetAsFirstSibling();

            // Title and current goal (top centre)
            var title = UIKit.Pin(p, "Title", new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(1000f, 52f));
            w.Title = UIKit.Text(title, "Text", "", 34, new Vector4(0, 0, 1, 1), TextAlignmentOptions.Center, UIStyle.Accent, FontStyles.Bold);
            var goal = UIKit.Pin(p, "Goal", new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(1000f, 40f));
            w.Objective = UIKit.Text(goal, "Text", "", 24, new Vector4(0, 0, 1, 1), TextAlignmentOptions.Center, UIStyle.Text);

            // Data sent (under the goal)
            var data = UIKit.Pin(p, "Data", new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(520f, 40f));
            w.DataBox = data.gameObject;
            w.DataFill = UIKit.Bar(data, "Bar", new Vector4(0f, 0f, 1f, 0.35f), UIStyle.Accent);
            w.DataLabel = UIKit.Text(data, "Label", "", 18, new Vector4(0f, 0.4f, 1f, 1f), TextAlignmentOptions.Center, UIStyle.Muted);

            // Three meters, top-left
            var meters = UIKit.Card(p, "Meters", new Vector4(0f, 1f, 0f, 1f), UIStyle.HudCard);
            meters.anchorMin = meters.anchorMax = meters.pivot = new Vector2(0f, 1f);
            meters.sizeDelta = new Vector2(430f, 180f);
            meters.anchoredPosition = new Vector2(24f, -12f);
            w.Meters = new Meter[3];
            for (int i = 0; i < 3; i++)
            {
                float y1 = 0.96f - i * 0.32f, y0 = y1 - 0.3f;
                var row = UIKit.Rect(meters, "Meter" + i, new Vector4(0.05f, y0, 0.95f, y1));
                var m = new Meter { Root = row.gameObject };
                m.Label = UIKit.Text(row, "Label", "", 18, new Vector4(0f, 0.45f, 0.5f, 1f), TextAlignmentOptions.Left, UIStyle.Muted, FontStyles.Bold);
                m.Value = UIKit.Text(row, "Value", "", 24, new Vector4(0.45f, 0.4f, 1f, 1f), TextAlignmentOptions.Right, UIStyle.Text, FontStyles.Bold);
                m.Fill  = UIKit.Bar(row, "Bar", new Vector4(0f, 0.05f, 1f, 0.3f), UIStyle.Accent);
                w.Meters[i] = m;
            }

            // Checklist of the mission, top-right
            var check = UIKit.Pin(p, "Checklist", new Vector2(1f, 1f), new Vector2(-24f, -86f), new Vector2(380f, 170f));
            w.Checklist = UIKit.Text(check, "Text", "", 20, new Vector4(0, 0, 1, 1), TextAlignmentOptions.TopRight, UIStyle.Text);

            // Hint above the buttons, and the big banner in the middle
            var prompt = UIKit.Pin(p, "Prompt", new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(860f, 70f));
            w.Prompt = UIKit.Text(prompt, "Text", "", 24, new Vector4(0, 0, 1, 1), TextAlignmentOptions.Center, UIStyle.Text, FontStyles.Bold);
            var banner = UIKit.Image(p, "Banner", new Vector4(0.2f, 0.6f, 0.8f, 0.72f), new Color(0.02f, 0.03f, 0.08f, 0.78f));
            UIKit.Rim(banner, UIStyle.Accent);
            banner.gameObject.AddComponent<UIPopIn>();
            w.BannerBox = banner.gameObject;
            w.Banner = UIKit.Text(banner.rectTransform, "Text", "", 38, new Vector4(0.03f, 0.05f, 0.97f, 0.95f), TextAlignmentOptions.Center, UIStyle.Accent, FontStyles.Bold);

            // Arrow to the next objective, with its name and distance
            var arrow = UIKit.Image(p, "Arrow", new Vector4(0.5f, 0.5f, 0.5f, 0.5f), UIStyle.Gold);
            arrow.sprite = ArrowSprite();
            arrow.type = UnityEngine.UI.Image.Type.Simple;
            arrow.rectTransform.sizeDelta = new Vector2(90f, 90f);
            w.Arrow = arrow.rectTransform;
            w.ArrowLabel = UIKit.Text(arrow.rectTransform, "Label", "", 20, new Vector4(0.5f, 0f, 0.5f, 0f), TextAlignmentOptions.Center, UIStyle.Gold, FontStyles.Bold);
            w.ArrowLabel.rectTransform.sizeDelta = new Vector2(360f, 34f);
            w.ArrowLabel.rectTransform.anchoredPosition = new Vector2(0f, -30f);

            // Timing bar: press when the needle is in the green
            var win = UIKit.Pin(p, "Window", new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(720f, 90f));
            w.WindowBox = win.gameObject;
            w.WindowLabel = UIKit.Text(win, "Label", "", 26, new Vector4(0f, 0.55f, 1f, 1f), TextAlignmentOptions.Center, UIStyle.Text, FontStyles.Bold);
            var track = UIKit.Image(win, "Track", new Vector4(0f, 0.05f, 1f, 0.45f), new Color(1f, 1f, 1f, 0.15f));
            var zone = UIKit.Image(track.rectTransform, "Zone", new Vector4(0.3f, 0f, 0.7f, 1f), new Color(UIStyle.Good.r, UIStyle.Good.g, UIStyle.Good.b, 0.75f));
            w.WindowZone = zone.rectTransform;
            var needle = UIKit.Image(track.rectTransform, "Needle", new Vector4(0.5f, 0.5f, 0.5f, 0.5f), Color.white);
            needle.sprite = null;
            needle.rectTransform.sizeDelta = new Vector2(8f, 58f);
            w.WindowNeedle = needle.rectTransform;

            // Entry: keep the dot inside the circle; heat bar under it
            var entry = UIKit.Pin(p, "Entry", new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(360f, 420f));
            w.EntryBox = entry.gameObject;
            var circle = UIKit.Image(entry, "Circle", new Vector4(0.5f, 0.57f, 0.5f, 0.57f), new Color(UIStyle.Good.r, UIStyle.Good.g, UIStyle.Good.b, 0.7f));
            circle.sprite = UIKit.RingSprite;
            circle.type = UnityEngine.UI.Image.Type.Simple;
            circle.rectTransform.sizeDelta = new Vector2(150f, 150f);
            var outer = UIKit.Image(entry, "Outer", new Vector4(0.5f, 0.57f, 0.5f, 0.57f), new Color(1f, 1f, 1f, 0.2f));
            outer.sprite = UIKit.RingSprite;
            outer.type = UnityEngine.UI.Image.Type.Simple;
            outer.rectTransform.sizeDelta = new Vector2(320f, 320f);
            var dot = UIKit.Image(outer.rectTransform, "Dot", new Vector4(0.5f, 0.5f, 0.5f, 0.5f), UIStyle.Gold);
            dot.sprite = UIKit.StickSprite;
            dot.type = UnityEngine.UI.Image.Type.Simple;
            dot.rectTransform.sizeDelta = new Vector2(46f, 46f);
            w.EntryDot = dot.rectTransform;
            w.EntryHeat = UIKit.Bar(entry, "Heat", new Vector4(0.1f, 0.02f, 0.9f, 0.07f), UIStyle.Warn);
            w.EntryLabel = UIKit.Text(entry, "Label", "@w.entry.heat", 18, new Vector4(0f, 0.08f, 1f, 0.15f), TextAlignmentOptions.Center, UIStyle.Muted);

            // Touchdown: vertical, sideways, tilt
            var land = UIKit.Pin(p, "Landing", new Vector2(0.5f, 0f), new Vector2(0f, 190f), new Vector2(760f, 44f));
            w.LandingBox = land.gameObject;
            var landBg = UIKit.Image(land, "Bg", new Vector4(0, 0, 1, 1), UIStyle.HudCard);
            landBg.raycastTarget = false;
            w.LandV    = UIKit.Text(land, "V", "", 22, new Vector4(0.02f, 0f, 0.34f, 1f), TextAlignmentOptions.Center, UIStyle.Good, FontStyles.Bold);
            w.LandH    = UIKit.Text(land, "H", "", 22, new Vector4(0.34f, 0f, 0.66f, 1f), TextAlignmentOptions.Center, UIStyle.Good, FontStyles.Bold);
            w.LandTilt = UIKit.Text(land, "Tilt", "", 22, new Vector4(0.66f, 0f, 0.98f, 1f), TextAlignmentOptions.Center, UIStyle.Good, FontStyles.Bold);

            foreach (var g in go.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;

            // Controls (the only parts that take taps)
            w.MoveStick = Stick(p, "WorldMove", new Vector2(0f, 0f), new Vector2(20f, 20f), "@hud.move",
                                new Color(0.4f, 0.85f, 1f, 0.35f), new Color(0.4f, 0.9f, 1f, 0.95f));
            w.LookStick = Stick(p, "WorldLook", new Vector2(1f, 0f), new Vector2(-20f, 20f), "@hud.look",
                                new Color(1f, 0.78f, 0.35f, 0.35f), new Color(1f, 0.82f, 0.4f, 0.95f));
            w.BtnA = WorldButton(p, "WorldA", new Vector2(-440f, 50f), 200f, new Color(1f, 0.5f, 0.15f, 0.95f), 28);
            w.BtnB = WorldButton(p, "WorldB", new Vector2(-465f, 280f), 150f, new Color(0.25f, 0.55f, 1f, 0.95f), 22);
            w.BtnC = WorldButton(p, "WorldC", new Vector2(-150f, 440f), 136f, new Color(0.2f, 0.95f, 0.5f, 0.95f), 20);

            w.ExitButton = UIKit.Button(p, "Exit", "@w.exit", new Vector4(0f, 1f, 0f, 1f),
                                        new Color(0.12f, 0.16f, 0.27f, 0.8f), UIStyle.Text, 20, FontStyles.Bold);
            var exitRt = (RectTransform)w.ExitButton.transform;
            exitRt.pivot = new Vector2(0f, 1f);
            exitRt.sizeDelta = new Vector2(170f, 56f);
            exitRt.anchoredPosition = new Vector2(24f, -206f);

            // Result card
            var result = UIKit.Card(p, "Result", new Vector4(0.24f, 0.08f, 0.76f, 0.92f), UIStyle.Card);
            result.GetComponent<Image>().raycastTarget = true;
            result.gameObject.AddComponent<UIPopIn>().From = 0.85f;
            w.ResultPanel = result.gameObject;
            w.ResultTitle = UIKit.Text(result, "Title", "", 40, new Vector4(0.05f, 0.85f, 0.95f, 0.97f), TextAlignmentOptions.Center, UIStyle.Good, FontStyles.Bold);
            w.ResultStars = UIKit.Text(result, "Stars", "", 60, new Vector4(0.05f, 0.72f, 0.95f, 0.85f), TextAlignmentOptions.Center, UIStyle.Gold, FontStyles.Bold);
            w.ResultBody  = UIKit.Text(result, "Body", "", 22, new Vector4(0.06f, 0.22f, 0.94f, 0.72f), TextAlignmentOptions.TopLeft, UIStyle.Text);
            w.RetryButton    = UIKit.Button(result, "Retry", "@w.retry", new Vector4(0.05f, 0.04f, 0.47f, 0.17f), UIStyle.ButtonIdle, UIStyle.Text, 30, FontStyles.Bold);
            w.ContinueButton = UIKit.Button(result, "Continue", "@w.continue", new Vector4(0.53f, 0.04f, 0.95f, 0.17f), UIStyle.Accent, UIStyle.Ink, 30, FontStyles.Bold);

            go.transform.SetAsLastSibling();
            go.SetActive(false);
        }

        // Round hold button for the worlds, with a radial ring for "hold to…" actions
        static ActionButton WorldButton(Transform p, string name, Vector2 pos, float size, Color color, float fontSize)
        {
            var hold = RoundHoldButton(p, name, "", pos, size, color, fontSize);
            var img = hold.GetComponent<Image>();
            var ring = UIKit.Image(img.transform, "Ring", new Vector4(0f, 0f, 1f, 1f), new Color(1f, 0.95f, 0.6f, 0.95f));
            ring.sprite = RoundButtonSprite(charge: true);
            ring.type = UnityEngine.UI.Image.Type.Filled;
            ring.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
            ring.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
            ring.fillAmount = 0f;
            ring.raycastTarget = false;
            var label = img.GetComponentInChildren<TMP_Text>();
            var lz = label.GetComponent<LocalizedText>();
            if (lz != null) Object.DestroyImmediate(lz);   // the label changes with the phase
            return new ActionButton { Root = img.gameObject, Hold = hold, Face = img, Label = label, Fill = ring };
        }

        // Invisible touch zone in a bottom corner with a floating stick that appears under the thumb
        static VirtualJoystick Stick(Transform p, string name, Vector2 corner, Vector2 pos, string label, Color idle, Color active)
        {
            var area = UIKit.Pin(p, name, corner, pos, new Vector2(400f, 400f));
            var zone = area.gameObject.AddComponent<Image>();
            zone.color = new Color(1f, 1f, 1f, 0f);   // invisible but still catches touches
            zone.raycastTarget = true;
            var ring = UIKit.Image(area, "Base", new Vector4(0.5f, 0.5f, 0.5f, 0.5f), idle);
            ring.sprite = UIKit.RingSprite;    // ring with ticks and arrows, turns toward the push
            ring.type   = UnityEngine.UI.Image.Type.Simple;
            ring.rectTransform.sizeDelta = new Vector2(240, 240);
            ring.raycastTarget = false;
            var knob = UIKit.Image(ring.rectTransform, "Knob", new Vector4(0.5f, 0.5f, 0.5f, 0.5f), idle);
            knob.sprite = UIKit.StickSprite;   // glowing disc
            knob.type   = UnityEngine.UI.Image.Type.Simple;
            knob.rectTransform.sizeDelta = new Vector2(104, 104);
            knob.raycastTarget = false;
            var tag = UIKit.Text(area, "Label", label, 18, new Vector4(0.2f, 0.02f, 0.8f, 0.1f), TextAlignmentOptions.Center,
                                 new Color(idle.r, idle.g, idle.b, 0.8f), FontStyles.Bold);
            tag.raycastTarget = false;
            var stick = area.gameObject.AddComponent<VirtualJoystick>();
            stick.Base  = ring.rectTransform;
            stick.Knob  = knob.rectTransform;
            stick.Range = 95f;
            stick.BaseGraphic = ring;
            stick.KnobGraphic = knob;
            stick.IdleColor   = idle;
            stick.ActiveColor = active;
            return stick;
        }

        // Round game button pinned from the bottom-right corner (pos = its bottom-right corner)
        static Button RoundButton(Transform p, string name, string label, Vector2 pos, float size, Color color, float fontSize)
        {
            var b = UIKit.Button(p, name, label, new Vector4(1f, 0f, 1f, 0f), color, UIStyle.Text, fontSize, FontStyles.Bold);
            PinButton(b, pos, new Vector2(size, size));
            Round(b.image);
            return b;
        }

        static HoldButton RoundHoldButton(Transform p, string name, string label, Vector2 pos, float size, Color color, float fontSize)
        {
            var img = UIKit.PinImage(p, name, new Vector2(1f, 0f), pos, new Vector2(size, size), color);
            img.raycastTarget = true;
            UIKit.Text(img.rectTransform, "Label", label, fontSize, new Vector4(0.1f, 0.3f, 0.9f, 0.7f),
                       TextAlignmentOptions.Center, UIStyle.Text, FontStyles.Bold);
            Round(img);
            var hold = img.gameObject.AddComponent<HoldButton>();
            hold.Target = img;
            img.gameObject.AddComponent<UIPress>().Pressed = 0.92f;   // stays squashed while held
            return hold;
        }

        // Glowing disc with a bright rim and an inner ring; white, so the Image colour tints it
        static void Round(Image img)
        {
            var rim = img.transform.Find("Rim");
            if (rim != null) Object.DestroyImmediate(rim.gameObject);
            img.sprite = RoundButtonSprite(charge: false);
            img.type   = UnityEngine.UI.Image.Type.Simple;
            img.preserveAspect = true;
            var label = img.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                var rt = label.rectTransform;
                rt.anchorMin = new Vector2(0.1f, 0.3f); rt.anchorMax = new Vector2(0.9f, 0.7f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                label.fontStyle = FontStyles.Bold | FontStyles.Italic;
                label.enableAutoSizing = true;
                label.fontSizeMin = 12;
            }
        }

        // charge = just the outer band (the fire button's recharge ring, drawn as a radial fill)
        static Sprite RoundButtonSprite(bool charge)
        {
            string path = charge ? "Assets/UI/Generated/RoundCharge.png" : "Assets/UI/Generated/RoundButton.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;
            const int N = 256;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v);
                float a;
                if (charge)
                    a = Mathf.Clamp01(1f - Mathf.Abs(r - 0.93f) / 0.035f);   // thin band on the rim
                else
                {
                    float edge  = Mathf.Clamp01(1f - Mathf.Abs(r - 0.93f) / 0.03f);         // bright rim
                    float inner = Mathf.Clamp01(1f - Mathf.Abs(r - 0.78f) / 0.012f) * 0.55f; // thin inner ring
                    float fill  = r < 0.9f ? 0.34f + 0.22f * Mathf.Pow(r / 0.9f, 3f) : 0f; // brighter toward the rim
                    float shine = r < 0.9f && v > 0.25f ? 0.12f * Mathf.Clamp01((v - 0.25f) * 2f) : 0f; // top highlight
                    a = Mathf.Max(edge, inner, fill + shine);
                }
                float aa = Mathf.Clamp01((0.97f - r) * N * 0.5f);   // anti-aliased outer edge
                px[y * N + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(a) * aa);
            }
            tex.SetPixels(px);
            tex.Apply();
            System.IO.Directory.CreateDirectory("Assets/UI/Generated");
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void PinButton(Button b, Vector2 pos, Vector2 size)
        {
            var rt = (RectTransform)b.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        // Game-style button: slanted chamfered plate, bright outline, inner glow and a segmented
        // gauge along the bottom. White, so each button is tinted by its Image colour.
        static void SciFi(Image img)
        {
            // The slanted plate carries its own outline: drop the rectangular frame
            var rim = img.transform.Find("Rim");
            if (rim != null) Object.DestroyImmediate(rim.gameObject);
            img.sprite = SciFiButtonSprite();
            img.type   = UnityEngine.UI.Image.Type.Simple;
            img.preserveAspect = false;
            // The label sits a little higher, above the gauge
            var label = img.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                var rt = label.rectTransform;
                rt.anchorMin = new Vector2(0.06f, 0.2f); rt.anchorMax = new Vector2(0.94f, 1f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                label.fontStyle = FontStyles.Bold | FontStyles.Italic;
            }
        }

        static Sprite SciFiButtonSprite()
        {
            const string path = "Assets/UI/Generated/SciFiButton.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;
            const int W = 288, H = 100;
            const float m = 4f, c = 22f, skew = 16f;   // margin, corner cut, slant
            // Convex outline, counter-clockwise: top-left and bottom-right corners cut, then slanted
            var pts = new[]
            {
                new Vector2(m, m), new Vector2(W - m - c, m), new Vector2(W - m, m + c),
                new Vector2(W - m, H - m), new Vector2(m + c, H - m), new Vector2(m, H - m - c),
            };
            for (int i = 0; i < pts.Length; i++) pts[i].x += skew * (pts[i].y / H - 0.5f);

            float Dist(Vector2 q)   // signed distance to the polygon (negative inside)
            {
                float d = float.MinValue;
                for (int i = 0; i < pts.Length; i++)
                {
                    Vector2 a = pts[i], b = pts[(i + 1) % pts.Length], e = (b - a).normalized;
                    Vector2 n = new Vector2(e.y, -e.x);   // outward normal for CCW
                    d = Mathf.Max(d, Vector2.Dot(q - a, n));
                }
                return d;
            }

            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            var px = new Color[W * H];
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                var q = new Vector2(x + 0.5f, y + 0.5f);
                float d = Dist(q);
                float edge    = Mathf.Clamp01(1f - Mathf.Abs(d + 1.5f) / 2f);       // 3 px outline
                float outside = Mathf.Clamp01(1f - (d + 0.5f));                      // anti-aliased shape
                float glow    = d < 0f ? 0.45f * Mathf.Exp(d / 7f) : 0f;             // brighter near the rim
                float fill    = d < 0f ? 0.30f : 0f;
                // Segmented gauge: 8 small slanted cells along the bottom-left
                float gx = x - (16f + skew * ((y + 0.5f) / H - 0.5f));
                bool cell = y >= 12 && y <= 19 && gx >= 0f && gx < 8 * 20f && (gx % 20f) < 13f;
                float a = Mathf.Max(edge, Mathf.Min(1f, fill + glow), cell && d < -3f ? 0.75f : 0f) * outside;
                px[y * W + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px);
            tex.Apply();
            System.IO.Directory.CreateDirectory("Assets/UI/Generated");
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // Thin ring with four ticks, generated once
        static Sprite ReticleSprite()
        {
            const string path = "Assets/UI/Generated/Reticle.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;
            const int N = 128;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v);
                float ring = Mathf.Clamp01(1f - Mathf.Abs(r - 0.78f) / 0.045f);
                bool tick = (Mathf.Abs(u) < 0.04f || Mathf.Abs(v) < 0.04f) && r > 0.82f && r < 0.98f;
                float a = Mathf.Max(ring, tick ? 1f : 0f);
                px[y * N + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px);
            tex.Apply();
            System.IO.Directory.CreateDirectory("Assets/UI/Generated");
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ══════════════════════════════════════════════════════════════════════
        // CIENCIA EN EL PLANETA
        // ══════════════════════════════════════════════════════════════════════

        static GameObject BuildScienceOps(Transform root, RectTransform safe, ScienceOpsUI ui)
        {
            var panel = UIKit.Panel(root, safe, "ScienceOpsPanel", UIStyle.Backdrop);
            var p = panel.content;
            ui.Panel = panel.go;

            ui.PlanetNameLabel = UIKit.Text(p, "Planet", "MARS", 80, new Vector4(0.02f, 0.87f, 0.49f, 0.98f),
                                            TextAlignmentOptions.Left, UIStyle.Accent, FontStyles.Bold);
            ui.ArrivalPositionLabel  = UIKit.Text(p, "Arrival", "", 32, new Vector4(0.02f, 0.81f, 0.27f, 0.87f), TextAlignmentOptions.Left, UIStyle.Gold, FontStyles.Bold);
            ui.MissionObjectiveLabel = UIKit.Text(p, "Objective", "", 28, new Vector4(0.27f, 0.81f, 0.49f, 0.87f), TextAlignmentOptions.Right, UIStyle.Text);
            ui.CommDelayLabel        = UIKit.Text(p, "CommDelay", "", 26, new Vector4(0.02f, 0.76f, 0.49f, 0.81f), TextAlignmentOptions.Left, UIStyle.Muted);

            var info = UIKit.Card(p, "PlanetInfo", new Vector4(0.02f, 0.6f, 0.49f, 0.75f), UIStyle.Card);
            ui.GravityLabel    = UIKit.Text(info, "Gravity",    "", 32, new Vector4(0.04f, 0.52f, 0.96f, 0.92f), TextAlignmentOptions.Left, UIStyle.Text);
            ui.AtmosphereLabel = UIKit.Text(info, "Atmosphere", "", 28, new Vector4(0.04f, 0.08f, 0.96f, 0.5f),  TextAlignmentOptions.Left, UIStyle.Muted);

            ui.DownlinkLabel = UIKit.Text(p, "Downlink", "", 26, new Vector4(0.02f, 0.54f, 0.49f, 0.59f), TextAlignmentOptions.Left, UIStyle.Muted);
            ui.WindowLabel   = UIKit.Text(p, "Window",   "", 34, new Vector4(0.02f, 0.48f, 0.49f, 0.54f), TextAlignmentOptions.Left, UIStyle.Text, FontStyles.Bold);

            var power = UIKit.Card(p, "Power", new Vector4(0.02f, 0.32f, 0.49f, 0.47f), UIStyle.Card);
            UIKit.Text(power, "Title", "@sci.power", 28, new Vector4(0.04f, 0.55f, 0.4f, 0.92f), TextAlignmentOptions.Left, UIStyle.Muted, FontStyles.Bold);
            ui.PowerLabel       = UIKit.Text(power, "Value",  "", 32, new Vector4(0.4f, 0.55f, 0.75f, 0.92f), TextAlignmentOptions.Center, UIStyle.Text);
            ui.PowerStatusLabel = UIKit.Text(power, "Status", "", 32, new Vector4(0.75f, 0.55f, 0.96f, 0.92f), TextAlignmentOptions.Right, UIStyle.Good, FontStyles.Bold);
            ui.PowerBar         = UIKit.Bar(power, "Bar", new Vector4(0.04f, 0.18f, 0.96f, 0.36f), UIStyle.Accent);

            UIKit.Text(p, "InstTitle", "@sci.pipeline", 28, new Vector4(0.52f, 0.92f, 0.98f, 0.98f),
                       TextAlignmentOptions.Left, UIStyle.Muted, FontStyles.Bold);
            var list = UIKit.Rect(p, "Instruments", new Vector4(0.52f, 0.2f, 0.98f, 0.91f));
            var vlg = list.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12;
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            vlg.childControlWidth  = true; vlg.childForceExpandWidth  = true;
            ui.InstrumentsContainer = list;
            ui.InstrumentItemPrefab = InstrumentItem();

            var score = UIKit.Card(p, "Score", new Vector4(0.02f, 0.03f, 0.49f, 0.3f), UIStyle.Card);
            ui.SciencePointsLabel = UIKit.Text(score, "Science", "", 32, new Vector4(0.04f, 0.62f, 0.5f, 0.92f), TextAlignmentOptions.Left, UIStyle.Accent, FontStyles.Bold);
            ui.TotalScoreLabel    = UIKit.Text(score, "Total",   "", 40, new Vector4(0.5f, 0.62f, 0.96f, 0.92f), TextAlignmentOptions.Right, UIStyle.Gold, FontStyles.Bold);
            ui.ScoreBar           = UIKit.Bar(score, "ScoreBar", new Vector4(0.04f, 0.46f, 0.96f, 0.56f), UIStyle.Gold);
            ui.FuelLabel          = UIKit.Text(score, "Fuel", "", 28, new Vector4(0.04f, 0.08f, 0.3f, 0.38f), TextAlignmentOptions.Left, UIStyle.Muted);
            ui.FuelBar            = UIKit.Bar(score, "FuelBar", new Vector4(0.32f, 0.17f, 0.96f, 0.29f), UIStyle.Good);

            ui.CompleteMissionBtn = UIKit.Button(p, "CompleteMission", "Operations in progress…",
                                                 new Vector4(0.52f, 0.03f, 0.98f, 0.17f),
                                                 UIStyle.Accent, UIStyle.Ink, 42, FontStyles.Bold);
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

            ui.WinnerLabel    = UIKit.Text(p, "Winner", "", 90, new Vector4(0.02f, 0.88f, 0.98f, 0.98f), TextAlignmentOptions.Center, UIStyle.Gold, FontStyles.Bold);
            ui.WinnerSubLabel = UIKit.Text(p, "WinnerSub", "", 30, new Vector4(0.02f, 0.83f, 0.98f, 0.88f), TextAlignmentOptions.Center, UIStyle.Text);

            var local = UIKit.Card(p, "Local", new Vector4(0.02f, 0.46f, 0.42f, 0.82f), UIStyle.Card);
            ui.LocalPlayerName   = UIKit.Text(local, "Name",  "You", 40, new Vector4(0.05f, 0.8f, 0.55f, 0.95f), TextAlignmentOptions.Left, UIStyle.Text, FontStyles.Bold);
            ui.LocalScoreLabel   = UIKit.Text(local, "Score", "0", 96, new Vector4(0.45f, 0.62f, 0.95f, 0.97f), TextAlignmentOptions.Right, UIStyle.Accent, FontStyles.Bold);
            ui.LocalArrivalLabel = UIKit.Text(local, "Arrival", "", 34, new Vector4(0.05f, 0.64f, 0.55f, 0.8f), TextAlignmentOptions.Left, UIStyle.Gold, FontStyles.Bold);
            ui.LocalScoreBar     = UIKit.Bar(local, "Bar", new Vector4(0.05f, 0.52f, 0.95f, 0.58f), UIStyle.Accent);
            ui.LocalScienceLabel  = UIKit.Text(local, "Science",   "", 32, new Vector4(0.05f, 0.33f, 0.95f, 0.47f), TextAlignmentOptions.Left, UIStyle.Text);
            ui.LocalFuelLabel     = UIKit.Text(local, "Fuel",      "", 32, new Vector4(0.05f, 0.18f, 0.95f, 0.32f), TextAlignmentOptions.Left, UIStyle.Text);
            ui.LocalTakedownLabel = UIKit.Text(local, "Takedowns", "", 32, new Vector4(0.05f, 0.03f, 0.95f, 0.17f), TextAlignmentOptions.Left, UIStyle.Text);

            var remote = UIKit.Card(p, "Remote", new Vector4(0.02f, 0.19f, 0.42f, 0.44f), UIStyle.Card);
            ui.RemoteBlock        = remote.gameObject;
            ui.RemotePlayerName   = UIKit.Text(remote, "Name", "", 30, new Vector4(0.04f, 0.78f, 0.6f, 0.96f), TextAlignmentOptions.Left, UIStyle.Accent, FontStyles.Bold);
            ui.RemoteScoreLabel   = UIKit.Text(remote, "Score", "-", 64, new Vector4(0.6f, 0.55f, 0.96f, 0.96f), TextAlignmentOptions.Right, UIStyle.Gold, FontStyles.Bold);
            ui.RemoteArrivalLabel = UIKit.Text(remote, "Status", "", 26, new Vector4(0.04f, 0.14f, 0.96f, 0.78f), TextAlignmentOptions.TopLeft, UIStyle.Text);
            ui.RemoteScoreBar     = UIKit.Bar(remote, "Bar", new Vector4(0.04f, 0.04f, 0.96f, 0.1f), UIStyle.Gold);

            var debrief = UIKit.Card(p, "Debrief", new Vector4(0.44f, 0.19f, 0.98f, 0.82f), UIStyle.Card);
            // The debrief (design effects, what-ifs, real mission) is long: it scrolls
            var debriefScroll = UIKit.Scroll(debrief, "Scroll", new Vector4(0f, 0.02f, 1f, 0.98f));
            ui.DebriefLabel = UIKit.Text(debriefScroll, "Text", "", 25, new Vector4(0f, 0f, 1f, 1f),
                                         TextAlignmentOptions.TopLeft, UIStyle.Text);
            ui.DebriefLabel.enableAutoSizing = false;
            ui.DebriefLabel.textWrappingMode = TextWrappingModes.Normal;

            ui.PlayAgainButton = UIKit.Button(p, "PlayAgain", "@sb.again", new Vector4(0.02f, 0.03f, 0.2f, 0.16f),
                                              UIStyle.ButtonIdle, UIStyle.Text, 32, FontStyles.Bold);
            ui.NextMissionButton = UIKit.Button(p, "NextMission", "@sb.next", new Vector4(0.21f, 0.03f, 0.42f, 0.16f),
                                                UIStyle.Accent, UIStyle.Ink, 40, FontStyles.Bold);
            ui.NextMissionButton.gameObject.SetActive(false);
            ui.ShareButton = UIKit.Button(p, "Share", "@sb.share", new Vector4(0.44f, 0.03f, 0.61f, 0.16f),
                                          UIStyle.Gold, UIStyle.Ink, 26, FontStyles.Bold);
            ui.SkyButton   = UIKit.Button(p, "Sky", "@sb.sky", new Vector4(0.62f, 0.03f, 0.79f, 0.16f),
                                          new Color(0.35f, 0.25f, 0.7f, 1f), UIStyle.Text, 26, FontStyles.Bold);
            ui.MainMenuButton  = UIKit.Button(p, "MainMenu", "@sb.menu", new Vector4(0.8f, 0.03f, 0.98f, 0.16f),
                                              UIStyle.ButtonIdle, UIStyle.Text, 26);
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
        public static Sprite KnobSprite => _knob    ??= AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        static Sprite CheckSprite   => _check   ??= AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");

        // ── Sci-fi skin: generated once into Assets/UI/Generated (delete a PNG to regenerate it) ──
        // Signed distance to the chamfered box (top-left and bottom-right corners cut), negative inside
        static float ChamferDist(float x, float y, float w, float h, float m, float c)
        {
            float d = Mathf.Max(Mathf.Max(m - x, x - (w - m)), Mathf.Max(m - y, y - (h - m)));
            d = Mathf.Max(d, ((m + c) - x + y - (h - m)) * 0.7071f);   // top-left cut
            d = Mathf.Max(d, (x - (w - m - c) - y + m) * 0.7071f);     // bottom-right cut
            return d;
        }

        public static Sprite GenSprite(string file, int w, int h, System.Func<float, float, float> alpha,
                                       Vector4 border = default, bool repeat = false)
        {
            string path = "Assets/UI/Generated/" + file;
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha(x + 0.5f, y + 0.5f)));
            tex.SetPixels(px);
            tex.Apply();
            Directory.CreateDirectory("Assets/UI/Generated");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.spriteBorder = border;
            ti.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            // Full-rect mesh (needed for tiling); set through the importer settings
            var st = new TextureImporterSettings();
            ti.ReadTextureSettings(st);
            st.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(st);
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        const int FrameN = 64;
        static readonly Vector4 FrameBorder = new(22, 22, 22, 22);

        // Solid body of cards and buttons (tinted by the Image colour)
        static Sprite _frame, _rim, _grid, _ring, _stick;
        public static Sprite FrameSprite => _frame ??= GenSprite("Frame.png", FrameN, FrameN, (x, y) =>
            Mathf.Clamp01(0.5f - ChamferDist(x, y, FrameN, FrameN, 1f, 14f)), FrameBorder);

        // Glowing outline drawn on top of the body, with brighter corner brackets
        public static Sprite RimSprite => _rim ??= GenSprite("Rim.png", FrameN, FrameN, (x, y) =>
        {
            float d = ChamferDist(x, y, FrameN, FrameN, 1f, 14f);
            float line = Mathf.Clamp01(1f - Mathf.Abs(d + 1.5f) / 1.3f);
            float glow = d < 0f ? 0.35f * Mathf.Exp(d / 4f) : 0f;
            bool corner = (x < 20f && y < 20f) || (x > FrameN - 20f && y > FrameN - 20f);
            float bracket = corner && d > -5f && d < -2.5f ? 0.9f : 0f;   // inner L at the square corners
            return Mathf.Max(line, glow, bracket) * Mathf.Clamp01(0.5f - d);
        }, FrameBorder);

        // Faint tiled grid behind every screen
        public static Sprite GridSprite => _grid ??= GenSprite("Grid.png", 64, 64, (x, y) =>
        {
            bool line = x < 1.5f || y < 1.5f;
            bool dot  = x < 3.5f && y < 3.5f;
            return dot ? 1f : line ? 0.45f : 0f;
        }, default, true);

        // Joystick base: ring with 24 ticks and 4 arrows; knob: glowing disc with a ring
        public static Sprite RingSprite => _ring ??= GenSprite("StickRing.png", 256, 256, (x, y) =>
        {
            float u = x / 128f - 1f, v = y / 128f - 1f, r = Mathf.Sqrt(u * u + v * v);
            float a = Mathf.Atan2(v, u);
            float ring  = Mathf.Clamp01(1f - Mathf.Abs(r - 0.92f) / 0.025f);
            float inner = Mathf.Clamp01(1f - Mathf.Abs(r - 0.62f) / 0.012f) * 0.5f;
            float fill  = r < 0.92f ? 0.10f + 0.12f * r : 0f;
            float m15   = Mathf.Repeat(a * Mathf.Rad2Deg, 15f);
            float tick  = r > 0.76f && r < 0.86f && Mathf.Min(m15, 15f - m15) < 1.6f ? 0.8f : 0f;
            float arrow = 0f;
            for (int k = 0; k < 4; k++)
            {
                float ang = Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, k * 90f));
                if (r > 0.66f && r < 0.74f && ang < (0.74f - r) * 110f) arrow = 1f;
            }
            return Mathf.Max(ring, inner, fill, tick, arrow) * (r < 0.96f ? 1f : 0f);
        });

        public static Sprite StickSprite => _stick ??= GenSprite("StickKnob.png", 128, 128, (x, y) =>
        {
            float u = x / 64f - 1f, v = y / 64f - 1f, r = Mathf.Sqrt(u * u + v * v);
            float ring = Mathf.Clamp01(1f - Mathf.Abs(r - 0.86f) / 0.06f);
            float core = r < 0.8f ? 0.55f + 0.45f * (1f - r / 0.8f) : 0f;
            float halo = r < 1f ? 0.25f * (1f - r) : 0f;
            return Mathf.Max(ring, core, halo);
        });

        // Outline colour for a body colour: dark bodies get the cyan accent, bright ones a lighter tint
        static Color RimFor(Color body)
        {
            float lum = 0.3f * body.r + 0.59f * body.g + 0.11f * body.b;
            if (lum < 0.3f) return new Color(UIStyle.Accent.r, UIStyle.Accent.g, UIStyle.Accent.b, 0.55f);
            var c = Color.Lerp(body, Color.white, 0.45f); c.a = 0.95f;
            return c;
        }

        // Adds the glowing outline as the first child (under labels, over the body)
        public static Image Rim(Image body, Color? color = null)
        {
            var rim = Stretch(body.rectTransform, "Rim").gameObject.AddComponent<Image>();
            rim.sprite = RimSprite;
            rim.type   = UnityEngine.UI.Image.Type.Sliced;
            rim.pixelsPerUnitMultiplier = 1f;
            rim.color  = color ?? RimFor(body.color);
            rim.raycastTarget = false;
            rim.transform.SetAsFirstSibling();
            return rim;
        }

        // Grid behind the content (only on screens with an opaque backdrop) and the intro animation
        public static void Dress(GameObject go)
        {
            if (go.GetComponent<UIPanelIntro>() != null) return;
            var content = go.transform.Find("Content") as RectTransform;
            var bg = go.GetComponent<Image>();
            if (bg != null && bg.color.a > 0.5f)
            {
                var grid = Stretch(go.transform, "Grid").gameObject.AddComponent<Image>();
                grid.sprite = GridSprite;
                grid.type   = UnityEngine.UI.Image.Type.Tiled;
                grid.color  = new Color(UIStyle.Accent.r, UIStyle.Accent.g, UIStyle.Accent.b, 0.05f);
                grid.raycastTarget = false;
                grid.transform.SetAsFirstSibling();
            }
            go.AddComponent<UIPanelIntro>().Content = content;
        }

        public struct PanelParts { public GameObject go; public RectTransform content; }

        // Full-screen backdrop (under the notch too) with content inside the safe area
        public static PanelParts Panel(Transform root, RectTransform safe, string name, Color backdrop)
        {
            var go = Stretch(root, name).gameObject;
            go.transform.SetSiblingIndex(0);
            var bg = go.AddComponent<Image>();
            bg.color = backdrop;
            bg.raycastTarget = true;   // blocks taps from reaching the flight controls

            // Faint cyan grid, then content that settles in each time the screen opens
            var grid = Stretch(go.transform, "Grid").gameObject.AddComponent<Image>();
            grid.sprite = GridSprite;
            grid.type   = UnityEngine.UI.Image.Type.Tiled;
            grid.color  = new Color(UIStyle.Accent.r, UIStyle.Accent.g, UIStyle.Accent.b, 0.05f);
            grid.raycastTarget = false;

            var content = Stretch(go.transform, "Content");
            content.gameObject.AddComponent<SafeAreaFitter>();
            go.AddComponent<UIPanelIntro>().Content = content;
            return new PanelParts { go = go, content = Column(content) };
        }

        public static RectTransform Stretch(Transform parent, string name) => Rect(parent, name, new Vector4(0, 0, 1, 1));

        // Landscape stage: full width on phones, centred and capped on very wide screens
        public static RectTransform Column(Transform parent)
        {
            var col = Stretch(parent, "Column");
            col.gameObject.AddComponent<PortraitColumn>().MaxAspect = 2.1f;
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

        // Fixed size (reference units) pinned to a point of `parent`; the pivot sits on that
        // point, so (0,0) = bottom-left corner, (1,0) = bottom-right, (0.5,0) = bottom-centre.
        // Unlike fractional anchors, it stays thumb-sized on both portrait and landscape phones.
        public static RectTransform Pin(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var rt = Rect(parent, name, new Vector4(anchor.x, anchor.y, anchor.x, anchor.y));
            rt.pivot            = anchor;
            rt.sizeDelta        = size;
            rt.anchoredPosition = pos;
            return rt;
        }

        public static Image PinImage(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color c)
        {
            var img = Pin(parent, name, anchor, pos, size).gameObject.AddComponent<Image>();
            Rounded(img, c);
            img.raycastTarget = false;
            return img;
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
            img.sprite = FrameSprite;   // chamfered sci-fi plate (was a rounded box)
            img.type   = UnityEngine.UI.Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;
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
            else Rim(img);
            return img.rectTransform;
        }

        public static RawImage Raw(Transform parent, string name, Vector4 a)
        {
            var raw = Rect(parent, name, a).gameObject.AddComponent<RawImage>();
            raw.raycastTarget = false;
            return raw;
        }

        // Single-line text box with a localized placeholder
        public static TMP_InputField InputTop(Transform parent, string name, string placeholder, float x0, float x1,
                                              float top, float h, float size)
        {
            var rt = RectTop(parent, name, x0, x1, top, h);
            var bg = rt.gameObject.AddComponent<Image>();
            Rounded(bg, UIStyle.ButtonIdle);
            bg.raycastTarget = true;
            Rim(bg);

            var area = Rect(rt, "Text Area", new Vector4(0f, 0f, 1f, 1f));
            area.offsetMin = new Vector2(18f, 4f);
            area.offsetMax = new Vector2(-18f, -4f);
            area.gameObject.AddComponent<RectMask2D>();

            var ph = Style(Rect(area, "Placeholder", new Vector4(0f, 0f, 1f, 1f)).gameObject.AddComponent<TextMeshProUGUI>(),
                           placeholder, size, TextAlignmentOptions.Center, UIStyle.Muted, FontStyles.Italic);
            var text = Style(Rect(area, "Text", new Vector4(0f, 0f, 1f, 1f)).gameObject.AddComponent<TextMeshProUGUI>(),
                             "", size, TextAlignmentOptions.Center, UIStyle.Text, FontStyles.Bold);
            // Typed text must not shrink or wrap while typing
            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode     = TextOverflowModes.Overflow;

            var input = rt.gameObject.AddComponent<TMP_InputField>();
            input.textViewport  = area;
            input.textComponent = text;
            input.placeholder   = ph;
            input.targetGraphic = bg;
            input.lineType      = TMP_InputField.LineType.SingleLine;
            input.contentType   = TMP_InputField.ContentType.Standard;
            input.caretColor    = UIStyle.Accent;
            input.customCaretColor = true;
            return input;
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
            if (bg.a > 0.05f) Rim(img);   // text-only links stay plain
            rt.gameObject.AddComponent<UIPress>();
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
            Rim(bg);
            rt.gameObject.AddComponent<UIPress>().Pressed = 0.96f;

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
            Rim(card);
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
