// Navigator 2.0 — VenusMission.cs · "The oven" (docs/mundos/02_VENUS.md)
// Entry → parachute → sulfuric-acid clouds with 100 m/s winds (drop the parachute in time or
// the acid eats the probe) → free fall in air 65× denser than Earth's, steering to the flat
// plains → a Venera lander that can't move: photos, two drill samples and transmitting before
// the heat kills it (Venera 13 lasted 127 minutes).
// Guacalita Inc. — SAL / Claude

using System.Collections.Generic;
using UnityEngine;
using Navigator.Core;
using Navigator.Game;
using Navigator.UI;

namespace Navigator.Worlds
{
    public class VenusMission : WorldMission
    {
        static readonly Vector2 PlainsAt = new(0f, 0f);
        const float PlainsR = 60f;
        static readonly Vector2[] RoughAt = { new(-120f, 60f), new(110f, 90f), new(40f, -130f) };
        const float CloudTop = 1150f, CloudBottom = 780f;   // metres above the ground (game scale)

        GameObject _probe, _chute, _reticle;
        DescentCraft _craft;
        ParticleSystem _plasma;
        float _corrosion, _heat, _drill, _sent, _buffered, _life = 150f, _elapsed;
        bool _chuteOff;
        int _photos, _samples;
        readonly List<Vector3> _photoTargets = new();
        readonly List<GameObject> _photoMarks = new();
        readonly List<Vector3> _drillSpots = new();
        readonly List<bool> _drilled = new();
        Vector2 _arm;

        public override void Shape(WorldTerrain t)
        {
            t.AddFlat(PlainsAt, PlainsR * 0.6f);
            foreach (var r in RoughAt)
            {
                t.AddCrater(r, 35f, -6f);   // negative depth = a rough dome (tessera)
                t.AddPaint(r, 40f, new Color(0.25f, 0.2f, 0.17f), 0.7f);
            }
        }

        bool Rough(Vector3 w)
        {
            foreach (var r in RoughAt) if (new Vector2(w.x - Ter.Center.x - r.x, w.z - Ter.Center.z - r.y).magnitude < 38f) return true;
            return false;
        }

        public override void Begin()
        {
            H.SetTitle(T("w.venus.title"), T("w.venus.goal.entry"));
            H.ShowBanner(T("w.venus.banner"), UIStyle.Warn, 3.5f);
            Env.AddCloudLayer(Ter.Center.y + CloudTop, new Color(0.95f, 0.88f, 0.6f), 0.85f);
            Env.AddCloudLayer(Ter.Center.y + CloudBottom, new Color(0.9f, 0.75f, 0.45f), 0.7f);
            Env.SetFog(new Color(0.95f, 0.9f, 0.7f), 0.004f);
            Env.SetSky(new Color(0.55f, 0.6f, 0.7f), new Color(0.98f, 0.92f, 0.75f));   // above the clouds: pale sky

            var start = Ter.Surface(new Vector2(-150f, -120f)) + Vector3.up * 1500f;
            _probe = Spawn(A.EntryCapsule, "capsule", 3f, start);
            _plasma = Plasma(_probe.transform);
            D.Focus = _probe.transform;
            Cam.Target = _probe.transform; Cam.Distance = 20f; Cam.Height = 5f; Cam.Pitch = 8f; Cam.AutoFollow = false;
            Cam.SnapBehind();
            H.SetPrompt(T("w.entry.hint"));
            EntryStart();
            Go("entry");
            Result.Lines.Add(T("w.venus.log.start"));
        }

        GameObject Spawn(GameObject model, string shape, float size, Vector3 at)
        {
            var go = model != null ? A.Spawn(model, PrimitiveType.Cube, Vector3.one, Color.white, size, Root) : VehicleShapes.Build(A, shape, Root);
            go.transform.position = at;
            return go;
        }

        public override void Tick(float dt)
        {
            switch (Phase)
            {
                case "entry":   TickEntry(dt); break;
                case "chute":   TickChute(dt); break;
                case "clouds":  TickFall(dt, true); break;
                case "fall":    TickFall(dt, false); break;
                case "surface": TickSurface(dt); break;
            }
        }

        void TickEntry(float dt)
        {
            _probe.transform.position += new Vector3(8f, -30f, 10f) * dt;
            if (!EntryTick(dt, 9f, 0.65f, 0.5f) || Finished) return;
            Score(300, "w.log.entry", Mathf.RoundToInt((1f - EntryHeat) * 100f));
            if (_plasma != null) Object.Destroy(_plasma.gameObject);
            H.SetTitle(T("w.venus.title"), T("w.venus.goal.chute"));
            H.SetPrompt(T("w.chute.hint"));
            H.SetButton(H.BtnB, T("w.btn.chute"), BtnState.Pulse);
            Go("chute");
        }

        void TickChute(float dt)
        {
            _probe.transform.position += Vector3.down * 25f * dt;
            float needle = 1f - Mathf.Clamp01(PhaseT / 7f);
            float r = WindowTick(7f, 0.3f, 0.7f, F("w.chute.speed", Mathf.Lerp(40f, 260f, needle)));
            if (r < 0f) return;
            bool ok = r >= 0.3f && r <= 0.7f;
            if (ok) Score(250, "w.log.chute.ok"); else Result.Lines.Add(T("w.venus.log.chutebad"));
            H.ShowBanner(T(ok ? "w.chute.ok" : "w.chute.late"), ok ? UIStyle.Good : UIStyle.Warn);
            FxJolt();

            // The Venera lander inside the capsule takes over
            var at = _probe.transform.position;
            Object.Destroy(_probe);
            _probe = Spawn(A.VenusLander, "venuslander", 3f, at);
            _chute = Spawn(A.Parachute, "parachute", 12f, at + Vector3.up * 14f);
            _chute.transform.SetParent(_probe.transform, true);
            _craft = new DescentCraft
            {
                T = _probe.transform, Terrain = Ter, Gravity = P.Gravity, AirDensity = P.AirDensity, ScaleHeight = 260f,
                Mass = 1500f, BodyCdA = 8f, ChuteCdA = ok ? 60f : 30f, Wind = 6f, WindDir = new Vector3(1f, 0f, 0.3f),
                Vel = new Vector3(0f, -30f, 0f), ThrustAllowed = false, Fuel = 0f, FuelMax = 1f,
            };
            D.Focus = _probe.transform;
            Cam.Target = _probe.transform; Cam.Distance = 22f; Cam.Height = 6f;
            H.SetTitle(T("w.venus.title"), T("w.venus.goal.clouds"));
            H.SetPrompt(T("w.venus.clouds.hint"));
            H.SetButton(H.BtnB, T("w.btn.chuteoff"), BtnState.On);
            H.SetButton(H.BtnC, T("w.btn.scan"), Has(ScienceInstrument.UVSpectrometer) ? BtnState.On : BtnState.Off, 0f, T("w.need.uv"));
            Go("clouds");
        }

        bool _cloudScanned;

        void TickFall(float dt, bool clouds)
        {
            // Steer the drift a little by tilting the airbrake
            var dir = Quaternion.Euler(0f, Cam.Yaw, 0f) * new Vector3(In.MoveV.x, 0f, In.MoveV.y);
            _craft.Vel += dir * 2.5f * dt;
            _craft.Step(dt);
            float alt = _craft.Altitude;
            bool inClouds = alt < CloudTop && alt > CloudBottom;
            if (inClouds)
            {
                _corrosion = Mathf.Min(1f, _corrosion + 0.03f * dt);
                if (Random.value < dt * 0.5f) { Env.Lightning(); AudioFX.Play(Sfx.Hit, 0.3f); }   // lightning in the clouds
            }

            // Below the clouds: dim orange light and thick haze
            float k = Mathf.InverseLerp(CloudBottom, CloudBottom - 300f, alt);
            Env.SetFog(Color.Lerp(new Color(0.95f, 0.9f, 0.7f), P.Fog, k), Mathf.Lerp(0.004f, P.FogDensity, k));
            Env.SetSky(Color.Lerp(new Color(0.55f, 0.6f, 0.7f), P.SkyTop, k), Color.Lerp(new Color(0.98f, 0.92f, 0.75f), P.SkyHorizon, k));

            H.SetMeter(0, T("w.m.alt"), $"{ShownAltitude(alt):F0} m", Mathf.Clamp01(alt / 1500f), UIStyle.Accent);
            H.SetMeter(1, T("w.m.acid"), $"{_corrosion * 100f:F0}%", _corrosion, _corrosion > 0.6f ? UIStyle.Bad : UIStyle.Warn);
            H.SetMeter(2, T("w.m.vspeed"), $"{_craft.VerticalSpeed:F1} m/s", Mathf.Clamp01(-_craft.VerticalSpeed / 40f), UIStyle.Good);
            H.SetArrow(Ter.Surface(PlainsAt), T("w.venus.plains"));
            if (_corrosion >= 1f) { Fail("w.fail.acid"); return; }

            if (CPressed && !_cloudScanned && inClouds)
            {
                _cloudScanned = true;
                AudioFX.Play(Sfx.Scan);
                Score(300, "w.venus.log.clouds");
                H.SetPrompt(T("w.venus.scan.clouds"), 4f);
                H.SetButton(H.BtnC, null, BtnState.Hidden);
            }

            if (clouds)
            {
                bool release = BPressed || alt < 320f;
                if (release)
                {
                    _chuteOff = true;
                    if (_chute != null) { _chute.transform.SetParent(Root, true); Object.Destroy(_chute, 3f); }
                    _craft.ChuteCdA = 0f;
                    FxJolt(0.4f);
                    if (alt < 320f) Result.Lines.Add(T("w.venus.log.chuteauto"));
                    else if (alt < CloudBottom) Score(250, "w.venus.log.chutegood");
                    H.SetButton(H.BtnB, null, BtnState.Hidden);
                    H.SetTitle(T("w.venus.title"), T("w.venus.goal.fall"));
                    H.SetPrompt(T("w.venus.fall.hint"));
                    Go("fall");
                }
                return;
            }

            if (alt > 0.3f) return;
            var pos = _probe.transform.position;
            if (Rough(pos)) { Fail("w.fail.tesserae"); return; }
            string problem = _craft.TouchdownProblem(vsMax: 6f, hsMax: 4f, tiltMax: 25f);
            if (problem != null && problem != "w.fail.slope") { Fail(problem); return; }
            pos.y = Ter.Height(pos);
            _probe.transform.position = pos;
            _probe.transform.rotation = Quaternion.identity;
            Result.Landed = true;
            FxTouchdown(pos);
            Score(1000, "w.venus.log.landed");
            if (new Vector2(pos.x - Ter.Center.x - PlainsAt.x, pos.z - Ter.Center.z - PlainsAt.y).magnitude < PlainsR) Score(300, "w.log.pad");
            Haptics.Buzz();
            StartSurface();
        }

        // ── On the ground: the clock of heat ─────────────────────────────────
        // The lander never moves: on the surface there is nothing to put back, and nothing is lost
        public override bool Recover() => Phase == "surface";

        void StartSurface()
        {
            var c = _probe.transform.position;
            float baseYaw = Random.Range(0f, 360f);
            for (int i = 0; i < 4; i++)
            {
                var d = Quaternion.Euler(0f, baseYaw + i * 90f + Random.Range(-20f, 20f), 0f) * Vector3.forward * 28f;
                var p = c + d; p.y = Ter.Height(p) + 1.5f;
                _photoTargets.Add(p);
                var m = WorldAssets.Primitive(PrimitiveType.Cylinder, Root, A.NewMaterial(A.PropMaterial, new Color(0.3f, 0.95f, 1f), 0.9f));
                m.transform.position = p + Vector3.up * 3f;
                m.transform.localScale = new Vector3(0.25f, 4f, 0.25f);
                _photoMarks.Add(m);
            }
            for (int i = 0; i < 2; i++)
            {
                var p = c + Quaternion.Euler(0f, i == 0 ? -35f : 35f, 0f) * Vector3.forward * 3.4f;
                p.y = Ter.Height(p);
                _drillSpots.Add(p);
                _drilled.Add(false);
                GameObject slab = A.VenusSlab != null ? Object.Instantiate(A.VenusSlab, Root) : null;
                if (slab != null) { ModelFit.FitToSize(slab, 1.2f); slab.transform.position = p; }
                var ring = WorldAssets.Primitive(PrimitiveType.Cylinder, Root, A.NewMaterial(A.PropMaterial, new Color(1f, 0.85f, 0.3f), 0.5f));
                ring.transform.position = p + Vector3.up * 0.05f;
                ring.transform.localScale = new Vector3(1.2f, 0.02f, 1.2f);
            }
            _reticle = WorldAssets.Primitive(PrimitiveType.Cylinder, Root, A.NewMaterial(A.PropMaterial, new Color(0.3f, 1f, 0.6f), 0.5f));
            _reticle.transform.localScale = new Vector3(0.6f, 0.03f, 0.6f);
            _arm = new Vector2(0f, 2.5f);

            // RTG heat inside the sealed sphere shortens its life (another double-edged choice)
            if (Design != null && Design.Power != PowerSystem.Solar) { _life *= 0.88f; Result.Lines.Add(T("w.venus.log.rtg")); }

            // First person from the top of the lander: look around with the RIGHT stick
            Cam.Target = _probe.transform; Cam.Distance = 0.6f; Cam.Height = 3.1f; Cam.LookAtHeight = 3f; Cam.Pitch = 10f; Cam.AutoFollow = false;
            Cam.MinPitch = -25f;
            H.SetTitle(T("w.venus.title"), T("w.venus.goal.surface"));
            H.SetPrompt(T("w.venus.surface.hint"));
            H.ShowBanner(T("w.venus.landed"), UIStyle.Good, 3f);
            Go("surface");
        }

        void TickSurface(float dt)
        {
            _elapsed += dt;
            var c = _probe.transform.position;

            // Arm: the left stick moves the drill over the ground (camera space)
            var mv = Quaternion.Euler(0f, Cam.Yaw, 0f) * new Vector3(In.MoveV.x, 0f, In.MoveV.y);
            _arm += new Vector2(mv.x, mv.z) * 2.2f * dt;
            _arm = Vector2.ClampMagnitude(_arm, 4.2f);
            var tip = new Vector3(c.x + _arm.x, 0f, c.z + _arm.y);
            tip.y = Ter.Height(tip) + 0.05f;
            _reticle.transform.position = tip;
            int spot = -1;
            for (int i = 0; i < _drillSpots.Count; i++)
                if (!_drilled[i] && new Vector2(tip.x - _drillSpots[i].x, tip.z - _drillSpots[i].z).magnitude < 0.7f) spot = i;

            bool hasDrill = Has(ScienceInstrument.SampleDrill);
            H.SetButton(H.BtnA, T("w.btn.drill"), hasDrill && spot >= 0 ? BtnState.Pulse : BtnState.Off, _drill / 3f,
                        hasDrill ? T("w.venus.drill.where") : T("w.need.drill"));
            bool drilling = spot >= 0 && AHeld;
            if (drilling)
            {
                _drill += dt;
                FxDrill(_drillSpots[spot], dt);
                if (_drill >= 3f) { _drill = 0f; _drilled[spot] = true; _samples++; _buffered += 2f; Score(400, "w.venus.log.sample", _samples); AudioFX.Play(Sfx.Pickup); FxSample(_drillSpots[spot]); }
            }
            else if (!AHeld) _drill = 0f;

            // B: photo when the view is centred on a marker
            int target = -1;
            var fwd = Cam.transform.forward;
            for (int i = 0; i < _photoTargets.Count; i++)
                if (_photoMarks[i] != null && Vector3.Angle(fwd, _photoTargets[i] - Cam.transform.position) < 9f) target = i;
            bool cam = Has(ScienceInstrument.WideAngleCamera) || Has(ScienceInstrument.NarrowAngleCamera);
            H.SetButton(H.BtnB, T("w.btn.photo"), cam && target >= 0 ? BtnState.Pulse : BtnState.Off, 0f, cam ? T("w.venus.photo.where") : T("w.need.camera"));
            if (BPressed && target >= 0)
            {
                Object.Destroy(_photoMarks[target]); _photoMarks[target] = null;
                _photos++; _buffered += 1f;
                Score(250, "w.venus.log.photo", _photos);
                H.Flash(Color.white, 0.8f);
                AudioFX.Play(Sfx.Scan);
            }

            // C: transmit to the orbiter while it is above the horizon
            bool orbiter = _elapsed > 5f && _elapsed < _life * 0.8f;
            H.SetButton(H.BtnC, T("w.btn.transmit"), orbiter && _buffered > 0f ? BtnState.On : BtnState.Off, 0f, orbiter ? T("w.nodata") : T("w.venus.noorbiter"));
            bool sending = CHeld && orbiter && _buffered > 0f;
            if (sending)
            {
                float s = Mathf.Min(_buffered, 0.35f * AntennaRate * dt);
                _buffered -= s; _sent += s;
            }

            // Heat: the lander dies when it runs out of "minutes"
            float heatRate = 1f + (drilling ? 0.6f : 0f) + (sending ? 0.3f : 0f);
            _heat += heatRate * dt;
            float left = Mathf.Max(0f, _life - _heat);
            H.SetMeter(0, T("w.m.life"), F("w.venus.minutes", left), left / _life, left < 30f ? UIStyle.Bad : UIStyle.Warn);
            H.SetMeter(1, T("w.m.temp"), "464 °C", 1f, UIStyle.Bad);
            H.SetMeter(2, T("w.m.data"), $"{_sent:F1}/{_photos + _samples * 2}", _photos + _samples * 2 > 0 ? _sent / (_photos + _samples * 2f) : 0f, UIStyle.Accent);
            H.SetData(true, _sent, _sent + _buffered);
            H.SetArrow(target < 0 ? NextPhoto() : (Vector3?)null, T("w.venus.photo"));
            H.SetChecklist(Checklist(("w.ck.land", true), ("w.ck.photos4", _photos >= 4), ("w.ck.samples2", _samples >= 2), ("w.ck.sent", _buffered <= 0.01f && _photos + _samples > 0)));

            if (left <= 0f)
            {
                int total = _photos + _samples * 2;
                int sentPts = Mathf.RoundToInt(_sent * 150f);
                Score(sentPts, "w.venus.log.sent", _sent, total);
                Score(0, "w.venus.log.lasted", _elapsed);
                bool all = _photos >= 4 && _samples >= 2 && _sent >= total - 0.05f;
                H.ShowBanner(T("w.venus.dead"), UIStyle.Bad, 3f);
                Complete(all ? (_elapsed >= 127f ? 3 : 2) : 1);   // 1 s = 1 real minute
            }
        }

        Vector3? NextPhoto()
        {
            for (int i = 0; i < _photoTargets.Count; i++) if (_photoMarks[i] != null) return _photoTargets[i];
            return null;
        }

        public override int StarsSoFar => Result.Landed ? 1 : 0;
    }
}
