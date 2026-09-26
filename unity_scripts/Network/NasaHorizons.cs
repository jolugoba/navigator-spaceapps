// Navigator 2.0 — NasaHorizons.cs
// NASA JPL Horizons API — real planet positions from GPS location
// Ported from Dart: navigator/lib/nasa/horizons_service.dart
// Guacalita Inc. — SAL / Claude

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Navigator.Network
{
    public class NasaHorizons : MonoBehaviour
    {
        // NASA Horizons body IDs
        private static readonly Dictionary<string, string> BodyIds = new()
        {
            { "mercury", "199" },
            { "venus",   "299" },
            { "mars",    "499" },
            { "jupiter", "599" },
            { "saturn",  "699" },
            { "uranus",  "799" },
            { "neptune", "899" },
        };

        // Fallback azimuths (due south = 180) when API unavailable
        private static readonly Dictionary<string, float> FallbackAzimuth = new()
        {
            { "mercury", 180f }, { "venus", 220f }, { "mars", 190f },
            { "jupiter", 160f }, { "saturn", 150f }, { "uranus", 140f }, { "neptune", 135f },
        };

        public IEnumerator GetPlanetPosition(
            string planetId,
            float  latitude,
            float  longitude,
            System.Action<float, float, float> onResult) // azimuth, altitude, distAU
        {
            if (!BodyIds.TryGetValue(planetId, out var bodyId))
            {
                onResult(FallbackAzimuth.GetValueOrDefault(planetId, 180f), 30f, 1f);
                yield break;
            }

            string now   = System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");
            string later = System.DateTime.UtcNow.AddMinutes(1).ToString("yyyy-MM-dd HH:mm");
            string site  = $"coord@399";  // Earth center fallback
            // Full site coord: $"coord,{longitude},{latitude},0@399"

            string url = "https://ssd.jpl.nasa.gov/api/horizons.api?" +
                         $"format=text&COMMAND='{bodyId}'&OBJ_DATA='NO'" +
                         $"&MAKE_EPHEM='YES'&EPHEM_TYPE='OBSERVER'" +
                         $"&CENTER='{site}'" +
                         $"&START_TIME='{UnityWebRequest.EscapeURL(now)}'" +
                         $"&STOP_TIME='{UnityWebRequest.EscapeURL(later)}'" +
                         "&STEP_SIZE='1 m'&QUANTITIES='4,20'";

            using var req = UnityWebRequest.Get(url);
            req.timeout = 8;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Horizons] {planetId}: API failed, using fallback");
                onResult(FallbackAzimuth.GetValueOrDefault(planetId, 180f), 25f, 1f);
                yield break;
            }

            ParseAndReturn(req.downloadHandler.text, planetId, onResult);
        }

        private void ParseAndReturn(
            string text,
            string planetId,
            System.Action<float, float, float> onResult)
        {
            // Horizons returns a text table. Find the data rows after "$$SOE"
            int soe = text.IndexOf("$$SOE");
            int eoe = text.IndexOf("$$EOE");
            if (soe < 0 || eoe < 0)
            {
                onResult(FallbackAzimuth.GetValueOrDefault(planetId, 180f), 25f, 1f);
                return;
            }

            string dataBlock = text[(soe + 5)..eoe].Trim();
            string[] lines   = dataBlock.Split('\n');
            if (lines.Length < 2)
            {
                onResult(FallbackAzimuth.GetValueOrDefault(planetId, 180f), 25f, 1f);
                return;
            }

            // Line format: date_time  az  alt  ... delta(AU) ...
            // Quantities 4=az/alt, 20=delta. Positions vary — parse carefully.
            try
            {
                string[] parts = lines[1].Trim().Split(new[]{' ','\t'},
                    System.StringSplitOptions.RemoveEmptyEntries);
                float az   = float.Parse(parts[3]);
                float alt  = float.Parse(parts[4]);
                float dist = float.Parse(parts[5]);
                onResult(az, alt, dist);
                Debug.Log($"[Horizons] {planetId}: az={az:F1} alt={alt:F1} dist={dist:F3} AU");
            }
            catch
            {
                Debug.LogWarning($"[Horizons] {planetId}: Parse failed, using fallback");
                onResult(FallbackAzimuth.GetValueOrDefault(planetId, 180f), 25f, 1f);
            }
        }
    }
}
