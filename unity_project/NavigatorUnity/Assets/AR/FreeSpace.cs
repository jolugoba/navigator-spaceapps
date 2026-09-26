// Navigator 2.0 — FreeSpace.cs
// Free flight: the whole Solar System as an open world, no mission, no clock.
// The Sun sits in the middle and the eight planets are where they really are TODAY
// (mean heliocentric longitude from NASA/JPL orbital elements, circular-orbit approximation),
// at compressed distances. Every phone builds the same world from the UTC date alone, so
// friends connected by QR meet in the same Solar System without sending the map around.
// Each planet pulls with real-ish gravity, has its assist ring, its moons and the probe that
// visited it; the asteroid belt sits between Mars and Jupiter. The player starts beside Earth.
// Guacalita Inc. — SAL / Claude

using System;
using System.Collections.Generic;
using UnityEngine;
using Navigator.Game;

namespace Navigator.AR
{
    public class FreeSpace : MonoBehaviour
    {
        public ARSpaceManager Space;
        public GravityField   Gravity;
        public AsteroidField  Asteroids;
        public PowerupSystem  Pods;
        public Material       SunMaterial;    // emissive lit material (tinted at runtime)
        public Material       GlowMaterial;   // additive particle material

        [Header("Scale (game units)")]
        public float RadiusBase      = 300f;  // Sun → orbit distance = base + perSqrtAU·√AU
        public float RadiusPerSqrtAU = 550f;
        public float SunDiameter     = 220f;

        // id, mean longitude at J2000 (deg), mean motion (deg/day), distance (AU), size (game units)
        // Longitudes and rates: NASA/JPL "Approximate Positions of the Planets" (Standish).
        static readonly (string id, double L0, double n, float au, float size)[] Planets =
        {
            ("mercury", 252.25084, 4.09233445, 0.39f,  34f),
            ("venus",   181.97973, 1.60213034, 0.72f,  52f),
            ("earth",   100.46435, 0.98560910, 1.00f,  56f),
            ("mars",    355.45332, 0.52403840, 1.52f,  42f),
            ("jupiter",  34.40438, 0.08308676, 5.20f, 150f),
            ("saturn",   49.94432, 0.03346063, 9.54f, 130f),
            ("uranus",  313.23218, 0.01173129, 19.19f, 90f),
            ("neptune", 304.88003, 0.00598106, 30.07f, 88f),
        };

        public static event Action SunTouched;

        public bool      Active { get; private set; }
        public Vector3   SunPosition { get; private set; }
        public Vector3   StartFacing { get; private set; } = Vector3.forward;
        public readonly List<(string id, Transform t)> Bodies = new();

        private GameObject      _root;
        private Transform       _sun;
        private Rigidbody       _ship;
        private CollisionSystem _collision;

        // Days since J2000 (2000-01-01 12:00 UTC), whole days so every phone gets the same map
        static double DaysSinceJ2000()
        {
            var j2000 = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            return Math.Floor((DateTime.UtcNow - j2000).TotalDays);
        }

        float OrbitRadius(float au) => RadiusBase + RadiusPerSqrtAU * Mathf.Sqrt(au);

        static Vector3 Helio(double L0, double n, double days, float r)
        {
            double L = (L0 + n * days) % 360.0 * Math.PI / 180.0;
            // Ecliptic plane = the game's horizontal plane
            return new Vector3((float)Math.Cos(L) * r, 0f, (float)Math.Sin(L) * r);
        }

        // Builds the Solar System around the launch point (the player starts next to Earth)
        public void Build(Transform ship)
        {
            Clear();
            Active = true;
            _ship      = ship != null ? ship.GetComponent<Rigidbody>() : null;
            _collision = ship != null ? ship.GetComponent<CollisionSystem>() : null;
            _root = new GameObject("FreeSpace");

            double days = DaysSinceJ2000();
            Vector3 earthHelio = Vector3.zero;
            foreach (var p in Planets)
                if (p.id == "earth") earthHelio = Helio(p.L0, p.n, days, OrbitRadius(p.au));

            // Shift everything so Earth sits just below the launch point
            Vector3 earthWorld = new(0f, -110f, 110f);   // far enough that its pull is a gentle drift
            Vector3 Shift(Vector3 helio) => helio - earthHelio + earthWorld;

            SunPosition = Shift(Vector3.zero);
            StartFacing = (SunPosition - Vector3.zero).normalized;

            // The Sun: glowing sphere + wide halo, scannable
            _sun = new GameObject("Sun").transform;
            _sun.SetParent(_root.transform, false);
            _sun.position = SunPosition;
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(ball.GetComponent<Collider>());
            ball.transform.SetParent(_sun, false);
            ball.transform.localScale = Vector3.one * SunDiameter;
            var r = ball.GetComponent<Renderer>();
            if (SunMaterial != null)
            {
                var m = new Material(SunMaterial);
                Color c = new(1f, 0.78f, 0.35f);
                m.color = c;
                if (m.HasProperty("_BaseColor"))     m.SetColor("_BaseColor", c);
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * 3f);
                r.sharedMaterial = m;
            }
            AddHalo(_sun, SunDiameter * 2.6f, new Color(1f, 0.7f, 0.3f, 0.45f));
            Scannable.Tag(_sun.gameObject, "sun", "sun", SunDiameter * 0.5f);

            // Cyan orbit lines around the Sun (circles at each planet's distance)
            foreach (var p in Planets)
                AddOrbit(OrbitRadius(p.au), p.id == "earth");

            // Planets as gravity wells (with assist rings), then their moons and probes
            var list = new List<(string, Vector3, float)>();
            foreach (var p in Planets)
                list.Add((p.id, Shift(Helio(p.L0, p.n, days, OrbitRadius(p.au))), p.size));
            if (Gravity != null)
            {
                Gravity.PrefabFor = id => Space != null ? Space.GetPlanetPrefab(id) : null;
                var roots = Gravity.SpawnBodies(list, ship);
                for (int i = 0; i < roots.Count; i++)
                {
                    Bodies.Add((Planets[i].id, roots[i]));
                    Vector3 toSun = (SunPosition - roots[i].position).normalized;
                    Space?.DecorateBody(roots[i], Planets[i].id, Planets[i].size, toSun);
                }
            }

            // Telescopes, ISS and SLS by the start; DART toward the belt
            Space?.PlaceLaunchLandmarks(Vector3.zero, StartFacing, 380f);

            // Asteroid belt between Mars and Jupiter, pods scattered on the way out
            Vector3 mars = list[3].Item2, jupiter = list[4].Item2;
            if (ship != null) Asteroids?.SpawnAlongRoute(mars, jupiter, ship);
            Pods?.SpawnAlongRoute(Vector3.zero, jupiter);
        }

        public void Clear()
        {
            Active = false;
            Bodies.Clear();
            if (_root != null) Destroy(_root);
            _root = null;
            Gravity?.Clear();
            Asteroids?.Clear();
            Pods?.ClearAll();
            Space?.ClearMissionTarget();
        }

        void FixedUpdate()
        {
            if (!Active || _ship == null || _ship.isKinematic || _sun == null) return;
            // Nobody survives touching the Sun: bounced out with a heavy hit
            Vector3 d = _ship.position - SunPosition;
            float limit = SunDiameter * 0.55f;
            if (d.sqrMagnitude < limit * limit)
            {
                Vector3 outward = d.sqrMagnitude > 0.01f ? d.normalized : Vector3.up;
                _ship.position = SunPosition + outward * SunDiameter * 0.8f;
                _ship.linearVelocity = outward * 30f;
                if (_collision != null && !_collision.IsInvincible)
                {
                    _collision.ApplyDamage(CollisionSeverity.Heavy);
                    _collision.StartInvincibility();
                }
                SunTouched?.Invoke();
            }
        }

        // The closest planet (or the Sun) whose scan isn't in the player's logbook yet
        public Transform NextToDiscover(Vector3 from)
        {
            Transform best = null;
            float bestD = float.MaxValue;
            foreach (var (id, t) in Bodies)
            {
                if (t == null || ShipTools.InLogbook("planet", id)) continue;
                float d = (t.position - from).sqrMagnitude;
                if (d < bestD) { bestD = d; best = t; }
            }
            if (best == null && _sun != null && !ShipTools.InLogbook("sun", "sun")) best = _sun;
            return best;
        }

        public (string id, float dist) Nearest(Vector3 from)
        {
            string id = "sun";
            float best = _sun != null ? Vector3.Distance(from, _sun.position) : float.MaxValue;
            foreach (var (bid, t) in Bodies)
            {
                if (t == null) continue;
                float d = Vector3.Distance(from, t.position);
                if (d < best) { best = d; id = bid; }
            }
            return (id, best);
        }

        void AddOrbit(float radius, bool home)
        {
            var go = new GameObject("Orbit");
            go.transform.SetParent(_root.transform, false);
            go.transform.position = SunPosition;
            var lr = go.AddComponent<LineRenderer>();
            const int seg = 128;
            lr.positionCount = seg;
            lr.loop = true;
            lr.useWorldSpace = false;
            lr.widthMultiplier = home ? 1.6f : 1.1f;
            for (int i = 0; i < seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius);
            }
            if (GlowMaterial != null) lr.sharedMaterial = GlowMaterial;
            lr.startColor = lr.endColor = home ? new Color(0.3f, 0.9f, 1f, 0.5f) : new Color(0.3f, 0.8f, 1f, 0.22f);
        }

        void AddHalo(Transform parent, float size, Color color)
        {
            var go = new GameObject("Halo");
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.prewarm = true;
            main.startLifetime = 2f; main.startSpeed = 0f;
            main.startSize = size;
            main.startColor = color;
            main.maxParticles = 3;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var em = ps.emission; em.rateOverTime = 1.5f;
            var sh = ps.shape; sh.enabled = false;
            if (GlowMaterial != null) go.GetComponent<ParticleSystemRenderer>().sharedMaterial = GlowMaterial;
            ps.Play();
        }
    }
}
