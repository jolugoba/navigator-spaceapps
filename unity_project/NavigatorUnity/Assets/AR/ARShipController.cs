// Navigator 2.0 — ARShipController.cs
// Piloting. The ship always flies toward where the camera looks; every input just moves that aim.
//   Phone:    tilt the phone (gyro) · on-screen joystick steers · ACELERAR / BOOST buttons
//             (or 1 finger anywhere = thrust, 2 fingers / double tap = boost)
//   Gamepad:  left stick steers · RT or A = thrust · LT or B = boost · LB / RB = barrel roll
//   Keyboard: W thrust · S brake · A/D turn · ↑/↓ or R/F climb · Space boost · Q/E barrel roll
// Guacalita Inc. — SAL / Claude

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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

        [Header("Controles en pantalla")]
        public VirtualJoystick Stick;
        public HoldButton      ThrustButton;
        public HoldButton      BoostButton;

        [Header("Sensibilidad")]
        public float TurnSharpness    = 3f;     // how fast the ship follows the aim
        public float TouchThrottle    = 1f;
        public float TapBoostDuration = 0.6f;
        public float RollSpeed        = 720f;   // degrees per second during a barrel roll

        // Only true while in flight — GameManager toggles it
        [HideInInspector] public bool ControlsEnabled;

        private Rigidbody  _rb;
        private float      _boostTimer;
        private float      _lastTapTime = -1f;
        private Quaternion _aim = Quaternion.identity;
        private float      _roll, _rollTarget;

        void Awake()
        {
            if (Ship == null) Ship = GetComponent<ShipController>();
            _rb = GetComponent<Rigidbody>();
            if (_rb != null) _rb.freezeRotation = true;   // rotation comes from the player's aim
            _aim = transform.rotation;
        }

        void Update()
        {
            if (Ship == null) return;
            if (!ControlsEnabled)
            {
                Ship.ThrustMain = 0f;
                Ship.Boosting   = false;
                _aim = transform.rotation;   // start the next flight from wherever the ship was placed
                _roll = _rollTarget = 0f;
                return;
            }
            HandleInput();
            _roll = Mathf.MoveTowards(_roll, _rollTarget, RollSpeed * Time.deltaTime);
            if (Mathf.Approximately(_roll, _rollTarget)) _roll = _rollTarget = 0f;
        }

        void FixedUpdate()
        {
            if (Ship == null || !ControlsEnabled) return;

            Quaternion look = Rig != null ? Rig.LookRotation
                            : ARCamera != null ? ARCamera.transform.rotation
                            : transform.rotation;
            float t = 1f - Mathf.Exp(-TurnSharpness * Time.fixedDeltaTime);
            _aim = Quaternion.Slerp(_aim, look, t);
            Quaternion next = _aim * Quaternion.Euler(0f, 0f, _roll);   // roll never changes the heading

            if (_rb != null) _rb.MoveRotation(next);
            else             transform.rotation = next;

            Ship.SteerPitch = 0f;
            Ship.SteerYaw   = 0f;
        }

        void HandleInput()
        {
            // ── Touch anywhere (not on UI) ────────────────────────────────────
            int  fingers  = 0;
            bool tapBegan = false;
            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (IsOverUI(touch.fingerId)) continue;
                fingers++;
                if (touch.phase == TouchPhase.Began) tapBegan = true;
            }
            if (tapBegan)
            {
                if (Time.time - _lastTapTime < 0.3f) _boostTimer = TapBoostDuration;
                _lastTapTime = Time.time;
            }
            _boostTimer = Mathf.Max(0f, _boostTimer - Time.deltaTime);

            bool thrust = fingers >= 1;
            bool boost  = fingers >= 2 || _boostTimer > 0f;
            bool brake  = false;
            Vector2 steer = Vector2.zero;

            // ── On-screen joystick and buttons ────────────────────────────────
            if (Stick != null)        steer  += Stick.Value;
            if (ThrustButton != null) thrust |= ThrustButton.Held;
            if (BoostButton != null)  boost  |= BoostButton.Held;

            // ── Gamepad (Bluetooth controllers on Android / iOS) ─────────────
            var pad = Gamepad.current;
            if (pad != null)
            {
                Vector2 ls = pad.leftStick.ReadValue();
                if (ls.sqrMagnitude > 0.02f) steer += ls;
                thrust |= pad.rightTrigger.ReadValue() > 0.2f || pad.buttonSouth.isPressed;
                boost  |= pad.leftTrigger.ReadValue()  > 0.2f || pad.buttonEast.isPressed;
                brake  |= pad.buttonWest.isPressed;
                if (pad.leftShoulder.wasPressedThisFrame)  BarrelRoll(+1);
                if (pad.rightShoulder.wasPressedThisFrame) BarrelRoll(-1);
            }

            // ── Keyboard (Editor / desktop) ───────────────────────────────────
            var kb = Keyboard.current;
            if (kb != null)
            {
                thrust |= kb.wKey.isPressed || (Mouse.current != null && Mouse.current.leftButton.isPressed && !IsOverUI(-1));
                boost  |= kb.spaceKey.isPressed;
                brake  |= kb.sKey.isPressed;
                if (kb.qKey.wasPressedThisFrame) BarrelRoll(+1);
                if (kb.eKey.wasPressedThisFrame) BarrelRoll(-1);
                // In gyro mode the rig ignores A/D and arrows, so route them here
                if (Rig != null && Rig.GyroOn)
                {
                    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  steer.x -= 1f;
                    if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) steer.x += 1f;
                    if (kb.upArrowKey.isPressed || kb.rKey.isPressed)    steer.y += 1f;
                    if (kb.downArrowKey.isPressed || kb.fKey.isPressed)  steer.y -= 1f;
                }
            }

            if (Rig != null && steer.sqrMagnitude > 0f) Rig.Steer(Vector2.ClampMagnitude(steer, 1f));

            Ship.ThrustMain = thrust || boost ? TouchThrottle : 0f;
            Ship.Boosting   = boost;

            // Brake: bleed off speed without spending fuel
            if (brake && _rb != null && !_rb.isKinematic)
                _rb.linearVelocity *= Mathf.Clamp01(1f - 2.5f * Time.deltaTime);
        }

        // Full 360° spin around the nose; purely a manoeuvre, the heading stays the same
        public void BarrelRoll(int direction)
        {
            if (!Mathf.Approximately(_roll, _rollTarget)) return;   // one roll at a time
            _rollTarget = _roll + 360f * Mathf.Sign(direction);
        }

        static bool IsOverUI(int pointerId) =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId);

        public void CalibrateGyro()
        {
            if (Rig != null) Rig.Calibrate(true);
        }
    }
}
