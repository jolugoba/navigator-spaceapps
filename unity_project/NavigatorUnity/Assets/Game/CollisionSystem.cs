// Navigator 2.0 — CollisionSystem.cs
// Conservation of momentum: m₁v₁ + m₂v₂ = total
// Heavy ship (more fuel) wins the collision — the physics lesson NASA teaches
// Guacalita Inc. — SAL / Claude

using UnityEngine;

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

        private float      _invTimer;
        private Rigidbody  _rb;
        private BurnoutSystem _burnout;

        public Rigidbody ShipRb => _rb;

        void Awake()
        {
            _rb      = GetComponent<Rigidbody>();
            _burnout = GetComponent<BurnoutSystem>();
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
            if (IsInvincible) return;

            var other = col.gameObject.GetComponent<CollisionSystem>();
            if (other != null) ResolveShipCollision(other, col.relativeVelocity.magnitude);
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

            _rb.linearVelocity       = vf1;
            other._rb.linearVelocity = vf2;

            // Apply damage based on relative speed
            CollisionSeverity severity = relSpeed < 20f
                ? CollisionSeverity.Light
                : relSpeed < 50f
                    ? CollisionSeverity.Medium
                    : CollisionSeverity.Heavy;

            ApplyDamage(severity);
            other.ApplyDamage(severity);

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
            ExplosionVFX.Spawn(ExplosionPrefab, impactPoint, isTakedown);

            StartInvincibility();
        }

        // ── Damage model ──────────────────────────────────────────────────────
        public void ApplyDamage(CollisionSeverity severity)
        {
            switch (severity)
            {
                case CollisionSeverity.Light:
                    // No life lost — just speed reduction (handled by momentum)
                    break;
                case CollisionSeverity.Medium:
                    // Random instrument disabled — handled by ScienceOpsUI
                    FuelMassKg *= 0.90f;  // 10% fuel leak
                    break;
                case CollisionSeverity.Heavy:
                    Lives      = Mathf.Max(0, Lives - 1);
                    FuelMassKg *= 0.75f;  // 25% fuel leak
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
