// Navigator 2.0 — MarsMission.cs · "The 7 minutes of terror" (docs/mundos/03_MARTE.md)
// Entry → parachute in its Mach window → drop the heat shield → powered descent to a safe
// (green) zone → sky crane lowers the rover → cut the cables → drive Jezero, drill 3 rocks,
// leave the tubes at the depot (like Perseverance's Three Forks depot).
// Guacalita Inc. — SAL / Claude

using System.Collections.Generic;
using UnityEngine;
using Navigator.Core;
using Navigator.Game;
using Navigator.UI;

namespace Navigator.Worlds
{
    public class MarsMission : WorldMission
    {
        GameObject _capsule, _chute, _shield, _stack, _rover, _crane;
        LineRenderer _cables;
        DescentCraft _craft;
        GroundVehicle _car;
        ParticleSystem _plasma, _flame;
        readonly List<GameObject> _pads = new();
        readonly List<(Vector2 c, float r)> _sand = new();
        static readonly Vector2[] PadAt = { new(0f, 0f), new(-70f, 50f), new(80f, -40f) };
        const float PadR = 14f;

        float _chuteQuality = 1f, _hover, _roverDrop, _drill, _stormT = -1f, _nextDevil;
        bool _shieldOff, _radarBlind, _cut, _onPad;
        int _samples, _deposited, _organics;
        Site _depot;
        ParticleSystem _wheelDust;

        public override void Shape(WorldTerrain t)
        {
            foreach (var p in PadAt) t.AddFlat(p, PadR);
            t.AddCrater(new Vector2(-150f, 90f), 42f, 7f);           // crater floor site
            t.AddFlat(new Vector2(25f, 30f), 8f);                   // depot
            foreach (var s in SiteAt) t.AddClear(s, 9f);            // no boulder sits on a sample
            // Soft sand patches (Spirit got stuck in one in 2009)
            var rnd = new System.Random(7);
            for (int i = 0; i < 7; i++)
            {
                var c = new Vector2((float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f) * 360f;
                float r = 8f + (float)rnd.NextDouble() * 8f;
                if (c.magnitude < 40f || NearKeySpot(c, r + 18f)) continue;   // never on a pad, the depot or a site
                _sand.Add((c, r));
                t.AddPaint(c, r, new Color(0.93f, 0.72f, 0.5f), 0.8f);
            }
            t.AddPaint(new Vector2(120f, 150f), 20f, new Color(0.78f, 0.62f, 0.45f), 0.6f);   // delta layers
            t.AddPaint(new Vector2(60f, -170f), 18f, new Color(0.85f, 0.8f, 0.7f), 0.6f);     // carbonate shore
        }

        static readonly Vector2[] SiteAt = { new(120f, 150f), new(-150f, 90f), new(60f, -170f) };
        static readonly Vector2 DepotAt = new(25f, 30f);

        static bool NearKeySpot(Vector2 c, float r)
        {
            foreach (var p in PadAt) if ((c - p).magnitude < r + PadR) return true;
            foreach (var s in SiteAt) if ((c - s).magnitude < r) return true;
            return (c - DepotAt).magnitude < r;
        }

        Vector3 _roverStart; float _roverYaw;

        public override bool Recover()
        {
            if (Phase != "rover" || _car == null) return false;
            _car.Place(_roverStart, _roverYaw);
            Cam.SnapBehind();
            return true;
        }

        public override void Begin()
        {
            H.SetTitle(T("w.mars.title"), T("w.mars.goal.entry"));
            Env.SetWeatherAmount(0.6f);
            Vector3 top = Ter.Surface(new Vector2(-40f, -260f)) + Vector3.up * 1400f;
            _capsule = Spawn(A.EntryCapsule, "capsule", 3.2f, top);
            _plasma = Plasma(_capsule.transform);
            D.Focus = _capsule.transform;
            Cam.Target = _capsule.transform; Cam.Distance = 20f; Cam.Height = 5f; Cam.Pitch = 8f; Cam.AutoFollow = false;
            Cam.Yaw = 20f; Cam.SnapBehind();
            SetButtons(null, BtnState.Hidden, null, BtnState.Hidden, null, BtnState.Hidden);
            H.ShowBanner(T("w.mars.banner"), UIStyle.Warn, 3.5f);
            H.SetPrompt(T("w.entry.hint"));
            EntryStart();
            Go("entry");
            Result.Lines.Add(T("w.mars.log.start"));
        }

        GameObject Spawn(GameObject model, string shape, float size, Vector3 at)
        {
            GameObject go;
            if (model != null) go = A.Spawn(model, PrimitiveType.Cube, Vector3.one, Color.white, size, Root, alignLong: shape == "rover");
            else go = VehicleShapes.Build(A, shape, Root);
            go.transform.position = at;
            return go;
        }

        void SetButtons(string a, BtnState sa, string b, BtnState sb, string c, BtnState sc, string reasonA = null, string reasonC = null)
        {
            H.SetButton(H.BtnA, a, sa, 0f, reasonA);
            H.SetButton(H.BtnB, b, sb);
            H.SetButton(H.BtnC, c, sc, 0f, reasonC);
        }

        public override void Tick(float dt)
        {
            switch (Phase)
            {
                case "entry":     TickEntry(dt); break;
                case "chute":     TickChute(dt); break;
                case "shield":    TickShield(dt); break;
                case "chutefall": TickChuteFall(dt); break;
                case "powered":   TickPowered(dt); break;
                case "crane":     TickCrane(dt); break;
                case "rover":     TickRover(dt); break;
            }
        }

        // ── 1. Entry ─────────────────────────────────────────────────────────
        void TickEntry(float dt)
        {
            _capsule.transform.position += new Vector3(0f, -60f, 25f) * dt;
            _capsule.transform.rotation = Quaternion.Euler(-15f, 0f, 0f);
            if (!EntryTick(dt, 10f, 0.7f, 0.55f)) return;
            if (Finished) return;
            Score(300, "w.log.entry", Mathf.RoundToInt((1f - EntryHeat) * 100f));
            if (_plasma != null) Object.Destroy(_plasma.gameObject);
            H.SetTitle(T("w.mars.title"), T("w.mars.goal.chute"));
            H.SetPrompt(T("w.chute.hint"));
            SetButtons(null, BtnState.Hidden, T("w.btn.chute"), BtnState.Pulse, null, BtnState.Hidden);
            Go("chute");
        }

        // ── 2. Parachute in its window (Mach 1.2 – 2.0) ──────────────────────
        void TickChute(float dt)
        {
            _capsule.transform.position += new Vector3(0f, -40f, 15f) * dt;
            float needle = 1f - Mathf.Clamp01(PhaseT / 8f);
            float r = WindowTick(8f, 0.25f, 0.75f, F("w.chute.mach", 0.8f + 1.6f * needle));
            if (r < 0f) return;
            if (r > 0.75f) { _chuteQuality = 0.4f; Result.Lines.Add(T("w.mars.log.torn")); H.ShowBanner(T("w.chute.torn"), UIStyle.Bad); }
            else if (r < 0.25f) { _chuteQuality = 0.55f; Result.Lines.Add(T("w.mars.log.late")); H.ShowBanner(T("w.chute.late"), UIStyle.Warn); }
            else { _chuteQuality = 1f; Score(300, "w.log.chute.ok"); H.ShowBanner(T("w.chute.ok"), UIStyle.Good); }
            _chute = Spawn(A.Parachute, "parachute", 14f * Mathf.Lerp(0.6f, 1f, _chuteQuality), _capsule.transform.position + Vector3.up * 16f);
            _chute.transform.SetParent(_capsule.transform, true);
            AudioFX.Play(Sfx.Beep);
            FxJolt();
            Cam.Distance = 30f; Cam.Height = 8f;
            H.SetTitle(T("w.mars.title"), T("w.mars.goal.shield"));
            H.SetPrompt(T("w.shield.hint"));
            SetButtons(null, BtnState.Hidden, T("w.btn.shield"), BtnState.Pulse, null, BtnState.Hidden);
            Go("shield");
        }

        // ── 3. Drop the heat shield (the radar can't see through it) ─────────
        void TickShield(float dt)
        {
            _capsule.transform.position += Vector3.down * 25f * dt;
            bool pressed = BPressed;
            if (!pressed && PhaseT < 6f) return;
            _shieldOff = pressed;
            if (!pressed) { _radarBlind = true; Result.Lines.Add(T("w.mars.log.shieldlate")); H.ShowBanner(T("w.shield.auto"), UIStyle.Warn); }
            else Score(150);
            var shield = Spawn(A.HeatShield, "capsule", 3f, _capsule.transform.position + Vector3.down * 1.5f);
            _shield = shield;
            FxJolt(0.4f);
            SetButtons(null, BtnState.Hidden, null, BtnState.Hidden, null, BtnState.Hidden);
            H.SetPrompt("");
            Go("chutefall");
        }

        void TickChuteFall(float dt)
        {
            _capsule.transform.position += Vector3.down * 25f * dt;
            if (_shield != null)
            {
                _shield.transform.position += Vector3.down * 45f * dt;
                _shield.transform.Rotate(90f * dt, 40f * dt, 25f * dt);   // tumbles away
            }
            H.SetMeter(0, T("w.m.alt"), $"{(_capsule.transform.position.y - Ter.Center.y):F0} m", 1f, UIStyle.Accent);
            if (PhaseT < 3f) return;
            if (_shield != null) Object.Destroy(_shield);
            StartPowered();
        }

        // ── 4. Powered descent: the sky crane with the rover under it ────────
        void StartPowered()
        {
            Vector3 at = _capsule.transform.position;
            at.y = Ter.Height(at) + 420f;
            Object.Destroy(_capsule);
            _stack = new GameObject("DescentStack");
            _stack.transform.SetParent(Root, false);
            _stack.transform.position = at;
            _crane = Spawn(A.SkyCrane, "skycrane", 3.6f, at);
            _crane.transform.SetParent(_stack.transform, true);
            _rover = Spawn(A.Rover, "rover", 3f, at + Vector3.down * 2.4f);   // hangs under the crane
            _rover.transform.SetParent(_stack.transform, true);
            _flame = Flame(_crane.transform, Vector3.zero);

            _craft = new DescentCraft
            {
                T = _stack.transform, Terrain = Ter, Gravity = P.Gravity, AirDensity = 0f,
                MaxThrustAcc = 8.5f, TiltMax = 22f, Yaw = 0f,
                Vel = new Vector3(3f, -32f - (1f - _chuteQuality) * 25f, 6f),
            };
            _craft.FuelMax = _craft.Fuel = DescentFuel(34f) * Mathf.Lerp(0.8f, 1f, _chuteQuality);

            foreach (var p in PadAt)
            {
                var disc = WorldAssets.Primitive(PrimitiveType.Cylinder, Root, A.NewMaterial(A.PropMaterial, new Color(0.3f, 1f, 0.5f), 0.2f));
                disc.transform.position = Ter.Surface(p) + Vector3.up * 0.15f;
                disc.transform.localScale = new Vector3(PadR * 2f, 0.05f, PadR * 2f);
                _pads.Add(disc);
            }
            D.Focus = _stack.transform;
            Cam.Target = _stack.transform; Cam.Distance = 16f; Cam.Height = 7f; Cam.Pitch = 28f; Cam.AutoFollow = false;
            H.SetTitle(T("w.mars.title"), T("w.mars.goal.powered"));
            H.SetPrompt(T("w.powered.hint"));
            SetButtons(T("w.btn.thrust"), BtnState.On, null, BtnState.Hidden, null, BtnState.Hidden);
            H.ShowBanner(T("w.mars.sep"), UIStyle.Accent);
            FxJolt(0.5f);
            Go("powered");
        }

        void TickPowered(float dt)
        {
            _craft.ThrustAllowed = true;
            _craft.Control(In.MoveV, Cam.Yaw, AHeld, dt);
            _craft.Step(dt);
            SetFlame(_flame, _craft.Throttle);
            FxRocketDust(_stack.transform.position, _craft.Altitude, _craft.Throttle);
            DescentMeters(_craft);
            if (_radarBlind && _craft.Altitude > 200f) H.SetMeter(0, T("w.m.alt"), "???", 0.5f, UIStyle.Warn);
            H.SetFill(H.BtnA, _craft.Fuel / _craft.FuelMax);
            H.SetArrow(NearestPad(), T("w.pad"));

            float alt = _craft.Altitude;
            // Hover low and slow: the crane can lower the rover
            if (alt < 26f && alt > 11f && Mathf.Abs(_craft.VerticalSpeed) < 1.8f && _craft.SideSpeed < 1.8f)
            {
                _hover += dt;
                H.SetPrompt(F("w.crane.hold", Mathf.Max(0f, 2f - _hover)));
                if (_hover >= 2f) StartCrane();
            }
            else
            {
                _hover = 0f;
                if (alt < 60f) H.SetPrompt(T("w.crane.hint"));
            }
            if (alt <= 0.3f)
            {
                // Landed the whole stack on its engines: possible, but not how Mars is done
                string problem = _craft.TouchdownProblem();
                if (problem != null) { Fail(problem); return; }
                Result.Lines.Add(T("w.mars.log.nocrane"));
                Score(400);
                Touchdown(_stack.transform.position);
                _crane.transform.SetParent(Root, true);
                _rover.transform.SetParent(Root, true);
                SetFlame(_flame, 0f);
                StartRover();
            }
        }

        Vector3? NearestPad()
        {
            Vector3 best = Vector3.zero; float bd = float.MaxValue;
            foreach (var p in PadAt)
            {
                var w = Ter.Surface(p);
                float d = (w - _stack.transform.position).sqrMagnitude;
                if (d < bd) { bd = d; best = w; }
            }
            return best;
        }

        // ── 5. Sky crane: lower the rover on cables, cut when the wheels touch ─
        void StartCrane()
        {
            _cables = new GameObject("Cables").AddComponent<LineRenderer>();
            _cables.transform.SetParent(Root, false);
            _cables.positionCount = 2;
            _cables.widthMultiplier = 0.06f;
            if (A.PropMaterial != null) _cables.sharedMaterial = A.NewMaterial(A.PropMaterial, Color.white, 0.2f);
            _rover.transform.SetParent(Root, true);
            _roverDrop = 0f;
            H.SetTitle(T("w.mars.title"), T("w.mars.goal.crane"));
            H.SetPrompt(T("w.crane.lowering"));
            SetButtons(T("w.btn.thrust"), BtnState.On, null, BtnState.Hidden, T("w.btn.cut"), BtnState.Off, null, T("w.cut.notyet"));
            Go("crane");
        }

        void TickCrane(float dt)
        {
            _craft.Control(In.MoveV, Cam.Yaw, AHeld, dt);
            _craft.Step(dt);
            SetFlame(_flame, _craft.Throttle);
            FxRocketDust(_stack.transform.position, _craft.Altitude, _craft.Throttle);
            DescentMeters(_craft);

            _roverDrop = Mathf.Min(9f, _roverDrop + 2.2f * dt);   // cables pay out at ~0.75 m/s (sped up)
            Vector3 hang = _stack.transform.position + Vector3.down * (2.4f + _roverDrop);
            float ground = Ter.Height(hang);
            bool touching = hang.y <= ground + 0.05f;
            if (touching) hang.y = ground;
            _rover.transform.position = hang;
            _rover.transform.rotation = Quaternion.Euler(0f, _stack.transform.eulerAngles.y, 0f);
            _cables.SetPosition(0, _stack.transform.position);
            _cables.SetPosition(1, hang + Vector3.up * 1.6f);

            H.SetButton(H.BtnC, T("w.btn.cut"), touching ? BtnState.Pulse : BtnState.On);
            if (_craft.Altitude < 5.5f) { Fail("w.fail.cranelow"); return; }
            if (CPressed)
            {
                if (!touching && hang.y - ground > 1.5f) { Fail("w.fail.dropped"); return; }
                _cut = true;
                WorldFX.Sparks(Root, _stack.transform.position + Vector3.down * 1.5f);
                Object.Destroy(_cables.gameObject);
                Score(600, "w.mars.log.crane");
                Touchdown(hang);
                StartRover();
            }
        }

        void Touchdown(Vector3 at)
        {
            Result.Landed = true;
            FxRocketDustOff();
            FxTouchdown(at);
            AudioFX.Play(Sfx.Hit, 0.6f);
            foreach (var p in PadAt)
                if (new Vector2(at.x - (Ter.Center.x + p.x), at.z - (Ter.Center.z + p.y)).magnitude < PadR) _onPad = true;
            if (_onPad) Score(300, "w.log.pad");
            else if (Ter.HitsRock(at, 1.6f, out _)) { Result.Lines.Add(T("w.mars.log.rocky")); }
            foreach (var d in _pads) Object.Destroy(d);
            _pads.Clear();
            H.SetLanding(false, 0, 0, 0);
            H.ShowBanner(T("w.mars.landed"), UIStyle.Good, 3f);
        }

        // ── 6. Rover: 3 samples to the depot ─────────────────────────────────
        void StartRover()
        {
            if (_crane != null)
            {
                if (_crane.transform.parent == _stack.transform) _crane.transform.SetParent(Root, true);
                D.StartCoroutine(FlyAway(_crane.transform));
            }
            _rover.transform.SetParent(Root, true);
            _car = new GroundVehicle
            {
                T = _rover.transform, Terrain = Ter, Gravity = P.Gravity, MaxSpeed = 5f, TurnRate = 65f, Radius = 1.5f,
                IsSoftSand = pos => { foreach (var s in _sand) if (new Vector2(pos.x - Ter.Center.x - s.c.x, pos.z - Ter.Center.z - s.c.y).magnitude < s.r) return true; return false; },
            };
            _car.Place(_rover.transform.position, _stack != null ? _stack.transform.eulerAngles.y : 0f);
            _roverStart = _rover.transform.position; _roverYaw = _car.Heading;
            _wheelDust = WheelDust(_rover.transform);
            if (_stack != null) Object.Destroy(_stack, 6f);

            // Three rocks, one of them hides organic molecules
            int special = new System.Random(System.DateTime.Now.Millisecond).Next(3);
            var s1 = AddSite("w.mars.site.delta",  Ter.Surface(new Vector2(120f, 150f)), 5f, A.MarsLayeredRock, 3f);
            var s2 = AddSite("w.mars.site.floor",  Ter.Surface(new Vector2(-150f, 90f)), 5f, A.MarsBoulder, 2.5f);
            var s3 = AddSite("w.mars.site.shore",  Ter.Surface(new Vector2(60f, -170f)), 5f, A.MarsLayeredRock, 2.5f);
            (special == 0 ? s1 : special == 1 ? s2 : s3).Special = true;
            _depot = new Site { Key = "w.mars.depot", Pos = Ter.Surface(new Vector2(25f, 30f)), Radius = 6f };
            var depotMark = WorldAssets.Primitive(PrimitiveType.Cylinder, Root, A.NewMaterial(A.PropMaterial, new Color(1f, 0.85f, 0.3f), 0.4f));
            depotMark.transform.position = _depot.Pos + Vector3.up * 0.05f;
            depotMark.transform.localScale = new Vector3(8f, 0.04f, 8f);

            D.Focus = _rover.transform;
            Cam.Target = _rover.transform; Cam.Distance = 9f; Cam.Height = 3f; Cam.Pitch = 14f; Cam.AutoFollow = true;
            Cam.SnapBehind();
            _nextDevil = 90f;
            H.SetTitle(T("w.mars.title"), T("w.mars.goal.rover"));
            H.SetPrompt(T("w.rover.hint"));
            H.SetMeter(2, "", "", 0f, UIStyle.Accent);
            Go("rover");
        }

        System.Collections.IEnumerator FlyAway(Transform crane)
        {
            float t = 0f;
            var flame = _flame;
            while (t < 5f && crane != null)
            {
                t += Time.deltaTime;
                crane.position += (Vector3.up * 8f + Vector3.right * 12f) * Time.deltaTime;
                SetFlame(flame, 1f);
                yield return null;
            }
            if (crane != null) Object.Destroy(crane.gameObject);
        }

        void TickRover(float dt)
        {
            bool drilling = false;
            Site here = null;
            foreach (var s in Sites) if (!s.Done && Near(s, _rover.transform.position)) here = s;
            bool atDepot = new Vector2(_depot.Pos.x - _rover.transform.position.x, _depot.Pos.z - _rover.transform.position.z).magnitude < _depot.Radius;

            // A: drill (needs the drill instrument)
            BtnState a = here != null ? BtnState.Pulse : BtnState.Off;
            H.SetButton(H.BtnA, SampleBtn, a, _drill / SampleTime, SampleHint);
            if (here != null && AHeld && _car.Battery > 0.02f)
            {
                drilling = true;
                _drill += dt;
                Cam.Shake(0.02f);
                FxDrill(here.Pos, dt);
                if (_drill >= SampleTime)
                {
                    _drill = 0f;
                    MarkDone(here);
                    _samples++;
                    Score(SamplePts(500), "w.mars.log.sample", T(here.Key));
                    FxSample(here.Pos);
                    if (here.Special && Has(ScienceInstrument.UVSpectrometer)) { _organics++; Score(400, "w.mars.log.organics"); }
                    H.ShowBanner(F("w.sample.got", _samples, 3), UIStyle.Good);
                }
            }
            else if (!AHeld) _drill = Mathf.MoveTowards(_drill, 0f, dt);

            // B: leave the tubes at the depot
            int carried = _samples - _deposited;
            H.SetButton(H.BtnB, T("w.btn.deposit"), atDepot && carried > 0 ? BtnState.Pulse : carried > 0 ? BtnState.Off : BtnState.Hidden,
                        0f, T("w.deposit.where"));
            if (BPressed && atDepot && carried > 0)
            {
                _deposited += carried;
                Score(300 * carried, "w.mars.log.deposit", carried);
                for (int i = 0; i < carried; i++)
                {
                    var tube = A.Spawn(A.SampleTube, PrimitiveType.Cylinder, new Vector3(0.12f, 0.35f, 0.12f), Color.white, 0.6f, Root);
                    tube.transform.position = _depot.Pos + new Vector3((_deposited - i) * 0.5f - 1.5f, 0.1f, 0f);
                    tube.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                }
                FxSample(_depot.Pos);
            }

            // C: scan the rock (the UV spectrometer says which one has organics)
            H.SetButton(H.BtnC, T("w.btn.scan"), here != null ? BtnState.On : BtnState.Off, 0f, T("w.scan.near"));
            if (CPressed && here != null)
            {
                AudioFX.Play(Sfx.Scan);
                string msg = !Has(ScienceInstrument.UVSpectrometer) ? T("w.mars.scan.nouv")
                           : here.Special ? T("w.mars.scan.organics") : T("w.mars.scan.plain");
                H.SetPrompt(T(here.Key) + ": " + msg, 5f);
            }

            // Drive
            _car.Step(In.MoveV, dt);
            WheelDustRate(_wheelDust, _car.Speed, _car.Airborne);
            float sun = _stormT >= 0f ? 0.4f : 1f;
            _car.Battery = Mathf.Clamp01(_car.Battery + ChargeRate(sun) * dt - (drilling ? 0.02f * dt : 0f));
            if (_car.Stuck) H.SetPrompt(T("w.rover.stuck"), 0.5f);

            // Dust storm halfway (Opportunity was lost to one in 2018), dust devils clean panels
            if (_stormT < 0f && PhaseT > 55f && PhaseT < 56f)
            {
                _stormT = 0f;
                Env.SetFog(P.Fog * 0.8f, P.FogDensity * 4f);
                Env.SetWeatherAmount(3f);
                H.ShowBanner(T("w.mars.storm"), UIStyle.Warn, 4f);
                Result.Lines.Add(T(Design != null && Design.Power == PowerSystem.Solar ? "w.mars.log.storm.solar" : "w.mars.log.storm.rtg"));
            }
            if (_stormT >= 0f)
            {
                _stormT += dt;
                if (_stormT > 30f) { _stormT = -2f; Env.SetFog(P.Fog, P.FogDensity); Env.SetWeatherAmount(0.6f); H.ShowBanner(T("w.mars.storm.end"), UIStyle.Good); }
            }
            if (PhaseT > _nextDevil && Design != null && Design.Power == PowerSystem.Solar)
            {
                _nextDevil = PhaseT + 999f;
                _car.Battery = Mathf.Min(1f, _car.Battery + 0.25f);
                H.ShowBanner(T("w.mars.devil"), UIStyle.Good, 3f);
            }

            H.SetMeter(0, T("w.m.battery"), $"{_car.Battery * 100f:F0}%", _car.Battery, _car.Battery < 0.2f ? UIStyle.Bad : UIStyle.Good);
            H.SetMeter(1, T("w.m.speed"), $"{Mathf.Abs(_car.Speed):F1} m/s", Mathf.Abs(_car.Speed) / _car.MaxSpeed, UIStyle.Accent);
            H.SetMeter(2, T("w.m.samples"), $"{_samples}/3 · {_deposited} ●", _samples / 3f, UIStyle.Gold);
            var next = _samples < 3 ? NextSite() : null;
            if (next != null) PointTo(next);
            else if (_samples > _deposited) PointTo(_depot);
            else H.SetArrow(null, null);
            H.SetChecklist(Checklist(("w.ck.land", true), ("w.ck.samples3", _samples >= 3), ("w.ck.depot", _deposited >= 3)));

            if (_car.Battery <= 0f && !_car.Airborne) { H.ShowBanner(T("w.battery.dead"), UIStyle.Bad, 4f); Abort(StarsSoFar); return; }
            if (_deposited >= 3)
            {
                bool three = _organics > 0 && _car.Battery > 0.2f;
                Complete(three ? 3 : 2);
            }
        }

        public override int StarsSoFar => !Result.Landed ? 0 : _deposited >= 3 ? 2 : 1;
    }
}
