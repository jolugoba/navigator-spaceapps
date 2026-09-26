// Navigator 2.0 — GyroCameraRig.cs
// AR de 3 grados de libertad: la cámara virtual gira igual que el celular (giroscopio)
// y persigue a la nave. Con el video de la cámara de fondo, el espacio se "pega" a tu cuarto.
// En el Editor (sin giroscopio): clic derecho + arrastrar para mirar.
// Guacalita Inc. — SAL / Claude

using UnityEngine;

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

        [Header("Editor / sin giroscopio")]
        public float MouseSensitivity = 3f;

        // Where the player is looking — the ship steers toward this
        public Quaternion LookRotation { get; private set; } = Quaternion.identity;

        private Quaternion _yawOffset = Quaternion.identity;
        private float _shake;

        // Impact feedback: amount in game units, decays in ~0.4 s
        public void Shake(float amount) => _shake = Mathf.Max(_shake, amount);
        private float _mouseYaw, _mousePitch;
        private bool  _useGyro;

        void Start()
        {
            _useGyro = SystemInfo.supportsGyroscope;
            if (_useGyro) Input.gyro.enabled = true;
            Calibrate(false);
        }

        // Makes the current phone heading "forward" (+Z). With alignToNorth and a live
        // compass, +Z is true north instead, so NASA Horizons azimuths line up with the sky.
        public void Calibrate(bool alignToNorth)
        {
            if (!_useGyro)
            {
                _mouseYaw = _mousePitch = 0f;
                return;
            }
            float currentYaw = DeviceRotation().eulerAngles.y;
            float targetYaw  = 0f;
            if (alignToNorth && Input.compass.enabled && Input.compass.timestamp > 0)
                targetYaw = Input.compass.trueHeading;   // clockwise from north, same as Unity yaw
            _yawOffset = Quaternion.Euler(0f, targetYaw - currentYaw, 0f);
        }

        void LateUpdate()
        {
            LookRotation = _useGyro ? _yawOffset * DeviceRotation() : MouseRotation();
            transform.rotation = LookRotation;

            if (Target == null) return;
            Vector3 desired = Target.position + LookRotation * new Vector3(0f, ChaseHeight, -ChaseDistance);
            float t = 1f - Mathf.Exp(-FollowSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, t);

            if (_shake > 0.001f)
            {
                transform.position += Random.insideUnitSphere * _shake;
                _shake = Mathf.MoveTowards(_shake, 0f, 2.5f * Time.deltaTime);
            }
        }

        // Unity-space orientation of a portrait phone from the gyro attitude
        static Quaternion DeviceRotation()
        {
            Quaternion q = Input.gyro.attitude;
            return Quaternion.Euler(90f, 0f, 0f) * new Quaternion(q.x, q.y, -q.z, -q.w);
        }

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
            return Quaternion.Euler(_mousePitch, _mouseYaw, 0f);
        }
    }
}
