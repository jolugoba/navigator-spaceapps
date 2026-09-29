// Navigator 2.0 — MercuryMission.cs · "Ice next to the Sun" (docs/mundos/01_MERCURIO.md)
// No air: brake from orbit with rockets, legs down, land on the sunlit rim of a polar crater.
// Then the thermal rover goes down into eternal shadow for ice, and back out to the Sun before
// it freezes — or cooks. The thermometer is the whole game here.
// Guacalita Inc. — SAL / Claude

using UnityEngine;
using Navigator.Core;
using Navigator.Game;
using Navigator.UI;

namespace Navigator.Worlds
{
    public class MercuryMission : RocketLandingMission
    {
        static readonly Vector2 CraterAt = new(70f, 70f);
        const float CraterR = 95f;

        GameObject _rover;
        GroundVehicle _car;
        Light _lights;
        bool _lightsOn, _irScanned, _redZone;
        float _tempC = 20f, _drill;
        int _ice;
        Site _home;
        ParticleSystem _wheelDust;

        public override void Shape(WorldTerrain t)
        {
            t.AddCrater(CraterAt, CraterR, 16f);
            // Floor in permanent shadow: almost black (the Sun never gets in)
            t.AddPaint(CraterAt, CraterR * 0.8f, new Color(0.03f, 0.03f, 0.035f), 0.95f);
            PadLocal = CraterAt + new Vector2(-1f, -1f).normalized * (CraterR + 28f);
            t.AddFlat(PadLocal, PadRadius);
            // Ice patches inside (only seen with lights or the infrared mapper)
            foreach (var o in IceOffsets) t.AddPaint(CraterAt + o, 5f, new Color(0.35f, 0.42f, 0.5f), 0.7f);
        }

        static readonly Vector2[] IceOffsets = { new(-25f, 10f), new(15f, 30f), new(20f, -22f) };

        bool InShadow(Vector3 w) =>
            new Vector2(w.x - Ter.Center.x - CraterAt.x, w.z - Ter.Center.z - CraterAt.y).magnitude < CraterR * 0.8f;

        public override void Begin()
        {
            H.SetTitle(T("w.merc.title"), T("w.merc.goal.land"));
            H.ShowBanner(T("w.merc.banner"), UIStyle.Warn, 3.5f);
            Vector3 pad = Ter.Surface(PadLocal);
            var start = pad + new Vector3(-170f, 360f, -120f);
            var toPad = (pad - start); toPad.y = 0f;
            StartRocketLanding(start, toPad.normalized * 26f + Vector3.down * 6f, 7f, 48f);
            Result.Lines.Add(T("w.merc.log.start"));
        }

        public override void Tick(float dt)
        {
            if (Phase == "landing") { if (TickRocketLanding(dt)) StartRover(); }
            else if (Phase == "rover") TickRover(dt);
        }

        void StartRover()
        {
            var at = Lander.transform.position + Lander.transform.right * 5f;
            _rover = SpawnVehicle(A.ThermalRover, "thermalrover", 3f, at, true);
            _car = new GroundVehicle { T = _rover.transform, Terrain = Ter, Gravity = P.Gravity, MaxSpeed = 5f, TurnRate = 65f };
            var toCrater = Ter.Surface(CraterAt) - at;
            _car.Place(at, Quaternion.LookRotation(new Vector3(toCrater.x, 0f, toCrater.z)).eulerAngles.y);
            _wheelDust = WheelDust(_rover.transform);

            var lg = new GameObject("Headlights");
            lg.transform.SetParent(_rover.transform, false);
            lg.transform.localPosition = new Vector3(0f, 1.4f, 1.4f);
            lg.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
            _lights = lg.AddComponent<Light>();
            _lights.type = LightType.Spot;
            _lights.range = 45f; _lights.spotAngle = 70f; _lights.intensity = 3f;
            _lights.color = new Color(1f, 0.97f, 0.9f);
            _lights.enabled = false;

            foreach (var o in IceOffsets)
            {
                var s = AddSite("w.merc.site.ice", Ter.Surface(CraterAt + o), 5f, A.IceChunk, 1.6f);
                s.Marker.SetActive(false);   // hidden until the lights or the infrared find it
            }
            _home = new Site { Key = "w.merc.site.lander", Pos = Lander.transform.position, Radius = 10f };

            D.Focus = _rover.transform;
            Cam.Target = _rover.transform; Cam.Distance = 9f; Cam.Height = 3f; Cam.Pitch = 14f; Cam.AutoFollow = true;
            Cam.SnapBehind();
            H.SetTitle(T("w.merc.title"), T("w.merc.goal.ice"));
            H.SetPrompt(T("w.merc.rover.hint"));
            H.ShowBanner(T("w.rover.out"), UIStyle.Good);
            Go("rover");
        }

        void TickRover(float dt)
        {
            var pos = _rover.transform.position;
            bool shadow = InShadow(pos);

            // Ice appears within the headlights' reach, or everywhere after an infrared scan
            foreach (var s in Sites)
                if (!s.Done && !s.Marker.activeSelf && (_irScanned || (_lightsOn && Vector3.Distance(s.Pos, pos) < 14f)))
                    s.Marker.SetActive(true);

            Site here = null;
            foreach (var s in Sites) if (!s.Done && s.Marker.activeSelf && Near(s, pos)) here = s;

            // A: drill ice
            bool hasDrill = Has(ScienceInstrument.SampleDrill);
            H.SetButton(H.BtnA, T("w.btn.drill"), hasDrill && here != null ? BtnState.Pulse : BtnState.Off, _drill / 3f,
                        hasDrill ? T("w.drill.go") : T("w.need.drill"));
            bool drilling = here != null && AHeld && _car.Battery > 0.02f;
            if (drilling)
            {
                _drill += dt;
                FxDrill(here.Pos, dt);
                if (_drill >= 3f)
                {
                    _drill = 0f; MarkDone(here); _ice++;
                    Score(700, "w.merc.log.ice", _ice);
                    FxSample(here.Pos);
                    H.ShowBanner(F("w.sample.got", _ice, 3), UIStyle.Good);
                }
            }
            else if (!AHeld) _drill = Mathf.MoveTowards(_drill, 0f, dt);

            // B: headlights
            H.SetButton(H.BtnB, _lightsOn ? T("w.btn.lightsoff") : T("w.btn.lights"), shadow && !_lightsOn ? BtnState.Pulse : BtnState.On);
            if (BPressed) { _lightsOn = !_lightsOn; _lights.enabled = _lightsOn; AudioFX.Play(Sfx.Beep, 0.5f); }

            // C: infrared scan (finds the ice: it's colder than the rock around it)
            bool ir = Has(ScienceInstrument.InfraredMapper);
            H.SetButton(H.BtnC, T("w.btn.irscan"), ir ? (_irScanned ? BtnState.On : BtnState.Pulse) : BtnState.Off, 0f, T("w.need.ir"));
            if (CPressed && ir && !_irScanned)
            {
                _irScanned = true;
                AudioFX.Play(Sfx.Scan);
                Score(300, "w.merc.log.ir");
                H.ShowBanner(T("w.merc.ir.found"), UIStyle.Accent);
            }

            _car.Step(In.MoveV, dt);
            WheelDustRate(_wheelDust, _car.Speed, _car.Airborne);

            // Thermometer: the Sun cooks, the shadow freezes; the parasol and the RTG change the maths
            var sunDir = Quaternion.Euler(0f, P.SunAzimuth + 180f, 0f) * Vector3.back;   // toward the Sun, horizontal
            bool facingSun = Vector3.Dot(_rover.transform.forward, sunDir) > 0.4f;
            float rate = shadow ? -3f : (facingSun ? 1.2f : 2.6f);
            if (Design != null && Design.Power != PowerSystem.Solar) rate += 1f;   // RTG / reactor heat
            if (_lightsOn) rate += 0.3f;
            if (drilling) rate += 1f;
            _tempC += rate * dt;
            if (_tempC < -40f || _tempC > 70f) _redZone = true;
            if (_tempC <= -60f) { Fail("w.fail.frozen"); return; }
            if (_tempC >= 90f) { Fail("w.fail.overheat"); return; }

            float charge = ChargeRate(shadow ? 0f : 1f) * dt;
            float drain = (_lightsOn ? 0.003f : 0f) * dt + (drilling ? 0.02f * dt : 0f);
            _car.Battery = Mathf.Clamp01(_car.Battery + charge - drain);

            Color tc = _tempC < -40f || _tempC > 70f ? UIStyle.Bad : _tempC < -15f || _tempC > 45f ? UIStyle.Warn : UIStyle.Good;
            H.SetMeter(0, T("w.m.temp"), $"{_tempC:F0} °C", Mathf.InverseLerp(-60f, 90f, _tempC), tc);
            H.SetMeter(1, T("w.m.battery"), $"{_car.Battery * 100f:F0}%", _car.Battery, _car.Battery < 0.2f ? UIStyle.Bad : UIStyle.Good);
            H.SetMeter(2, T("w.m.ice"), $"{_ice}/3", _ice / 3f, UIStyle.Accent);
            if (tc == UIStyle.Bad) H.SetPrompt(T(shadow ? "w.merc.tooCold" : "w.merc.tooHot"), 0.3f);

            if (_ice < 3)
            {
                var next = NextSite();
                if (next != null && next.Marker.activeSelf) PointTo(next);
                else H.SetArrow(Ter.Surface(CraterAt), T("w.merc.crater"));
            }
            else PointTo(_home);
            H.SetChecklist(Checklist(("w.ck.land", true), ("w.ck.ice3", _ice >= 3), ("w.ck.back", false)));

            if (_car.Battery <= 0f) { H.ShowBanner(T("w.battery.dead"), UIStyle.Bad, 4f); Abort(StarsSoFar); return; }
            if (_ice >= 3 && Near(_home, pos))
            {
                Score(400, "w.log.back");
                Complete(_redZone ? 2 : 3);
            }
        }

        public override int StarsSoFar => Result.Landed ? 1 : 0;
    }
}
