// Navigator 2.0 — MissionReport.cs
// Final debrief: how each design decision shaped the mission's result.
// Direct answer to "evaluate how each choice shapes the success of the mission".
// Guacalita Inc. — SAL / Claude

using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Navigator.Core;

namespace Navigator.UI
{
    public static class MissionReport
    {
        static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);
        static readonly string Good = Hex(UIStyle.Good), Warn = Hex(UIStyle.Warn), Bad = Hex(UIStyle.Bad);
        static readonly string Muted = Hex(UIStyle.Muted), Accent = Hex(UIStyle.Accent);

        public static string Build(MissionDesign d, MissionOutcome o)
        {
            if (d == null || o == null) return "";
            var sb = new StringBuilder();

            sb.AppendLine($"<color={Accent}><b>{Loc.T("rep.title")}</b></color>");
            sb.AppendLine($"<color={Muted}>{Loc.F("rep.header", Loc.Objective(d.Objective), Loc.Planet(d.TargetPlanetId), MissionDesign.RealMission(d.TargetPlanetId, d.Objective))}</color>");

            // Propulsion
            float margin = d.DeltaVms / d.RequiredDeltaV - 1f;
            if (o.RanOutOfFuel)
                Line(sb, Bad, Loc.F("rep.prop.nofuel", d.DeltaVms, d.RequiredDeltaV));
            else if (margin < 0.1f)
                Line(sb, Warn, Loc.F("rep.prop.tight", margin, o.FuelAtArrival));
            else
                Line(sb, Good, Loc.F("rep.prop.good", d.DeltaVms, margin, o.FuelAtArrival,
                                     Mathf.RoundToInt(o.FuelAtArrival * 500)));

            // Objective
            if (d.IsOrbit || d.IsLanding)
                Line(sb, Good, Loc.F("rep.obj.paid", Loc.Objective(d.Objective).ToLower(),
                                     d.InsertionDeltaV + d.LandingDeltaV,
                                     d.IsLanding ? Loc.F("rep.obj.lander", MissionDesign.LanderMassKg) : "",
                                     d.ScienceMultiplier));
            else
                Line(sb, Muted, Loc.F("rep.obj.flyby",
                                      MissionDesign.OrbitInsertionDV.GetValueOrDefault(d.TargetPlanetId, 1000f)));

            if (!o.Arrived)
            {
                string reason = o.FailReason != null ? Loc.T(o.FailReason) : Loc.T("fail.notarrived");
                Line(sb, Bad, Loc.F("rep.fail", reason, o.FlightTimeS));
                foreach (var e in o.EventLog) Line(sb, Accent, Loc.F("rep.ops", e));
                Budget(sb, d);
                return sb.ToString().TrimEnd();
            }

            // Power
            if (o.PowerWaitS > 1f)
                Line(sb, Warn, Loc.F("rep.power.wait", d.EffectivePowerW, o.PowerWaitS) +
                               (d.Power == PowerSystem.Solar ? Loc.T("rep.power.solar") : ""));
            else
                Line(sb, Good, Loc.F("rep.power.good", d.EffectivePowerW, d.PowerUsedW));

            // Communications
            if (o.InstrumentsPlanned == 0)
                Line(sb, Warn, Loc.T("rep.sci.none"));
            else if (o.PointsLost > 0)
                Line(sb, Bad, Loc.F("rep.comm.lost", o.InstrumentsReceived, o.InstrumentsPlanned, o.PointsLost, d.TotalDataMb));
            else
                Line(sb, Good, Loc.F("rep.comm.good", o.InstrumentsReceived, d.TotalDataMb, o.PointsReceived));

            // Science strategy: the most valuable instrument for this planet that wasn't carried
            ScienceInstrument? best = null;
            float bestMult = 1.4f;
            foreach (ScienceInstrument i in System.Enum.GetValues(typeof(ScienceInstrument)))
            {
                float m = MissionDesign.PlanetScienceMult(d.TargetPlanetId, i);
                if (m > bestMult && !d.Instruments.Contains(i)) { best = i; bestMult = m; }
            }
            if (best.HasValue)
                Line(sb, Warn, Loc.F("rep.sci.missed", Loc.Planet(d.TargetPlanetId), Loc.Instrument(best.Value),
                                     bestMult, d.InstrumentPoints(best.Value)));

            // Operations: every cruise decision and its consequence
            foreach (var e in o.EventLog) Line(sb, Accent, Loc.F("rep.ops", e));
            if (o.EventBonusPoints > 0) Line(sb, Good, Loc.F("rep.ops.bonus", o.EventBonusPoints));

            Budget(sb, d);

            if (o.Takedowns > 0 || o.HullLeft < 3)
                Line(sb, Muted, Loc.F("rep.race", o.Takedowns, o.HullLeft, o.FlightTimeS));

            return sb.ToString().TrimEnd();
        }

        static void Budget(StringBuilder sb, MissionDesign d)
        {
            float cap   = MissionDesign.LaunchCapacityKg[d.LaunchVeh];
            float spare = MissionDesign.BudgetLimitM - d.TotalCostM;
            string msg  = Loc.F("rep.budget", d.TotalCostM, d.WetMassKg, cap);
            if (spare > 150f) msg += Loc.F("rep.budget.spare", spare);
            Line(sb, spare > 150f ? Warn : Good, msg);
        }

        static void Line(StringBuilder sb, string color, string text) =>
            sb.AppendLine($"<color={color}>•</color> {text}");
    }
}
