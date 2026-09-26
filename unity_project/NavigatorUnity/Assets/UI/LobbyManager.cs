// Navigator 2.0 — LobbyManager.cs
// Flujo de conexión P2P por QR:
//   Jugador A → muestra su NodeId como QR en pantalla
//   Jugador B → escanea el QR → Iroh conecta directo sin servidor
// ZXing.Net 0.16.9 estándar (netstandard2.0) — sin ZXing.Unity
// Guacalita Inc. — SAL / Claude

using System.Collections;
using System.Linq;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
using UnityEngine.UI;
using TMPro;
using Navigator.Core;
using Navigator.Game;
using ZXing;
using ZXing.QrCode;
using ZXing.Common;

namespace Navigator.UI
{
    public class LobbyManager : MonoBehaviour
    {
        [Header("Mi QR (Jugador A — Host)")]
        public RawImage MyQRImage;
        public TMP_Text MyNodeIdLabel;
        public TMP_Text MyNameLabel;
        public TMP_InputField NameInput;   // the player's name: rivals, scoreboard and the teacher see it

        [Header("Escanear QR (Jugador B — Guest)")]
        public Button   ScanButton;
        public TMP_Text ScanStatusText;
        public RawImage ScanPreview;      // muestra la cámara mientras escaneas

        [Header("Estado de la sesión")]
        public TMP_Text ConnectedPlayersText;
        public Button   StartGameButton;
        public TMP_Text StartGameLabel;

        [Header("Idioma")]
        public Button   LanguageButton;

        [Header("Retos y cielo")]
        public Button   PasteChallengeButton;   // código recibido por chat
        public Button   SkyButton;              // ¿Dónde está esta noche?
        public Button   ExploreButton;          // free flight, no mission

        [Header("Componentes")]
        public Navigator.Network.SendaBridge      Senda;
        public Navigator.Network.ShipSyncService  Sync;

        private WebCamTexture _cam;
        private bool          _scanning = false;
        private int           _peersConnected = 0;

        void Start()
        {
            // Buttons first: nothing below may leave the lobby without a working "play" button
            if (StartGameButton != null)
            {
                StartGameButton.onClick.AddListener(() => { StopScan(); GameManager.Instance?.OnLobbyReady(); });
                StartGameButton.interactable = true;
            }
            if (ScanButton != null) ScanButton.onClick.AddListener(ToggleScan);
            if (LanguageButton != null) LanguageButton.onClick.AddListener(Loc.Toggle);
            if (PasteChallengeButton != null) PasteChallengeButton.onClick.AddListener(PasteChallenge);
            if (ExploreButton != null) ExploreButton.onClick.AddListener(() => { StopScan(); GameManager.Instance?.StartFreeFlight(); });
            if (SkyButton != null) SkyButton.onClick.AddListener(() => { StopScan(); GameManager.Instance?.OpenSkyFinder("mars"); });
            Loc.Changed += OnLanguageChanged;
            SetupNameInput();
            if (ScanPreview != null) ScanPreview.gameObject.SetActive(false);
            UpdateConnectedLabel();

            if (Senda != null && !Senda.Initialized) Senda.Init();
            bool online = Senda != null && Senda.Initialized;
            if (ScanButton != null) ScanButton.interactable = online;

            if (!online)
            {
                if (MyNodeIdLabel != null) MyNodeIdLabel.text = Loc.T("lobby.offline");
                return;
            }

            Senda.OnPeerConnected.AddListener(OnPeerConnected);
            Senda.OnMessage.AddListener(OnLobbyMessage);
            Senda.OnPeerDisconnected.AddListener(OnPeerDisconnected);
            if (Sync != null) Sync.OnPeerList.AddListener(OnPeerList);
            Senda.ShareCodeChanged += RefreshMyQR;
            Senda.ConnectError     += OnConnectError;
            RefreshMyQR();
        }

        // Web build: the QR is an OFFER (scan me to join) or, after scanning a friend, the ANSWER
        // they must scan back. Natively it's the NodeId.
        void RefreshMyQR()
        {
            if (Senda == null) return;
            string code = Senda.ShareCode;
            if (string.IsNullOrEmpty(code))
            {
                if (MyNodeIdLabel != null) MyNodeIdLabel.text = Loc.T("lobby.web.preparing");
                return;
            }
            ShowMyQR(code);
            if (!Navigator.Network.SendaBridge.IsWeb || MyNodeIdLabel == null) return;
            MyNodeIdLabel.text  = Loc.T(Senda.ShareIsAnswer ? "lobby.web.answer" : "lobby.web.offer");
            MyNodeIdLabel.color = Senda.ShareIsAnswer ? UIStyle.Gold : UIStyle.Muted;
            if (Senda.ShareIsAnswer) SetScanStatus(Loc.T("lobby.web.showanswer"));
        }

        void OnConnectError(string why) => SetScanStatus(Loc.T("lobby.connecterror"));

        void ShowMyQR(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) return;
            if (MyNodeIdLabel != null)
                MyNodeIdLabel.text = nodeId[..Mathf.Min(20, nodeId.Length)] + "…";

            try
            {
                // Web codes are long (~550 chars): a bigger texture keeps every module crisp
                if (MyQRImage != null) MyQRImage.texture = GenerateQR(nodeId, nodeId.Length > 100 ? 600 : 256);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Lobby] No se pudo generar el QR: {e}");
                if (MyNodeIdLabel != null) MyNodeIdLabel.text = nodeId;   // still shareable by hand
            }
        }

        // QR from ZXing's bit matrix: dark modules black, light modules white.
        // ZXing's row 0 is the top; Unity's texture row 0 is the bottom, so rows are flipped.
        public static Texture2D GenerateQR(string content, int size)
        {
            var hints = new System.Collections.Generic.Dictionary<EncodeHintType, object>
            {
                { EncodeHintType.MARGIN, 2 },
                { EncodeHintType.CHARACTER_SET, "UTF-8" },
            };
            BitMatrix m = new MultiFormatWriter().encode(content, BarcodeFormat.QR_CODE, size, size, hints);
            int w = m.Width, h = m.Height;

            var px = new Color32[w * h];
            var dark  = new Color32(0, 0, 0, 255);
            var light = new Color32(255, 255, 255, 255);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[(h - 1 - y) * w + x] = m[x, y] ? dark : light;

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        void ToggleScan()
        {
            if (_scanning) StopScan();
            else           StartScan();
        }

        void StartScan()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Permission.RequestUserPermission(Permission.Camera);
                SetScanStatus(Loc.T("lobby.campermission"));
                return;
            }
#endif
            if (!Navigator.AR.CameraPicker.HasPermission) { StartCoroutine(AskCameraThenScan()); return; }
            _scanning = true;
            SetScanButtonLabel(Loc.T("lobby.cancel"));
            SetScanStatus(Loc.T("lobby.aim"));

            string device = Navigator.AR.CameraPicker.BackCamera();
            _cam = device != null ? new WebCamTexture(device, 1280, 720, 30)
                                  : new WebCamTexture { requestedFPS = 30 };
            _cam.Play();

            if (ScanPreview != null)
            {
                ScanPreview.texture = _cam;
                ScanPreview.gameObject.SetActive(true);
            }
            StartCoroutine(ScanLoop());
        }

        // Browser / iOS: ask for the camera first, then scan
        IEnumerator AskCameraThenScan()
        {
            SetScanStatus(Loc.T("lobby.campermission"));
            yield return Navigator.AR.CameraPicker.RequestPermission();
            if (Navigator.AR.CameraPicker.HasPermission) StartScan();
        }

        void StopScan()
        {
            _scanning = false;
            if (_cam != null) _cam.Stop();
            _cam = null;
            if (ScanPreview != null) ScanPreview.gameObject.SetActive(false);
            SetScanButtonLabel(Loc.T("lobby.scan"));
        }

        void SetScanStatus(string s) { if (ScanStatusText != null) ScanStatusText.text = s; }

        void SetScanButtonLabel(string s)
        {
            var label = ScanButton != null ? ScanButton.GetComponentInChildren<TMP_Text>() : null;
            if (label != null) label.text = s;
        }

        IEnumerator ScanLoop()
        {
            while (_scanning && _cam != null && _cam.isPlaying)
            {
                // WebCamTexture reports 16x16 until the first real frame arrives
                if (_cam.width <= 16) { yield return null; continue; }
                if (ScanPreview != null)
                    ScanPreview.rectTransform.localEulerAngles = new Vector3(0f, 0f, -_cam.videoRotationAngle);

                Color32[] pixels = _cam.GetPixels32();

                // Convierte Color32[] → byte[] RGBA
                byte[] rgba = new byte[pixels.Length * 4];
                for (int i = 0; i < pixels.Length; i++)
                {
                    rgba[i * 4]     = pixels[i].r;
                    rgba[i * 4 + 1] = pixels[i].g;
                    rgba[i * 4 + 2] = pixels[i].b;
                    rgba[i * 4 + 3] = pixels[i].a;
                }

                // API de bajo nivel ZXing.Net — evita BarcodeReader<T> genérico
                var lum    = new RGBLuminanceSource(rgba, _cam.width, _cam.height,
                                                    RGBLuminanceSource.BitmapFormat.RGBA32);
                var bitmap = new BinaryBitmap(new HybridBinarizer(lum));
                Result result = null;
                try { result = new MultiFormatReader().decode(bitmap); }
                catch (ReaderException) { }

                if (result != null)
                {
                    Debug.Log($"[Lobby] QR escaneado: {result.Text}");
                    StopScan();
                    ConnectToScanned(result.Text);
                    yield break;
                }
                yield return new WaitForSeconds(0.1f);
            }
        }

        void PasteChallenge()
        {
            WebPlatform.Paste(text =>
            {
                if (Challenge.TryDecode(text, out var mission))
                {
                    StopScan();
                    GameManager.Instance?.StartChallenge(mission);
                }
                else SetScanStatus(Loc.T("lobby.badcode"));
            });
        }

        // The same scanner joins a game (a NodeId) or accepts a shared challenge (NAVC1:…)
        void ConnectToScanned(string remoteNodeId)
        {
            if (Challenge.TryDecode(remoteNodeId, out var mission))
            {
                GameManager.Instance?.StartChallenge(mission);
                return;
            }
            SetScanStatus(Loc.T("lobby.connecting"));
            // Web: scanning a friend's OFFER turns my QR into the ANSWER they must scan back
            if (Navigator.Network.SendaBridge.IsWeb)
            {
                if (!Senda.ConnectToNode(remoteNodeId)) SetScanStatus(Loc.T("lobby.connecterror"));
                else if (remoteNodeId.StartsWith("NVO")) SetScanStatus(Loc.T("lobby.web.preparing"));
                return;
            }
            bool ok = Senda.ConnectToNode(remoteNodeId);
            if (ScanStatusText != null) ScanStatusText.text = ok
                ? Loc.F("lobby.connected", remoteNodeId[..Mathf.Min(12, remoteNodeId.Length)])
                : Loc.T("lobby.connecterror");
        }

        void SetupNameInput()
        {
            GameManager.PlayerNameChanged += OnNameChanged;
            ShipCatalog.SelectionChanged += OnShipChanged;
            if (NameInput == null) return;
            NameInput.characterLimit = GameManager.MaxNameLength;
            NameInput.SetTextWithoutNotify(GameManager.Instance != null ? GameManager.Instance.CustomPlayerName : "");
            NameInput.onEndEdit.AddListener(v =>
            {
                if (GameManager.Instance == null) return;
                GameManager.Instance.PlayerName = v;
                NameInput.SetTextWithoutNotify(GameManager.Instance.CustomPlayerName);
            });
        }

        // Typed here or sent by the Flutter app: show it and tell everyone connected
        void OnNameChanged(string name)
        {
            if (NameInput != null && GameManager.Instance != null && !NameInput.isFocused)
                NameInput.SetTextWithoutNotify(GameManager.Instance.CustomPlayerName);
            if (Senda != null && Senda.Initialized && Senda.ConnectedPeers.Count > 0) BroadcastLobbyInfo();
            ClassroomSession.Instance?.OnNameChanged();
        }

        // A new hull in the hangar: rivals update the ship they see
        void OnShipChanged(int index)
        {
            if (Senda != null && Senda.Initialized && Senda.ConnectedPeers.Count > 0) BroadcastLobbyInfo();
        }

        // A friend started free flight: invite shown here, EXPLORE joins them in the same world
        void OnLobbyMessage(Navigator.Network.NavMsg msg)
        {
            if (msg.t != nameof(Navigator.Network.NavMsgType.shipEvent) || msg.pl_json == null) return;
            if (!msg.pl_json.Contains("\"free\"")) return;
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Lobby) return;
            string who = null;
            int i = msg.pl_json.IndexOf("\"name\":\"", System.StringComparison.Ordinal);
            if (i >= 0)
            {
                i += 8;
                int e = msg.pl_json.IndexOf('"', i);
                if (e > i) who = msg.pl_json[i..e];
            }
            SetScanStatus(Loc.F("free.invite", who ?? Loc.T("rival")));
        }

        void OnPeerConnected(string nodeId)
        {
            if (Navigator.Network.SendaBridge.IsWeb) SetScanStatus(Loc.F("lobby.connected", nodeId[..Mathf.Min(12, nodeId.Length)]));
            _peersConnected = Senda.ConnectedPeers.Count;
            UpdateConnectedLabel();
            BroadcastLobbyInfo();
        }

        void BroadcastLobbyInfo()
        {
            // Share who we know so everyone connects to everyone (full mesh, no host)
            string myName = GameManager.Instance != null ? GameManager.Instance.PlayerName : Loc.T("player.default");
            string peers  = string.Join(";", Senda.ConnectedPeers);
            string lobbyJson = $"{{\"name\":\"{myName}\",\"nodeId\":\"{Senda.NodeId}\",\"peers\":\"{peers}\",\"ship\":{ShipCatalog.Selected}}}";
            Senda.Broadcast(Navigator.Network.NavMsg.Build(
                Navigator.Network.NavMsgType.lobbyInfo,
                Senda.NodeId,
                lobbyJson));
        }

        void OnPeerList(string[] peers)
        {
            foreach (var id in peers)
            {
                if (id == Senda.NodeId || Senda.ConnectedPeers.Contains(id)) continue;
                Senda.ConnectToNode(id);
            }
        }

        void OnPeerDisconnected(string nodeId)
        {
            _peersConnected = Senda.ConnectedPeers.Count;
            UpdateConnectedLabel();
        }

        void UpdateConnectedLabel()
        {
            if (StartGameLabel != null)
                StartGameLabel.text = _peersConnected == 0 ? Loc.T("lobby.solo") : Loc.T("lobby.launch");
            if (ConnectedPlayersText == null) return;
            ConnectedPlayersText.text = _peersConnected == 0
                ? Loc.T("lobby.waiting")
                : Loc.F("lobby.players", _peersConnected + 1);
        }

        void OnLanguageChanged()
        {
            UpdateConnectedLabel();
            if (!_scanning) SetScanButtonLabel(Loc.T("lobby.scan"));
            if (Senda == null || !Senda.Initialized)
            {
                if (MyNodeIdLabel != null) MyNodeIdLabel.text = Loc.T("lobby.offline");
            }
            else if (Navigator.Network.SendaBridge.IsWeb) RefreshMyQR();
        }

        void OnDestroy()
        {
            Loc.Changed -= OnLanguageChanged;
            if (Senda != null) { Senda.ShareCodeChanged -= RefreshMyQR; Senda.ConnectError -= OnConnectError; }
            GameManager.PlayerNameChanged -= OnNameChanged;
            ShipCatalog.SelectionChanged -= OnShipChanged;
            if (_cam != null) _cam.Stop();
        }
    }
}
