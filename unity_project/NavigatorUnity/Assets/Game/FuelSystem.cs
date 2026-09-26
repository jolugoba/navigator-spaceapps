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

        // ── Burn time from the design's ΔV margin ─────────────────────────────
        // Exactly the minimum ΔV = BaseBurnTimeS of full thrust; double the ΔV = double the time
        [Header("Tiempo de empuje")]
        public float BaseBurnTimeS = 45f;
        public float BurnTimeS { get; private set; } = 45f;

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
            // Margin over the whole mission profile (cruise + insertion + descent)
            BurnTimeS    = BaseBurnTimeS * Mathf.Clamp(design.DeltaVms / design.RequiredDeltaV, 0.25f, 4f);
            FuelFraction = 1f;
            _empty       = false;
            _lowFuelWarned = false;
        }

        // ── Drain (call from PhysicsEngine or ShipController each FixedUpdate)

        public void Drain(float thrustMain, bool boosting, float dt)
        {
            if (_empty) return;

            float usage = thrustMain / Mathf.Max(1f, BurnTimeS) * (boosting ? 2.2f : 1f) * dt;
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
