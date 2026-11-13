// Navigator 2.0 — CollisionSystem.cs
// Conservation of momentum: m₁v₁ + m₂v₂ = total
// Heavy ship (more fuel) wins the collision — the physics lesson NASA teaches
// Guacalita Inc. — SAL / Claude

using UnityEngine;
using UnityEngine.Events;
using Navigator.Network;

namespace Navigator.Game
{
    [RequireComponent(typeof(Rigidbody))]
    public class CollisionSystem : MonoBehaviour
    {
        public float  FuelMassKg        = 1200f;  // set from MissionDesign
        public float  DryMassKg         = 500f;   // set from MissionDesign
        public int    Lives             = 3;
        public float  InvincibilityTime = 3f;

        [Header("VFX")]
        public GameObject ExplosionPrefab;   // asignar en Inspector

        public bool IsInvincible { get; private set; }

        [HideInInspector] public string RemoteNodeId;   // set on rival proxies by RemoteShipManager

        // HUD and camera shake listen to these (local ship only)
        public UnityEvent<CollisionSeverity> OnDamaged = new();
        public UnityEvent                    OnBlocked = new();   // shield / Burnout absorbed it

        private float      _invTimer;
        private Rigidbody  _rb;
        private BurnoutSystem _burnout;
        private FuelSystem    _fuel;

        public Rigidbody ShipRb => _rb;

        void Awake()
        {
            _rb      = GetComponent<Rigidbody>();
            _burnout = GetComponent<BurnoutSystem>();
            _fuel    = GetComponent<FuelSystem>();
        }

        void Update()
        {
            if (IsInvincible)
            {
                _invTimer -= Time.deltaTime;
                if (_invTimer <= 0f) IsInvincible = false;
            }
        }

        // Called by trigger/collision events
        void OnCollisionEnter(Collision col)
        {
            // The remote proxy is kinematic; only the local ship resolves the hit
            if (IsInvincible || _rb.isKinematic) return;

            var other = col.gameObject.GetComponent<CollisionSystem>();
            if (other == null) return;

            // Both phones see the same crash; only the lower NodeId resolves it so damage isn't doubled
            if (other._rb.isKinematic && !IsAuthorityOver(other.RemoteNodeId)) return;
            ResolveShipCollision(other, col.relativeVelocity.magnitude);
        }

        // ── Resolve ship-to-ship collision ────────────────────────────────────
        // Conservation of linear momentum: m₁v₁ + m₂v₂ = (m₁+m₂)vf
        public void ResolveShipCollision(CollisionSystem other, float relSpeed)
        {
            float m1 = WetMass;
            float m2 = other.WetMass;
            Vector3 v1 = _rb.linearVelocity;
            Vector3 v2 = other._rb.linearVelocity;

            // Elastic collision result for each body
            Vector3 vf1 = ((m1 - m2) * v1 + 2f * m2 * v2) / (m1 + m2);
            Vector3 vf2 = ((m2 - m1) * v2 + 2f * m1 * v1) / (m1 + m2);

            _rb.linearVelocity = vf1;
            if (!other._rb.isKinematic) other._rb.linearVelocity = vf2;

            // Apply damage based on relative speed
            CollisionSeverity severity = relSpeed < 20f
                ? CollisionSeverity.Light
                : relSpeed < 50f
                    ? CollisionSeverity.Medium
                    : CollisionSeverity.Heavy;

            // Burnout mode = invincible
            if (_burnout == null || !_burnout.BurnoutActive) ApplyDamage(severity);
            else OnBlocked.Invoke();

            // The rival simulates their own ship: tell them they were hit and how they bounce
            if (other._rb.isKinematic) SendHit(other.RemoteNodeId, severity, vf2);
            else other.ApplyDamage(severity);

            // Determine takedown — faster lighter ship vs heavier slower = takedown
            bool isTakedown = false;
            if (severity == CollisionSeverity.Heavy)
            {
                isTakedown = m1 > m2 * 2f;
                if (isTakedown)
                {
                    TakedownType type = DetermineTakedownType(v1, v2);
                    _burnout?.RegisterTakedown(type);
                }
            }

            // Explosión VFX en el punto de impacto
            Vector3 impactPoint = (ShipRb.position + other.ShipRb.position) * 0.5f;
            // A light bump only plays the hit sound (the HUD); explosions are for real damage and takedowns
            if (isTakedown || severity != CollisionSeverity.Light) ExplosionVFX.Spawn(ExplosionPrefab, impactPoint, isTakedown);

            StartInvincibility();
        }

        static bool IsAuthorityOver(string rivalId)
        {
            var senda = FindObjectOfType<SendaBridge>();
            if (senda == null || string.IsNullOrEmpty(senda.NodeId) || string.IsNullOrEmpty(rivalId)) return true;
            return string.CompareOrdinal(senda.NodeId, rivalId) < 0;
        }

        // Addressed to the rammed ship only ("to"): everyone receives broadcasts
        void SendHit(string targetNodeId, CollisionSeverity severity, Vector3 newVelocity)
        {
            var senda = FindObjectOfType<SendaBridge>();
            if (senda == null || !senda.Initialized) return;
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            senda.Broadcast(NavMsg.Build(NavMsgType.shipEvent, senda.NodeId,
                "{\"hit\":" + (int)severity +
                ",\"to\":\"" + targetNodeId + "\"" +
                ",\"vx\":" + newVelocity.x.ToString("F2", ci) +
                ",\"vy\":" + newVelocity.y.ToString("F2", ci) +
                ",\"vz\":" + newVelocity.z.ToString("F2", ci) +
                ",\"name\":\"SYSTEM\",\"text\":\"HIT\"}"));
        }

        // Rival rammed us (applied on the victim's own device)
        public void ReceiveHit(CollisionSeverity severity, Vector3 newVelocity)
        {
            if (_rb.isKinematic) return;
            if (IsInvincible || (_burnout != null && _burnout.BurnoutActive)) { OnBlocked.Invoke(); return; }
            ExplosionVFX.Spawn(ExplosionPrefab, _rb.position, false);
            _rb.linearVelocity = newVelocity;
            ApplyDamage(severity);
            StartInvincibility();
        }

        // A rival's laser hit us: like a heavy crash (one hull point), unless shielded
        public bool ReceiveLaser()
        {
            if (_rb == null || _rb.isKinematic) return false;
            if (IsInvincible || (_burnout != null && _burnout.BurnoutActive)) { OnBlocked.Invoke(); return false; }
            ExplosionVFX.Spawn(ExplosionPrefab, _rb.position, false);
            ApplyDamage(CollisionSeverity.Heavy);
            StartInvincibility();
            return true;
        }

        // ── Damage model ──────────────────────────────────────────────────────
        public void ApplyDamage(CollisionSeverity severity)
        {
            OnDamaged.Invoke(severity);
            switch (severity)
            {
                case CollisionSeverity.Light:
                    // No life lost — just speed reduction (handled by momentum)
                    break;
                case CollisionSeverity.Medium:
                    // Random instrument disabled — handled by ScienceOpsUI
                    FuelMassKg *= 0.90f;  // 10% fuel leak
                    _fuel?.Leak(0.10f);
                    break;
                case CollisionSeverity.Heavy:
                    Lives      = Mathf.Max(0, Lives - 1);
                    FuelMassKg *= 0.75f;  // 25% fuel leak
                    _fuel?.Leak(0.25f);
                    break;
            }
        }

        public void StartInvincibility()
        {
            IsInvincible = true;
            _invTimer    = InvincibilityTime;
        }

        // ── Powerup: escudo temporal (ignora colisiones N segundos) ───────────
        public void ActivateShield(float seconds)
        {
            IsInvincible = true;
            _invTimer    = Mathf.Max(_invTimer, seconds);
            Debug.Log($"[Shield] Escudo activado {seconds}s");
        }

        // ── Ship state ────────────────────────────────────────────────────────
        public float WetMass => DryMassKg + FuelMassKg;

        // Damage percentage tier for visual feedback
        public DamageState DamageLevel => Lives switch
        {
            3 => DamageState.Perfect,
            2 => DamageState.Damaged,
            1 => DamageState.Critical,
            _ => DamageState.Destroyed,
        };

        private TakedownType DetermineTakedownType(Vector3 v1, Vector3 v2)
        {
            float dot = Vector3.Dot(v1.normalized, v2.normalized);
            if (dot < -0.7f) return TakedownType.Frontal;
            if (dot >  0.7f) return TakedownType.Trasero;
            return TakedownType.Lateral;
        }
    }

    public enum CollisionSeverity { Light, Medium, Heavy }

    public enum DamageState
    {
        Perfect,    // 100% — full performance
        Damaged,    // ~60% — speed -20%, 1 instrument off
        Critical,   // ~30% — speed -40%, sparks
        Destroyed   //   0% — end of race
    }
}
