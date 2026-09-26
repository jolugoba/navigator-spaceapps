// Navigator 2.0 — ARSessionManager.cs
// Inicia el modo AR sin AR Foundation — usa cámara estándar + giroscopio
// Funciona en cualquier dispositivo Android/iOS sin necesitar ARCore/ARKit
// Guacalita Inc. — SAL / Claude

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Navigator.AR
{
    public class ARSessionManager : MonoBehaviour
    {
        [Header("Referencias (sin AR Foundation)")]
        public Camera ARCamera;          // cámara principal

        [Header("UI de carga")]
        public GameObject LoadingPanel;
        public TMP_Text   StatusText;
        public Button     CalibrateCameraBtn;

        [Header("Refs")]
        public ARShipController ShipController;
        public ARSpaceManager   SpaceManager;

        void Start() => StartCoroutine(InitAR());

        IEnumerator InitAR()
        {
            ShowStatus("Iniciando modo AR…");

            // Activa el giroscopio si está disponible
            if (SystemInfo.supportsGyroscope)
                Input.gyro.enabled = true;

            yield return new WaitForSeconds(0.5f);

            ShowStatus("Cámara lista.");
            yield return new WaitForSeconds(0.5f);

            if (LoadingPanel != null) LoadingPanel.SetActive(false);

            ShipController?.CalibrateGyro();
            if (SpaceManager != null) SpaceManager.enabled = true;

            if (CalibrateCameraBtn != null)
                CalibrateCameraBtn.onClick.AddListener(() => ShipController?.CalibrateGyro());

            Debug.Log("[AR] Modo giroscopio listo.");
        }

        void ShowStatus(string msg)
        {
            if (StatusText != null) StatusText.text = msg;
            Debug.Log($"[AR] {msg}");
        }
    }
}
