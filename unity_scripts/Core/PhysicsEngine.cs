// Navigator 2.0 — PhysicsEngine.cs
// Ported from Dart: navigator/lib/flight/physics_engine.dart + systems.dart
// Tsiolkovsky rocket equation + planetary gravity F=μ/r²
// Guacalita Inc. — SAL / Claude

using System.Collections.Generic;
using UnityEngine;

namespace Navigator.Core
{
    public class PhysicsEngine : MonoBehaviour
    {
        // ── Engine thrust multipliers ──────────────────────────────────────────
        public const float ThrustChemical = 1.0f;
        public const float ThrustIon      = 0.55f;
        public const float ThrustNuclear  = 1.4f;

        // ── Planetary gravity μ (game units — scaled 1:1000u) ─────────────────
        public static readonly Dictionary<string, float> PlanetMu = new()
        {
            { "mercury", 190000f  },
            { "venus",   455000f  },
            { "mars",    190000f  },
            { "jupiter", 1265000f },
            { "saturn",  535000f  },
            { "uranus",  445000f  },
            { "neptune", 570000f  },
        };

        // ── Ship state (set per-frame by ShipController) ───────────────────────
        [HideInInspector] public Rigidbody ShipRb;
        [HideInInspector] public float     ThrustMain;      // 0–1
        [HideInInspector] public bool      Boosting;
        [HideInInspector] public float     FuelFraction;    // 0–1
        [HideInInspector] public float     ThrustMultiplier = ThrustChemical;

        // Active planet for gravity (set from MissionDesign)
        public string  ActivePlanetId  { get; private set; }
        public Vector3 PlanetPosition  { get; private set; }

        private const float BaseThrust = 40f;
        private const float BoostMult  = 2.2f;

        // ── Tsiolkovsky rocket equation ────────────────────────────────────────
        // ΔV = Isp × g₀ × ln(wet_mass / dry_mass)
        public float DeltaV(float ispS, float wetMass, float dryMass)
        {
            if (dryMass <= 0f || wetMass <= dryMass) return 0f;
            return ispS * 9.80665f * Mathf.Log(wetMass / dryMass);
        }

        // ── Configure engine from MissionDesign ───────────────────────────────
        public void SetEngineConfig(EngineType type)
        {
            ThrustMultiplier = MissionDesign.EngineThrustMult[type];
        }

        public void RegisterPlanetGravity(string planetId, Vector3 worldPos)
        {
            ActivePlanetId = planetId;
            PlanetPosition = worldPos;
        }

        public void SetPlanetPosition(Vector3 worldPos)
        {
            PlanetPosition = worldPos;
        }

        // ── Per-frame physics (call from FixedUpdate) ─────────────────────────
        public void Tick(float dt)
        {
            if (ShipRb == null) return;

            // Main thrust
            float thrust = ThrustMain * BaseThrust * ThrustMultiplier;
            if (Boosting) thrust *= BoostMult;
            ShipRb.AddForce(ShipRb.transform.forward * thrust * dt, ForceMode.VelocityChange);

            // Planetary gravity F = μ / r²
            if (!string.IsNullOrEmpty(ActivePlanetId))
            {
                Vector3 delta = PlanetPosition - ShipRb.position;
                float r2 = delta.sqrMagnitude;
                if (r2 > 100f)
                {
                    float mu    = PlanetMu.TryGetValue(ActivePlanetId, out var m) ? m : 200000f;
                    float force = mu / r2;
                    ShipRb.AddForce(delta.normalized * force * dt, ForceMode.VelocityChange);
                }
            }

            // Fuel drain: heavier engine = faster drain
            if (ThrustMain > 0.01f || Boosting)
            {
                float drainRate = ThrustMultiplier * 0.0003f * (Boosting ? 2f : 1f);
                FuelFraction = Mathf.Max(0f, FuelFraction - drainRate * dt);
            }
        }

        // ── Solar power degradation ────────────────────────────────────────────
        // efficiency = (1 AU / planet_AU)²
        public static float SolarEfficiency(string planetId)
        {
            float au = MissionDesign.PlanetAU.TryGetValue(planetId, out var v) ? v : 1f;
            return 1f / (au * au);
        }
    }
}
