// Navigator 2.0 — SpaceDustFX.cs
// Dust and faint stars floating around the camera. They stay put in the world, so flying
// past them — and the streaks they stretch into at speed — is what makes speed readable.
// Guacalita Inc. — SAL / Claude

using UnityEngine;

namespace Navigator.Game
{
    [RequireComponent(typeof(Camera))]
    public class SpaceDustFX : MonoBehaviour
    {
        public Material ParticleMaterial;
        public float    Radius = 45f;

        void Start()
        {
            var go = new GameObject("SpaceDust");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, Radius * 0.6f);

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop            = true;
            main.prewarm         = true;
            main.startLifetime   = 3.5f;
            main.startSpeed      = 0f;
            main.startSize       = new ParticleSystem.MinMaxCurve(0.06f, 0.22f);
            main.startColor      = new ParticleSystem.MinMaxGradient(new Color(0.7f, 0.85f, 1f, 0.5f), new Color(1f, 1f, 1f, 0.9f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;   // dust stays, the ship moves
            main.maxParticles    = 900;

            var emission = ps.emission;
            emission.rateOverTime = 220f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale     = new Vector3(Radius * 1.6f, Radius, Radius * 1.6f);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode          = ParticleSystemRenderMode.Stretch;
            r.lengthScale         = 1f;
            r.cameraVelocityScale = 0.05f;   // streaks grow with camera speed
            if (ParticleMaterial != null) r.sharedMaterial = ParticleMaterial;

            ps.Play();
        }
    }
}
