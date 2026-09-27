// Navigator 2.0 — NasaHorizons.cs
// NASA JPL Horizons API: where each planet is in YOUR sky right now (topocentric),
// how far it is, and whether the Sun is up.
// Guacalita Inc. — SAL / Claude

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;

namespace Navigator.Network
{
    public struct SkyPosition
    {
        public bool  Ok;           // false = no data (offline, no GPS…)
        public float Azimuth;      // degrees clockwise from true north
        public float Altitude;     // degrees above the horizon (negative = below)
        public float DistanceAU;
        public char  Sun;          // '*' day · 'C' civil / 'N' nautical / 'A' astronomical twilight · ' ' night

        public bool IsDaytime   => Sun == '*' || Sun == 'C';
        public bool AboveHorizon => Altitude > 0f;
    }

    public class NasaHorizons : MonoBehaviour
    {
        const string Api = "https://ssd.jpl.nasa.gov/api/horizons.api";

        private static readonly Dictionary<string, string> BodyIds = new()
        {
            { "mercury", "199" }, { "venus", "299" }, { "mars", "499" }, { "jupiter", "599" },
            { "saturn", "699" }, { "uranus", "799" }, { "neptune", "899" },
        };

        // Used only by the flight when Horizons can't be reached
        private static readonly Dictionary<string, float> FallbackAzimuth = new()
        {
            { "mercury", 180f }, { "venus", 220f }, { "mars", 190f },
            { "jupiter", 160f }, { "saturn", 150f }, { "uranus", 140f }, { "neptune", 135f },
        };

        // Real sky position seen from (latitude, longitude) right now
        public IEnumerator GetSky(string planetId, float latitude, float longitude, System.Action<SkyPosition> onResult)
        {
            if (!BodyIds.TryGetValue(planetId, out var bodyId)) { onResult(default); yield break; }

            var ci    = CultureInfo.InvariantCulture;
            string now   = System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm", ci);
            string later = System.DateTime.UtcNow.AddMinutes(1).ToString("yyyy-MM-dd HH:mm", ci);
            // Geodetic site: east longitude, latitude (degrees), altitude (km)
            string site  = $"{longitude.ToString("F4", ci)},{latitude.ToString("F4", ci)},0";

            string query = "?format=text" +
                         Q("COMMAND", bodyId) + Q("OBJ_DATA", "NO") + Q("MAKE_EPHEM", "YES") +
                         Q("EPHEM_TYPE", "OBSERVER") + Q("CENTER", "coord@399") +
                         Q("COORD_TYPE", "GEODETIC") + Q("SITE_COORD", site) +
                         Q("START_TIME", now) + Q("STOP_TIME", later) + Q("STEP_SIZE", "1 m") +
                         Q("QUANTITIES", "4,20") + Q("CSV_FORMAT", "YES");

            string text = null;
            foreach (string api in Endpoints())
            {
                using var req = UnityWebRequest.Get(api + query);
                req.timeout = 10;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success) { text = req.downloadHandler.text; break; }
                Debug.LogWarning($"[Horizons] {planetId} ({api}): {req.error}");
            }
            if (text == null)
            {
                onResult(default);
                yield break;
            }
            var pos = Parse(text);
            if (pos.Ok) Debug.Log($"[Horizons] {planetId}: az={pos.Azimuth:F1} alt={pos.Altitude:F1} dist={pos.DistanceAU:F3} AU sun='{pos.Sun}'");
            else        Debug.LogWarning($"[Horizons] {planetId}: could not parse the ephemeris");
            onResult(pos);
        }

        // Flight helper: always answers, falling back to a plausible direction
        public IEnumerator GetPlanetPosition(string planetId, float latitude, float longitude,
                                             System.Action<float, float, float> onResult) // azimuth, altitude, distAU
        {
            SkyPosition sky = default;
            yield return GetSky(planetId, latitude, longitude, p => sky = p);
            if (sky.Ok) onResult(sky.Azimuth, sky.Altitude, sky.DistanceAU);
            else        onResult(FallbackAzimuth.GetValueOrDefault(planetId, 180f), 25f, 1f);
        }

        // A browser may refuse to call NASA directly (CORS): the web build then asks its own server,
        // which forwards /nasa-horizons/ to ssd.jpl.nasa.gov (see space-nginx.conf)
        static IEnumerable<string> Endpoints()
        {
            yield return Api;
            if (Application.platform == RuntimePlatform.WebGLPlayer && !string.IsNullOrEmpty(Application.absoluteURL) &&
                System.Uri.TryCreate(Application.absoluteURL, System.UriKind.Absolute, out var page) &&
                (page.Scheme == "https" || page.Scheme == "http"))
                yield return new System.Uri(page, "/nasa-horizons/api/horizons.api").ToString();
        }

        static string Q(string key, string value) => $"&{key}={UnityWebRequest.EscapeURL($"'{value}'")}";

        // CSV row: " 2026-Sep-26 05:00, *, m,  123.4567,  45.1234, 1.23456789,  -12.3456,"
        //           date            sun moon  azimuth    elevation delta(AU)   deldot
        static SkyPosition Parse(string text)
        {
            int soe = text.IndexOf("$$SOE"), eoe = text.IndexOf("$$EOE");
            if (soe < 0 || eoe < soe) return default;

            foreach (var raw in text[(soe + 5)..eoe].Split('\n'))
            {
                var f = raw.Split(',');
                if (f.Length < 6) continue;
                var ci = CultureInfo.InvariantCulture;
                if (!float.TryParse(f[3].Trim(), NumberStyles.Float, ci, out var az))    continue;
                if (!float.TryParse(f[4].Trim(), NumberStyles.Float, ci, out var el))    continue;
                if (!float.TryParse(f[5].Trim(), NumberStyles.Float, ci, out var delta)) continue;
                string sun = f[1].Trim();
                return new SkyPosition
                {
                    Ok = true, Azimuth = az, Altitude = el, DistanceAU = delta,
                    Sun = sun.Length > 0 ? sun[0] : ' ',
                };
            }
            return default;
        }
    }
}
