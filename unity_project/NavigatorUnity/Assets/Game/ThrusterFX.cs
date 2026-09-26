// Navigator 2.0 — ThrusterFX.cs
// Engine flame, trail and glow driven by thrust. Makes fuel visible:
//   cyan = thrust · orange = boost · white = Burnout · sputters when low · dead when empty
// Works on the local ship (reads its components) and on rival ships (values set by RemoteShipManager).
// Guacalita Inc. — SAL / Claude

using UnityEngine;

namespace Navigator.Game
{
    public class ThrusterFX : MonoBehaviour
    {
        public Material ParticleMaterial;
        public float    ShipLength = 3f;

        // Driven externally when there is no ShipController (rival ships)
        [HideInInspector] public float Thrust;
        [HideInInspector] public bool  Boost;
        [HideInInspector] public bool  Burnout;
        [HideInInspector] public float Fuel = 1f;

        static readonly Color Cyan   = new(0.3f, 0.85f, 1f);
        static readonly Color Orange = new(1f, 0.55f, 0.1f);

        private ShipController _ship;
        private BurnoutSystem  _burnout;
        private FuelSystem     _fuel;
        private Rigidbody      _rb;

        private ParticleSystem _ps;
        private TrailRenderer  _trail;
        private Light          _light;

        void Awake()
        {
            _ship    = GetComponent<ShipController>();
            _burnout = GetComponent<BurnoutSystem>();
            _fuel    = GetComponent<FuelSystem>();
            _rb      = GetComponent<Rigidbody>();
            Build();
        }

        void Build()
        {
            var nozzle = new GameObject("Exhaust").transform;
            nozzle.SetParent(transform, false);
            nozzle.localPosition = new Vector3(0f, 0f, -ShipLength * 0.5f);
            nozzle.localRotation = Quaternion.Euler(0f, 180f, 0f);   // particles shoot backwards

            _ps = nozzle.gameObject.AddComponent<ParticleSystem>();
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _ps.main;
            main.loop            = true;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(10f, 16f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles    = 500;

            var emission = _ps.emission;
            emission.rateOverTime = 0f;

            var shape = _ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle     = 7f;
            shape.radius    = 0.25f;

            var col = _ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.4f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var size = _ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

            var pr = nozzle.GetComponent<ParticleSystemRenderer>();
            if (ParticleMaterial != null) pr.sharedMaterial = ParticleMaterial;
            _ps.Play();

            _trail = nozzle.gameObject.AddComponent<TrailRenderer>();
            _trail.time        = 0.8f;
            _trail.minVertexDistance = 0.2f;
            _trail.widthCurve  = AnimationCurve.Linear(0f, 0.5f, 1f, 0f);
            _trail.emitting    = false;
            if (ParticleMaterial != null) _trail.sharedMaterial = ParticleMaterial;

            _light = nozzle.gameObject.AddComponent<Light>();
            _light.type      = LightType.Point;
            _light.range     = ShipLength * 3f;
            _light.intensity = 0f;
        }

        // For components added at runtime (Awake already ran before fields could be set)
        public void SetMaterial(Material m)
        {
            ParticleMaterial = m;
            if (m == null) return;
            _ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = m;
            _trail.sharedMaterial = m;
        }

        void Update()
        {
            if (_ship != null)
            {
                Thrust  = _ship.ThrustMain;
                Boost   = _ship.Boosting;
                Burnout = _burnout != null && _burnout.BurnoutActive;
                Fuel    = _fuel != null ? _fuel.FuelFraction : 1f;
            }

            float thrust = Fuel <= 0f ? 0f : Thrust;
            // Low fuel: the engine coughs
            if (Fuel < 0.15f && thrust > 0f && Random.value < 0.35f) thrust = 0f;

            Color c = Burnout ? Color.white : Boost ? Orange : Cyan;
            float rate = thrust <= 0.01f ? 0f : (Burnout ? 320f : Boost ? 220f : 120f) * thrust;

            var main = _ps.main;
            main.startColor = c;
            main.startSpeedMultiplier = Boost || Burnout ? 22f : 13f;
            var emission = _ps.emission;
            emission.rateOverTime = rate;

            _light.color     = c;
            _light.intensity = Mathf.MoveTowards(_light.intensity, rate > 0f ? (Boost ? 6f : 3.5f) : 0f, 30f * Time.deltaTime);

            float speed = _rb != null && !_rb.isKinematic ? _rb.linearVelocity.magnitude : (Thrust > 0f ? 10f : 0f);
            _trail.emitting   = speed > 4f;
            _trail.startColor = new Color(c.r, c.g, c.b, 0.8f);
            _trail.endColor   = new Color(c.r, c.g, c.b, 0f);
        }
    }
}
