// Navigator 2.0 — LobbyManager.cs
// Flujo de conexión P2P por QR:
//   Jugador A → muestra su NodeId como QR en pantalla
//   Jugador B → escanea el QR → Iroh conecta directo sin servidor
// ZXing.Net 0.16.9 estándar (netstandard2.0) — sin ZXing.Unity
// Guacalita Inc. — SAL / Claude

using System.Collections;
using UnityEngine;
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
        public Button  ScanButton;
        public TMP_Text ScanStatusText;

        [Header("Estado de la sesión")]
        public TMP_Text ConnectedPlayersText;
        public Button   StartGameButton;

        [Header("Componentes")]
        public Navigator.Network.SendaBridge      Senda;
        public Navigator.Network.ShipSyncService  Sync;

        private WebCamTexture _cam;
        private bool          _scanning = false;
        private int           _peersConnected = 0;

        void Start()
        {
            if (!Senda.Initialized) Senda.Init();

            ShowMyQR(Senda.NodeId);

            Senda.OnPeerConnected.AddListener(OnPeerConnected);
            Senda.OnPeerDisconnected.AddListener(OnPeerDisconnected);

            ScanButton.onClick.AddListener(ToggleScan);
            StartGameButton.interactable = false;

            UpdateConnectedLabel();
        }

        void ShowMyQR(string nodeId)
        {
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
            _scanning = true;
            ScanButton.GetComponentInChildren<TMP_Text>().text = "Cancelar";
            ScanStatusText.text = "Apunta la cámara al QR del otro jugador…";

            _cam = new WebCamTexture { requestedFPS = 30 };
            _cam.Play();
            StartCoroutine(ScanLoop());
        }

        void StopScan()
        {
            _scanning = false;
            _cam?.Stop();
            _cam = null;
            ScanButton.GetComponentInChildren<TMP_Text>().text = "Escanear QR";
        }

        IEnumerator ScanLoop()
        {
            while (_scanning && _cam != null && _cam.isPlaying)
            {
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
            ScanStatusText.text = "Conectando…";
            bool ok = Senda.ConnectToNode(remoteNodeId);
            ScanStatusText.text = ok
                ? $"Conectado a {remoteNodeId[..Mathf.Min(12, remoteNodeId.Length)]}…"
                : "Error de conexión. Intenta de nuevo.";
        }

        void OnPeerConnected(string nodeId)
        {
            _peersConnected++;
            UpdateConnectedLabel();

            string lobbyJson = $"{{\"name\":\"{MyNameLabel?.text ?? "Cadete"}\"," +
                               $"\"nodeId\":\"{Senda.NodeId}\"}}";
            Senda.Broadcast(Navigator.Network.NavMsg.Build(
                Navigator.Network.NavMsgType.lobbyInfo,
                Senda.NodeId,
                lobbyJson));

            StartGameButton.interactable = _peersConnected >= 1;
        }

        void OnPeerDisconnected(string nodeId)
        {
            _peersConnected = Mathf.Max(0, _peersConnected - 1);
            UpdateConnectedLabel();
            StartGameButton.interactable = _peersConnected >= 1;
        }

        void UpdateConnectedLabel()
        {
            if (ConnectedPlayersText == null) return;
            ConnectedPlayersText.text = _peersConnected == 0
                ? "Esperando jugadores…\nMuestra tu QR o escanea el de otro jugador"
                : $"{_peersConnected + 1} jugador(es) conectados — listo para despegar!";
        }

        void OnDestroy() { _cam?.Stop(); }
    }
}
