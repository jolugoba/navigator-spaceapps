// Navigator 2.0 — WorldControls.cs
// Input and camera for the worlds. Same controls as the flight: LEFT stick moves, RIGHT stick
// looks, three round buttons A (big), B, C. Keyboard and gamepad work too:
//   keyboard  WASD move · arrows look · Space = A · E = B · Q = C
//   gamepad   left stick move · right stick look · South/RT = A · East/LT = B · North/West = C
// Guacalita Inc. — SAL / Claude

using UnityEngine;
using UnityEngine.InputSystem;
using Navigator.AR;

namespace Navigator.Worlds
{
    public class WorldInput
    {
        public VirtualJoystick Move, Look;
        public HoldButton A, B, C;

        public Vector2 MoveV
        {
            get
            {
                Vector2 v = Move != null ? Move.Value : Vector2.zero;
                var kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.wKey.isPressed) v.y += 1f;
                    if (kb.sKey.isPressed) v.y -= 1f;
                    if (kb.aKey.isPressed) v.x -= 1f;
                    if (kb.dKey.isPressed) v.x += 1f;
                }
                var pad = Gamepad.current;
                if (pad != null) v += pad.leftStick.ReadValue();
                return Vector2.ClampMagnitude(v, 1f);
            }
        }

        public Vector2 LookV
        {
            get
            {
                Vector2 v = Look != null ? Look.Value : Vector2.zero;
                var kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.upArrowKey.isPressed)    v.y += 1f;
                    if (kb.downArrowKey.isPressed)  v.y -= 1f;
                    if (kb.leftArrowKey.isPressed)  v.x -= 1f;
                    if (kb.rightArrowKey.isPressed) v.x += 1f;
                }
                var pad = Gamepad.current;
                if (pad != null) v += pad.rightStick.ReadValue();
                return Vector2.ClampMagnitude(v, 1f);
            }
        }

        public bool AHeld => (A != null && A.Held) || Key(k => k.spaceKey) || Pad(p => p.buttonSouth.isPressed || p.rightTrigger.ReadValue() > 0.3f);
        public bool BHeld => (B != null && B.Held) || Key(k => k.eKey) || Pad(p => p.buttonEast.isPressed || p.leftTrigger.ReadValue() > 0.3f);
        public bool CHeld => (C != null && C.Held) || Key(k => k.qKey) || Pad(p => p.buttonNorth.isPressed || p.buttonWest.isPressed);

        public bool APressed => (A != null && A.PressedThisFrame) || KeyDown(k => k.spaceKey) || Pad(p => p.buttonSouth.wasPressedThisFrame);
        public bool BPressed => (B != null && B.PressedThisFrame) || KeyDown(k => k.eKey) || Pad(p => p.buttonEast.wasPressedThisFrame);
        public bool CPressed => (C != null && C.PressedThisFrame) || KeyDown(k => k.qKey) || Pad(p => p.buttonNorth.wasPressedThisFrame || p.buttonWest.wasPressedThisFrame);

        static bool Key(System.Func<Keyboard, UnityEngine.InputSystem.Controls.KeyControl> k)
        {
            var kb = Keyboard.current;
            return kb != null && k(kb).isPressed;
        }

        static bool KeyDown(System.Func<Keyboard, UnityEngine.InputSystem.Controls.KeyControl> k)
        {
            var kb = Keyboard.current;
            return kb != null && k(kb).wasPressedThisFrame;
        }

        static bool Pad(System.Func<Gamepad, bool> f)
        {
            var pad = Gamepad.current;
            return pad != null && f(pad);
        }
    }

    // Third-person camera: orbits the vehicle with the look stick; when you stop looking it
    // swings back behind the vehicle. Never goes under the ground.
    public class WorldCamera : MonoBehaviour
    {
        public Transform    Target;
        public WorldTerrain Terrain;
        public float Distance = 9f, Height = 2.5f, LookAtHeight = 1f;
        public float Yaw, Pitch = 14f;
        public bool  AutoFollow = true;
        public float MinPitch = -20f, MaxPitch = 75f;
        public float Fov = 60f;

        float _idle, _shake;
        Camera _cam;

        public void Look(Vector2 v, float dt)
        {
            if (v.sqrMagnitude > 0.01f) _idle = 0f;
            Yaw   += v.x * 140f * dt;
            Pitch  = Mathf.Clamp(Pitch - v.y * 90f * dt, MinPitch, MaxPitch);
        }

        public void Shake(float amount) => _shake = Mathf.Max(_shake, amount);

        // A quick widening of the view: impacts, jumps, the big moments
        public void Punch(float degrees) => _punch = Mathf.Max(_punch, degrees);
        float _punch;

        public void SnapBehind()
        {
            if (Target == null) return;
            Yaw = Target.eulerAngles.y;
            Place(1f);
        }

        void LateUpdate()
        {
            if (Target == null) return;
            _idle += Time.deltaTime;
            if (AutoFollow && _idle > 1.4f)
                Yaw = Mathf.LerpAngle(Yaw, Target.eulerAngles.y, 1f - Mathf.Exp(-1.6f * Time.deltaTime));
            Place(1f - Mathf.Exp(-10f * Time.deltaTime));
        }

        void Place(float t)
        {
            if (_cam == null) _cam = GetComponent<Camera>();
            _punch = Mathf.MoveTowards(_punch, 0f, Time.unscaledDeltaTime * 25f);
            if (_cam != null) _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, Fov + _punch, 0.2f);
            var rot = Quaternion.Euler(Pitch, Yaw, 0f);
            Vector3 focus = Target.position + Vector3.up * LookAtHeight;
            Vector3 want = focus + Vector3.up * (Height - LookAtHeight) - rot * Vector3.forward * Distance;
            if (Terrain != null)
            {
                float ground = Terrain.Height(want) + 1.2f;
                if (want.y < ground) want.y = ground;
            }
            transform.position = Vector3.Lerp(transform.position, want, t);
            if (_shake > 0.001f)
            {
                transform.position += Random.insideUnitSphere * _shake;
                _shake = Mathf.MoveTowards(_shake, 0f, Time.deltaTime * 1.5f);
            }
            transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
        }
    }
}
