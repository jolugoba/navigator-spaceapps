// Navigator 2.0 — ProbeMissions.cs
// The giants have no ground: the probe falls through their cloud layers, measuring each one and
// transmitting to the orbiter above before the pressure crushes it. The probe never comes back:
// success is how much science reached home.
//   JupiterMission  (docs/mundos/04_JUPITER.md) Galileo probe: the fastest entry ever, radiation,
//                   choose clouds or a dry "hot spot" (Galileo fell into one), lightning.
//   UranusMission   (docs/mundos/06_URANO.md) Uranus Orbiter and Probe: release it in the right
//                   moment from orbit, transmit only while the orbiter is overhead, keep it warm.
// Guacalita Inc. — SAL / Claude

using System.Collections.Generic;
using UnityEngine;
using Navigator.Core;
using Navigator.Game;
using Navigator.UI;

namespace Navigator.Worlds
{
    public abstract class ProbeMission : WorldMission
    {
        protected class Layer
        {
            public string Key;
            public float Top, Bottom;   // depth in metres (game scale)
            public Color Color;
            public bool Measured;
        }

        protected readonly List<Layer> Layers = new();
        protected float EndDepth = 1800f;
        protected float[] PressureDepth, PressureBar;   // table: depth → bar (log interpolated)
        protected float TempTop, TempBottom;             // °C outside, top and bottom
        protected string TitleKey, EndBarLabel;

        protected GameObject Probe, Chute;
        protected float Depth, FallSpeed = 30f, Buffer, Sent, Battery = 1f, Measure, FallTime;
        protected Vector2 Offset;                        // sideways drift from the start point
        protected bool ChuteOpen;
        protected Vector3 Top;
        ParticleSystem _plasma;

        protected abstract void Layout();
        protected virtual void OnFall(float dt) { }
        protected virtual bool CanTransmit => true;
        protected virtual float MeasureValue(Layer l) => 1f;

        public override void Begin()
        {
            if (Layers.Count == 0) Layout();
            Top = Env.Root.transform.position;
            foreach (var l in Layers) Env.AddCloudLayer(Top.y - l.Top, l.Color, 0.8f);
            Probe = Spawn(A.AtmoProbe, "probe", 2f, Top + Vector3.up * 900f);
            _plasma = Plasma(Probe.transform);
            D.Focus = Probe.transform;
            Cam.Target = Probe.transform; Cam.Distance = 16f; Cam.Height = 5f; Cam.Pitch = 10f; Cam.AutoFollow = false;
            Cam.SnapBehind();
            H.SetPrompt(T("w.entry.hint"));
            EntryStart();
        }

        protected GameObject Spawn(GameObject model, string shape, float size, Vector3 at)
        {
            var go = model != null ? A.Spawn(model, PrimitiveType.Cube, Vector3.one, Color.white, size, Root) : VehicleShapes.Build(A, shape, Root);
            go.transform.position = at;
            return go;
        }

        protected float Pressure(float depth)
        {
            for (int i = 1; i < PressureDepth.Length; i++)
                if (depth <= PressureDepth[i])
                {
                    float t = Mathf.InverseLerp(PressureDepth[i - 1], PressureDepth[i], depth);
                    return Mathf.Exp(Mathf.Lerp(Mathf.Log(PressureBar[i - 1]), Mathf.Log(PressureBar[i]), t));
                }
            return PressureBar[PressureBar.Length - 1];
        }

        protected Layer LayerAt(float depth)
        {
            foreach (var l in Layers) if (depth >= l.Top && depth <= l.Bottom) return l;
            return null;
        }

        // ── Entry (called by the subclass when its own intro is over) ────────
        protected bool TickEntryPhase(float dt, float turbulence, float heatRate)
        {
            Probe.transform.position += new Vector3(6f, -45f, 14f) * dt;
            if (!EntryTick(dt, 9f, turbulence, heatRate) || Finished) return false;
            if (_plasma != null) Object.Destroy(_plasma.gameObject);
            Score(400, "w.log.entry", Mathf.RoundToInt((1f - EntryHeat) * 100f));
            H.SetPrompt(T("w.chute.hint"));
            H.SetButton(H.BtnB, T("w.btn.chute"), BtnState.Pulse);
            return true;
        }

        protected bool TickChutePhase(float dt)
        {
            Probe.transform.position += Vector3.down * 40f * dt;
            float needle = 1f - Mathf.Clamp01(PhaseT / 7f);
            float r = WindowTick(7f, 0.3f, 0.7f, F("w.chute.speed", Mathf.Lerp(60f, 900f, needle)));
            if (r < 0f) return false;
            ChuteOpen = r >= 0.3f && r <= 0.7f;
            if (ChuteOpen) Score(300, "w.log.chute.ok"); else Result.Lines.Add(T("w.probe.log.chutebad"));
            H.ShowBanner(T(ChuteOpen ? "w.chute.ok" : "w.chute.torn"), ChuteOpen ? UIStyle.Good : UIStyle.Bad);
            FallSpeed = ChuteOpen ? 14f : 24f;
            Chute = Spawn(A.Parachute, "parachute", ChuteOpen ? 8f : 4f, Probe.transform.position + Vector3.up * 10f);
            Chute.transform.SetParent(Probe.transform, true);
            Probe.transform.position = new Vector3(Top.x, Top.y, Top.z);
            Result.Landed = true;   // survived entry: the probe is "in"
            Cam.Distance = 14f; Cam.Pitch = 30f; Cam.Height = 7f;
            H.SetButton(H.BtnB, T("w.btn.transmit"), BtnState.Off, 0f, T("w.nodata"));
            return true;
        }

        // ── The fall through the layers ──────────────────────────────────────
        protected void TickFall(float dt)
        {
            FallTime += dt;
            var wish = Quaternion.Euler(0f, Cam.Yaw, 0f) * new Vector3(In.MoveV.x, 0f, In.MoveV.y) * 8f;
            Offset += new Vector2(wish.x, wish.z) * dt;
            Depth += FallSpeed * dt;
            Probe.transform.position = new Vector3(Top.x + Offset.x, Top.y - Depth, Top.z + Offset.y);
            Probe.transform.Rotate(0f, 20f * dt, 0f);
            OnFall(dt);
            if (Finished) return;

            var layer = LayerAt(Depth);
            float p = Pressure(Depth);
            float temp = Mathf.Lerp(TempTop, TempBottom, Depth / EndDepth);

            // Fog: thick inside a layer, thinner between; colour of the layer
            Color c = layer != null ? layer.Color : Color.Lerp(P.Fog, P.Rock, Depth / EndDepth);
            Env.SetFog(c * (1f - 0.5f * Depth / EndDepth), layer != null ? 0.03f : 0.006f);
            Env.SetSky(Color.Lerp(P.SkyTop, c * 0.3f, Depth / EndDepth), c);

            // A: measure the layer you are in (hold)
            bool canMeasure = layer != null && !layer.Measured;
            H.SetButton(H.BtnA, T("w.btn.measure"), canMeasure ? BtnState.Pulse : BtnState.Off, Measure / 2.5f, T("w.probe.measure.where"));
            if (canMeasure && AHeld)
            {
                Measure += dt;
                if (Measure >= 2.5f)
                {
                    Measure = 0f;
                    layer.Measured = true;
                    float v = MeasureValue(layer);
                    Buffer += v;
                    Score(Mathf.RoundToInt(500 * v), "w.probe.log.measured", T(layer.Key), p);
                    H.ShowBanner(F("w.probe.measured", T(layer.Key)), UIStyle.Good);
                    AudioFX.Play(Sfx.Pickup);
                }
            }
            else if (!AHeld) Measure = 0f;

            // B: transmit to the orbiter (hold)
            bool can = CanTransmit && Buffer > 0.001f;
            H.SetButton(H.BtnB, T("w.btn.transmit"), can ? BtnState.On : BtnState.Off, 0f, CanTransmit ? T("w.nodata") : T("w.uranus.noorbiter"));
            if (can && BHeld)
            {
                float s = Mathf.Min(Buffer, 0.1f * AntennaRate * dt);
                Buffer -= s; Sent += s;
                Battery -= 0.003f * dt;
            }
            Battery -= 0.004f * dt;

            H.SetMeter(0, T("w.m.pressure"), $"{p:F1} bar", Mathf.Clamp01(Depth / EndDepth), p > PressureBar[PressureBar.Length - 1] * 0.7f ? UIStyle.Bad : UIStyle.Warn);
            H.SetMeter(1, T("w.m.temp"), $"{temp:F0} °C", Mathf.InverseLerp(TempTop, TempBottom, temp), UIStyle.Accent);
            H.SetMeter(2, T("w.m.battery"), $"{Battery * 100f:F0}%", Battery, Battery < 0.2f ? UIStyle.Bad : UIStyle.Good);
            H.SetData(true, Sent, Sent + Buffer);
            var items = new List<(string, bool)>();
            foreach (var l in Layers) items.Add((l.Key, l.Measured));
            H.SetChecklist(Checklist(items.ToArray()));
            H.SetPrompt(layer != null ? F("w.probe.inlayer", T(layer.Key)) : T("w.probe.between"));

            if (Battery <= 0f) { Result.Lines.Add(T("w.probe.log.battery")); EndFall(); return; }
            if (Depth >= EndDepth) EndFall();
        }

        protected virtual void EndFall()
        {
            H.Flash(Color.black, 1f);
            H.ShowBanner(F("w.probe.crushed", EndBarLabel), UIStyle.Bad, 4f);
            Score(Mathf.RoundToInt(Sent * 700f), "w.probe.log.sent", Sent, Sent + Buffer);
            if (Buffer > 0.05f) Result.Lines.Add(F("w.probe.log.lost", Buffer));
            bool allLayers = true;
            foreach (var l in Layers) allLayers &= l.Measured;
            bool allSent = Buffer < 0.05f;
            Complete(!allLayers || !allSent ? 1 : ThirdStar() ? 3 : 2);
        }

        protected abstract bool ThirdStar();
        public override int StarsSoFar => Result.Landed ? 1 : 0;
    }

    // ═════════════════════════════════════════════════════════════════════════
    public class JupiterMission : ProbeMission
    {
        bool _hotSpot, _zoneChosen, _radiation, _winds, _magnet;
        float _lightning = -1f, _nextBolt;
        GameObject _spotMark;

        protected override void Layout()
        {
            TitleKey = "w.jup.title";
            EndDepth = 1800f;
            PressureDepth = new[] { 0f, 400f, 900f, 1400f, 1800f };
            PressureBar   = new[] { 0.5f, 0.7f, 2f, 5f, 22f };
            TempTop = -110f; TempBottom = 150f;
            EndBarLabel = "22 bar";
            Layers.Add(new Layer { Key = "w.jup.layer.ammonia", Top = 250f,  Bottom = 550f,  Color = new Color(0.93f, 0.9f, 0.82f) });
            Layers.Add(new Layer { Key = "w.jup.layer.nh4sh",   Top = 750f,  Bottom = 1050f, Color = new Color(0.72f, 0.46f, 0.3f) });
            Layers.Add(new Layer { Key = "w.jup.layer.water",   Top = 1250f, Bottom = 1550f, Color = new Color(0.45f, 0.53f, 0.66f) });
        }

        public override void Begin()
        {
            H.SetTitle(T("w.jup.title"), T("w.jup.goal.entry"));
            H.ShowBanner(T("w.jup.banner"), UIStyle.Warn, 4f);
            base.Begin();
            Go("entry");
            Result.Lines.Add(T("w.jup.log.start"));
        }

        public override void Tick(float dt)
        {
            switch (Phase)
            {
                case "entry":
                    if (TickEntryPhase(dt, 1f, 0.8f)) { H.SetTitle(T("w.jup.title"), T("w.jup.goal.chute")); Go("chute"); }
                    break;
                case "chute":
                    if (TickChutePhase(dt)) StartFall();
                    break;
                case "fall":
                    TickFall(dt);
                    break;
            }
        }

        void StartFall()
        {
            // The dark "hot spot" (dry, clear) to one side, the cloud band to the other
            _spotMark = WorldAssets.Primitive(PrimitiveType.Cylinder, Root, A.NewMaterial(A.PropMaterial, new Color(0.15f, 0.1f, 0.08f), 0.1f));
            _spotMark.transform.position = Top + new Vector3(120f, -240f, 0f);
            _spotMark.transform.localScale = new Vector3(140f, 0.2f, 140f);
            H.SetTitle(T("w.jup.title"), T("w.jup.goal.choose"));
            H.SetPrompt(T("w.jup.choose.hint"));
            // Radiation belts: the particle detector warns and the probe is shielded in time
            if (Has(ScienceInstrument.ParticleDetector)) Score(250, "w.jup.log.detector");
            else { Battery -= 0.15f; _radiation = true; Result.Lines.Add(T("w.jup.log.rad")); H.ShowBanner(T("w.jup.rad"), UIStyle.Bad, 3f); }
            Go("fall");
        }

        protected override void OnFall(float dt)
        {
            if (!_zoneChosen)
            {
                H.SetArrow(Top + new Vector3(-120f, -240f, 0f), T("w.jup.band"));
                if (Depth >= Layers[0].Top)
                {
                    _zoneChosen = true;
                    _hotSpot = Offset.x > 40f;
                    Result.Lines.Add(T(_hotSpot ? "w.jup.log.hotspot" : "w.jup.log.band"));
                    H.ShowBanner(T(_hotSpot ? "w.jup.hotspot" : "w.jup.inband"), _hotSpot ? UIStyle.Warn : UIStyle.Good, 3f);
                    H.SetArrow(null, null);
                    H.SetTitle(T("w.jup.title"), T("w.jup.goal.layers"));
                }
            }
            // Lightning in the water clouds cuts the radio for a moment
            var l = LayerAt(Depth);
            if (l != null && l == Layers[2] && Time.time > _nextBolt)
            {
                _nextBolt = Time.time + Random.Range(3f, 6f);
                _lightning = Time.time + 1.5f;
                H.Flash(Color.white, 0.9f);
                AudioFX.Play(Sfx.Hit, 0.4f);
            }
            if (!_winds && Depth > 600f && Has(ScienceInstrument.RadioScience)) { _winds = true; Score(300, "w.jup.log.winds"); }
            if (!_magnet && Depth > 200f && Has(ScienceInstrument.Magnetometer)) { _magnet = true; Score(200, "w.jup.log.magnet"); }
        }

        protected override bool CanTransmit => Time.time > _lightning;
        protected override float MeasureValue(Layer l)
        {
            float v = Has(ScienceInstrument.UVSpectrometer) ? 1f : 0.5f;
            return _hotSpot ? v * 0.5f : v;   // the hot spot is dry and clear: little to measure
        }

        // Galileo transmitted 58 minutes; 1 s of fall ≈ 0.5 min here (the parachute buys time)
        protected override bool ThirdStar() => !_hotSpot && FallTime * 0.5f >= 58f && !_radiation;
    }

    // ═════════════════════════════════════════════════════════════════════════
    public class UranusMission : ProbeMission
    {
        GameObject _orbiter, _planet;
        float _heaterTemp = 10f, _visT;
        bool _heater = true, _magnet;
        float _entryHeatRate = 0.6f;

        protected override void Layout()
        {
            TitleKey = "w.uranus.title";
            EndDepth = 1800f;
            PressureDepth = new[] { 0f, 450f, 1050f, 1500f, 1800f };
            PressureBar   = new[] { 0.1f, 1.2f, 4f, 8f, 10f };
            TempTop = -224f; TempBottom = -60f;
            EndBarLabel = "10 bar";
            Layers.Add(new Layer { Key = "w.uranus.layer.methane", Top = 300f,  Bottom = 600f,  Color = new Color(0.8f, 0.95f, 0.97f) });
            Layers.Add(new Layer { Key = "w.uranus.layer.h2s",     Top = 900f,  Bottom = 1200f, Color = new Color(0.85f, 0.85f, 0.6f) });
            Layers.Add(new Layer { Key = "w.uranus.layer.helium",  Top = 1350f, Bottom = 1650f, Color = new Color(0.35f, 0.62f, 0.7f) });
        }

        public override void Begin()
        {
            Layout();
            H.SetTitle(T("w.uranus.title"), T("w.uranus.goal.release"));
            H.ShowBanner(T("w.uranus.banner"), UIStyle.Accent, 4f);
            // In orbit: Uranus below (on its side), the orbiter with the probe
            var at = Env.Root.transform.position + Vector3.up * 2600f;
            var planet = A.SkyBodyPrefab?.Invoke("uranus");
            if (planet != null)
            {
                _planet = Object.Instantiate(planet, Root);
                ModelFit.FitToSize(_planet, 1600f);
                _planet.transform.position = at + new Vector3(0f, -1150f, 400f);
                _planet.transform.rotation = Quaternion.Euler(0f, 0f, 98f);   // rolls on its side
            }
            _orbiter = Spawn(A.UranusOrbiter, "orbiter", 5f, at);
            D.Focus = _orbiter.transform;
            Cam.Target = _orbiter.transform; Cam.Distance = 18f; Cam.Height = 6f; Cam.Pitch = 18f; Cam.AutoFollow = false;
            Cam.SnapBehind();
            H.SetPrompt(T("w.uranus.release.hint"));
            H.SetButton(H.BtnA, T("w.btn.release"), BtnState.Pulse);
            Go("release");
            Result.Lines.Add(T("w.uranus.log.start"));
        }

        public override void Tick(float dt)
        {
            switch (Phase)
            {
                case "release":
                {
                    float r = WindowTick(8f, 0.42f, 0.6f, T("w.uranus.window"), useA: true);
                    if (r < 0f) return;
                    H.SetButton(H.BtnA, null, BtnState.Hidden);
                    if (r > 0.6f) { _entryHeatRate = 0.4f; Result.Lines.Add(T("w.uranus.log.early")); H.ShowBanner(T("w.uranus.early"), UIStyle.Warn); }
                    else if (r < 0.42f) { _entryHeatRate = 1f; Result.Lines.Add(T("w.uranus.log.late")); H.ShowBanner(T("w.uranus.late"), UIStyle.Warn); }
                    else { Score(400, "w.uranus.log.release"); H.ShowBanner(T("w.uranus.released"), UIStyle.Good); }
                    if (_planet != null) Object.Destroy(_planet, 1f);
                    base.Begin();   // the probe starts its own entry
                    H.SetTitle(T("w.uranus.title"), T("w.uranus.goal.entry"));
                    Go("entry");
                    break;
                }
                case "entry":
                    if (TickEntryPhase(dt, 0.8f, _entryHeatRate)) { H.SetTitle(T("w.uranus.title"), T("w.uranus.goal.chute")); Go("chute"); }
                    break;
                case "chute":
                    if (TickChutePhase(dt)) { H.SetTitle(T("w.uranus.title"), T("w.uranus.goal.layers")); Go("fall"); }
                    break;
                case "fall":
                    TickFall(dt);
                    break;
            }
        }

        protected override void OnFall(float dt)
        {
            _visT += dt;
            // C: heater. On: keeps the electronics warm, costs battery. Off: saves it, but freezes
            H.SetButton(H.BtnC, _heater ? T("w.btn.heateroff") : T("w.btn.heater"), BtnState.On);
            if (CPressed) { _heater = !_heater; AudioFX.Play(Sfx.Beep, 0.5f); }
            float outside = Mathf.Lerp(TempTop, TempBottom, Depth / EndDepth);
            float target = _heater ? 15f : outside * 0.35f;
            _heaterTemp = Mathf.MoveTowards(_heaterTemp, target, (_heater ? 3f : 2.2f) * dt);
            if (_heater) Battery -= 0.006f * dt;
            if (_heaterTemp < -40f) { Fail("w.fail.frozen"); return; }
            if (_heaterTemp < -20f) H.SetPrompt(T("w.uranus.cold"), 0.3f);

            // The orbiter crosses the sky: an arrow shows it, transmission only while visible
            bool vis = CanTransmit;
            var orbiterPos = Probe.transform.position + Quaternion.Euler(0f, _visT * 12f, 0f) * new Vector3(0f, 220f, vis ? 160f : -160f);
            H.SetArrow(orbiterPos, T(vis ? "w.uranus.orbiter.up" : "w.uranus.orbiter.down"));
            if (!_magnet && Depth > 200f && Has(ScienceInstrument.Magnetometer)) { _magnet = true; Score(250, "w.uranus.log.magnet"); }
        }

        protected override bool CanTransmit => Mathf.Sin(_visT * 0.35f) > -0.25f;
        protected override float MeasureValue(Layer l) => Has(ScienceInstrument.UVSpectrometer) ? 1f : 0.5f;
        protected override bool ThirdStar() => Battery > 0f && Depth >= EndDepth;
    }
}
