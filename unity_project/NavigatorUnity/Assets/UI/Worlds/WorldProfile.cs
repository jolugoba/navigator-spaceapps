// Navigator 2.0 — WorldProfile.cs
// The 7 worlds as data: real gravity, air, weather, colours and terrain shape, plus which
// vehicle and mission each one gets. Design: docs/mundos/ (one file per world).
// Guacalita Inc. — SAL / Claude

using UnityEngine;

namespace Navigator.Worlds
{
    public enum WorldVehicle { ThermalRover, FixedLander, Rover, FallingProbe, Drone, Hopper }
    public enum Weather { None, Dust, AcidHaze, MethaneRain, Frost }

    public class WorldProfile
    {
        public string Planet;          // mission target id ("saturn" plays on Titan)
        public string Body;            // where you actually are ("titan", "triton"…)
        public WorldVehicle Vehicle;
        public bool   HasGround = true;

        // Physics (real values, SI units)
        public float Gravity;          // m/s²
        public float AirDensity;       // kg/m³ at the ground (0 = no air: no parachute)
        public float Wind;             // m/s at the top of the descent

        // Look
        public Color SkyTop, SkyHorizon, Fog;
        public float FogDensity;       // exponential fog
        public Color GroundA, GroundB, Rock, Accent;   // accent: ice, lakes, frost streaks…
        public Color SunColor = Color.white;
        public float SunIntensity = 1f;
        public float SunElevation = 40f;               // degrees above the horizon
        public float SunAzimuth   = 30f;
        public Color Ambient;
        public bool  StarsVisible;     // black sky (no air to scatter light)
        public Weather Weather;

        // Terrain shape (metres)
        public float Relief = 6f;      // rolling hills
        public int   Craters;          // how many craters
        public float Dunes;            // 0..1 ridge amount
        public float Slabs;            // 0..1 flat broken plates (Venus)
        public int   Seed = 1;

        // Planet or moon hanging in the sky: id for ARSpaceManager.GetPlanetPrefab, angular size
        public string SkyBody;
        public float  SkyBodyDeg;
        public float  SkyBodyElevation = 18f;

        public static WorldProfile Get(string planet) => planet switch
        {
            "mercury" => Mercury,
            "venus"   => Venus,
            "mars"    => Mars,
            "jupiter" => Jupiter,
            "saturn"  => Titan,
            "uranus"  => Uranus,
            "neptune" => Triton,
            _         => null,
        };

        static Color H(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        // ── 1. Mercury: north pole, the Sun on the horizon, ice in eternal shadow ─
        public static readonly WorldProfile Mercury = new()
        {
            Planet = "mercury", Body = "mercury", Vehicle = WorldVehicle.ThermalRover,
            Gravity = 3.70f, AirDensity = 0f, Wind = 0f,
            SkyTop = H("#000000"), SkyHorizon = H("#05060A"), Fog = H("#000000"), FogDensity = 0f,
            GroundA = H("#7D7873"), GroundB = H("#4B4845"), Rock = H("#B8B3AC"), Accent = H("#CFE6F5"),
            SunColor = H("#FFF8EE"), SunIntensity = 2.4f, SunElevation = 4f, SunAzimuth = 60f,
            Ambient = H("#101012"), StarsVisible = true, Weather = Weather.None,
            Relief = 5f, Craters = 26, Seed = 11,
        };

        // ── 2. Venus: orange murk, 92 bar, 464 °C ────────────────────────────
        public static readonly WorldProfile Venus = new()
        {
            Planet = "venus", Body = "venus", Vehicle = WorldVehicle.FixedLander,
            Gravity = 8.87f, AirDensity = 65f, Wind = 100f,
            SkyTop = H("#B8792E"), SkyHorizon = H("#D98C3A"), Fog = H("#C9853A"), FogDensity = 0.012f,
            GroundA = H("#4A3B30"), GroundB = H("#6A4A30"), Rock = H("#2E241D"), Accent = H("#E6D38F"),
            SunColor = H("#FFB060"), SunIntensity = 0.55f, SunElevation = 70f, SunAzimuth = 0f,
            Ambient = H("#6A3E1A"), Weather = Weather.AcidHaze,
            Relief = 3f, Craters = 2, Slabs = 1f, Seed = 22,
        };

        // ── 3. Mars: Jezero crater, butterscotch sky ─────────────────────────
        public static readonly WorldProfile Mars = new()
        {
            Planet = "mars", Body = "mars", Vehicle = WorldVehicle.Rover,
            Gravity = 3.71f, AirDensity = 0.020f, Wind = 15f,
            SkyTop = H("#B98A64"), SkyHorizon = H("#D9B48E"), Fog = H("#C9A27E"), FogDensity = 0.0035f,
            GroundA = H("#B5532D"), GroundB = H("#D08A5A"), Rock = H("#5A3A2A"), Accent = H("#E7C9A8"),
            SunColor = H("#FFE2C0"), SunIntensity = 1.05f, SunElevation = 35f, SunAzimuth = 40f,
            Ambient = H("#5A3A2A"), Weather = Weather.Dust,
            Relief = 9f, Craters = 10, Dunes = 0.5f, Seed = 33,
            SkyBody = "phobos", SkyBodyDeg = 3f, SkyBodyElevation = 30f,
        };

        // ── 4. Jupiter: no ground, a column of cloud layers ──────────────────
        public static readonly WorldProfile Jupiter = new()
        {
            Planet = "jupiter", Body = "jupiter", Vehicle = WorldVehicle.FallingProbe, HasGround = false,
            Gravity = 24.79f, AirDensity = 0.16f, Wind = 150f,
            SkyTop = H("#1A1410"), SkyHorizon = H("#E8DCC0"), Fog = H("#E8DCC0"), FogDensity = 0.004f,
            GroundA = H("#E8DCC0"), GroundB = H("#B5764A"), Rock = H("#A0482E"), Accent = H("#5C6E8C"),
            SunColor = H("#FFF4DD"), SunIntensity = 0.9f, SunElevation = 50f,
            Ambient = H("#6B5A48"), Weather = Weather.None,
        };

        // ── 5. Saturn → Titan: orange haze, methane lakes, 0.14 g, 1.5 bar ───
        public static readonly WorldProfile Titan = new()
        {
            Planet = "saturn", Body = "titan", Vehicle = WorldVehicle.Drone,
            Gravity = 1.352f, AirDensity = 5.3f, Wind = 6f,
            SkyTop = H("#A86E28"), SkyHorizon = H("#D8A24A"), Fog = H("#C8923E"), FogDensity = 0.010f,
            GroundA = H("#6E4B2A"), GroundB = H("#4E3620"), Rock = H("#CFC7B8"), Accent = H("#0E0E10"),
            SunColor = H("#FFC070"), SunIntensity = 0.45f, SunElevation = 45f,
            Ambient = H("#5A3A18"), Weather = Weather.MethaneRain,
            Relief = 6f, Craters = 3, Dunes = 1f, Seed = 55,
        };

        // ── 6. Uranus: orbit, then a probe into cyan haze ────────────────────
        public static readonly WorldProfile Uranus = new()
        {
            Planet = "uranus", Body = "uranus", Vehicle = WorldVehicle.FallingProbe, HasGround = false,
            Gravity = 8.69f, AirDensity = 0.42f, Wind = 250f,
            SkyTop = H("#0A1418"), SkyHorizon = H("#7FD1D9"), Fog = H("#9EDDE2"), FogDensity = 0.004f,
            GroundA = H("#BDE8EA"), GroundB = H("#7FD1D9"), Rock = H("#2E6E7A"), Accent = H("#E8E6C0"),
            SunColor = H("#DDF4FF"), SunIntensity = 0.5f, SunElevation = 30f,
            Ambient = H("#2E5A62"), Weather = Weather.None,
        };

        // ── 7. Neptune → Triton: pink frost, geysers, Neptune in the sky ─────
        public static readonly WorldProfile Triton = new()
        {
            Planet = "neptune", Body = "triton", Vehicle = WorldVehicle.Hopper,
            Gravity = 0.779f, AirDensity = 0f, Wind = 0f,
            SkyTop = H("#000000"), SkyHorizon = H("#04060C"), Fog = H("#000000"), FogDensity = 0f,
            GroundA = H("#E8D5CF"), GroundB = H("#9AA3B5"), Rock = H("#C9C2C8"), Accent = H("#3B3530"),
            SunColor = H("#DCE6FF"), SunIntensity = 0.7f, SunElevation = 25f, SunAzimuth = -40f,
            Ambient = H("#26324F"), StarsVisible = true, Weather = Weather.Frost,
            Relief = 4f, Craters = 6, Seed = 77,
            SkyBody = "neptune", SkyBodyDeg = 8f, SkyBodyElevation = 14f,
        };
    }
}
