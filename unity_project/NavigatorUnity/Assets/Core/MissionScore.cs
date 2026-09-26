// Navigator 2.0 — MissionScore.cs
// Score = arrival position bonus + science points + fuel remaining
// Guacalita Inc. — SAL / Claude

using UnityEngine;

namespace Navigator.Core
{
    public class MissionScore : MonoBehaviour
    {
        public int   ArrivalPosition  = 0;    // 1st=3, 2nd=2, 3rd=1
        public int   SciencePoints    = 0;
        public float FuelFraction     = 1f;
        public float FlightTimeS      = 0f;
        public int   TakedownCount    = 0;
        public bool  GravityAssistUsed = false;

        // Arrival position bonus multiplier (matches NAVIGATOR_RACING_MECHANICS.md)
        private static float ArrivalBonus(int pos) => pos switch
        {
            1 => 1.50f,
            2 => 1.25f,
            3 => 1.10f,
            _ => 1.00f,
        };

        public int TotalScore()
        {
            int baseScore     = Mathf.RoundToInt(1000f * ArrivalBonus(ArrivalPosition));
            int scienceBonus  = SciencePoints;
            int fuelBonus     = Mathf.RoundToInt(FuelFraction * 500f);
            int takedownBonus = TakedownCount * 150;
            int gravAssist    = GravityAssistUsed ? 200 : 0;

            return baseScore + scienceBonus + fuelBonus + takedownBonus + gravAssist;
        }

        public string Summary() =>
            $"Llegada #{ArrivalPosition} | Ciencia {SciencePoints}pt | " +
            $"Combustible {FuelFraction:P0} | Takedowns {TakedownCount} | TOTAL {TotalScore()}";
    }
}
