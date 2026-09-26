// Navigator 2.0 — ExplosionVFX.cs
// Partículas de colisión/explosión — se instancia en el punto de impacto
// Attach: al prefab de explosión. CollisionSystem lo instancia en colisión Heavy.
// Guacalita Inc. — SAL / Claude

using System.Collections;
using UnityEngine;

namespace Navigator.Game
{
    [RequireComponent(typeof(ParticleSystem))]
    public class ExplosionVFX : MonoBehaviour
    {
        [Header("Duración")]
        public float LifetimeS = 2.5f;

        [Header("Flash de luz")]
        public Light FlashLight;
        public float FlashIntensity = 8f;
        public float FlashDecayS   = 0.4f;

        [Header("Sonido")]
        public AudioSource AudioSrc;
        public AudioClip   ExplosionClip;
        public AudioClip   TakedownClip;

        private ParticleSystem _ps;

        void Awake()
        {
            _ps = GetComponent<ParticleSystem>();
        }

        void Start()
        {
            _ps.Play();

            if (FlashLight != null)
            {
                FlashLight.intensity = FlashIntensity;
                StartCoroutine(DecayLight());
            }

            Destroy(gameObject, LifetimeS);
        }

        IEnumerator DecayLight()
        {
            float elapsed = 0f;
            float start   = FlashLight.intensity;
            while (elapsed < FlashDecayS)
            {
                elapsed        += Time.deltaTime;
                FlashLight.intensity = Mathf.Lerp(start, 0f, elapsed / FlashDecayS);
                yield return null;
            }
            FlashLight.intensity = 0f;
        }

        // ── API pública ─────────────────────────────────────────────────────────

        public void PlayExplosion()
        {
            if (AudioSrc != null && ExplosionClip != null)
                AudioSrc.PlayOneShot(ExplosionClip);
        }

        public void PlayTakedown()
        {
            if (AudioSrc != null && TakedownClip != null)
                AudioSrc.PlayOneShot(TakedownClip);
        }

        // ── Spawn helper: CollisionSystem llama esto ────────────────────────────

        public static ExplosionVFX Spawn(GameObject prefab, Vector3 position, bool isTakedown = false)
        {
            if (prefab == null) return null;
            var go  = Instantiate(prefab, position, Quaternion.identity);
            var vfx = go.GetComponent<ExplosionVFX>();
            if (vfx != null)
            {
                if (isTakedown) vfx.PlayTakedown();
                else            vfx.PlayExplosion();
            }
            return vfx;
        }
    }
}
