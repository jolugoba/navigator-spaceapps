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
            Loc.Changed += OnLanguageChanged;
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
            Senda.OnPeerDisconnected.AddListener(OnPeerDisconnected);
            if (Sync != null) Sync.OnPeerList.AddListener(OnPeerList);
            ShowMyQR(Senda.NodeId);
        }

        void ShowMyQR(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) return;
            if (MyNodeIdLabel != null)
                MyNodeIdLabel.text = nodeId[..Mathf.Min(20, nodeId.Length)] + "…";

            try
            {
                if (MyQRImage != null) MyQRImage.texture = GenerateQR(nodeId, 256);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Lobby] No se pudo generar el QR: {e}");
                if (MyNodeIdLabel != null) MyNodeIdLabel.text = nodeId;   // still shareable by hand
            }
        }

        // QR from ZXing's bit matrix: dark modules black, light modules white.
        // ZXing's row 0 is the top; Unity's texture row 0 is the bottom, so rows are flipped.
        private static Texture2D GenerateQR(string content, int size)
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
            _scanning = true;
            SetScanButtonLabel(Loc.T("lobby.cancel"));
            SetScanStatus(Loc.T("lobby.aim"));

            string device = null;
            foreach (var d in WebCamTexture.devices)
                if (!d.isFrontFacing) { device = d.name; break; }
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

        void ConnectToScanned(string remoteNodeId)
        {
            SetScanStatus(Loc.T("lobby.connecting"));
            bool ok = Senda.ConnectToNode(remoteNodeId);
            if (ScanStatusText != null) ScanStatusText.text = ok
                ? Loc.F("lobby.connected", remoteNodeId[..Mathf.Min(12, remoteNodeId.Length)])
                : Loc.T("lobby.connecterror");
        }

        public const int MaxPlayers = 4;

        void OnPeerConnected(string nodeId)
        {
            _peersConnected = Senda.ConnectedPeers.Count;
            UpdateConnectedLabel();

            // Share who we know so everyone connects to everyone (full mesh, no host)
            string myName = GameManager.Instance != null ? GameManager.Instance.PlayerName : Loc.T("player.default");
            string peers  = string.Join(";", Senda.ConnectedPeers);
            string lobbyJson = $"{{\"name\":\"{myName}\",\"nodeId\":\"{Senda.NodeId}\",\"peers\":\"{peers}\"}}";
            Senda.Broadcast(Navigator.Network.NavMsg.Build(
                Navigator.Network.NavMsgType.lobbyInfo,
                Senda.NodeId,
                lobbyJson));
        }

        void OnPeerList(string[] peers)
        {
            foreach (var id in peers)
            {
                if (Senda.ConnectedPeers.Count >= MaxPlayers - 1) return;
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
                : Loc.F("lobby.players", _peersConnected + 1, MaxPlayers);
        }

        void OnLanguageChanged()
        {
            UpdateConnectedLabel();
            if (!_scanning) SetScanButtonLabel(Loc.T("lobby.scan"));
            if (Senda == null || !Senda.Initialized)
                if (MyNodeIdLabel != null) MyNodeIdLabel.text = Loc.T("lobby.offline");
        }

        void OnDestroy()
        {
            Loc.Changed -= OnLanguageChanged;
            if (_cam != null) _cam.Stop();
        }
    }
}
