// Navigator 2.0 — ShipController.cs
// Ship movement + Burnout speed multiplier + fuel drain
// Attach to ship prefab alongside Rigidbody
// Guacalita Inc. — SAL / Claude

using UnityEngine;
using Navigator.Core;
using Navigator.Network;

namespace Navigator.Game
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BurnoutSystem))]
    [RequireComponent(typeof(CollisionSystem))]
    public class ShipController : MonoBehaviour
    {
        [Header("Config")]
        public string  NodeId;
        public string  DisplayName;
        public bool    IsLocalPlayer = true;

        [Header("Physics")]
        public PhysicsEngine  Physics;
        public float          BaseMaxSpeed   = 80f;
        public MissionDesign  Mission;

        [Tooltip("Arcade drag: the ship slows down unless you keep thrusting")]
        public float          LinearDamping  = 0.5f;

        [Header("Steering")]
        public float YawSpeed   = 60f;
        public float PitchSpeed = 45f;

        // ── Runtime input (set by UI / gyroscope) ─────────────────────────────
        [HideInInspector] public float  ThrustMain;
        [HideInInspector] public float  SteerYaw;
        [HideInInspector] public float  SteerPitch;
        [HideInInspector] public bool   Boosting;

        // ── Components ────────────────────────────────────────────────────────
        private Rigidbody       _rb;
        private BurnoutSystem   _burnout;
        private CollisionSystem _collision;
        private ShipSyncService _sync;
        private FuelSystem      _fuelSys;
        private float           _fuelFraction = 1f;

        void Awake()
        {
            _rb        = GetComponent<Rigidbody>();
            _burnout   = GetComponent<BurnoutSystem>();
            _collision = GetComponent<CollisionSystem>();
            _fuelSys   = GetComponent<FuelSystem>();
            _sync      = FindObjectOfType<ShipSyncService>();

            _rb.useGravity = false;
            _rb.linearDamping = LinearDamping;

            if (Physics != null)
            {
                Physics.ShipRb = _rb;
            }
        }

        void FixedUpdate()
        {
            if (!IsLocalPlayer) return;
            if (_collision.Lives <= 0) return;

            float dt = Time.fixedDeltaTime;

            bool hasFuel = _fuelSys == null || !_fuelSys.IsEmpty;
            float thrust = hasFuel ? ThrustMain : 0f;
            bool  boost  = hasFuel && (Boosting || _burnout.BurnoutActive);

            // Steering
            transform.Rotate(Vector3.up,    SteerYaw   * YawSpeed   * dt, Space.Self);
            transform.Rotate(Vector3.right, SteerPitch * PitchSpeed * dt, Space.Self);

            // Feed inputs to physics engine
            if (Physics != null)
            {
                Physics.ThrustMain     = thrust;
                Physics.Boosting       = boost;
                Physics.FuelFraction   = _fuelFraction;
                Physics.Tick(dt);
                _fuelFraction = Physics.FuelFraction;
            }

            // FuelSystem is the source of truth for fuel (HUD, events, score)
            if (_fuelSys != null)
            {
                _fuelSys.Drain(thrust, boost, dt);
                _fuelFraction = _fuelSys.FuelFraction;
            }

            // Speed cap (Burnout mode = 2.5x)
            float maxSpeed = BaseMaxSpeed * (_burnout.BurnoutActive ? _burnout.SpeedMultiplier
                                           : boost ? 1.5f : 1f);
            if (Time.time < _slingUntil) maxSpeed *= SlingshotSpeedMult;   // gravity assist in progress
            if (_rb.linearVelocity.magnitude > maxSpeed)
                _rb.linearVelocity = _rb.linearVelocity.normalized * maxSpeed;

            // Low-fuel fills Burnout bar
            if (_fuelFraction < 0.05f)
                _burnout.RegisterLowFuel(dt);

            // Boost fills Burnout bar
            if (boost && !_burnout.BurnoutActive)
                _burnout.RegisterBoostFlight(dt);

            // Sync state to other players
            if (_sync != null)
            {
                var state = ShipStateNet.FromTransform(
                    NodeId, DisplayName, transform, _rb,
                    thrust, boost, _burnout.BurnoutActive,
                    Mission?.TargetPlanetId ?? "mars", 0f,
                    _collision.Lives, _fuelFraction, 0);
                _sync.Broadcast(state);
            }
        }

        // ── Remote ships: apply received state ───────────────────────────────
        // ── Gravity assist: a planet's pull swings the ship and hands it free speed ──
        public const float SlingshotSpeedMult = 1.6f;
        private float _slingUntil;

        public void Slingshot(float deltaV, float seconds)
        {
            if (_rb == null || _rb.isKinematic) return;
            Vector3 dir = _rb.linearVelocity.sqrMagnitude > 1f ? _rb.linearVelocity.normalized : transform.forward;
            _rb.linearVelocity += dir * deltaV;
            _slingUntil = Time.time + seconds;
        }

        public void ApplyRemoteState(ShipStateNet state)
        {
            if (IsLocalPlayer) return;
            transform.position    = state.Position;
            transform.eulerAngles = new Vector3(state.Pitch, state.Yaw, state.Roll);
            if (_rb != null) _rb.linearVelocity = state.Velocity;
        }
    }
}
