// Navigator 2.0 — TitanMission.cs · "Flying between methane lakes" (docs/mundos/05_SATURNO_TITAN.md)
// Saturn has no surface: like Huygens (2005) you land on its moon Titan. Entry → big parachute
// → swap to the small one (or the descent takes forever) → drift clear of the methane lake →
// a Dragonfly-style drone flies to three sites. It recharges only when landed (RTG).
// Guacalita Inc. — SAL / Claude

using UnityEngine;
using Navigator.Core;
using Navigator.Game;
using Navigator.UI;

namespace Navigator.Worlds
{
    public class TitanMission : WorldMission
    {
        static readonly Vector2 LakeAt = new(90f, -70f);
        const float LakeR = 75f;
        static readonly Vector2 SelkAt = new(-120f, 130f);
        static readonly Vector2 ShoreAt = LakeAt + (new Vector2(-40f, -20f) - LakeAt).normalized * (LakeR * 0.95f);

        GameObject _capsule, _chute, _drone;
        DescentCraft _craft;
        Drone _fly;
        ParticleSystem _plasma;
        float _sample, _rainT = -1f;
        bool _smallChute, _lowBattery;
        int _samples, _organics;
        ParticleSystem _wash;

        public override void Shape(WorldTerrain t)
        {
            t.AddLake(LakeAt, LakeR, 7f);
            t.AddCrater(SelkAt, 45f, 8f);
            t.AddFlat(new Vector2(-40f, -20f), 14f);
            t.AddPaint(new Vector2(-150f, -100f), 45f, P.GroundB, 0.6f);   // dark organic dunes
            t.AddClear(new Vector2(-150f, -100f), 10f);
            t.AddClear(SelkAt, 10f);
            t.AddClear(ShoreAt, 10f);
        }

        public override void Begin()
        {
            H.SetTitle(T("w.titan.title"), T("w.titan.goal.entry"));
            H.ShowBanner(T("w.titan.banner"), UIStyle.Accent, 4f);
            // Above the haze: Saturn and its rings fill the sky for a moment
            var saturn = A.SkyBodyPrefab?.Invoke("saturn");
            var start = Ter.Surface(new Vector2(-60f, -160f)) + Vector3.up * 1100f;
            if (saturn != null)
            {
                var s = Object.Instantiate(saturn, Root);
                ModelFit.FitToSize(s, 900f);
                s.transform.position = start + new Vector3(600f, 350f, 1400f);
                Object.Destroy(s, 25f);
            }
            _capsule = Spawn(A.EntryCapsule, "capsule", 2.7f, start);
            _plasma = Plasma(_capsule.transform);
            D.Focus = _capsule.transform;
            Cam.Target = _capsule.transform; Cam.Distance = 18f; Cam.Height = 5f; Cam.Pitch = 6f; Cam.AutoFollow = false;
            Cam.SnapBehind();
            Env.SetFog(P.Fog, P.FogDensity * 0.3f);
            H.SetPrompt(T("w.entry.hint"));
            EntryStart();
            Go("entry");
            Result.Lines.Add(T("w.titan.log.start"));
        }

        GameObject Spawn(GameObject model, string shape, float size, Vector3 at, bool alignLong = false)
        {
            var go = model != null ? A.Spawn(model, PrimitiveType.Cube, Vector3.one, Color.white, size, Root, alignLong) : VehicleShapes.Build(A, shape, Root);
            go.transform.position = at;
            return go;
        }

        public override void Tick(float dt)
        {
            switch (Phase)
            {
                case "entry": TickEntry(dt); break;
                case "chute": TickChute(dt); break;
                case "descent": TickDescent(dt); break;
                case "drone": TickDrone(dt); break;
            }
        }

        void TickEntry(float dt)
        {
            _capsule.transform.position += new Vector3(4f, -25f, 12f) * dt;
            if (!EntryTick(dt, 8f, 0.5f, 0.45f) || Finished) return;
            Score(300, "w.log.entry", Mathf.RoundToInt((1f - EntryHeat) * 100f));
            if (_plasma != null) Object.Destroy(_plasma.gameObject);
            H.SetTitle(T("w.titan.title"), T("w.titan.goal.chute"));
            H.SetPrompt(T("w.chute.hint"));
            H.SetButton(H.BtnB, T("w.btn.chute"), BtnState.Pulse);
            Go("chute");
        }

        void TickChute(float dt)
        {
            _capsule.transform.position += Vector3.down * 20f * dt;
            float needle = 1f - Mathf.Clamp01(PhaseT / 7f);
            float r = WindowTick(7f, 0.3f, 0.75f, F("w.chute.speed", Mathf.Lerp(30f, 400f, needle)));
            if (r < 0f) return;
            bool ok = r >= 0.3f && r <= 0.75f;
            if (ok) Score(250, "w.log.chute.ok"); else Result.Lines.Add(T("w.mars.log.late"));
            H.ShowBanner(T(ok ? "w.chute.ok" : "w.chute.late"), ok ? UIStyle.Good : UIStyle.Warn);
            _chute = Spawn(A.Parachute, "parachute", 14f, _capsule.transform.position + Vector3.up * 15f);
            FxJolt();
            _chute.transform.SetParent(_capsule.transform, true);
            _craft = new DescentCraft
            {
                T = _capsule.transform, Terrain = Ter, Gravity = P.Gravity, AirDensity = P.AirDensity, ScaleHeight = 420f,
                Mass = 1200f, BodyCdA = 1.5f, ChuteCdA = 55f, Wind = 2.5f, WindDir = new Vector3(0.6f, 0f, 1f),
                Vel = new Vector3(0f, -12f, 0f), ThrustAllowed = false, Fuel = 0f, FuelMax = 1f,
            };
            H.SetTitle(T("w.titan.title"), T("w.titan.goal.descent"));
            H.SetPrompt(T("w.titan.swap.hint"));
            H.SetButton(H.BtnB, T("w.btn.smallchute"), BtnState.Pulse);
            Go("descent");
        }

        // Huygens swapped to a smaller parachute so the descent took 2 h 27 min, not 6 h
        void TickDescent(float dt)
        {
            var dir = Quaternion.Euler(0f, Cam.Yaw, 0f) * new Vector3(In.MoveV.x, 0f, In.MoveV.y);
            _craft.Vel += dir * 1.4f * dt;
            _craft.Step(dt);
            float alt = _craft.Altitude;
            if (!_smallChute && (BPressed || PhaseT > 12f))
            {
                _smallChute = true;
                _craft.ChuteCdA = 16f;
                FxJolt(0.4f);
                if (_chute != null) _chute.transform.localScale *= 0.55f;
                if (PhaseT <= 12f) Score(200, "w.titan.log.swap"); else Result.Lines.Add(T("w.titan.log.slow"));
                H.SetButton(H.BtnB, null, BtnState.Hidden);
                H.SetPrompt(T("w.titan.drift.hint"));
            }
            Env.SetFog(P.Fog, Mathf.Lerp(P.FogDensity, P.FogDensity * 0.3f, Mathf.InverseLerp(100f, 900f, alt)));
            H.SetMeter(0, T("w.m.alt"), $"{ShownAltitude(alt):F0} m", Mathf.Clamp01(alt / 1000f), UIStyle.Accent);
            H.SetMeter(1, T("w.m.vspeed"), $"{_craft.VerticalSpeed:F1} m/s", Mathf.Clamp01(-_craft.VerticalSpeed / 15f), UIStyle.Good);
            H.HideMeter(2);
            H.SetArrow(Ter.Surface(new Vector2(-40f, -20f)), T("w.titan.dry"));
            if (alt > 0.3f) return;

            if (Ter.InWater(_capsule.transform.position)) { Fail("w.fail.lake"); return; }
            string problem = _craft.TouchdownProblem(vsMax: 5f, hsMax: 4f, tiltMax: 30f);
            if (problem == "w.fail.crash") { Fail(problem); return; }
            var p = _capsule.transform.position; p.y = Ter.Height(p);
            _capsule.transform.position = p;
            _capsule.transform.rotation = Quaternion.identity;
            if (_chute != null) { _chute.transform.SetParent(Root, true); Object.Destroy(_chute, 2f); }
            Result.Landed = true;
            FxTouchdown(p);
            Score(1000, "w.titan.log.landed", -_craft.VerticalSpeed);
            Haptics.Buzz();
            StartDrone();
        }

        void StartDrone()
        {
            var at = _capsule.transform.position + Vector3.right * 4f;
            at.y = Ter.Height(at);
            _drone = Spawn(A.Dragonfly, "dragonfly", 3.2f, at, true);
            _fly = new Drone
            {
                T = _drone.transform, Terrain = Ter, Gravity = P.Gravity, AirDensity = P.AirDensity,
                FlyDrain = 0.011f, Recharge = Mathf.Max(0.004f, ChargeRate(0.01f) * 5f),
            };
            if (Design != null && Design.Power == PowerSystem.Solar) Result.Lines.Add(T("w.titan.log.solar"));
            // Rotor wash: the thick air blows the sand around when flying low
            var c = P.GroundA; c.a = 0.5f;
            _wash = WorldFX.Emitter(Root, Vector3.zero, c, P.GroundB, new Vector2(0.6f, 1.8f), 2.5f, 6f, 0.02f, false, 85f);
            if (_wash != null) _wash.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

            AddSite("w.titan.site.dunes", Ter.Surface(new Vector2(-150f, -100f)), 7f, A.TitanPebbles, 1.5f);
            AddSite("w.titan.site.selk", Ter.Surface(SelkAt), 7f, A.TitanPebbles, 1.5f).Special = true;
            AddSite("w.titan.site.shore", Ter.Surface(ShoreAt), 7f, A.TitanPebbles, 1.5f);

            D.Focus = _drone.transform;
            Cam.Target = _drone.transform; Cam.Distance = 11f; Cam.Height = 4f; Cam.Pitch = 16f; Cam.AutoFollow = false;
            Cam.SnapBehind();
            H.SetTitle(T("w.titan.title"), T("w.titan.goal.drone"));
            H.SetPrompt(T("w.titan.drone.hint"));
            H.ShowBanner(T("w.titan.dragonfly"), UIStyle.Good);
            Go("drone");
        }

        void TickDrone(float dt)
        {
            bool rain = _rainT >= 0f;
            _fly.Step(In.MoveV, Cam.Yaw, In.AHeld && !(rain && Random.value < 0.1f), dt);
            var p = _drone.transform.position;
            if (_wash != null)
            {
                var g = p; g.y = _fly.Surface + 0.2f;
                _wash.transform.position = g;
                WorldFX.Rate(_wash, !_fly.Landed && In.AHeld ? 90f * Mathf.Clamp01((8f - _fly.Altitude) / 8f) : 0f);
            }
            if (_fly.Landed && Ter.InWater(p)) { Fail("w.fail.lake"); return; }
            if (!_fly.Landed && _fly.Battery < 0.15f) _lowBattery = true;
            if (!_fly.Landed && _fly.Battery <= 0f && _fly.Altitude > 2f) { Fail("w.fail.battery.air"); return; }

            Site here = null;
            foreach (var s in Sites) if (!s.Done && Near(s, p)) here = s;

            H.SetButton(H.BtnA, T("w.btn.lift"), _fly.Battery > 0f ? BtnState.On : BtnState.Off, _fly.Battery, T("w.battery.dead"));
            BtnState b = here != null && _fly.Landed ? BtnState.Pulse : BtnState.Off;
            float need = SampleTime * 0.85f;
            H.SetButton(H.BtnB, SampleBtn, b, _sample / need, T("w.titan.land.first"));
            if (here != null && _fly.Landed && BHeld)
            {
                _sample += dt;
                FxDrill(p, dt);
                if (_sample >= need)
                {
                    _sample = 0f; MarkDone(here); _samples++;
                    Score(SamplePts(600), "w.titan.log.sample", T(here.Key));
                    FxSample(p);
                    if (here.Special && Has(ScienceInstrument.UVSpectrometer)) { _organics++; Score(400, "w.titan.log.selk"); }
                    H.ShowBanner(F("w.sample.got", _samples, 3), UIStyle.Good);
                }
            }
            else if (!BHeld) _sample = 0f;

            H.SetButton(H.BtnC, T("w.btn.scan"), Has(ScienceInstrument.UVSpectrometer) ? BtnState.On : BtnState.Off, 0f, T("w.need.uv"));
            if (CPressed)
            {
                AudioFX.Play(Sfx.Scan);
                H.SetPrompt(here != null ? T(here.Special ? "w.titan.scan.selk" : "w.titan.scan.organics") : T("w.titan.scan.air"), 4f);
            }

            // Methane rain: drops bigger than on Earth, falling slowly
            if (_rainT < 0f && PhaseT > 40f && PhaseT < 41f) { _rainT = 0f; Env.SetWeatherAmount(1f); Env.SetFog(P.Fog * 0.9f, P.FogDensity * 1.8f); H.ShowBanner(T("w.titan.rain"), UIStyle.Accent, 3.5f); }
            if (_rainT >= 0f) { _rainT += dt; if (_rainT > 25f) { _rainT = -2f; Env.SetWeatherAmount(0f); Env.SetFog(P.Fog, P.FogDensity); } }

            H.SetMeter(0, T("w.m.battery"), $"{_fly.Battery * 100f:F0}%{(_fly.Landed ? " ▲" : "")}", _fly.Battery, _fly.Battery < 0.2f ? UIStyle.Bad : UIStyle.Good);
            H.SetMeter(1, T("w.m.alt"), $"{Mathf.Max(0f, _fly.Altitude):F1} m", Mathf.Clamp01(_fly.Altitude / 30f), UIStyle.Accent);
            H.SetMeter(2, T("w.m.samples"), $"{_samples}/3", _samples / 3f, UIStyle.Gold);
            PointTo(NextSite());
            H.SetChecklist(Checklist(("w.ck.land", true), ("w.ck.titan3", _samples >= 3), ("w.ck.nolow", !_lowBattery)));

            if (_samples >= 3) Complete(_lowBattery ? 2 : 3);
        }

        public override int StarsSoFar => Result.Landed ? 1 : 0;
    }
}
