// Navigator 2.0 — ARSessionManager.cs
// Inicia AR Foundation y avisa cuando la cámara está lista
// Todo lo demás (camera background, tracking) lo hace Unity automáticamente
// Guacalita Inc. — SAL / Claude

using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;
using TMPro;

namespace Navigator.AR
{
    public class ARSessionManager : MonoBehaviour
    {
        [Header("AR Foundation (asignar desde escena)")]
        public ARSession    Session;         // GameObject AR Session
        public ARCameraManager CameraManager; // en el AR Camera

        [Header("UI de carga")]
        public GameObject LoadingPanel;      // "Iniciando AR..."
        public TMP_Text   StatusText;
        public Button     CalibrateCameraBtn; // "Recalibrar"

        [Header("Refs")]
        public ARShipController ShipController;
        public ARSpaceManager   SpaceManager;

        void Start() => StartCoroutine(InitAR());

        IEnumerator InitAR()
        {
            ShowStatus("Verificando soporte AR…");

            // Espera que AR Foundation compruebe el dispositivo
            yield return new WaitUntil(() =>
                ARSession.state != ARSessionState.None &&
                ARSession.state != ARSessionState.CheckingAvailability);

            if (ARSession.state == ARSessionState.Unsupported)
            {
                ShowStatus("AR no disponible en este dispositivo.\nUsando modo pantalla.");
                // Fallback: desactiva AR background, usa skybox de espacio
                if (CameraManager != null) CameraManager.enabled = false;
                LoadingPanel?.SetActive(false);
                yield break;
            }

            ShowStatus("Iniciando cámara…");

            // AR Foundation enciende la cámara automáticamente
            // Solo esperamos que el estado sea SessionTracking
            float timeout = 8f;
            while (ARSession.state != ARSessionState.SessionTracking && timeout > 0f)
            {
                yield return new WaitForSeconds(0.2f);
                timeout -= 0.2f;
            }

            if (ARSession.state == ARSessionState.SessionTracking)
            {
                ShowStatus("¡AR listo!");
                yield return new WaitForSeconds(0.8f);
                LoadingPanel?.SetActive(false);

                // Calibra giroscopio con la posición actual del celular
                ShipController?.CalibrateGyro();
                SpaceManager?.enabled = true;
            }
            else
            {
                ShowStatus("AR iniciando… mueve el celular lentamente.");
                LoadingPanel?.SetActive(false);
            }

            if (CalibrateCameraBtn != null)
                CalibrateCameraBtn.onClick.AddListener(() => ShipController?.CalibrateGyro());
        }

        void ShowStatus(string msg)
        {
            if (StatusText != null) StatusText.text = msg;
            Debug.Log($"[AR] {msg}");
        }
    }
}
