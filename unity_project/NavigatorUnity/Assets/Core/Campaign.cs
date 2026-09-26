// Navigator 2.0 — Campaign.cs
// "NASA Program": six missions of rising difficulty, each with a fixed target and objective,
// its own budget and a science goal for 1-3 stars. One star unlocks the next mission.
// Guacalita Inc. — SAL / Claude

using UnityEngine;

namespace Navigator.Core
{
    public class CampaignMission
    {
        public int    Index;
        public string Key;          // Loc: "camp.<Key>.name" / "camp.<Key>.brief"
        public string Planet;
        public string Objective;
        public float  BudgetM;
        public int[]  StarGoals;    // score needed for 1, 2, 3 stars

        // Shared challenges reuse this shape (Index = -1): beat TargetScore spending at most BudgetM
        public string Challenger;
        public int    TargetScore;
        public bool   IsChallenge => Index < 0;
    }

    public static class Campaign
    {
        const string PrefPrefix = "navigator.campaign.stars.";

        public static readonly CampaignMission[] Missions =
        {
            new() { Index = 0, Key = "firstlight", Planet = "mars",    Objective = "flyby",   BudgetM = 600f,  StarGoals = new[] { 2000, 3500, 5000 } },
            new() { Index = 1, Key = "venus",      Planet = "venus",   Objective = "orbit",   BudgetM = 800f,  StarGoals = new[] { 4000, 7000, 10000 } },
            new() { Index = 2, Key = "rover",      Planet = "mars",    Objective = "landing", BudgetM = 1000f, StarGoals = new[] { 5000, 9000, 13000 } },
            new() { Index = 3, Key = "jupiter",    Planet = "jupiter", Objective = "orbit",   BudgetM = 1000f, StarGoals = new[] { 4000, 8000, 12000 } },
            new() { Index = 4, Key = "saturn",     Planet = "saturn",  Objective = "orbit",   BudgetM = 1100f, StarGoals = new[] { 4000, 8000, 12000 } },
            new() { Index = 5, Key = "neptune",    Planet = "neptune", Objective = "flyby",   BudgetM = 1200f, StarGoals = new[] { 3000, 6000, 9000 } },
        };

        public static int Stars(int index) => PlayerPrefs.GetInt(PrefPrefix + index, 0);

        public static bool IsUnlocked(int index) => index == 0 || Stars(index - 1) > 0;

        public static int StarsFor(CampaignMission m, int score, bool arrived)
        {
            if (!arrived) return 0;
            int stars = 0;
            foreach (int goal in m.StarGoals) if (score >= goal) stars++;
            return stars;
        }

        // Keeps the best result; returns true if this run improved it
        public static bool Record(int index, int stars)
        {
            if (stars <= Stars(index)) return false;
            PlayerPrefs.SetInt(PrefPrefix + index, stars);
            PlayerPrefs.Save();
            return true;
        }

        public static int TotalStars()
        {
            int total = 0;
            for (int i = 0; i < Missions.Length; i++) total += Stars(i);
            return total;
        }

        public static string StarString(int stars) =>
            new string('●', Mathf.Clamp(stars, 0, 3)) + new string('○', 3 - Mathf.Clamp(stars, 0, 3));
    }
}
