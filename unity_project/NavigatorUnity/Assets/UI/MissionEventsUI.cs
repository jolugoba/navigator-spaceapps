// Navigator 2.0 — MissionEventsUI.cs
// Mission operations during cruise: real anomalies that force a decision with limited resources.
// Two events per flight (at 30% and 65% of the route), picked from what fits the design.
// Shown as a banner at the top: the flight never pauses, so rivals aren't advantaged.
// The design decides how bad each one is: redundancy, power margin, antenna and instruments pay off here.
// Guacalita Inc. — SAL / Claude

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Navigator.Core;
using Navigator.Game;

namespace Navigator.UI
{
    public class MissionEventsUI : MonoBehaviour
    {
        [Header("UI")]
        public GameObject Panel;
        public TMP_Text   TitleLabel;
        public TMP_Text   BodyLabel;
        public TMP_Text   TimerLabel;
        public Button     OptionA;
        public Button     OptionB;
        public TMP_Text   OptionALabel;
        public TMP_Text   OptionBLabel;
        public Image      TimerBar;       // drains while mission control waits for you

        [Header("Reglas")]
        public float   DecisionSeconds = 12f;
        public const float SafeModeSeconds = 5f;   // engines off while the probe protects itself
        public float[] Triggers = { 0.30f, 0.65f };

        public bool Active => _current != Kind.None;

        enum Kind { None, SolarStorm, Debris, InstrumentGlitch, NavError, CommBlackout, SolarFade }

        private MissionDesign  _d;
        private MissionOutcome _o;
        private ShipController _ship;
        private readonly List<Kind> _plan = new();
        private int   _next;
        private Kind  _current = Kind.None;
        private float _timeLeft;
        private ScienceInstrument _glitched;

        void Awake()
        {
            OptionA?.onClick.AddListener(() => Resolve(true));
            OptionB?.onClick.AddListener(() => Resolve(false));
            if (Panel != null) Panel.SetActive(false);
        }

        public void BeginFlight(MissionDesign design, MissionOutcome outcome, ShipController ship)
        {
            Cancel();
            _d = design; _o = outcome; _ship = ship; _next = 0;

            var pool = new List<Kind> { Kind.SolarStorm, Kind.Debris, Kind.NavError };
            if (design.Instruments.Count > 0) pool.Add(Kind.InstrumentGlitch);
            float au = MissionDesign.PlanetAU.TryGetValue(design.TargetPlanetId, out var a) ? a : 1.5f;
            if (au >= 5f || design.Antenna == AntennaType.LowGain) pool.Add(Kind.CommBlackout);
            if (design.Power == PowerSystem.Solar && au >= 1.5f) pool.Add(Kind.SolarFade);

            // Events tied to the design weigh more: they're where choices matter most
            _plan.Clear();
            var weighted = pool.OrderBy(k => (k == Kind.CommBlackout || k == Kind.SolarFade ? 0f : 0.5f) + Random.value).ToList();
            for (int i = 0; i < Triggers.Length && i < weighted.Count; i++) _plan.Add(weighted[i]);
        }

        public void Cancel()
        {
            _current = Kind.None;
            _plan.Clear();
            if (Panel != null) Panel.SetActive(false);
        }

        void Update()
        {
            if (Active)
            {
                _timeLeft -= Time.deltaTime;
                if (TimerLabel != null) TimerLabel.text = Loc.F("ev.timer", Mathf.CeilToInt(Mathf.Max(0f, _timeLeft)));
                if (TimerBar   != null) TimerBar.fillAmount = Mathf.Clamp01(_timeLeft / DecisionSeconds);
                if (_timeLeft <= 0f) Resolve(true);   // no decision = mission control plays safe
                return;
            }

            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.InFlight || gm.SpaceManager == null) return;
            if (_next >= _plan.Count || _next >= Triggers.Length) return;
            if (gm.SpaceManager.Progress01 >= Triggers[_next]) Trigger(_plan[_next++]);
        }

        void Trigger(Kind kind)
        {
            _current  = kind;
            _timeLeft = DecisionSeconds;
            if (kind == Kind.InstrumentGlitch)
                _glitched = _d.Instruments[Random.Range(0, _d.Instruments.Count)];

            string body = Loc.T($"ev.{kind}.body");
            string a = Loc.T($"ev.{kind}.a"), b = Loc.T($"ev.{kind}.b");

            switch (kind)
            {
                case Kind.SolarStorm:
                    if (_d.Instruments.Contains(ScienceInstrument.ParticleDetector)) body += "\n\n" + Loc.T("ev.SolarStorm.payoff");
                    break;
                case Kind.InstrumentGlitch:
                    body = Loc.F("ev.InstrumentGlitch.body", Loc.Instrument(_glitched), PowerMargin);
                    break;
                case Kind.CommBlackout:
                    if (_d.Antenna == AntennaType.HighGain) { body += "\n\n" + Loc.T("ev.CommBlackout.payoff"); a = b = Loc.T("ev.ok"); }
                    break;
            }

            if (TitleLabel   != null) TitleLabel.text   = Loc.T($"ev.{kind}.title");
            if (BodyLabel    != null) BodyLabel.text    = body;
            if (OptionALabel != null) OptionALabel.text = a;
            if (OptionBLabel != null) OptionBLabel.text = b;
            if (Panel != null) Panel.SetActive(true);
            AudioFX.Play(Sfx.Alert);
        }

        float PowerMargin => _d.EffectivePowerW - _d.PowerUsedW;

        void Resolve(bool optionA)
        {
            if (!Active) return;
            var kind = _current;
            var fuel = _ship != null ? _ship.GetComponent<FuelSystem>()      : null;
            var col  = _ship != null ? _ship.GetComponent<CollisionSystem>() : null;
            string result;

            switch (kind)
            {
                case Kind.SolarStorm:
                {
                    bool detector = _d.Instruments.Contains(ScienceInstrument.ParticleDetector);
                    if (optionA)
                    {
                        GameManager.Instance?.LockThrust(SafeModeSeconds);
                        result = Loc.T("ev.SolarStorm.ra");
                    }
                    else if (detector)
                    {
                        int bonus = _d.InstrumentPoints(ScienceInstrument.ParticleDetector) / 2;
                        _o.EventBonusPoints += bonus;
                        _o.GoodCalls++;
                        result = Loc.F("ev.SolarStorm.rbonus", bonus);
                        if (Random.value < 0.35f) result += " " + LoseRandomInstrument(ScienceInstrument.ParticleDetector);
                    }
                    else
                    {
                        result = Random.value < 0.5f ? LoseRandomInstrument(null) : Loc.T("ev.lucky");
                    }
                    break;
                }
                case Kind.Debris:
                    if (optionA) { fuel?.Leak(0.08f); result = Loc.T("ev.Debris.ra"); }
                    else if (Random.value < 0.6f) { col?.ApplyDamage(CollisionSeverity.Heavy); result = Loc.T("ev.Debris.rhit"); }
                    else result = Loc.T("ev.lucky");
                    break;

                case Kind.InstrumentGlitch:
                    if (optionA && PowerMargin >= 20f) { _o.GoodCalls++; result = Loc.F("ev.InstrumentGlitch.rfixed", Loc.Instrument(_glitched)); }
                    else
                    {
                        Lose(_glitched);
                        result = Loc.F(optionA ? "ev.InstrumentGlitch.rnopower" : "ev.InstrumentGlitch.roff", Loc.Instrument(_glitched));
                    }
                    break;

                case Kind.NavError:
                    // The classic lesson: a small correction early is cheaper than a big one late
                    fuel?.Leak(optionA ? 0.05f : 0.15f);
                    result = Loc.T(optionA ? "ev.NavError.ra" : "ev.NavError.rb");
                    break;

                case Kind.CommBlackout:
                    if (_d.Antenna == AntennaType.HighGain) { _o.GoodCalls++; result = Loc.T("ev.CommBlackout.rhigh"); }
                    else if (optionA) { GameManager.Instance?.LockThrust(SafeModeSeconds); result = Loc.T("ev.CommBlackout.ra"); }
                    else if (Random.value < 0.4f) { col?.ApplyDamage(CollisionSeverity.Medium); result = Loc.T("ev.CommBlackout.rhit"); }
                    else result = Loc.T("ev.lucky");
                    break;

                case Kind.SolarFade:
                    if (optionA)
                    {
                        var hungriest = _d.Instruments.Where(i => !_o.LostInstruments.Contains(i))
                                                      .OrderByDescending(i => MissionDesign.InstrumentPowerW[i]).FirstOrDefault();
                        if (_d.Instruments.Count > 0) Lose(hungriest);
                        result = Loc.F("ev.SolarFade.ra", _d.Instruments.Count > 0 ? Loc.Instrument(hungriest) : "—");
                    }
                    else { _o.PowerLossW += 30f; result = Loc.T("ev.SolarFade.rb"); }
                    break;

                default: result = ""; break;
            }

            _o.EventLog.Add($"{Loc.T($"ev.{kind}.title")}: {result}");
            GameManager.Instance?.HUD?.Flash(null, UIStyle.Text, 0f, result);

            _current = Kind.None;
            if (Panel != null) Panel.SetActive(false);
        }

        string LoseRandomInstrument(ScienceInstrument? except)
        {
            var candidates = _d.Instruments.Where(i => i != except && !_o.LostInstruments.Contains(i)).ToList();
            if (candidates.Count == 0) return Loc.T("ev.lucky");
            var lost = candidates[Random.Range(0, candidates.Count)];
            Lose(lost);
            return Loc.F("ev.lost", Loc.Instrument(lost));
        }

        void Lose(ScienceInstrument inst)
        {
            if (!_o.LostInstruments.Contains(inst)) _o.LostInstruments.Add(inst);
        }
    }
}
