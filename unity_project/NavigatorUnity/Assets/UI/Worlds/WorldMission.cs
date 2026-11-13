// Navigator 2.0 — WorldMission.cs
// Base of the 7 world missions: phase machine, the pieces every world shares (atmospheric entry,
// parachute timing, powered descent with the touchdown check, objectives with markers) and the
// link to the design (instruments, power, antenna, fuel left). Each world adds its own mission.
// Guacalita Inc. — SAL / Claude

using System.Collections.Generic;
using UnityEngine;
using Navigator.Core;
using Navigator.Game;
using Navigator.UI;

namespace Navigator.Worlds
{
    public class WorldResult
    {
        public bool   Landed;          // reached the ground (or, on the giants, survived entry)
        public bool   Success;
        public int    Points;
        public int    Stars;
        public string FailKey;
        public readonly List<string> Lines = new();   // localized lines for the debrief
    }

    public abstract class WorldMission
    {
        protected WorldDirector   D;
        protected WorldProfile    P;
        protected WorldHUD        H;
        protected WorldInput      In;
        protected WorldEnvironment Env;
        protected WorldCamera     Cam;
        protected WorldAssets     A;
        protected MissionDesign   Design;
        protected MissionOutcome  Flight;
        protected WorldTerrain    Ter => Env.Terrain;
        protected Transform       Root => Env.Root.transform;

        public WorldResult Result { get; private set; } = new();
        public bool Finished { get; private set; }

        protected string Phase { get; private set; } = "";
        protected float  PhaseT { get; private set; }

        public void Init(WorldDirector d, WorldProfile p, MissionDesign design, MissionOutcome flight)
        {
            D = d; P = p; Design = design; Flight = flight;
            H = d.Hud; In = d.Input; Env = d.Env; Cam = d.WorldCam; A = d.Assets;
            Result = new WorldResult();
            Finished = false;
        }

        // Before the world is built: add craters, pads, lakes, painted zones
        public virtual void Shape(WorldTerrain t) { }
        public abstract void Begin();
        public abstract void Tick(float dt);

        public void Update(float dt)
        {
            if (Finished) return;
            PhaseT += dt;
            Tick(dt);
        }

        protected void Go(string phase) { Phase = phase; PhaseT = 0f; }

        // Checkpoint button. Surface vehicles go back to where they were deployed, at rest, keeping
        // everything collected so far. False = nothing to go back to in this phase (the descent):
        // the director restarts the attempt from the top instead.
        public virtual bool Recover() => false;

        // ── Design links ─────────────────────────────────────────────────────
        protected bool Has(ScienceInstrument i) => Design != null && Design.Instruments.Contains(i);

        // Without the drill the arm can still scoop loose soil: slower, worth a third, and 3 stars are lost
        protected bool   HasDrill   => Has(ScienceInstrument.SampleDrill);
        protected float  SampleTime => HasDrill ? 3f : 7f;
        protected string SampleBtn  => T(HasDrill ? "w.btn.drill" : "w.btn.scoop");
        protected string SampleHint => T(HasDrill ? "w.drill.go" : "w.scoop.go");
        protected int    SamplePts(int pts)
        {
            if (HasDrill) return pts;
            if (!_scooped) { _scooped = true; Result.Lines.Add(T("w.log.nodrill")); }
            return pts / 3;
        }
        bool _scooped;

        // Battery charge per second (0..1 scale) for this world's light and the chosen power source
        protected float ChargeRate(float sunlight = 1f)
        {
            if (Design == null) return 0.002f;
            return Design.Power switch
            {
                PowerSystem.Solar          => 0.004f * Mathf.Min(3f, Design.SolarEfficiency()) * sunlight,
                PowerSystem.RTG            => 0.0035f,
                _                          => 0.006f,
            };
        }

        // Data rate multiplier from the antenna (game scale)
        protected float AntennaRate => Design == null ? 1f : Design.Antenna switch
        {
            AntennaType.LowGain    => 0.35f,
            AntennaType.MediumGain => 1f,
            _                      => 2.2f,
        };

        // Rocket fuel left for the descent, in seconds of full thrust
        protected float DescentFuel(float baseSeconds)
        {
            float left = Flight != null ? Flight.FuelAtArrival : 0.6f;
            float k = Mathf.Clamp(0.55f + left, 0.6f, 1.4f);
            if (Design != null && Design.Engine == EngineType.Ion) k *= 0.85f;
            return baseSeconds * k;
        }

        // Without a radar altimeter the altitude on screen is an estimate (±20 %)
        protected float ShownAltitude(float real) =>
            Has(ScienceInstrument.RadarAltimeter) ? real : real * (1f + 0.2f * Mathf.Sin(Time.time * 0.7f));

        // ── Effects of the big moments ───────────────────────────────────────
        protected void FxTouchdown(Vector3 pos, bool slowMo = true)
        {
            WorldFX.Touchdown(Root, pos, P.GroundB, P.Gravity / 9.81f);
            Cam.Shake(0.45f);
            Cam.Punch(8f);
            H.Flash(Color.white, 0.35f);
            H.SetCinematic(false);
            if (slowMo) WorldFX.SlowMo(D, 0.35f, 0.7f);
            Haptics.Buzz();
        }

        protected void FxSample(Vector3 pos)
        {
            WorldFX.Sample(Root, pos);
            Cam.Punch(5f);
            H.Flash(new Color(1f, 0.85f, 0.35f), 0.4f);
            Haptics.Buzz();
        }

        // Parachute opening, stage separation: a hard jolt
        protected void FxJolt(float amount = 0.7f)
        {
            Cam.Shake(amount);
            Cam.Punch(10f * amount);
            H.Flash(Color.white, 0.3f * amount);
            Haptics.Buzz();
            AudioFX.Play(Sfx.Hit, 0.5f);
        }

        float _sparkT;
        protected void FxDrill(Vector3 pos, float dt)
        {
            _sparkT += dt;
            if (_sparkT < 0.08f) return;
            _sparkT = 0f;
            WorldFX.Sparks(Root, pos + Vector3.up * 0.3f);
        }

        // Dust blown off the ground by a rocket close to it
        ParticleSystem _groundDust;
        protected void FxRocketDust(Vector3 craftPos, float altitude, float throttle)
        {
            if (Ter == null) return;
            if (_groundDust == null)
            {
                var c = P.GroundB; c.a = 0.7f;
                _groundDust = WorldFX.Emitter(Root, Vector3.zero, c, Color.Lerp(P.GroundA, Color.white, 0.2f),
                                              new Vector2(0.8f, 2.4f), 2.2f, 14f, 0.02f, false, 85f);
                if (_groundDust == null) return;
                _groundDust.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);   // the cone opens upward and outward
            }
            var g = craftPos; g.y = Ter.Height(g) + 0.2f;
            _groundDust.transform.position = g;
            float k = throttle * Mathf.Clamp01((35f - altitude) / 35f);
            WorldFX.Rate(_groundDust, P.AirDensity <= 0f && P.Gravity < 1f ? k * 60f : k * 160f);
        }

        protected void FxRocketDustOff() => WorldFX.Rate(_groundDust, 0f);

        // Dust thrown up behind the wheels: in low gravity it flies higher and hangs longer
        protected ParticleSystem WheelDust(Transform vehicle)
        {
            float g = P.Gravity / 9.81f;
            var c = P.GroundB; c.a = 0.6f;
            var ps = WorldFX.Emitter(vehicle, new Vector3(0f, 0.3f, -1.4f), c, Color.Lerp(P.GroundA, Color.white, 0.25f),
                                     new Vector2(0.25f, 0.8f), 1.6f / Mathf.Sqrt(Mathf.Max(0.1f, g)), 2.5f, g * 0.35f, false, 30f);
            if (ps != null) ps.transform.localRotation = Quaternion.Euler(-25f, 180f, 0f);
            return ps;
        }

        protected static void WheelDustRate(ParticleSystem ps, float speed, bool airborne) =>
            WorldFX.Rate(ps, airborne ? 0f : Mathf.Abs(speed) * 14f);

        // ── Result ───────────────────────────────────────────────────────────
        protected void Fail(string key)
        {
            if (Finished) return;
            Result.Success = false;
            Result.FailKey = key;
            Result.Lines.Add(Loc.T(key));
            Finished = true;
            H.SetEntry(false, Vector2.zero, 0f);
            H.SetWindow(false, 0, 0, 0, "");
            AudioFX.Play(Sfx.Explosion);
            Haptics.Buzz();
        }

        protected void Complete(int stars)
        {
            if (Finished) return;
            Result.Success = true;
            if (_scooped) stars = Mathf.Min(stars, 2);
            Result.Stars = Mathf.Clamp(stars, 1, 3);
            Finished = true;
            AudioFX.Play(Sfx.Go);
        }

        // The player leaves early: keep what was achieved
        public void Abort(int stars)
        {
            if (Finished) return;
            Result.Success = Result.Landed;
            Result.Stars = Result.Landed ? Mathf.Clamp(stars, 1, 3) : 0;
            Finished = true;
        }

        public virtual int StarsSoFar => Result.Landed ? 1 : 0;

        protected void Score(int points, string lineKey = null, params object[] args)
        {
            Result.Points += points;
            if (lineKey != null) Result.Lines.Add(Loc.F(lineKey, args));
        }

        protected static string T(string key) => Loc.T(key);
        protected static string F(string key, params object[] a) => Loc.F(key, a);

        // Buttons act only when shown as "on"
        protected bool Pressed(ActionButton b, bool input) => input && WorldHUD.Active(b);
        protected bool APressed => Pressed(H.BtnA, In.APressed);
        protected bool BPressed => Pressed(H.BtnB, In.BPressed);
        protected bool CPressed => Pressed(H.BtnC, In.CPressed);
        protected bool AHeld => In.AHeld && WorldHUD.Active(H.BtnA);
        protected bool BHeld => In.BHeld && WorldHUD.Active(H.BtnB);
        protected bool CHeld => In.CHeld && WorldHUD.Active(H.BtnC);

        // ═════════════════════════════════════════════════════════════════════
        // Shared piece 1 — atmospheric entry: keep the dot inside the circle
        // ═════════════════════════════════════════════════════════════════════
        protected float EntryHeat;
        Vector2 _entryDot;
        float _entrySeed;

        protected void EntryStart()
        {
            EntryHeat = 0f; _entryDot = Vector2.zero; _entrySeed = Random.value * 100f;
            H.SetCinematic(true);   // letterbox for the plunge through the atmosphere
            Cam.Punch(14f);
        }

        // Returns true when the entry is over (duration s); fails the mission if the shield burns
        protected bool EntryTick(float dt, float duration, float turbulence, float heatRate)
        {
            float t = Time.time * 0.6f + _entrySeed;
            var drift = new Vector2(Mathf.PerlinNoise(t, 1.3f) - 0.5f, Mathf.PerlinNoise(2.7f, t) - 0.5f) * 2f * turbulence;
            _entryDot += (drift - In.MoveV * 1.6f) * dt;
            _entryDot = Vector2.ClampMagnitude(_entryDot, 1f);
            float off = Mathf.Max(0f, _entryDot.magnitude - 0.45f);
            EntryHeat = Mathf.Clamp01(EntryHeat + off * heatRate * dt);
            H.SetEntry(true, _entryDot, EntryHeat);
            if (EntryHeat >= 1f) { H.SetEntry(false, Vector2.zero, 0f); Fail("w.fail.burned"); return true; }
            Cam.Shake(0.08f + off * 0.25f);
            if (off > 0.05f) H.Flash(new Color(1f, 0.45f, 0.1f), off * 0.6f);   // the shield glows
            if (PhaseT >= duration) { H.SetEntry(false, Vector2.zero, 0f); H.SetCinematic(false); return true; }
            return false;
        }

        // ═════════════════════════════════════════════════════════════════════
        // Shared piece 2 — a timing window (parachute, release): press in the green
        // ═════════════════════════════════════════════════════════════════════
        // Needle falls from 1 to 0 over `duration`. Returns -1 while waiting, else the needle
        // value when B was pressed (or 0 if time ran out).
        protected float WindowTick(float duration, float zoneMin, float zoneMax, string label, bool useA = false)
        {
            float needle = 1f - Mathf.Clamp01(PhaseT / duration);
            H.SetWindow(true, zoneMin, zoneMax, needle, label);
            bool press = useA ? APressed : BPressed;
            if (press) { H.SetWindow(false, 0, 0, 0, ""); return needle; }
            if (needle <= 0f) { H.SetWindow(false, 0, 0, 0, ""); return 0f; }
            return -1f;
        }

        // ═════════════════════════════════════════════════════════════════════
        // Shared piece 3 — powered descent HUD and touchdown
        // ═════════════════════════════════════════════════════════════════════
        protected void DescentMeters(DescentCraft c, bool showFuel = true)
        {
            float alt = ShownAltitude(c.Altitude);
            H.SetMeter(0, T("w.m.alt"), Has(ScienceInstrument.RadarAltimeter) ? $"{alt:F0} m" : $"≈{alt:F0} m",
                       Mathf.Clamp01(c.Altitude / 800f), UIStyle.Accent);
            float vs = c.VerticalSpeed;
            H.SetMeter(1, T("w.m.vspeed"), $"{vs:F1} m/s", Mathf.Clamp01(-vs / 60f), -vs > 3f ? UIStyle.Warn : UIStyle.Good);
            if (showFuel)
                H.SetMeter(2, T("w.m.fuel"), $"{c.Fuel / Mathf.Max(0.01f, c.FuelMax) * 100f:F0}%", c.Fuel / Mathf.Max(0.01f, c.FuelMax),
                           c.Fuel < c.FuelMax * 0.2f ? UIStyle.Bad : UIStyle.Good);
            else H.HideMeter(2);
            H.SetLanding(c.Altitude < 80f, -vs, c.SideSpeed, c.TiltDeg);
        }

        // Flame under a craft, driven by its throttle
        protected ParticleSystem Flame(Transform parent, Vector3 local)
        {
            if (A.GlowMaterial == null) return null;
            var go = new GameObject("Flame");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);   // emit downward
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.startLifetime = 0.35f;
            main.startSpeed = 14f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.5f, 0.9f), new Color(1f, 0.45f, 0.1f, 0.8f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8f; shape.radius = 0.2f;
            var e = ps.emission; e.rateOverTime = 0f;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = A.GlowMaterial;
            ps.Play();
            return ps;
        }

        protected static void SetFlame(ParticleSystem ps, float throttle)
        {
            if (ps == null) return;
            var e = ps.emission;
            e.rateOverTime = throttle * 140f;
        }

        // Plasma glow around a capsule during entry
        protected ParticleSystem Plasma(Transform parent)
        {
            var ps = Flame(parent, Vector3.zero);
            if (ps == null) return null;
            ps.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);   // trails upward (the capsule falls)
            var main = ps.main;
            main.startSize = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            main.startSpeed = 25f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.6f, 0.2f, 0.8f), new Color(1f, 0.9f, 0.6f, 0.6f));
            var shape = ps.shape; shape.radius = 1.2f; shape.angle = 20f;
            var e = ps.emission; e.rateOverTime = 120f;
            return ps;
        }

        // ── Objectives: beacons with a label, the arrow points to the next one ─
        protected class Site
        {
            public string   Key;          // localization key of the name
            public Vector3  Pos;
            public float    Radius = 4f;
            public bool     Done;
            public GameObject Marker;
            public bool     Special;      // e.g. the rock with organics
        }

        protected readonly List<Site> Sites = new();

        protected Site AddSite(string key, Vector3 pos, float radius = 4f, GameObject prop = null, float propSize = 2f)
        {
            var s = new Site { Key = key, Pos = pos, Radius = radius };
            var marker = new GameObject("Site_" + key);
            marker.transform.SetParent(Root, false);
            marker.transform.position = pos;
            if (prop != null)
            {
                var m = Object.Instantiate(prop, marker.transform);
                ModelFit.FitToSize(m, propSize);
                m.transform.localPosition = Vector3.up * propSize * 0.3f;
            }
            // A glowing column so it can be seen from far (the beacon model if there is one)
            if (A.Beacon != null)
            {
                var b = Object.Instantiate(A.Beacon, marker.transform);
                ModelFit.FitToSize(b, 1.6f);
                b.transform.localPosition = Vector3.up * (propSize + 1.2f);
            }
            var beam = WorldAssets.Primitive(PrimitiveType.Cylinder, marker.transform,
                                             A.NewMaterial(A.PropMaterial, new Color(0.3f, 0.95f, 1f), 0.9f));
            beam.transform.localScale = new Vector3(0.15f, 6f, 0.15f);
            beam.transform.localPosition = Vector3.up * 6f;
            s.Marker = marker;
            Sites.Add(s);
            return s;
        }

        protected Site NextSite()
        {
            Site best = null; float bd = float.MaxValue;
            var from = D.Focus != null ? D.Focus.position : Vector3.zero;
            foreach (var s in Sites)
            {
                if (s.Done) continue;
                float d = (s.Pos - from).sqrMagnitude;
                if (d < bd) { bd = d; best = s; }
            }
            return best;
        }

        protected void PointTo(Site s)
        {
            if (s == null) { H.SetArrow(null, null); return; }
            float d = D.Focus != null ? Vector3.Distance(new Vector3(s.Pos.x, 0f, s.Pos.z), new Vector3(D.Focus.position.x, 0f, D.Focus.position.z)) : 0f;
            H.SetArrow(s.Pos + Vector3.up * 3f, $"{T(s.Key)} · {d:F0} m");
        }

        protected void MarkDone(Site s)
        {
            s.Done = true;
            if (s.Marker != null)
                foreach (var r in s.Marker.GetComponentsInChildren<Renderer>())
                    if (r.gameObject.name.StartsWith("Cylinder")) r.gameObject.SetActive(false);
            AudioFX.Play(Sfx.Pickup);
            Haptics.Buzz();
        }

        protected bool Near(Site s, Vector3 p) => new Vector2(s.Pos.x - p.x, s.Pos.z - p.z).magnitude < s.Radius;

        protected string Checklist(params (string key, bool done)[] items)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var (key, done) in items)
                sb.Append(done ? "<color=#52EB8C>● " : "<color=#8FA3C7>○ ").Append(T(key)).Append("</color>\n");
            return sb.ToString();
        }
    }
}
