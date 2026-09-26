// Navigator 2.0 — GravityField.cs
// Real gravity on the route: the planets that real NASA probes used for gravity assists
// (MESSENGER, Galileo, Cassini, Voyager 2) sit along the path. Each pulls the ship with
// a = μ / r². Skim one inside its assist ring and it swings you out faster — free ΔV,
// exactly why real missions fly these detours. Fly too low and you hit the planet.
// Guacalita Inc. — SAL / Claude

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Navigator.Game
{
    public class GravityField : MonoBehaviour
    {
        public Material GlowMaterial;          // additive particle material
        public float    BodyDiameter  = 44f;   // game units
        public float    MuScale       = 0.06f; // flyby bodies are close: scale their pull down
        public float    MaxAssistDv   = 32f;   // game m/s at a perfect skim
        public float    SideOffset    = 70f;   // bodies sit off the straight line — a detour you choose

        // Supplied by the scene (the AR assembly knows the planet models)
        public Func<string, GameObject> PrefabFor;

        // bodyId, ΔV gained (game m/s), fuel saved (fraction of the tank)
        public static event Action<string, float, float> AssistGained;
        public static event Action<string>               BodyImpact;

        // The flybys real missions flew to reach each planet
        public static readonly Dictionary<string, (string probe, string[] bodies)> RealFlybys = new()
        {
            { "mercury", ("MESSENGER",  new[] { "venus" }) },
            { "venus",   ("",           new string[0]) },
            { "mars",    ("",           new string[0]) },
            { "jupiter", ("Galileo",    new[] { "venus", "earth" }) },
            { "saturn",  ("Cassini",    new[] { "venus", "earth", "jupiter" }) },
            { "uranus",  ("Voyager 2",  new[] { "jupiter", "saturn" }) },
            { "neptune", ("Voyager 2",  new[] { "jupiter", "saturn", "uranus" }) },
        };

        static readonly Dictionary<string, float> Mu = new()
        {
            { "venus", 455000f }, { "earth", 470000f }, { "jupiter", 1265000f },
            { "saturn", 535000f }, { "uranus", 445000f }, { "neptune", 570000f },
        };

        class Body
        {
            public string    Id;
            public Transform T;
            public float     Radius, Mu;
            public bool      InRing, Used;
            public float     Closest;
            public LineRenderer Ring;
        }

        private readonly List<Body> _bodies = new();
        private Rigidbody       _ship;
        private CollisionSystem _collision;
        private ShipController  _ctrl;
        private FuelSystem      _fuel;
        private Camera          _cam;

        public int  AssistCount { get; private set; }
        public string Probe     { get; private set; }

        public void SpawnAlongRoute(string targetPlanet, Vector3 from, Vector3 to, Transform ship)
        {
            Clear();
            _ship      = ship != null ? ship.GetComponent<Rigidbody>()       : null;
            _collision = ship != null ? ship.GetComponent<CollisionSystem>() : null;
            _ctrl      = ship != null ? ship.GetComponent<ShipController>()  : null;
            _fuel      = ship != null ? ship.GetComponent<FuelSystem>()      : null;
            _cam       = Camera.main;

            if (!RealFlybys.TryGetValue(targetPlanet ?? "", out var plan)) return;
            Probe = plan.probe;

            Vector3 route = to - from;
            Vector3 side  = Vector3.Cross(route.normalized, Vector3.up);
            if (side.sqrMagnitude < 0.01f) side = Vector3.right;
            side.Normalize();

            int n = plan.bodies.Length;
            for (int i = 0; i < n; i++)
            {
                float t   = Mathf.Lerp(0.25f, 0.75f, n == 1 ? 0.5f : i / (float)(n - 1));
                float dir = i % 2 == 0 ? 1f : -1f;
                Vector3 pos = from + route * t + side * SideOffset * dir + Vector3.up * 12f * dir;
                _bodies.Add(MakeBody(plan.bodies[i], pos));
            }
        }

        public void Clear()
        {
            foreach (var b in _bodies) if (b.T != null) Destroy(b.T.gameObject);
            _bodies.Clear();
            AssistCount = 0;
            Probe = "";
        }

        Body MakeBody(string id, Vector3 pos)
        {
            var root = new GameObject($"Flyby_{id}").transform;
            root.SetParent(transform, false);
            root.position = pos;

            var prefab = PrefabFor?.Invoke(id);
            GameObject model;
            if (prefab != null)
            {
                model = Instantiate(prefab, root);
            }
            else
            {
                model = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(model.GetComponent<Collider>());
                model.transform.SetParent(root, false);
                var r = model.GetComponent<Renderer>();
                if (r != null) r.material.color = id == "earth" ? new Color(0.25f, 0.5f, 0.95f) : new Color(0.8f, 0.7f, 0.55f);
            }
            model.transform.localPosition = Vector3.zero;
            Navigator.Core.ModelFit.FitToSize(model, BodyDiameter);
            model.AddComponent<Spin>();

            float radius = BodyDiameter * 0.5f;
            AddAtmosphereGlow(root, id, radius);
            var ring = AddAssistRing(root, radius * AssistRingFactor);

            return new Body
            {
                Id = id, T = root, Radius = radius,
                Mu = (Mu.TryGetValue(id, out var m) ? m : 450000f) * MuScale,
                Closest = float.MaxValue, Ring = ring,
            };
        }

        const float AssistRingFactor = 3f;   // skim inside 3 radii to get the swing

        void AddAtmosphereGlow(Transform root, string id, float radius)
        {
            var go = new GameObject("Glow");
            go.transform.SetParent(root, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.prewarm = true;
            main.startLifetime = 2f; main.startSpeed = 0f;
            main.startSize  = radius * 3.2f;
            main.startColor = id switch
            {
                "earth"   => new Color(0.3f, 0.6f, 1f, 0.35f),
                "venus"   => new Color(1f, 0.85f, 0.5f, 0.3f),
                "jupiter" => new Color(1f, 0.7f, 0.45f, 0.3f),
                "saturn"  => new Color(1f, 0.9f, 0.6f, 0.3f),
                _         => new Color(0.5f, 0.85f, 1f, 0.3f),
            };
            main.maxParticles = 3;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var em = ps.emission; em.rateOverTime = 1.5f;
            var shape = ps.shape; shape.enabled = false;
            var r = go.GetComponent<ParticleSystemRenderer>();
            if (GlowMaterial != null) r.sharedMaterial = GlowMaterial;
            ps.Play();
        }

        LineRenderer AddAssistRing(Transform root, float radius)
        {
            var go = new GameObject("AssistRing");
            go.transform.SetParent(root, false);
            var lr = go.AddComponent<LineRenderer>();
            const int seg = 64;
            lr.positionCount = seg;
            lr.loop          = true;
            lr.useWorldSpace = false;
            lr.widthMultiplier = 0.6f;
            for (int i = 0; i < seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius);
            }
            if (GlowMaterial != null) lr.sharedMaterial = GlowMaterial;
            lr.startColor = lr.endColor = new Color(0.3f, 0.9f, 1f, 0.55f);
            return lr;
        }

        void LateUpdate()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;
            foreach (var b in _bodies)
            {
                if (b.Ring == null) continue;
                // The ring always faces the player so the "fly through here" band reads clearly
                b.Ring.transform.rotation = Quaternion.LookRotation(b.T.position - _cam.transform.position);
                float pulse = 0.4f + 0.25f * Mathf.Sin(Time.time * 3f);
                var c = b.Used ? new Color(0.3f, 1f, 0.5f, 0.25f) : new Color(0.3f, 0.9f, 1f, pulse);
                b.Ring.startColor = b.Ring.endColor = c;
            }
        }

        void FixedUpdate()
        {
            if (_ship == null || _ship.isKinematic) return;
            foreach (var b in _bodies)
            {
                Vector3 delta = b.T.position - _ship.position;
                float r = delta.magnitude;
                if (r < 0.01f) continue;

                // Newton: a = μ / r², softened near the surface so it never explodes
                float soft = Mathf.Max(r, b.Radius * 1.2f);
                _ship.AddForce(delta / r * (b.Mu / (soft * soft)), ForceMode.Acceleration);

                // Too low: you hit the planet
                if (r < b.Radius * 1.05f)
                {
                    Vector3 outward = -delta / r;
                    _ship.position = b.T.position + outward * b.Radius * 1.3f;
                    _ship.linearVelocity = Vector3.Reflect(_ship.linearVelocity, outward) * 0.4f;
                    if (_collision != null && !_collision.IsInvincible)
                    {
                        _collision.ApplyDamage(CollisionSeverity.Heavy);
                        _collision.StartInvincibility();
                    }
                    BodyImpact?.Invoke(b.Id);
                    b.InRing = false;
                    continue;
                }

                if (b.Used) continue;
                float ring = b.Radius * AssistRingFactor;
                if (r < ring)
                {
                    b.InRing  = true;
                    b.Closest = Mathf.Min(b.Closest, r);
                }
                else if (b.InRing)
                {
                    // Left the ring: the closer the skim, the bigger the swing
                    b.InRing = false;
                    b.Used   = true;
                    float quality = Mathf.Clamp01(1f - (b.Closest - b.Radius) / (ring - b.Radius));
                    float dv = MaxAssistDv * Mathf.Lerp(0.35f, 1f, quality);
                    _ctrl?.Slingshot(dv, 4f);
                    // ΔV you didn't burn is propellant you still have
                    float saved = 0.06f * Mathf.Lerp(0.35f, 1f, quality);
                    _fuel?.Refuel(saved);
                    AssistCount++;
                    AssistGained?.Invoke(b.Id, dv, saved);
                    AudioFX.Play(Sfx.Pickup);
                }
            }
        }

        // Slow planet rotation
        class Spin : MonoBehaviour
        {
            void Update() => transform.Rotate(0f, 6f * Time.deltaTime, 0f, Space.Self);
        }
    }
}
