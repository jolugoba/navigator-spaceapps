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

            // Trajectory: the real launch date and flight time from the Lambert solution
            if (!string.IsNullOrEmpty(d.LaunchDateIso) && d.TransferDays > 0f)
                Line(sb, Muted, Loc.F("rep.traj", d.LaunchDateIso, MissionDesignUI.TripText(d.TransferDays), d.WindowPenaltyDV));

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
                WhatIf(sb, d, o);
                RealComparison(sb, d);
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

            World(sb, d, o);
            Budget(sb, d);

            if (o.Takedowns > 0 || o.HullLeft < 3)
                Line(sb, Muted, Loc.F("rep.race", o.Takedowns, o.HullLeft, o.FlightTimeS));

            WhatIf(sb, d, o);
            RealComparison(sb, d);
            return sb.ToString().TrimEnd();
        }

        const float G0 = 9.80665f;

        // The world at the planet: how the descent and the surface mission went, and which
        // design choices changed it (power source, drill, radar, infrared, antenna)
        static void World(StringBuilder sb, MissionDesign d, MissionOutcome o)
        {
            if (!o.WorldPlayed) return;
            var p = Navigator.Worlds.WorldProfile.Get(d.TargetPlanetId);
            string body = p != null ? Loc.T("w.body." + p.Body) : Loc.Planet(d.TargetPlanetId);
            Line(sb, o.WorldSuccess ? Good : o.WorldLanded ? Warn : Bad,
                 Loc.F("rep.world", body, new string('★', o.WorldStars) + new string('☆', 3 - o.WorldStars), o.WorldPoints));
            int shown = 0;
            foreach (var l in o.WorldLines)
            {
                if (shown++ >= 8) break;
                Line(sb, Muted, "   " + l);
            }
            // Design lessons of the world
            bool ground = p == null || p.HasGround;
            if (p != null && ground && !d.Instruments.Contains(ScienceInstrument.SampleDrill) && p.Vehicle != Navigator.Worlds.WorldVehicle.FallingProbe)
                Line(sb, Warn, Loc.T("rep.world.nodrill"));
            if (!d.Instruments.Contains(ScienceInstrument.RadarAltimeter) && ground)
                Line(sb, Warn, Loc.T("rep.world.noradar"));
            if (d.Power == PowerSystem.Solar && p != null && (p.Body == "mars" || p.Body == "titan" || p.Body == "triton" || p.Body == "venus"))
                Line(sb, Warn, Loc.T("rep.world.solar." + p.Body));
            if (p != null && (p.Body == "mercury" || p.Body == "triton") && !d.Instruments.Contains(ScienceInstrument.InfraredMapper))
                Line(sb, Warn, Loc.T("rep.world.noir." + p.Body));
            if (d.Antenna == AntennaType.LowGain && p != null && (p.Body == "venus" || !p.HasGround))
                Line(sb, Warn, Loc.T("rep.world.antenna"));
        }

        // "What if…?": the same mission with one decision changed, recomputed with the real equations
        // (Tsiolkovsky, inverse-square sunlight, the downlink rates and the porkchop scan)
        static void WhatIf(StringBuilder sb, MissionDesign d, MissionOutcome o)
        {
            var lines = new List<string>();
            float dry = d.DryMassKg, req = d.RequiredDeltaV;

            // Launch date
            float saved = d.WindowPenaltyDV - d.RecommendedPenaltyDV;
            if (saved >= 100f && !string.IsNullOrEmpty(d.RecommendedDateIso))
            {
                float kg = MissionDesign.FuelFor(dry, req, d.Engine) - MissionDesign.FuelFor(dry, req - saved, d.Engine);
                lines.Add(Loc.F("rep.if.window", d.RecommendedDateIso, saved, kg));
            }
            else if (d.LaunchDelayDays > 0 && d.TodayPenaltyDV - d.WindowPenaltyDV >= 100f)
            {
                float extra = d.TodayPenaltyDV - d.WindowPenaltyDV;
                float kg = MissionDesign.FuelFor(dry, req + extra, d.Engine) - MissionDesign.FuelFor(dry, req, d.Engine);
                lines.Add(Loc.F("rep.if.waited", d.LaunchDelayDays, d.WaitCostM, extra, kg));
            }

            // Propellant: too little or too much
            float needed = MissionDesign.FuelFor(dry, req, d.Engine);
            if (o.RanOutOfFuel || d.DeltaVms < req)
            {
                float more = Mathf.Max(0f, needed - d.FuelMassKg);
                lines.Add(Loc.F("rep.if.fuel", more, req, MissionDesign.EngineIspS[d.Engine]));
            }
            else
            {
                float withMargin = MissionDesign.FuelFor(dry, req * 1.1f, d.Engine);
                float excess = d.FuelMassKg - withMargin;
                if (excess > 300f)
                {
                    float lighter = d.WetMassKg - excess;
                    LaunchVehicle? cheaper = null;
                    foreach (LaunchVehicle lv in System.Enum.GetValues(typeof(LaunchVehicle)))
                        if (lv != d.LaunchVeh && lighter <= MissionDesign.LaunchCapacityKg[lv] &&
                            MissionDesign.LaunchCostM[lv] < MissionDesign.LaunchCostM[d.LaunchVeh] &&
                            (cheaper == null || MissionDesign.LaunchCostM[lv] < MissionDesign.LaunchCostM[cheaper.Value]))
                            cheaper = lv;
                    lines.Add(cheaper.HasValue
                        ? Loc.F("rep.if.excess.launcher", excess, lighter, LauncherName(cheaper.Value),
                                MissionDesign.LaunchCostM[d.LaunchVeh] - MissionDesign.LaunchCostM[cheaper.Value])
                        : Loc.F("rep.if.excess", excess, lighter));
                }
            }

            // Engine: the same propellant with the ion engine
            if (d.Engine != EngineType.Ion && d.FuelMassKg > 0f)
            {
                float dryIon = dry - MissionDesign.EngineMassKg[d.Engine] + MissionDesign.EngineMassKg[EngineType.Ion];
                float dvIon  = MissionDesign.EngineIspS[EngineType.Ion] * G0 * Mathf.Log((dryIon + d.FuelMassKg) / dryIon);
                if (dvIon > d.DeltaVms * 1.3f)
                    lines.Add(Loc.F("rep.if.ion", d.FuelMassKg, dvIon, d.DeltaVms,
                                    MissionDesign.EngineCostM[EngineType.Ion] - MissionDesign.EngineCostM[d.Engine]));
            }

            // Power: sunlight falls with 1/r²
            float au = MissionDesign.PlanetAU.TryGetValue(d.TargetPlanetId ?? "", out var a) ? a : 1f;
            if (d.Power == PowerSystem.Solar && d.SolarEfficiency() < 0.5f)
                lines.Add(Loc.F("rep.if.rtg", au, d.SolarEfficiency(), d.EffectivePowerW,
                                MissionDesign.PowerOutputW[PowerSystem.RTG],
                                MissionDesign.PowerCostM[PowerSystem.RTG] - MissionDesign.PowerCostM[PowerSystem.Solar]));
            else if (d.Power != PowerSystem.Solar &&
                     MissionDesign.PowerOutputW[PowerSystem.Solar] / (au * au) >= d.PowerUsedW)
                lines.Add(Loc.F("rep.if.solar", au, MissionDesign.PowerOutputW[PowerSystem.Solar] / (au * au), d.PowerUsedW,
                                MissionDesign.PowerCostM[d.Power] - MissionDesign.PowerCostM[PowerSystem.Solar]));

            // Antenna: time to send everything home
            if (o.PointsLost > 0 && d.Antenna != AntennaType.HighGain && d.TotalDataMb > 0f)
                lines.Add(Loc.F("rep.if.antenna", d.TotalDataMb,
                                d.TotalDataMb / MissionDesign.GameDownlinkMbps[d.Antenna],
                                d.TotalDataMb / MissionDesign.GameDownlinkMbps[AntennaType.HighGain],
                                o.PointsLost,
                                MissionDesign.AntennaCostM[AntennaType.HighGain] - MissionDesign.AntennaCostM[d.Antenna]));

            if (lines.Count == 0) return;
            sb.AppendLine($"\n<color={Accent}><b>{Loc.T("rep.if.title")}</b></color>");
            foreach (var l in lines) Line(sb, Warn, l);
        }

        // Your mission next to the real one that flew the same profile
        static void RealComparison(StringBuilder sb, MissionDesign d)
        {
            if (!MissionDesign.RealMissionFacts.TryGetValue(d.TargetPlanetId + ":" + d.Objective, out var r)) return;
            string cost = r.costM > 0f ? Loc.F("rep.real.cost", r.costM) : Loc.T("rep.real.nocost");
            string route = "";
            if (!string.IsNullOrEmpty(r.route))
            {
                var names = new List<string>();
                foreach (var id in r.route.Split(',')) names.Add(Loc.Planet(id));
                route = Loc.F("rep.real.route", string.Join(", ", names));
            }
            sb.AppendLine($"\n<color={Accent}><b>{Loc.T("rep.real.title")}</b></color>");
            Line(sb, Muted, Loc.F("rep.real.them", r.name, r.massKg, cost, MissionDesignUI.TripText(r.days), route));
            string trip = d.TransferDays > 0f ? MissionDesignUI.TripText(d.TransferDays) : "—";
            Line(sb, Accent, Loc.F("rep.real.you", d.WetMassKg, d.TotalCostM, trip));
            sb.AppendLine($"<size=80%><color={Muted}>{Loc.T("rep.real.note")}</color></size>");
        }

        static string LauncherName(LaunchVehicle lv) => lv switch
        {
            LaunchVehicle.Falcon9     => "Falcon 9",
            LaunchVehicle.FalconHeavy => "Falcon Heavy",
            LaunchVehicle.SLS         => "SLS",
            _                         => "Starship",
        };

        static void Budget(StringBuilder sb, MissionDesign d)
        {
            float cap   = MissionDesign.LaunchCapacityKg[d.LaunchVeh];
            float spare = d.BudgetM - d.TotalCostM;
            string msg  = Loc.F("rep.budget", d.TotalCostM, d.WetMassKg, cap, d.BudgetM);
            if (spare > 150f) msg += Loc.F("rep.budget.spare", spare);
            Line(sb, spare > 150f ? Warn : Good, msg);
        }

        static void Line(StringBuilder sb, string color, string text) =>
            sb.AppendLine($"<color={color}>•</color> {text}");
    }
}
