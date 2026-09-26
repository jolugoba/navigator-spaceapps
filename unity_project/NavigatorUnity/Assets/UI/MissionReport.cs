// Navigator 2.0 — MissionReport.cs
// Informe final: cómo cada decisión de diseño influyó en el resultado de la misión.
// Es la respuesta directa a "evaluar cómo cada elección moldea el éxito de la misión".
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
            string planet = MissionDesignUI.PlanetDisplayName(d.TargetPlanetId);

            sb.AppendLine($"<color={Accent}><b>INFORME DE MISIÓN</b></color>");
            sb.AppendLine($"<color={Muted}>{ScienceOpsUI.ObjectiveLabel(d.Objective)} a {planet} · " +
                          $"misión real parecida: {MissionDesign.RealMission(d.TargetPlanetId, d.Objective)}</color>");

            // ΔV / propulsión
            float margin = d.DeltaVms / d.RequiredDeltaV - 1f;
            if (o.RanOutOfFuel)
                Line(sb, Bad, $"Propulsión: te quedaste sin combustible. ΔV {d.DeltaVms:N0} de {d.RequiredDeltaV:N0} m/s " +
                              "requeridos: más combustible o un motor de mayor Isp (iónico) da más empuje.");
            else if (margin < 0.1f)
                Line(sb, Warn, $"Propulsión: ΔV justo ({margin:P0} de margen). Llegaste con {o.FuelAtArrival:P0} de " +
                               "combustible; un margen mayor permite más boost y maniobras.");
            else
                Line(sb, Good, $"Propulsión: ΔV {d.DeltaVms:N0} m/s, {margin:P0} sobre lo requerido. " +
                               $"Llegaste con {o.FuelAtArrival:P0} de combustible (+{Mathf.RoundToInt(o.FuelAtArrival * 500)} pts).");

            // Objetivo
            if (d.IsOrbit || d.IsLanding)
                Line(sb, Good, $"Objetivo: {ScienceOpsUI.ObjectiveLabel(d.Objective).ToLower()} costó " +
                               $"+{d.InsertionDeltaV + d.LandingDeltaV:N0} m/s" +
                               (d.IsLanding ? $" y {MissionDesign.LanderMassKg:F0} kg de módulo" : "") +
                               $", y multiplicó la ciencia ×{d.ScienceMultiplier:0.#}.");
            else
                Line(sb, Muted, "Objetivo: un sobrevuelo es barato, pero una órbita daría ×1.5 de ciencia " +
                                $"(+{MissionDesign.OrbitInsertionDV.GetValueOrDefault(d.TargetPlanetId, 1000f):N0} m/s).");

            if (!o.Arrived)
            {
                Line(sb, Bad, $"Vuelo: {o.FailReason ?? "no llegaste"} a los {o.FlightTimeS:F0} s. " +
                              "Sin llegada no hay ciencia ni bono de combustible.");
                Budget(sb, d);
                return sb.ToString().TrimEnd();
            }

            // Energía
            if (o.PowerWaitS > 1f)
                Line(sb, Warn, $"Energía: {d.EffectivePowerW:F0} W no alcanzaron para operar todo a la vez; " +
                               $"los instrumentos esperaron {o.PowerWaitS:F0} s." +
                               (d.Power == PowerSystem.Solar ? " A esta distancia del Sol un RTG rinde más." : ""));
            else
                Line(sb, Good, $"Energía: {d.EffectivePowerW:F0} W disponibles, {d.PowerUsedW:F0} W de consumo máximo. Sin esperas.");

            // Comunicaciones
            if (o.InstrumentsPlanned == 0)
                Line(sb, Warn, "Ciencia: no llevaste instrumentos. Cada kilo de instrumento son puntos.");
            else if (o.PointsLost > 0)
                Line(sb, Bad, $"Comunicaciones: llegaron {o.InstrumentsReceived} de {o.InstrumentsPlanned} instrumentos; " +
                              $"se perdieron {o.PointsLost:N0} pts porque la antena no envió " +
                              $"{d.TotalDataMb:F0} Mb a tiempo. Una antena de mayor ganancia los habría salvado.");
            else
                Line(sb, Good, $"Comunicaciones: los {o.InstrumentsReceived} instrumentos enviaron {d.TotalDataMb:F0} Mb " +
                               $"a la Tierra ({o.PointsReceived:N0} pts).");

            Budget(sb, d);

            if (o.Takedowns > 0 || o.HullLeft < 3)
                Line(sb, Muted, $"Carrera: {o.Takedowns} takedown(s), casco {o.HullLeft}/3, vuelo de {o.FlightTimeS:F0} s.");

            return sb.ToString().TrimEnd();
        }

        static void Budget(StringBuilder sb, MissionDesign d)
        {
            float cap   = MissionDesign.LaunchCapacityKg[d.LaunchVeh];
            float spare = MissionDesign.BudgetLimitM - d.TotalCostM;
            string msg  = $"Recursos: ${d.TotalCostM:N0}M de $1,000M y {d.WetMassKg:N0} de {cap:N0} kg del lanzador.";
            if (spare > 150f) msg += $" Sobraron ${spare:N0}M: cabían más instrumentos.";
            Line(sb, spare > 150f ? Warn : Good, msg);
        }

        static void Line(StringBuilder sb, string color, string text) =>
            sb.AppendLine($"<color={color}>•</color> {text}");
    }
}
