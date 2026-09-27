// Navigator 2.0 — GyroCameraRig.cs
// AR de 3 grados de libertad: la cámara virtual gira igual que el celular (giroscopio)
// y persigue a la nave. Con el video de la cámara de fondo, el espacio se "pega" a tu cuarto.
// En el Editor (sin giroscopio): clic derecho + arrastrar para mirar.
// Guacalita Inc. — SAL / Claude

using UnityEngine;
using Navigator.Game;

namespace Navigator.AR
{
    [RequireComponent(typeof(Camera))]
    public class GyroCameraRig : MonoBehaviour
    {
        [Header("Nave a seguir")]
        public Transform Target;

        [Header("Cámara de persecución")]
        public float ChaseDistance   = 7f;
        public float ChaseHeight     = 1.6f;
        public float FollowSharpness = 8f;

        [Header("Sensación de velocidad (campo de visión)")]
        public float BaseFov    = 60f;
        public float SpeedFov   = 8f;    // extra at top speed
        public float BoostFov   = 6f;
        public float BurnoutFov = 12f;

        [Header("Editor / sin giroscopio")]
        public float MouseSensitivity = 3f;

        [Header("Joystick / control")]
        public float StickTurnRate = 90f;   // degrees per second at full deflection

        const string GyroPref = "navigator.gyro";
        public bool GyroAvailable => DeviceGyro.Supported;
        public bool GyroOn => _useGyro;

        private Vector2 _steer;
        private float   _stickYaw, _stickPitch;

        // Called every frame by whoever reads the stick/keys; x = turn, y = climb (-1..1)
        public void Steer(Vector2 input) => _steer += input;

        // Players without room to move (or with a gamepad) can switch the gyro off
        public void SetGyro(bool on)
        {
            _useGyro = on && GyroAvailable;
            PlayerPrefs.SetInt(GyroPref, on ? 1 : 0);
            if (_useGyro) DeviceGyro.Enable();
            Calibrate(false);
        }

        // Where the player is looking — the ship steers toward this
        public Quaternion LookRotation { get; private set; } = Quaternion.identity;

        private Quaternion _yawOffset = Quaternion.identity;
        private float _shake;

        // Impact feedback: amount in game units, decays in ~0.4 s
        public void Shake(float amount) { if (Navigator.Core.FxPrefs.Motion) _shake = Mathf.Max(_shake, amount); }
        private float _mouseYaw, _mousePitch;
        private bool  _useGyro;

        void Start()
        {
            _useGyro = DeviceGyro.Supported && PlayerPrefs.GetInt(GyroPref, 1) == 1;
            if (_useGyro) DeviceGyro.Enable();
            Calibrate(false);
        }

        // Makes the current phone heading "forward" (+Z). With alignToNorth and a live
        // compass, +Z is true north instead, so NASA Horizons azimuths line up with the sky.
        public void Calibrate(bool alignToNorth)
        {
            _stickYaw = _stickPitch = 0f;
            if (!_useGyro)
            {
                _mouseYaw = _mousePitch = 0f;
                return;
            }
            // Browser: the first reading may not have arrived yet; calibrate when it does
            _calibrated = DeviceGyro.HasData;
            float currentYaw = DeviceRotation().eulerAngles.y;
            float targetYaw  = 0f;
            // Compass: heading of the camera from north (clockwise, same as Unity yaw)
            if (alignToNorth && DeviceLocation.TryHeading(ToUnity, out float heading))
                targetYaw = heading;
            _yawOffset = Quaternion.Euler(0f, targetYaw - currentYaw, 0f);
        }

        private bool _calibrated;

        // True once a compass reading exists (in a browser it can arrive after the first tap)
        public bool HasCompass => _useGyro && DeviceLocation.TryHeading(ToUnity, out _);

        void LateUpdate()
        {
            if (_useGyro && !_calibrated && DeviceGyro.HasData) Calibrate(false);
            float dt = Time.deltaTime;
            _stickYaw  += Mathf.Clamp(_steer.x, -1f, 1f) * StickTurnRate * dt;
            _stickPitch = Mathf.Clamp(_stickPitch - Mathf.Clamp(_steer.y, -1f, 1f) * StickTurnRate * 0.8f * dt, -80f, 80f);
            _steer = Vector2.zero;

            LookRotation = _useGyro
                ? Quaternion.Euler(0f, _stickYaw, 0f) * _yawOffset * DeviceRotation() * Quaternion.Euler(_stickPitch, 0f, 0f)
                : MouseRotation();
            transform.rotation = LookRotation;

            if (Target == null) return;
            UpdateFov();
            Vector3 desired = Target.position + LookRotation * new Vector3(0f, ChaseHeight, -ChaseDistance);
            float t = 1f - Mathf.Exp(-FollowSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, t);

            if (_shake > 0.001f)
            {
                transform.position += Random.insideUnitSphere * _shake;
                _shake = Mathf.MoveTowards(_shake, 0f, 2.5f * Time.deltaTime);
            }
        }

        private Camera         _cam;
        private Rigidbody      _targetRb;
        private ShipController _targetShip;
        private BurnoutSystem  _targetBurnout;

        void UpdateFov()
        {
            if (_cam == null) _cam = GetComponent<Camera>();
            if (_targetRb == null)
            {
                _targetRb      = Target.GetComponent<Rigidbody>();
                _targetShip    = Target.GetComponent<ShipController>();
                _targetBurnout = Target.GetComponent<BurnoutSystem>();
            }
            float speed = _targetRb != null && !_targetRb.isKinematic ? _targetRb.linearVelocity.magnitude : 0f;
            float fov = BaseFov + SpeedFov * Mathf.Clamp01(speed / 80f);
            if (_targetBurnout != null && _targetBurnout.BurnoutActive) fov += BurnoutFov;
            else if (_targetShip != null && _targetShip.Boosting)       fov += BoostFov;
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, fov, 1f - Mathf.Exp(-4f * Time.deltaTime));
        }

        // Unity-space orientation of a portrait phone from the gyro attitude
        static Quaternion DeviceRotation() => ToUnity(DeviceGyro.Attitude);

        static Quaternion ToUnity(Quaternion q) =>
            Quaternion.Euler(90f, 0f, 0f) * new Quaternion(q.x, q.y, -q.z, -q.w);

        Quaternion MouseRotation()
        {
            if (Input.GetMouseButton(1))
            {
                _mouseYaw   += Input.GetAxis("Mouse X") * MouseSensitivity;
                _mousePitch -= Input.GetAxis("Mouse Y") * MouseSensitivity;
                _mousePitch  = Mathf.Clamp(_mousePitch, -85f, 85f);
            }
            // A/D or ←/→ turn; ↑/↓ or R/F pitch (W/S are thrust and brake)
            _mouseYaw += Input.GetAxis("Horizontal") * 90f * Time.deltaTime;
            float pitch = 0f;
            if (Input.GetKey(KeyCode.UpArrow)   || Input.GetKey(KeyCode.R)) pitch -= 1f;
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.F)) pitch += 1f;
            _mousePitch = Mathf.Clamp(_mousePitch + pitch * 70f * Time.deltaTime, -85f, 85f);
            return Quaternion.Euler(Mathf.Clamp(_mousePitch + _stickPitch, -85f, 85f), _mouseYaw + _stickYaw, 0f);
        }
    }
}
