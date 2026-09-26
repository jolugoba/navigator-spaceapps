// Navigator 2.0 — Challenge.cs
// Shareable challenge: "I reached Venus orbit with 7,420 pts spending $610M — beat me."
// Encoded as a short text code (QR, chat message, clipboard). The receiver flies the same
// target and objective with the challenger's spending as the budget cap.
// Guacalita Inc. — SAL / Claude

using System;
using System.Text;
using UnityEngine;

namespace Navigator.Core
{
    public static class Challenge
    {
        public const string Prefix = "NAVC1:";

        public static string Encode(MissionDesign d, int score, string name)
        {
            // Budget cap = what the challenger actually spent, rounded up to $10M
            int budget = Mathf.CeilToInt(d.TotalCostM / 10f) * 10;
            int mask = 0;
            foreach (var i in d.Instruments) mask |= 1 << (int)i;
            string safeName = (name ?? "").Replace("|", "").Replace(":", "");
            if (safeName.Length > 16) safeName = safeName[..16];

            string payload = string.Join("|",
                d.TargetPlanetId, d.Objective, budget, score, safeName,
                (int)d.Engine, (int)d.Power, (int)d.Antenna, (int)d.LaunchVeh,
                Mathf.RoundToInt(d.FuelMassKg), mask);
            return Prefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))
                                   .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        public static bool TryDecode(string text, out CampaignMission mission)
        {
            mission = null;
            if (string.IsNullOrEmpty(text)) return false;
            int at = text.IndexOf(Prefix, StringComparison.Ordinal);
            if (at < 0) return false;

            try
            {
                string b64 = text[(at + Prefix.Length)..].Trim().Split(' ', '\n', '\r')[0]
                                 .Replace('-', '+').Replace('_', '/');
                b64 = b64.PadRight(b64.Length + (4 - b64.Length % 4) % 4, '=');
                var f = Encoding.UTF8.GetString(Convert.FromBase64String(b64)).Split('|');
                if (f.Length < 5 || !MissionDesign.PlanetAU.ContainsKey(f[0])) return false;
                if (f[1] != "flyby" && f[1] != "orbit" && f[1] != "landing") return false;

                int score = int.Parse(f[3]);
                mission = new CampaignMission
                {
                    Index       = -1,
                    Key         = "challenge",
                    Planet      = f[0],
                    Objective   = f[1],
                    BudgetM     = Mathf.Clamp(int.Parse(f[2]), 100, 5000),
                    TargetScore = score,
                    Challenger  = string.IsNullOrEmpty(f[4]) ? "?" : f[4],
                    StarGoals   = new[] { score + 1, score + 1, score + 1 },
                };
                return true;
            }
            catch (Exception) { return false; }
        }
    }
}
