// Navigator 2.0 — WorldFX.cs
// The "wow" of the worlds, all built from two particle materials (additive glow, soft dust):
// touchdown dust rings, rocket dust blown off the ground, wheel dust that hangs longer where
// gravity is weak, drill sparks, the golden burst of a collected sample, the fireworks of a
// completed mission, explosions, entry streaks, the Sun's glare, and a short slow-motion beat
// at the big moments. Effects clean themselves up.
// Guacalita Inc. — SAL / Claude

using System.Collections;
using UnityEngine;

namespace Navigator.Worlds
{
    public static class WorldFX
    {
        public static Material Glow, Dust;   // set by WorldDirector from WorldAssets

        // ── One-shot bursts ──────────────────────────────────────────────────
        public static ParticleSystem Burst(Transform parent, Vector3 pos, Color a, Color b, int count, float speed,
                                           Vector2 size, float life, float gravity, bool additive,
                                           ParticleSystemShapeType shape = ParticleSystemShapeType.Sphere, float lifeKeep = 4f)
        {
            var mat = additive ? Glow : Dust;
            if (mat == null) return null;
            var go = new GameObject("FX");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.duration = 0.2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startColor = new ParticleSystem.MinMaxGradient(a, b);
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = Mathf.Max(count, 10);
            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var sh = ps.shape;
            sh.shapeType = shape;
            sh.radius = 0.3f;
            if (shape == ParticleSystemShapeType.Circle) sh.rotation = new Vector3(90f, 0f, 0f);   // flat ring on the ground
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.8f));
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
            ps.Play();
            Object.Destroy(go, life + lifeKeep);
            return ps;
        }

        // Dust ring kicked up by a landing (gravity: the world's, relative to Earth)
        public static void Touchdown(Transform parent, Vector3 pos, Color ground, float gravityRatio)
        {
            var c = ground; c.a = 0.75f;
            var c2 = Color.Lerp(ground, Color.white, 0.3f); c2.a = 0.5f;
            Burst(parent, pos + Vector3.up * 0.2f, c, c2, 90, 9f, new Vector2(0.8f, 2.2f), 2.6f / Mathf.Max(0.3f, Mathf.Sqrt(gravityRatio)),
                  0.05f * gravityRatio, false, ParticleSystemShapeType.Circle);
        }

        public static void Sample(Transform parent, Vector3 pos)
        {
            Burst(parent, pos + Vector3.up, new Color(1f, 0.9f, 0.4f, 1f), new Color(0.4f, 1f, 0.9f, 1f), 70, 5f,
                  new Vector2(0.15f, 0.5f), 1.6f, -0.25f, true);   // negative gravity: the glow rises
        }

        public static void Sparks(Transform parent, Vector3 pos)
        {
            Burst(parent, pos, new Color(1f, 0.85f, 0.4f, 1f), new Color(1f, 0.5f, 0.15f, 1f), 12, 4f,
                  new Vector2(0.05f, 0.15f), 0.5f, 0.6f, true, ParticleSystemShapeType.Hemisphere, 1f);
        }

        public static void Explosion(Transform parent, Vector3 pos)
        {
            Burst(parent, pos, new Color(1f, 0.9f, 0.5f, 1f), new Color(1f, 0.35f, 0.05f, 1f), 120, 18f,
                  new Vector2(1f, 3.5f), 1.2f, 0.1f, true);
            Burst(parent, pos, new Color(0.25f, 0.22f, 0.2f, 0.8f), new Color(0.1f, 0.1f, 0.1f, 0.6f), 60, 6f,
                  new Vector2(2f, 5f), 3.5f, -0.05f, false);
        }

        // Victory: three coloured bursts, one after another
        public static IEnumerator Fireworks(Transform parent, Vector3 pos)
        {
            Color[] cs = { new(0.3f, 0.95f, 1f), new(1f, 0.85f, 0.3f), new(0.4f, 1f, 0.55f), new(1f, 0.45f, 0.8f) };
            for (int i = 0; i < 4; i++)
            {
                if (parent == null) yield break;
                var at = pos + new Vector3(Random.Range(-6f, 6f), 10f + Random.Range(0f, 6f), Random.Range(-6f, 6f));
                Burst(parent, at, cs[i], Color.white, 110, 12f, new Vector2(0.2f, 0.6f), 1.8f, 0.25f, true);
                Navigator.Game.AudioFX.Play(Navigator.Game.Sfx.Pickup, 0.6f);
                yield return new WaitForSecondsRealtime(0.35f);
            }
        }

        // ── Continuous emitters (rate set every frame) ───────────────────────
        public static ParticleSystem Emitter(Transform parent, Vector3 local, Color a, Color b, Vector2 size, float life,
                                             float speed, float gravity, bool additive, float coneAngle = 25f, bool worldSpace = true)
        {
            var mat = additive ? Glow : Dust;
            if (mat == null) return null;
            var go = new GameObject("Emitter");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.5f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.3f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startColor = new ParticleSystem.MinMaxGradient(a, b);
            main.gravityModifier = gravity;
            main.simulationSpace = worldSpace ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            main.maxParticles = 500;
            var em = ps.emission; em.rateOverTime = 0f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = coneAngle; sh.radius = 0.4f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.7f, 1f, 2f));
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
            ps.Play();
            return ps;
        }

        public static void Rate(ParticleSystem ps, float rate)
        {
            if (ps == null) return;
            var e = ps.emission;
            e.rateOverTime = rate;
        }

        // ── Slow motion beat at the big moments (touchdown, sample, victory) ─
        static Coroutine _slow;
        public static void SlowMo(MonoBehaviour host, float scale, float seconds)
        {
            if (host == null || !host.isActiveAndEnabled) return;
            if (_slow != null) host.StopCoroutine(_slow);
            _slow = host.StartCoroutine(SlowRoutine(scale, seconds));
        }

        static IEnumerator SlowRoutine(float scale, float seconds)
        {
            Time.timeScale = scale;
            yield return new WaitForSecondsRealtime(seconds);
            float t = 0f;
            while (t < 0.25f) { t += Time.unscaledDeltaTime; Time.timeScale = Mathf.Lerp(scale, 1f, t / 0.25f); yield return null; }
            Time.timeScale = 1f;
            _slow = null;
        }

        public static void ResetTime() { Time.timeScale = 1f; _slow = null; }
    }
}
