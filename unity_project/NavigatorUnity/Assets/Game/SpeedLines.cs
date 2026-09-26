// Navigator 2.0 — SpeedLines.cs
// Streaks of light rush past the camera when the ship is fast: more and longer with speed,
// orange while boosting, white in Burnout. Space has no wind — they are the classic
// "warp streaks" that show motion when there's nothing close to compare against.
// Guacalita Inc. — SAL / Claude

using UnityEngine;

namespace Navigator.Game
{
    public class SpeedLines : MonoBehaviour
    {
        public Material ParticleMaterial;   // additive
        public float    MaxRate = 90f;

        Rigidbody      _rb;
        ShipController _ship;
        BurnoutSystem  _burnout;
        ParticleSystem _ps;
        Camera         _cam;

        void Awake()
        {
            _rb      = GetComponent<Rigidbody>();
            _ship    = GetComponent<ShipController>();
            _burnout = GetComponent<BurnoutSystem>();
        }

        void Build()
        {
            _cam = Camera.main;
            if (_cam == null) return;
            var go = new GameObject("SpeedLines");
            go.transform.SetParent(_cam.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, 60f);
            _ps = go.AddComponent<ParticleSystem>();
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = _ps.main;
            main.loop = true;
            main.startLifetime = 0.45f;
            main.startSpeed    = 0f;
            main.startSize     = 0.35f;
            main.maxParticles  = 300;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            // A ring around the view, well away from the centre so the reticle stays clear
            var shape = _ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 26f;
            shape.radiusThickness = 0.55f;

            var vel = _ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(0f);
            vel.y = new ParticleSystem.MinMaxCurve(0f);
            vel.z = new ParticleSystem.MinMaxCurve(-160f);   // towards and past the camera

            var col = _ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.8f, 0.3f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode    = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.12f;
            r.lengthScale   = 3f;
            if (ParticleMaterial != null) r.sharedMaterial = ParticleMaterial;

            var em = _ps.emission;
            em.rateOverTime = 0f;
            _ps.Play();
        }

        void Update()
        {
            if (_ps == null) { Build(); if (_ps == null) return; }
            float max = _ship != null ? _ship.BaseMaxSpeed : 80f;
            float speed = _rb != null && !_rb.isKinematic ? _rb.linearVelocity.magnitude : 0f;
            bool burn  = _burnout != null && _burnout.BurnoutActive;
            bool boost = _ship != null && _ship.Boosting;
            // Nothing below 45 % of top speed, full at top speed (and beyond, with boost)
            float k = Mathf.Clamp01((speed / max - 0.45f) / 0.55f);
            if (boost || burn) k = Mathf.Max(k, 0.6f);

            var em = _ps.emission;
            em.rateOverTime = Navigator.Core.FxPrefs.Motion ? MaxRate * k * (burn ? 1.6f : 1f) : 0f;
            var main = _ps.main;
            main.startColor = burn ? new Color(1f, 1f, 1f, 0.9f)
                            : boost ? new Color(1f, 0.6f, 0.25f, 0.8f)
                            : new Color(0.6f, 0.85f, 1f, 0.6f);
        }

        void OnDestroy() { if (_ps != null) Destroy(_ps.gameObject); }
    }
}
