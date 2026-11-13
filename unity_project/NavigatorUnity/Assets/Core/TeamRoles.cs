// Navigator 2.0 — TeamRoles.cs
// Team play (2 to 4 players joined by QR): the four jobs of a real mission team are dealt out
// among whoever is connected. Nobody talks to agree on it: every phone sorts the same list of
// node ids and reads its own place, so all of them reach the same answer.
// With 4 players each one has a job; with 2 or 3, some carry two.
// Guacalita Inc. — SAL / Claude

using System.Collections.Generic;

namespace Navigator.Core
{
    public static class TeamRoles
    {
        // Loc keys: "role.<key>" is the name, "role.<key>.duty" what that person answers for
        public static readonly string[] Keys = { "propulsion", "science", "power", "comms" };

        // The roles of `me` in a team made of `me` and `others` (empty when playing alone)
        public static List<int> For(string me, IEnumerable<string> others)
        {
            var roles = new List<int>();
            if (string.IsNullOrEmpty(me) || others == null) return roles;
            var ids = new List<string>(others);
            if (ids.Count == 0) return roles;
            if (!ids.Contains(me)) ids.Add(me);
            ids.Sort(string.CompareOrdinal);
            int place = ids.IndexOf(me);
            for (int r = 0; r < Keys.Length; r++)
                if (r % ids.Count == place) roles.Add(r);
            return roles;
        }

        // "SCIENCE + POWER"; the short names fit a ranking row
        public static string Names(List<int> roles, bool shortNames = false)
        {
            var parts = new List<string>();
            foreach (int r in roles) parts.Add(Loc.T("role." + Keys[r] + (shortNames ? ".short" : "")));
            return string.Join(" + ", parts);
        }

        // What the first role answers for (one line for the design screen)
        public static string Duty(List<int> roles) => roles.Count > 0 ? Loc.T("role." + Keys[roles[0]] + ".duty") : "";
    }
}
