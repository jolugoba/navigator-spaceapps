// Navigator 2.0 — MissionOutcome.cs
// Lo que realmente pasó en la misión: alimenta el informe final
// Guacalita Inc. — SAL / Claude

using System.Collections.Generic;

namespace Navigator.Core
{
    public class MissionOutcome
    {
        // Vuelo
        public bool   Arrived;
        public string FailReason;
        public float  FlightTimeS;
        public float  FuelAtArrival = 1f;
        public int    HullLeft      = 3;
        public int    Takedowns;
        public bool   RanOutOfFuel;

        // Ciencia y comunicaciones
        public int    InstrumentsPlanned;
        public int    InstrumentsScanned;
        public int    InstrumentsReceived;
        public int    PointsReceived;     // ya con el multiplicador del objetivo
        public int    PointsLost;         // datos que no llegaron a la Tierra
        public float  PowerWaitS;         // segundos con instrumentos parados por falta de energía
        public float  DataSentMb;
        public float  DataTotalMb;

        // Operaciones en crucero (eventos y decisiones)
        public readonly List<ScienceInstrument> LostInstruments = new();
        public readonly List<string> EventLog = new();   // already-localized lines for the debrief
        public int    EventBonusPoints;
        public float  PowerLossW;
        public int    GoodCalls;                          // decisions where the design paid off

        public void Clear()
        {
            Arrived = false; FailReason = null; FlightTimeS = 0f; FuelAtArrival = 1f;
            HullLeft = 3; Takedowns = 0; RanOutOfFuel = false;
            InstrumentsPlanned = InstrumentsScanned = InstrumentsReceived = 0;
            PointsReceived = PointsLost = 0; PowerWaitS = 0f; DataSentMb = DataTotalMb = 0f;
            LostInstruments.Clear(); EventLog.Clear(); EventBonusPoints = 0; PowerLossW = 0f; GoodCalls = 0;
        }
    }
}
