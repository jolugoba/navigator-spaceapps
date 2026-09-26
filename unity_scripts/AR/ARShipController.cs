// Navigator 2.0 — ARShipController.cs
// Controla la nave en el espacio AR con input táctil + giroscopio del celular
// Funciona en cualquier lugar (cuarto, sala, evento) — sin apuntar al cielo
// Guacalita Inc. — SAL / Claude

using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Navigator.Game;
using Navigator.Core;

namespace Navigator.AR
{
    public class ARShipController : MonoBehaviour
    {
        [Header("Nave a controlar")]
        public ShipController Ship;

        [Header("AR Camera")]
        public Camera ARCamera;

        [Header("Sensibilidad")]
        public float TiltSensitivity   = 1.8f;   // giroscopio → pitch/yaw
        public float TouchThrottle     = 0.9f;    // mantener pantalla → thrust
        public float TapBoostDuration  = 0.3f;    // doble toque → boost breve

        // ── Runtime ────────────────────────────────────────────────────────────
        private Quaternion _gyroRef;              // orientación de calibración
        private bool       _gyroCalibrated = false;
        private float      _boostTimer     = 0f;
        private int        _touchCount     = 0;
        private float      _lastTapTime    = 0f;

        // La nave flota en el espacio AR anclada 2m adelante de la cámara
        private Vector3    _anchorOffset   = new(0f, 0f, 2f);

        void Start()
        {
            // Activa el giroscopio del dispositivo
            Input.gyro.enabled = true;
            CalibrateGyro();
        }

        void Update()
        {
            HandleGyroSteering();
            HandleTouchInput();
            AnchorShipToARCamera();
        }

        // ── Giroscopio → dirección de la nave ─────────────────────────────────
        // Inclinar el celular = inclinar la nave en el espacio AR

        void HandleGyroSteering()
        {
            if (!_gyroCalibrated) return;

            Quaternion gyroRot = Input.gyro.attitude;
            // Convierte coordenadas del giroscopio de iOS/Android a Unity
            Quaternion deviceRot = new Quaternion(gyroRot.x, gyroRot.y, -gyroRot.z, -gyroRot.w);
            Quaternion delta     = Quaternion.Inverse(_gyroRef) * deviceRot;

            float pitch = delta.eulerAngles.x;
            float yaw   = delta.eulerAngles.y;

            // Normaliza de 0-360 a -180/+180
            if (pitch > 180f) pitch -= 360f;
            if (yaw   > 180f) yaw   -= 360f;

            if (Ship != null)
            {
                Ship.SteerPitch = Mathf.Clamp(pitch / 90f * TiltSensitivity, -1f, 1f);
                Ship.SteerYaw   = Mathf.Clamp(yaw   / 90f * TiltSensitivity, -1f, 1f);
            }
        }

        // ── Toque táctil → thrust + boost ─────────────────────────────────────
        // 1 dedo mantenido = thrust
        // 2 dedos mantenidos = thrust + boost
        // Doble toque rápido = boost breve (Burnout fill)

        void HandleTouchInput()
        {
            _touchCount = Input.touchCount;
            bool thrusting = _touchCount >= 1;
            bool boosting  = _touchCount >= 2 || _boostTimer > 0f;

            // Doble toque → boost 0.3s
            if (_touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                if (Time.time - _lastTapTime < 0.3f) _boostTimer = TapBoostDuration;
                _lastTapTime = Time.time;
            }
            _boostTimer = Mathf.Max(0f, _boostTimer - Time.deltaTime);

            if (Ship != null)
            {
                Ship.ThrustMain = thrusting ? TouchThrottle : 0f;
                Ship.Boosting   = boosting;
            }
        }

        // ── La nave sigue la cámara AR ─────────────────────────────────────────
        // Siempre aparece 2m adelante de donde miras — impacto inmediato en AR

        void AnchorShipToARCamera()
        {
            if (ARCamera == null || Ship == null) return;

            Vector3 targetPos = ARCamera.transform.position
                              + ARCamera.transform.TransformDirection(_anchorOffset);

            // Suavizado para que no salte bruscamente
            transform.position = Vector3.Lerp(
                transform.position, targetPos, Time.deltaTime * 8f);
        }

        // ── Calibración: la posición actual del celular = "recto" ─────────────

        public void CalibrateGyro()
        {
            Quaternion raw   = Input.gyro.attitude;
            _gyroRef         = new Quaternion(raw.x, raw.y, -raw.z, -raw.w);
            _gyroCalibrated  = true;
            Debug.Log("[ARShip] Giroscopio calibrado.");
        }
    }
}
