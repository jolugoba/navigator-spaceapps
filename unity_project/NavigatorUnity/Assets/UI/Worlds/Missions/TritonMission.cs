// Navigator 2.0 — TritonMission.cs · "Jumping over geysers" (docs/mundos/07_NEPTUNO_TRITON.md)
// Neptune has no surface: you land on its moon Triton (0.08 g, almost no air) with rockets,
// then a hopper jumps between nitrogen geysers and samples their plumes while they erupt.
// Guacalita Inc. — SAL / Claude

using System.Collections.Generic;
using UnityEngine;
using Navigator.Core;
using Navigator.Game;
using Navigator.UI;

namespace Navigator.Worlds
{
    public class TritonMission : RocketLandingMission
    {
        static readonly Vector2[] GeyserAt = { new(90f, 45f), new(-75f, 110f), new(40f, -125f) };
        static readonly float[] GeyserPhase = { 0f, 7f, 13f };
        const float Period = 20f, EruptFor = 6f, PlumeHeight = 26f, PlumeR = 3.2f;

        readonly List<ParticleSystem> _plumes = new();
        readonly List<(Vector2 c, float r)> _frost = new();
        GameObject _hop;
        Hopper _hopper;
        LineRenderer _arc;
        int _samples;
        float _t;
        Site _home;
        bool _wasAirborne;

        public override void Shape(WorldTerrain t)
        {
            PadLocal = new Vector2(0f, 0f);
            t.AddFlat(PadLocal, PadRadius);
            foreach (var g in GeyserAt)
            {
                t.AddClear(g, PlumeR + 4f);
                // Dark fan-shaped streak blown by the thin wind
                for (int k = 0; k < 5; k++) t.AddPaint(g + new Vector2(k * 5f, k * 2f), 6f - k * 0.7f, P.Accent, 0.8f);
            }
            var rnd = new System.Random(3);
            for (int i = 0; i < 8; i++)   // bright slippery frost
            {
                var c = new Vector2((float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f) * 380f;
                if (c.magnitude < 30f) continue;
                float r = 10f + (float)rnd.NextDouble() * 10f;
                _frost.Add((c, r));
                t.AddPaint(c, r, new Color(0.98f, 0.95f, 0.97f), 0.7f);
            }
        }

        public override void Begin()
        {
            H.SetTitle(T("w.triton.title"), T("w.triton.goal.land"));
            H.ShowBanner(T("w.triton.banner"), UIStyle.Accent, 4f);
            Vector3 pad = Ter.Surface(PadLocal);
            StartRocketLanding(pad + new Vector3(-60f, 260f, -140f), new Vector3(3f, -4f, 8f), 2.2f, 42f);
            Result.Lines.Add(T("w.triton.log.start"));
            foreach (var g in GeyserAt) BuildGeyser(g);
        }

        void BuildGeyser(Vector2 at)
        {
            var root = new GameObject("Geyser");
            root.transform.SetParent(Root, false);
            root.transform.position = Ter.Surface(at);
            if (A.GeyserVent != null) { var v = Object.Instantiate(A.GeyserVent, root.transform); ModelFit.FitToSize(v, 6f); }
            if (A.DustMaterial == null) { _plumes.Add(null); return; }
            var ps = root.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.startLifetime = 4f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(1.2f, 3f);
            main.startColor = new Color(0.35f, 0.33f, 0.32f, 0.55f);
            main.gravityModifier = 0.02f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 400;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 6f; shape.radius = 0.6f;
            shape.rotation = new Vector3(-90f, 0f, 0f);   // straight up
            var e = ps.emission; e.rateOverTime = 0f;
            root.GetComponent<ParticleSystemRenderer>().sharedMaterial = A.DustMaterial;
            ps.Play();
            _plumes.Add(ps);
        }

        bool Erupting(int i) => Mathf.Repeat(_t - GeyserPhase[i], Period) < EruptFor;
        float UntilEruption(int i) => Period - Mathf.Repeat(_t - GeyserPhase[i], Period);

        public override void Tick(float dt)
        {
            _t += dt;
            for (int i = 0; i < _plumes.Count; i++)
                if (_plumes[i] != null) { var e = _plumes[i].emission; e.rateOverTime = Erupting(i) ? 70f : 0f; }

            if (Phase == "landing")
            {
                // Landing on an active vent is not an option
                foreach (var g in GeyserAt)
                    if (Craft.Altitude < 3f && new Vector2(Lander.transform.position.x - Ter.Center.x - g.x, Lander.transform.position.z - Ter.Center.z - g.y).magnitude < 7f)
                    { Fail("w.fail.vent"); return; }
                if (TickRocketLanding(dt)) StartHopper();
            }
            else if (Phase == "hop") TickHop(dt);
        }

        void StartHopper()
        {
            var at = Lander.transform.position + Lander.transform.forward * 5f;
            at.y = Ter.Height(at);
            _hop = SpawnVehicle(A.Hopper, "hopper", 1.8f, at);
            _hopper = new Hopper
            {
                T = _hop.transform, Terrain = Ter, Gravity = P.Gravity,
                IsFrost = p => { foreach (var f in _frost) if (new Vector2(p.x - Ter.Center.x - f.c.x, p.z - Ter.Center.z - f.c.y).magnitude < f.r) return true; return false; },
            };
            float fuelLeft = Craft.Fuel / Mathf.Max(0.01f, Craft.FuelMax);
            _hopper.Nitrogen = Mathf.Clamp(0.6f + fuelLeft * 0.4f, 0.6f, 1f);

            _arc = new GameObject("JumpArc").AddComponent<LineRenderer>();
            _arc.transform.SetParent(Root, false);
            _arc.positionCount = 24;
            _arc.widthMultiplier = 0.12f;
            _arc.sharedMaterial = A.NewMaterial(A.PropMaterial, new Color(0.4f, 1f, 0.9f), 0.9f);

            for (int i = 0; i < GeyserAt.Length; i++) AddSite("w.triton.site.geyser", Ter.Surface(GeyserAt[i]), PlumeR + 1f);
            _home = new Site { Key = "w.merc.site.lander", Pos = Lander.transform.position, Radius = 10f };

            D.Focus = _hop.transform;
            Cam.Target = _hop.transform; Cam.Distance = 10f; Cam.Height = 3.5f; Cam.Pitch = 16f; Cam.AutoFollow = false;
            Cam.SnapBehind();
            H.SetTitle(T("w.triton.title"), T("w.triton.goal.hop"));
            H.SetPrompt(T("w.triton.hop.hint"));
            H.ShowBanner(T("w.rover.out"), UIStyle.Good);
            Go("hop");
        }

        void TickHop(float dt)
        {
            var dir = _hopper.Aim(In.MoveV, Cam.Yaw);

            // A: hold to charge, release to jump
            H.SetButton(H.BtnA, T("w.btn.jump"), _hopper.Airborne ? BtnState.Off : BtnState.On, _hopper.Charge, T("w.jump.air"));
            if (!_hopper.Airborne && In.AHeld) _hopper.Hold(dt);
            else if (!_hopper.Airborne && _hopper.Charge > 0f && !In.AHeld)
            {
                float c = _hopper.Charge;
                _hopper.Release(dir);
                AudioFX.Play(Sfx.Go, 0.4f);
                if (_hopper.Airborne)
                {
                    WorldFX.Touchdown(Root, _hop.transform.position, P.GroundA, 0.3f);   // frost blown off by the jump
                    Cam.Punch(4f + 6f * c);
                }
            }

            // B: nitrogen jets in the air
            H.SetButton(H.BtnB, T("w.btn.jets"), _hopper.Airborne && _hopper.Nitrogen > 0f ? BtnState.On : BtnState.Off, _hopper.Nitrogen, T("w.jets.ground"));
            if (_hopper.Airborne && BHeld) _hopper.Jets(dir, dt);

            // C: sample inside an erupting plume
            int inPlume = -1;
            var p = _hop.transform.position;
            for (int i = 0; i < GeyserAt.Length; i++)
            {
                var g = Ter.Surface(GeyserAt[i]);
                if (!Sites[i].Done && new Vector2(p.x - g.x, p.z - g.z).magnitude < PlumeR && p.y - g.y < PlumeHeight && Erupting(i)) inPlume = i;
                // The plume throws you up
                if (Erupting(i) && new Vector2(p.x - g.x, p.z - g.z).magnitude < PlumeR && p.y - g.y < PlumeHeight)
                    _hopper.Kick(Vector3.up * 3.2f * dt);
            }
            H.SetButton(H.BtnC, T("w.btn.sample"), inPlume >= 0 ? BtnState.Pulse : BtnState.Off, 0f, T("w.triton.sample.where"));
            if (CPressed && inPlume >= 0)
            {
                MarkDone(Sites[inPlume]);
                _samples++;
                Score(800, "w.triton.log.sample", _samples);
                FxSample(_hop.transform.position);
                H.ShowBanner(F("w.sample.got", _samples, 3), UIStyle.Good);
            }

            _hopper.Step(dt);
            if (_wasAirborne && !_hopper.Airborne) { WorldFX.Touchdown(Root, _hop.transform.position, P.GroundA, 0.2f); Cam.Shake(0.15f); Haptics.Buzz(); }
            _wasAirborne = _hopper.Airborne;
            if (_hopper.Airborne && BHeld && _hopper.Nitrogen > 0f) WorldFX.Sparks(Root, _hop.transform.position);   // jet puffs
            DrawArc(dir);

            H.SetMeter(0, T("w.m.nitrogen"), $"{_hopper.Nitrogen * 100f:F0}%", _hopper.Nitrogen, _hopper.Nitrogen < 0.25f ? UIStyle.Bad : UIStyle.Accent);
            H.SetMeter(1, T("w.m.height"), $"{Mathf.Max(0f, p.y - Ter.Height(p)):F1} m", Mathf.Clamp01((p.y - Ter.Height(p)) / 20f), UIStyle.Accent);
            H.SetMeter(2, T("w.m.samples"), $"{_samples}/3", _samples / 3f, UIStyle.Gold);

            if (_samples < 3)
            {
                var next = NextSite();
                if (next != null)
                {
                    int i = Sites.IndexOf(next);
                    // The infrared mapper sees the vent warming up before it erupts
                    string label = Has(ScienceInstrument.InfraredMapper)
                        ? (Erupting(i) ? T("w.triton.erupting") : F("w.triton.in", UntilEruption(i)))
                        : T(next.Key);
                    float d = new Vector2(next.Pos.x - p.x, next.Pos.z - p.z).magnitude;
                    H.SetArrow(next.Pos + Vector3.up * 4f, $"{label} · {d:F0} m");
                }
            }
            else PointTo(_home);
            H.SetChecklist(Checklist(("w.ck.land", true), ("w.ck.geysers3", _samples >= 3), ("w.ck.back", false)));

            if (_samples >= 3 && !_hopper.Airborne && Near(_home, p))
            {
                Score(400, "w.log.back");
                Complete(_hopper.Nitrogen >= 0.5f ? 3 : 2);
            }
        }

        void DrawArc(Vector3 dir)
        {
            bool show = !_hopper.Airborne;
            _arc.enabled = show;
            if (!show) return;
            float charge = Mathf.Max(0.15f, _hopper.Charge);
            var v = _hopper.LaunchVelocity(dir, charge);
            var p = _hop.transform.position + Vector3.up * 0.5f;
            float flight = 2f * v.y / P.Gravity;
            for (int i = 0; i < _arc.positionCount; i++)
            {
                float t = flight * i / (_arc.positionCount - 1);
                _arc.SetPosition(i, p + v * t + 0.5f * Vector3.down * P.Gravity * t * t);
            }
        }
    }
}
