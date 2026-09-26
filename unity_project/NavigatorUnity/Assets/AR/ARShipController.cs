// Navigator 2.0 — ARShipController.cs
// Controla la nave en el espacio AR con input táctil + giroscopio del celular
// Funciona en cualquier lugar (cuarto, sala, evento) — sin ARCore/ARKit
// Guacalita Inc. — SAL / Claude

using UnityEngine;
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
        public float TiltSensitivity   = 1.8f;
        public float TouchThrottle     = 0.9f;
        public float TapBoostDuration  = 0.3f;

        private Quaternion _gyroRef;
        private bool       _gyroCalibrated = false;
        private float      _boostTimer     = 0f;
        private int        _touchCount     = 0;
        private float      _lastTapTime    = 0f;
        private Vector3    _anchorOffset   = new(0f, 0f, 2f);

        void Start()
        {
            Input.gyro.enabled = true;
            CalibrateGyro();
        }

        void Update()
        {
            HandleGyroSteering();
            HandleTouchInput();
            AnchorShipToARCamera();
        }

        void HandleGyroSteering()
        {
            if (!_gyroCalibrated) return;

            Quaternion gyroRot = Input.gyro.attitude;
            Quaternion deviceRot = new Quaternion(gyroRot.x, gyroRot.y, -gyroRot.z, -gyroRot.w);
            Quaternion delta     = Quaternion.Inverse(_gyroRef) * deviceRot;

            float pitch = delta.eulerAngles.x;
            float yaw   = delta.eulerAngles.y;
            if (pitch > 180f) pitch -= 360f;
            if (yaw   > 180f) yaw   -= 360f;

            if (Ship != null)
            {
                Ship.SteerPitch = Mathf.Clamp(pitch / 90f * TiltSensitivity, -1f, 1f);
                Ship.SteerYaw   = Mathf.Clamp(yaw   / 90f * TiltSensitivity, -1f, 1f);
            }
        }

        void HandleTouchInput()
        {
            _touchCount = Input.touchCount;
            bool thrusting = _touchCount >= 1;
            bool boosting  = _touchCount >= 2 || _boostTimer > 0f;

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

        void AnchorShipToARCamera()
        {
            Camera cam = ARCamera != null ? ARCamera : Camera.main;
            if (cam == null || Ship == null) return;

            Vector3 targetPos = cam.transform.position
                              + cam.transform.TransformDirection(_anchorOffset);

            transform.position = Vector3.Lerp(
                transform.position, targetPos, Time.deltaTime * 8f);
        }

        public void CalibrateGyro()
        {
            Quaternion raw  = Input.gyro.attitude;
            _gyroRef        = new Quaternion(raw.x, raw.y, -raw.z, -raw.w);
            _gyroCalibrated = true;
            Debug.Log("[ARShip] Giroscopio calibrado.");
        }
    }
}
