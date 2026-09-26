// Navigator 2.0 — ShipAnimator.cs
// Anima la nave según el estado de vuelo — sin Mixamo, puro código Unity
// Attach al prefab NV-01 junto con ShipController
// Guacalita Inc. — SAL / Claude

using UnityEngine;

namespace Navigator.Game
{
    public class ShipAnimator : MonoBehaviour
    {
        [Header("Sub-meshes nombrados del modelo Meshy")]
        public Transform NozzlesLeft;       // sub-mesh "nozzles_left"
        public Transform NozzlesRight;      // sub-mesh "nozzles_right"
        public Transform WingLeft;          // sub-mesh "wing_left"
        public Transform WingRight;         // sub-mesh "wing_right"
        public Transform LandingGearFL;     // sub-mesh "gear_fl"
        public Transform LandingGearFR;     // sub-mesh "gear_fr"

        [Header("Materiales (MaterialPropertyBlock)")]
        public Renderer NozzleRenderer;     // material index 2 = engine glow

        [Header("Partículas (asignar en Inspector)")]
        public ParticleSystem ExhaustLeft;
        public ParticleSystem ExhaustRight;
        public ParticleSystem BurnoutExhaust; // exhaust blanco para Burnout mode

        // ── Refs ──────────────────────────────────────────────────────────────
        private ShipController  _ship;
        private BurnoutSystem   _burnout;
        private FuelSystem      _fuel;
        private MaterialPropertyBlock _mpb;
        private static readonly int _emissionId = Shader.PropertyToID("_EmissionColor");

        // ── Animación state ───────────────────────────────────────────────────
        private float  _wingFlex      = 0f;   // -1 (izq) a +1 (der)
        private float  _glowIntensity = 0f;
        private bool   _gearDeployed  = false;
        private float  _gearProgress  = 0f;   // 0=guardado 1=desplegado

        void Awake()
        {
            _ship    = GetComponent<ShipController>();
            _burnout = GetComponent<BurnoutSystem>();
            _fuel    = GetComponent<FuelSystem>();
            _mpb     = new MaterialPropertyBlock();
        }

        void Update()
        {
            AnimateEngineGlow();
            AnimateWingFlex();
            AnimateLandingGear();
            AnimateExhaust();
        }

        // ── Motor glow — pulso de luz en las toberas ──────────────────────────
        // Más thrust = más brillo. Burnout = blanco intenso.

        void AnimateEngineGlow()
        {
            if (NozzleRenderer == null) return;

            float targetGlow = _ship != null
                ? _ship.ThrustMain * 2.5f + (_ship.Boosting ? 1.5f : 0f)
                : 0f;

            // Burnout: glow blanco máximo
            if (_burnout != null && _burnout.BurnoutActive)
                targetGlow = 5f;

            _glowIntensity = Mathf.Lerp(_glowIntensity, targetGlow, Time.deltaTime * 8f);

            // Color normal = cyan. Burnout = blanco. Sin combustible = apagado.
            Color glowColor = _burnout != null && _burnout.BurnoutActive
                ? Color.white * _glowIntensity
                : new Color(0f, 0.9f, 1f) * _glowIntensity;

            NozzleRenderer.GetPropertyBlock(_mpb, 2);
            _mpb.SetColor(_emissionId, glowColor);
            NozzleRenderer.SetPropertyBlock(_mpb, 2);
        }

        // ── Alas se inclinan al girar (yaw input) ────────────────────────────

        void AnimateWingFlex()
        {
            if (WingLeft == null || WingRight == null) return;

            float targetFlex = _ship != null ? _ship.SteerYaw : 0f;
            _wingFlex = Mathf.Lerp(_wingFlex, targetFlex, Time.deltaTime * 5f);

            // Ala izquierda sube, derecha baja al girar derecha (y viceversa)
            WingLeft.localRotation  = Quaternion.Euler(_wingFlex * -12f, 0f, 0f);
            WingRight.localRotation = Quaternion.Euler(_wingFlex *  12f, 0f, 0f);
        }

        // ── Tren de aterrizaje baja/sube ──────────────────────────────────────
        // Se despliega automáticamente al acercarse al planeta (distancia < 50u)

        public void DeployLandingGear()  { _gearDeployed = true; }
        public void RetractLandingGear() { _gearDeployed = false; }

        void AnimateLandingGear()
        {
            if (LandingGearFL == null) return;

            float target = _gearDeployed ? 1f : 0f;
            _gearProgress = Mathf.Lerp(_gearProgress, target, Time.deltaTime * 2f);

            // Ruedas rotan 90° hacia abajo al desplegarse
            float angle = _gearProgress * 90f;
            if (LandingGearFL != null) LandingGearFL.localRotation = Quaternion.Euler(angle, 0f, 0f);
            if (LandingGearFR != null) LandingGearFR.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }

        // ── Partículas exhaust ────────────────────────────────────────────────

        void AnimateExhaust()
        {
            if (_ship == null) return;
            bool thrusting = _ship.ThrustMain > 0.05f || _ship.Boosting;
            bool burnout   = _burnout != null && _burnout.BurnoutActive;

            SetParticles(ExhaustLeft,   thrusting && !burnout);
            SetParticles(ExhaustRight,  thrusting && !burnout);
            SetParticles(BurnoutExhaust, burnout);

            // Ajusta emission rate según throttle
            if (ExhaustLeft != null && thrusting)
            {
                var em = ExhaustLeft.emission;
                em.rateOverTime = _ship.ThrustMain * 80f + (_ship.Boosting ? 40f : 0f);
            }
        }

        void SetParticles(ParticleSystem ps, bool active)
        {
            if (ps == null) return;
            if (active && !ps.isPlaying) ps.Play();
            if (!active && ps.isPlaying) ps.Stop();
        }
    }
}
