// Navigator 2.0 — LobbyManager.cs
// Flujo de conexión P2P por QR:
//   Jugador A → muestra su NodeId como QR en pantalla
//   Jugador B → escanea el QR → Iroh conecta directo sin servidor
// ZXing.Net 0.16.9 estándar (netstandard2.0) — sin ZXing.Unity
// Guacalita Inc. — SAL / Claude

using System.Collections;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
using UnityEngine.UI;
using TMPro;
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

        [Header("Componentes")]
        public Navigator.Network.SendaBridge      Senda;
        public Navigator.Network.ShipSyncService  Sync;

        private WebCamTexture _cam;
        private bool          _scanning = false;
        private int           _peersConnected = 0;

        void Start()
        {
            if (Senda != null && !Senda.Initialized) Senda.Init();

            bool online = Senda != null && Senda.Initialized;
            if (online)
            {
                ShowMyQR(Senda.NodeId);
                Senda.OnPeerConnected.AddListener(OnPeerConnected);
                Senda.OnPeerDisconnected.AddListener(OnPeerDisconnected);
            }
            else if (MyNodeIdLabel != null)
            {
                MyNodeIdLabel.text = "Red P2P no disponible — puedes jugar solo";
            }

            if (ScanButton != null)
            {
                ScanButton.onClick.AddListener(ToggleScan);
                ScanButton.interactable = online;
            }
            if (ScanPreview != null) ScanPreview.gameObject.SetActive(false);

            // Solo play is always allowed so the game can be demoed on one phone
            if (StartGameButton != null)
            {
                StartGameButton.onClick.AddListener(() => { StopScan(); GameManager.Instance?.OnLobbyReady(); });
                StartGameButton.interactable = true;
            }

            UpdateConnectedLabel();
        }

        void ShowMyQR(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) return;
            if (MyNodeIdLabel != null)
                MyNodeIdLabel.text = nodeId[..Mathf.Min(20, nodeId.Length)] + "…";

            Texture2D qrTex = GenerateQR(nodeId, 256);
            if (MyQRImage != null) MyQRImage.texture = qrTex;
        }

        // Genera textura QR usando BarcodeWriterPixelData (API estándar ZXing.Net)
        private static Texture2D GenerateQR(string content, int size)
        {
            var writer = new BarcodeWriterPixelData
            {
                Format  = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions { Width = size, Height = size, Margin = 1 }
            };
            var pd  = writer.Write(content);
            var tex = new Texture2D(pd.Width, pd.Height, TextureFormat.RGBA32, false);

            // Convierte int[] ARGB → Color32[]
            var c32 = new Color32[pd.Pixels.Length];
            for (int i = 0; i < pd.Pixels.Length; i++)
            {
                int p = pd.Pixels[i];
                c32[i] = new Color32(
                    (byte)((p >> 16) & 0xFF),
                    (byte)((p >> 8)  & 0xFF),
                    (byte)( p        & 0xFF),
                    (byte)((p >> 24) & 0xFF));
            }
            tex.SetPixels32(c32);
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
                SetScanStatus("Permite el uso de la cámara y vuelve a tocar Escanear.");
                return;
            }
#endif
            _scanning = true;
            SetScanButtonLabel("Cancelar");
            SetScanStatus("Apunta la cámara al QR del otro jugador…");

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
            SetScanButtonLabel("Escanear QR");
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
            SetScanStatus("Conectando…");
            bool ok = Senda.ConnectToNode(remoteNodeId);
            if (ScanStatusText != null) ScanStatusText.text = ok
                ? $"Conectado a {remoteNodeId[..Mathf.Min(12, remoteNodeId.Length)]}…"
                : "Error de conexión. Intenta de nuevo.";
        }

        void OnPeerConnected(string nodeId)
        {
            _peersConnected++;
            UpdateConnectedLabel();

            string myName = GameManager.Instance != null ? GameManager.Instance.PlayerName : "Cadete";
            string lobbyJson = $"{{\"name\":\"{myName}\"," +
                               $"\"nodeId\":\"{Senda.NodeId}\"}}";
            Senda.Broadcast(Navigator.Network.NavMsg.Build(
                Navigator.Network.NavMsgType.lobbyInfo,
                Senda.NodeId,
                lobbyJson));
        }

        void OnPeerDisconnected(string nodeId)
        {
            _peersConnected = Mathf.Max(0, _peersConnected - 1);
            UpdateConnectedLabel();
        }

        void UpdateConnectedLabel()
        {
            if (StartGameLabel != null)
                StartGameLabel.text = _peersConnected == 0 ? "JUGAR SOLO" : "¡DESPEGAR!";
            if (ConnectedPlayersText == null) return;
            ConnectedPlayersText.text = _peersConnected == 0
                ? "Esperando jugadores…\nMuestra tu QR o escanea el de otro jugador"
                : $"{_peersConnected + 1} jugador(es) conectados — listo para despegar!";
        }

        void OnDestroy() { _cam?.Stop(); }
    }
}
