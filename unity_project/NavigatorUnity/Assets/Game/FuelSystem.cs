// Navigator 2.0 — FuelSystem.cs
// Gestión de combustible en vuelo — drain rates por tipo de motor
// Ported from Dart: navigator/lib/core/components.dart (ShipFuel)
// Guacalita Inc. — SAL / Claude

using UnityEngine;
using UnityEngine.Events;
using Navigator.Core;

namespace Navigator.Game
{
    public class FuelSystem : MonoBehaviour
    {
        [Header("Config (set from MissionDesign)")]
        public float MaxFuelKg      = 1200f;
        public EngineType Engine    = EngineType.Chemical;

        [Header("State")]
        [Range(0f, 1f)] public float FuelFraction = 1f;

        // ── Events ─────────────────────────────────────────────────────────────
        public UnityEvent        OnFuelEmpty    = new();
        public UnityEvent<float> OnFuelChanged  = new();  // fraction 0-1
        public UnityEvent        OnLowFuelWarning = new(); // < 15%

        // ── Drain rates per engine (fraction/second at full thrust) ───────────
        // Ion uses way less fuel, chemical burns fast, nuclear moderate
        private static float DrainRate(EngineType engine) => engine switch
        {
            EngineType.Chemical => 0.00025f,
            EngineType.Ion      => 0.00004f,   // 6x more efficient
            EngineType.Nuclear  => 0.00012f,
            _                   => 0.00025f,
        };

        private bool _lowFuelWarned = false;
        private bool _empty         = false;

        public float FuelKg      => FuelFraction * MaxFuelKg;
        public bool  IsEmpty     => FuelFraction <= 0f;
        public bool  IsLow       => FuelFraction < 0.15f;
        public bool  IsCritical  => FuelFraction < 0.05f;

        // ── Init from MissionDesign ───────────────────────────────────────────

        public void SetMissionFuel(float gameMaxFuel)
        {
            MaxFuelKg      = gameMaxFuel;
            FuelFraction   = 1f;
            _empty         = false;
            _lowFuelWarned = false;
            OnFuelChanged.Invoke(FuelFraction);
        }

        public void InitFromDesign(MissionDesign design)
        {
            MaxFuelKg    = design.FuelMassKg;
            Engine       = design.Engine;
            FuelFraction = 1f;
            _empty       = false;
            _lowFuelWarned = false;
        }

        // ── Drain (call from PhysicsEngine or ShipController each FixedUpdate)

        public void Drain(float thrustMain, bool boosting, float dt)
        {
            if (_empty) return;

            float rate  = DrainRate(Engine);
            float usage = thrustMain * rate * (boosting ? 2.2f : 1f) * dt;
            FuelFraction = Mathf.Max(0f, FuelFraction - usage);

            OnFuelChanged.Invoke(FuelFraction);

            if (!_lowFuelWarned && IsLow)
            {
                _lowFuelWarned = true;
                OnLowFuelWarning.Invoke();
            }

            if (!_empty && IsEmpty)
            {
                _empty = true;
                OnFuelEmpty.Invoke();
                Debug.Log("[Fuel] ¡Sin combustible!");
            }
        }

        // ── Recargar desde POD de combustible (+15%) ──────────────────────────

        public void Refuel(float fraction = 0.15f)
        {
            FuelFraction = Mathf.Clamp01(FuelFraction + fraction);
            if (FuelFraction > 0f) _empty = false;
            OnFuelChanged.Invoke(FuelFraction);
            Debug.Log($"[Fuel] Recarga +{fraction:P0} → {FuelFraction:P0}");
        }

        // ── Fuel leak from collision ──────────────────────────────────────────

        public void Leak(float fraction)
        {
            FuelFraction = Mathf.Max(0f, FuelFraction - fraction);
            OnFuelChanged.Invoke(FuelFraction);
        }
    }
}
