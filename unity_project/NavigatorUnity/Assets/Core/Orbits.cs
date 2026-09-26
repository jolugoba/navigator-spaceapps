// Navigator 2.0 — Orbits.cs
// Real orbital mechanics for launch windows (the "orbital constraints" of the challenge):
//   · Planet positions: NASA/JPL approximate Keplerian elements (E. M. Standish, valid 1800–2050),
//     solved with Kepler's equation — real ellipses, projected on the ecliptic (inclinations ignored).
//   · Transfers: Lambert's problem (universal variables, Curtis alg. 5.2), the same method behind
//     NASA "porkchop plots": for each launch date, the cheapest time of flight is searched.
//   · Burns: departure from a 300 km circular Earth orbit, capture into an elliptical orbit
//     (periapsis 1.1 radii, e = 0.8) at the target (patched conics). Validated against real windows: Mars Nov 2026 and Dec 2028,
//     Venus Jul 2026 and Mar 2028, Jupiter yearly, Mercury every ~116 days; Earth–Mars Hohmann
//     flight 259 days.
// Guacalita Inc. — SAL / Claude

using System;
using System.Collections.Generic;

namespace Navigator.Core
{
    public static class Orbits
    {
        public const double AU      = 149597870.7;        // km
        public const double MuSun   = 1.32712440018e11;   // km³/s²
        public const double MuEarth = 398600.4418;
        public const double Day     = 86400.0;
        const double J2000 = 2451545.0;
        const double LeoRadius = 6378.137 + 300.0;

        // a (AU), e, L (deg), ϖ (deg) at J2000 and their rates per Julian century
        static readonly Dictionary<string, double[]> Elements = new()
        {
            { "mercury", new[] { 0.38709927, 0.20563593, 252.25032350,  77.45779628,  0.00000037,  0.00001906, 149472.67411175,  0.16047689 } },
            { "venus",   new[] { 0.72333566, 0.00677672, 181.97909950, 131.60246718,  0.00000390, -0.00004107,  58517.81538729,  0.00268329 } },
            { "earth",   new[] { 1.00000261, 0.01671123, 100.46457166, 102.93768193,  0.00000562, -0.00004392,  35999.37244981,  0.32327364 } },
            { "mars",    new[] { 1.52371034, 0.09339410,  -4.55343205, -23.94362959,  0.00001847,  0.00007882,  19140.30268499,  0.44441088 } },
            { "jupiter", new[] { 5.20288700, 0.04838624,  34.39644051,  14.72847983, -0.00011607, -0.00013253,   3034.74612775,  0.21252668 } },
            { "saturn",  new[] { 9.53667594, 0.05386179,  49.95424423,  92.59887831, -0.00125060, -0.00050991,   1222.49362201, -0.41897216 } },
            { "uranus",  new[] { 19.18916464, 0.04725744, 313.23810451, 170.95427630, -0.00196176, -0.00004397,   428.48202785,  0.40805281 } },
            { "neptune", new[] { 30.06992276, 0.00859048, -55.12002969,  44.96476227,  0.00026291,  0.00005105,    218.45945325, -0.32241464 } },
        };

        // Gravitational parameter (km³/s²) and mean radius (km) for capture burns
        static readonly Dictionary<string, (double mu, double radius)> Bodies = new()
        {
            { "mercury", (22032.0,     2439.7) },
            { "venus",   (324859.0,    6051.8) },
            { "mars",    (42828.37,    3389.5) },
            { "jupiter", (126686534.0, 71492.0) },
            { "saturn",  (37931187.0,  60268.0) },
            { "uranus",  (5793939.0,   25559.0) },
            { "neptune", (6836529.0,   24764.0) },
        };

        public static bool Knows(string planet) => planet != null && Elements.ContainsKey(planet) && planet != "earth";

        public static double JulianDate(DateTime utc) =>
            (utc - new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc)).TotalDays + J2000;

        public static DateTime FromJulian(double jd) =>
            new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc).AddDays(jd - J2000);

        // Heliocentric position (km) and velocity (km/s) on the ecliptic plane
        public static void State(string planet, double jd, out double x, out double y, out double vx, out double vy)
        {
            var el = Elements[planet];
            double T = (jd - J2000) / 36525.0;
            double a = el[0] + el[4] * T, e = el[1] + el[5] * T;
            double L = el[2] + el[6] * T, w = el[3] + el[7] * T;
            double M = Deg((L - w) % 360.0);
            double E = e < 0.8 ? M : Math.PI;
            for (int i = 0; i < 30; i++) E -= (E - e * Math.Sin(E) - M) / (1 - e * Math.Cos(E));

            double ak = a * AU, n = Math.Sqrt(MuSun / (ak * ak * ak));
            double cosE = Math.Cos(E), sinE = Math.Sin(E), den = 1 - e * cosE, q = Math.Sqrt(1 - e * e);
            double px = ak * (cosE - e), py = ak * q * sinE;
            double pvx = -ak * n * sinE / den, pvy = ak * n * q * cosE / den;
            double c = Math.Cos(Deg(w)), s = Math.Sin(Deg(w));
            x = px * c - py * s;  y = px * s + py * c;
            vx = pvx * c - pvy * s; vy = pvx * s + pvy * c;
        }

        static double Deg(double d) => d * Math.PI / 180.0;

        // ── Lambert (prograde, single revolution) ─────────────────────────────
        static double C(double z) =>
            z > 1e-8 ? (1 - Math.Cos(Math.Sqrt(z))) / z : z < -1e-8 ? (Math.Cosh(Math.Sqrt(-z)) - 1) / -z : 0.5;
        static double S(double z)
        {
            if (z > 1e-8)  { double sz = Math.Sqrt(z);  return (sz - Math.Sin(sz)) / (sz * sz * sz); }
            if (z < -1e-8) { double sz = Math.Sqrt(-z); return (Math.Sinh(sz) - sz) / (sz * sz * sz); }
            return 1.0 / 6.0;
        }

        public static bool Lambert(double r1x, double r1y, double r2x, double r2y, double tofSeconds,
                                   out double v1x, out double v1y, out double v2x, out double v2y)
        {
            v1x = v1y = v2x = v2y = 0;
            double R1 = Math.Sqrt(r1x * r1x + r1y * r1y), R2 = Math.Sqrt(r2x * r2x + r2y * r2y);
            double cosd = Math.Max(-1, Math.Min(1, (r1x * r2x + r1y * r2y) / (R1 * R2)));
            double dth = Math.Acos(cosd);
            if (r1x * r2y - r1y * r2x < 0) dth = 2 * Math.PI - dth;
            if (Math.Abs(1 - Math.Cos(dth)) < 1e-9) return false;
            double A = Math.Sin(dth) * Math.Sqrt(R1 * R2 / (1 - Math.Cos(dth)));

            double Y(double z) => R1 + R2 + A * (z * S(z) - 1) / Math.Sqrt(C(z));
            double sqrtMu = Math.Sqrt(MuSun);
            double F(double z) { double y = Y(z); return y < 0 ? double.NaN : Math.Pow(y / C(z), 1.5) * S(z) + A * Math.Sqrt(y) - sqrtMu * tofSeconds; }

            double lo = -4 * Math.PI * Math.PI + 1e-6, hi = 4 * Math.PI * Math.PI - 1e-6;
            while (Y(lo) < 0 && lo < hi) lo += 0.1;
            double fl = F(lo), fh = F(hi);
            if (double.IsNaN(fl) || double.IsNaN(fh) || fl * fh > 0) return false;
            for (int i = 0; i < 100; i++)
            {
                double zm = 0.5 * (lo + hi), fm = F(zm);
                if (double.IsNaN(fm)) { lo = zm; continue; }
                if (fl * fm <= 0) { hi = zm; } else { lo = zm; fl = fm; }
            }
            double zf = 0.5 * (lo + hi), yf = Y(zf);
            double f = 1 - yf / R1, g = A * Math.Sqrt(yf / MuSun), gd = 1 - yf / R2;
            v1x = (r2x - f * r1x) / g;  v1y = (r2y - f * r1y) / g;
            v2x = (gd * r2x - r1x) / g; v2y = (gd * r2y - r1y) / g;
            return true;
        }

        // ── Burns (patched conics) ────────────────────────────────────────────
        // From a 300 km circular Earth orbit to escape with v∞ (km/s → km/s)
        public static double DepartureDv(double vinf) =>
            Math.Sqrt(vinf * vinf + 2 * MuEarth / LeoRadius) - Math.Sqrt(MuEarth / LeoRadius);

        // Capture from v∞ into an elliptical orbit (periapsis 1.1 planet radii, e = 0.8), the way
        // real orbiters arrive (Juno, Cassini, MRO) before trimming or aerobraking
        public const double CaptureEccentricity = 0.8;
        public static double CaptureDv(string planet, double vinf)
        {
            if (!Bodies.TryGetValue(planet, out var b)) return 0;
            double r = b.radius * 1.1;
            return Math.Sqrt(vinf * vinf + 2 * b.mu / r) - Math.Sqrt(b.mu * (1 + CaptureEccentricity) / r);
        }

        public static double HohmannDays(string planet)
        {
            double at = (Elements["earth"][0] + Elements[planet][0]) * 0.5 * AU;
            return Math.PI * Math.Sqrt(at * at * at / MuSun) / Day;
        }

        public static double SynodicDays(string planet)
        {
            double n1 = 360.0 / (365.25 * Math.Pow(Elements["earth"][0], 1.5));
            double n2 = 360.0 / (365.25 * Math.Pow(Elements[planet][0], 1.5));
            return 360.0 / Math.Abs(n1 - n2);
        }

        public struct Transfer
        {
            public bool   Ok;
            public double DepartureDv, CaptureDv, VinfDeparture, VinfArrival, TofDays;   // km/s, days
            public double TotalKms => DepartureDv + CaptureDv;
        }

        // Cheapest transfer leaving Earth on `jd` (searches the time of flight)
        public static Transfer Best(string planet, double jd, bool capture)
        {
            var best = new Transfer();
            double th = HohmannDays(planet);
            State("earth", jd, out var ex, out var ey, out var evx, out var evy);
            for (int k = 0; k < 40; k++)
            {
                double tof = th * (0.5 + k * 0.03);
                State(planet, jd + tof, out var px, out var py, out var pvx, out var pvy);
                if (!Lambert(ex, ey, px, py, tof * Day, out var v1x, out var v1y, out var v2x, out var v2y)) continue;
                double vd = Math.Sqrt((v1x - evx) * (v1x - evx) + (v1y - evy) * (v1y - evy));
                double va = Math.Sqrt((v2x - pvx) * (v2x - pvx) + (v2y - pvy) * (v2y - pvy));
                var t = new Transfer
                {
                    Ok = true, VinfDeparture = vd, VinfArrival = va, TofDays = tof,
                    DepartureDv = DepartureDv(vd), CaptureDv = capture ? CaptureDv(planet, va) : 0,
                };
                if (!best.Ok || t.TotalKms < best.TotalKms) best = t;
            }
            return best;
        }

        // Porkchop strip: the cheapest transfer for every launch date from `start` over the next
        // synodic period (one full cycle of windows)
        public class WindowScan
        {
            public string     Planet;
            public bool       Capture;
            public DateTime   Start;
            public int        StepDays;
            public Transfer[] ByStep;
            public int        BestStep;          // cheapest launch date of the whole cycle
            public int        RecommendedStep;   // first window that is (almost) as good: don't wait years for 1 %
            public Transfer   BestTransfer => ByStep[BestStep];
            public int        BestDelayDays        => BestStep * StepDays;
            public int        RecommendedDelayDays => RecommendedStep * StepDays;
            public int        HorizonDays => (ByStep.Length - 1) * StepDays;

            public Transfer At(int delayDays) => ByStep[Math.Max(0, Math.Min(ByStep.Length - 1, (int)Math.Round(delayDays / (double)StepDays)))];

            public DateTime LaunchDate(int delayDays) => Start.AddDays(delayDays);

            // Extra m/s compared with the best window of the cycle
            public float PenaltyMs(int delayDays)
            {
                var t = At(delayDays);
                return t.Ok && BestTransfer.Ok ? (float)Math.Max(0, (t.TotalKms - BestTransfer.TotalKms) * 1000.0) : 0f;
            }
        }

        public static WindowScan Scan(string planet, bool capture, DateTime startUtc, int stepDays = 4)
        {
            int horizon = (int)Math.Ceiling(Math.Min(SynodicDays(planet) * 1.02, 800));
            int n = horizon / stepDays + 1;
            var scan = new WindowScan { Planet = planet, Capture = capture, Start = startUtc.Date, StepDays = stepDays, ByStep = new Transfer[n] };
            double jd0 = JulianDate(startUtc.Date);
            double bestV = double.MaxValue;
            for (int i = 0; i < n; i++)
            {
                scan.ByStep[i] = Best(planet, jd0 + i * stepDays, capture);
                if (scan.ByStep[i].Ok && scan.ByStep[i].TotalKms < bestV) { bestV = scan.ByStep[i].TotalKms; scan.BestStep = i; }
            }

            // Recommended: the earliest date within 150 m/s (or 3 %) of the best, then down to
            // the bottom of that window
            double tol = Math.Max(0.15, 0.03 * bestV);
            int rec = scan.BestStep;
            for (int i = 0; i < n; i++)
                if (scan.ByStep[i].Ok && scan.ByStep[i].TotalKms <= bestV + tol) { rec = i; break; }
            while (rec + 1 < n && scan.ByStep[rec + 1].Ok && scan.ByStep[rec + 1].TotalKms < scan.ByStep[rec].TotalKms) rec++;
            scan.RecommendedStep = rec;
            return scan;
        }
    }
}
