// Navigator 2.0 — ARShipController.cs
// Pilotaje AR: la nave gira hacia donde apuntas el celular (giroscopio vía GyroCameraRig)
//   1 dedo en pantalla  → empuje
//   2 dedos o doble toque → boost
// Editor: clic izquierdo / W = empuje, Espacio = boost, clic derecho + arrastrar = mirar
// Guacalita Inc. — SAL / Claude

using UnityEngine;
using UnityEngine.EventSystems;
using Navigator.Game;

namespace Navigator.AR
{
    public class ARShipController : MonoBehaviour
    {
        [Header("Nave a controlar")]
        public ShipController Ship;

        [Header("AR")]
        public Camera        ARCamera;
        public GyroCameraRig Rig;

        [Header("Sensibilidad")]
        public float TurnSharpness    = 3f;    // qué tan rápido la nave sigue tu mirada
        public float TouchThrottle    = 1f;
        public float TapBoostDuration = 0.6f;

        // Only true while in flight — GameManager toggles it
        [HideInInspector] public bool ControlsEnabled;

        private Rigidbody _rb;
        private float     _boostTimer;
        private float     _lastTapTime = -1f;

        void Awake()
        {
            if (Ship == null) Ship = GetComponent<ShipController>();
            _rb = GetComponent<Rigidbody>();
            if (_rb != null) _rb.freezeRotation = true;   // rotation comes from the player's aim
        }

        void Update()
        {
            if (Ship == null) return;
            if (!ControlsEnabled)
            {
                Ship.ThrustMain = 0f;
                Ship.Boosting   = false;
                return;
            }
            HandleInput();
        }

        void FixedUpdate()
        {
            if (Ship == null || !ControlsEnabled) return;

            Quaternion look = Rig != null ? Rig.LookRotation
                            : ARCamera != null ? ARCamera.transform.rotation
                            : transform.rotation;
            float t = 1f - Mathf.Exp(-TurnSharpness * Time.fixedDeltaTime);
            Quaternion next = Quaternion.Slerp(transform.rotation, look, t);

            if (_rb != null) _rb.MoveRotation(next);
            else             transform.rotation = next;

            Ship.SteerPitch = 0f;
            Ship.SteerYaw   = 0f;
        }

        void HandleInput()
        {
            int  fingers   = 0;
            bool tapBegan  = false;
            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (IsOverUI(touch.fingerId)) continue;
                fingers++;
                if (touch.phase == TouchPhase.Began) tapBegan = true;
            }

            bool mouseThrust = Application.isEditor &&
                               ((Input.GetMouseButton(0) && !IsOverUI(-1)) || Input.GetKey(KeyCode.W));
            bool keyBoost    = Application.isEditor && Input.GetKey(KeyCode.Space);

            if (tapBegan)
            {
                if (Time.time - _lastTapTime < 0.3f) _boostTimer = TapBoostDuration;
                _lastTapTime = Time.time;
            }
            _boostTimer = Mathf.Max(0f, _boostTimer - Time.deltaTime);

            bool thrusting = fingers >= 1 || mouseThrust;
            bool boosting  = fingers >= 2 || _boostTimer > 0f || keyBoost;

            Ship.ThrustMain = thrusting || boosting ? TouchThrottle : 0f;
            Ship.Boosting   = boosting;
        }

        static bool IsOverUI(int pointerId) =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId);

        public void CalibrateGyro()
        {
            if (Rig != null) Rig.Calibrate(true);
        }
    }
}
