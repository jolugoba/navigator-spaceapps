// Navigator 2.0 — AssistFX.cs
// The moment a gravity assist hands you free speed: a gold-and-cyan burst of sparks at the ship.
// Built in code (no prefab or scene wiring), so it shows in space, AR and the free Solar System.
// Guacalita Inc. — SAL / Claude

using UnityEngine;

namespace Navigator.Game
{
    public static class AssistFX
    {
        static Material _mat;

        public static void Burst(Vector3 pos, float deltaV)
        {
            if (_mat == null)
            {
                var sh = Shader.Find("Sprites/Default");
                if (sh == null) return;                       // no shader on this platform: skip the sparks
                _mat = new Material(sh) { mainTexture = Texture2D.whiteTexture };
            }

            var go = new GameObject("AssistBurst");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = false;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 10f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.3f), new Color(0.4f, 0.9f, 1f));
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 80;

            // More sparks for a closer skim (bigger swing = bigger deltaV)
            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(20f + deltaV * 3f, 20f, 70f)) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = _mat;
            ps.Play();
            Object.Destroy(go, 3f);
        }
    }
}
