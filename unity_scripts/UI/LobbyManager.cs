// Navigator 2.0 — LobbyManager.cs
// Flujo de conexión P2P por QR:
//   Jugador A → muestra su NodeId como QR en pantalla
//   Jugador B → escanea el QR → Iroh conecta directo sin servidor
//
// Requiere: ZXing.Net.Unity (Assets/Plugins/ZXing) para generar/leer QR
// Guacalita Inc. — SAL / Claude

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ZXing;          // ZXing.Net.Unity — asset gratuito en Unity Asset Store
using ZXing.QrCode;
using ZXing.Unity;

namespace Navigator.UI
{
    public class LobbyManager : MonoBehaviour
    {
        // ── Inspector refs ────────────────────────────────────────────────────
        [Header("Mi QR (Jugador A — Host)")]
        public RawImage  MyQRImage;          // muestra el QR de mi NodeId
        public TMP_Text  MyNodeIdLabel;      // texto del NodeId debajo del QR
        public TMP_Text  MyNameLabel;        // nombre del jugador local

        [Header("Escanear QR (Jugador B — Guest)")]
        public Button    ScanButton;         // abre cámara para escanear
        public TMP_Text  ScanStatusText;     // "Esperando..." / "Conectado!"

        [Header("Estado de la sesión")]
        public TMP_Text  ConnectedPlayersText;
        public Button    StartGameButton;    // activo cuando hay ≥2 jugadores

        [Header("Componentes")]
        public Navigator.Network.SendaBridge Senda;
        public Navigator.Network.ShipSyncService Sync;

        // ── Runtime ────────────────────────────────────────────────────────────
        private WebCamTexture _cam;
        private bool          _scanning = false;
        private int           _peersConnected = 0;

        // ── Init ──────────────────────────────────────────────────────────────

        void Start()
        {
            if (!Senda.Initialized) Senda.Init();

            // Muestra mi propio NodeId como QR
            ShowMyQR(Senda.NodeId);

            // Eventos Senda
            Senda.OnPeerConnected.AddListener(OnPeerConnected);
            Senda.OnPeerDisconnected.AddListener(OnPeerDisconnected);

            ScanButton.onClick.AddListener(ToggleScan);
            StartGameButton.interactable = false;

            UpdateConnectedLabel();
        }

        // ── Genera y muestra el QR de mi NodeId ──────────────────────────────

        void ShowMyQR(string nodeId)
        {
            if (MyNodeIdLabel != null)
                MyNodeIdLabel.text = nodeId[..Mathf.Min(20, nodeId.Length)] + "…";

            // Genera textura QR 256×256
            Texture2D qrTex = GenerateQR(nodeId, 256);
            if (MyQRImage != null) MyQRImage.texture = qrTex;
        }

        private static Texture2D GenerateQR(string content, int size)
        {
            var writer  = new BarcodeWriter<Color32[]>
            {
                Format   = BarcodeFormat.QR_CODE,
                Options  = new QrCodeEncodingOptions { Width = size, Height = size, Margin = 1 },
                Renderer = new Color32Renderer(),
            };
            Color32[] pixels = writer.Write(content);
            var tex = new Texture2D(size, size);
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        // ── Escanear QR del otro jugador ──────────────────────────────────────

        void ToggleScan()
        {
            if (_scanning) StopScan();
            else StartScan();
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
            var reader = new BarcodeReader();
            while (_scanning && _cam != null && _cam.isPlaying)
            {
                // Decodifica un frame de la cámara
                Result result = reader.Decode(
                    _cam.GetPixels32(), _cam.width, _cam.height);

                if (result != null)
                {
                    string scannedNodeId = result.Text;
                    Debug.Log($"[Lobby] QR escaneado: {scannedNodeId}");
                    StopScan();
                    ConnectToScanned(scannedNodeId);
                    yield break;
                }
                yield return new WaitForSeconds(0.1f);
            }
        }

        // ── Conectar al NodeId escaneado ──────────────────────────────────────

        void ConnectToScanned(string remoteNodeId)
        {
            ScanStatusText.text = "Conectando…";
            bool ok = Senda.ConnectToNode(remoteNodeId);
            ScanStatusText.text = ok
                ? $"Conectado a {remoteNodeId[..Mathf.Min(12, remoteNodeId.Length)]}…"
                : "Error de conexión. Intenta de nuevo.";
        }

        // ── Eventos de conexión ───────────────────────────────────────────────

        void OnPeerConnected(string nodeId)
        {
            _peersConnected++;
            UpdateConnectedLabel();

            // Envía tu info de jugador al peer recién conectado
            string lobbyJson = $"{{\"name\":\"{MyNameLabel?.text ?? "Cadete"}\"," +
                               $"\"nodeId\":\"{Senda.NodeId}\"}}";
            Senda.Broadcast(Navigator.Network.NavMsg.Build(
                Navigator.Network.NavMsgType.lobbyInfo,
                Senda.NodeId,
                lobbyJson));

            // Con ≥1 peer ya se puede iniciar la carrera
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
                : $"✓ {_peersConnected + 1} jugador(es) conectados — ¡listo para despegar!";
        }

        void OnDestroy()
        {
            _cam?.Stop();
        }
    }
}
