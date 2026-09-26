// Navigator 2.0 — BurnoutSystem.cs
// Inspired by Burnout 3/Revenge: near-misses + takedowns fill boost bar
// BURNOUT MODE: x2.5 speed, invincible, white exhaust, 10 seconds
// Guacalita Inc. — SAL / Claude

using System;
using UnityEngine;
using UnityEngine.Events;

namespace Navigator.Game
{
    public enum TakedownType { Frontal, Lateral, Trasero, Chain }

    public class BurnoutSystem : MonoBehaviour
    {
        // ── Public state ───────────────────────────────────────────────────────
        [Range(0f, 1f)] public float BurnoutBar    = 0f;
        public bool  BurnoutActive   { get; private set; }
        public float BurnoutTimeLeft { get; private set; }
        public int   NearMissCombo   { get; private set; }
        public int   TakedownCount   { get; private set; }

        // ── Config ─────────────────────────────────────────────────────────────
        [Header("Burnout")]
        public float BurnoutDuration    = 10f;
        public float SpeedMultiplier    = 2.5f;   // applied by ShipController

        [Header("Near-Miss thresholds (game units)")]
        public float NearMissAsteroidDist = 3f;
        public float NearMissShipDist     = 5f;
        public float NearMissWindow       = 3f;   // seconds before combo resets

        // ── Events ─────────────────────────────────────────────────────────────
        public UnityEvent OnBurnoutStart  = new();
        public UnityEvent OnBurnoutEnd    = new();
        public UnityEvent<int>    OnNearMiss    = new();  // sends combo count
        public UnityEvent<TakedownType> OnTakedown = new();

        // ── Private ────────────────────────────────────────────────────────────
        private float _comboTimer = 0f;

        // ── Gain per action (matches NAVIGATOR_BURNOUT_P2P.md) ─────────────────
        private static float GainForNearMissAsteroid => 0.08f;
        private static float GainForNearMissShip     => 0.12f;
        private static float GainForDebrisField      => 0.15f;
        private static float GainForTakedown(TakedownType t) => t switch
        {
            TakedownType.Frontal  => 0.35f,
            TakedownType.Lateral  => 0.25f,
            TakedownType.Trasero  => 0.20f,
            TakedownType.Chain    => 1.00f,  // instant full burnout
            _                     => 0.20f,
        };
        private static float GainForGravityAssist   => 0.20f;
        private static float GainPerSecRadiation     => 0.10f;
        private static float GainPerSecLowFuel       => 0.05f;

        void Update()
        {
            float dt = Time.deltaTime;

            if (_suppressTimer > 0f) _suppressTimer -= dt;

            if (BurnoutActive)
            {
                BurnoutTimeLeft -= dt;
                if (BurnoutTimeLeft <= 0f)
                {
                    BurnoutActive   = false;
                    BurnoutBar      = 0f;
                    BurnoutTimeLeft = 0f;
                    OnBurnoutEnd.Invoke();
                }
            }
            else
            {
                // Combo timer reset
                _comboTimer += dt;
                if (_comboTimer > NearMissWindow) NearMissCombo = 0;

                // Natural decay when not doing risky things
                BurnoutBar = Mathf.Max(0f, BurnoutBar - 0.02f * dt);
            }
        }

        // ── Call from collision/proximity system ──────────────────────────────

        public void RegisterNearMissAsteroid()
        {
            if (BurnoutActive) return;
            _comboTimer = 0f;
            NearMissCombo++;
            float mult = ComboMultiplier();
            AddGain(GainForNearMissAsteroid * mult);
            OnNearMiss.Invoke(NearMissCombo);
        }

        public void RegisterNearMissShip()
        {
            if (BurnoutActive) return;
            _comboTimer = 0f;
            NearMissCombo++;
            float mult = ComboMultiplier();
            AddGain(GainForNearMissShip * mult);
            OnNearMiss.Invoke(NearMissCombo);
        }

        public void RegisterDebrisField()    => AddGain(GainForDebrisField);
        public void RegisterGravityAssist()  => AddGain(GainForGravityAssist);

        public void RegisterRadiationZone(float dt) =>
            AddGain(GainPerSecRadiation * dt);

        public void RegisterLowFuel(float dt) =>
            AddGain(GainPerSecLowFuel * dt);

        public void RegisterBoostFlight(float dt) =>
            AddGain(0.008f * dt);

        // ── Powerup: pod de boost llena la barra al instante ─────────────────
        public void ForceFullBar()
        {
            BurnoutBar = 1f;
            if (!BurnoutActive) TriggerBurnout();
        }

        // ── Powerup: EMP del rival suprime el Burnout N segundos ──────────────
        private float _suppressTimer = 0f;
        public bool IsSuppressed => _suppressTimer > 0f;

        public void SuppressFor(float seconds)
        {
            if (BurnoutActive)
            {
                BurnoutActive   = false;
                BurnoutBar      = 0f;
                BurnoutTimeLeft = 0f;
                OnBurnoutEnd.Invoke();
            }
            _suppressTimer = seconds;
            Debug.Log($"[Burnout] EMP — Burnout suprimido {seconds}s");
        }

        public void RegisterTakedown(TakedownType type)
        {
            if (BurnoutActive && type != TakedownType.Chain) return;
            TakedownCount++;
            AddGain(GainForTakedown(type));
            OnTakedown.Invoke(type);

            // Chain takedown = instant burnout
            if (type == TakedownType.Chain) TriggerBurnout();
        }

        public void ResetState()
        {
            BurnoutActive   = false;
            BurnoutBar      = 0f;
            BurnoutTimeLeft = 0f;
            NearMissCombo   = 0;
            TakedownCount   = 0;
            _suppressTimer  = 0f;
            _comboTimer     = 0f;
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void AddGain(float amount)
        {
            if (_suppressTimer > 0f) return;   // EMP activo — no carga
            BurnoutBar = Mathf.Clamp01(BurnoutBar + amount);
            if (BurnoutBar >= 1f && !BurnoutActive) TriggerBurnout();
        }

        private void TriggerBurnout()
        {
            BurnoutActive   = true;
            BurnoutTimeLeft = BurnoutDuration;
            NearMissCombo   = 0;
            OnBurnoutStart.Invoke();
        }

        private float ComboMultiplier() => NearMissCombo switch
        {
            >= 10 => 3.0f,
            >= 5  => 2.0f,
            >= 3  => 1.5f,
            _     => 1.0f,
        };
    }
}
