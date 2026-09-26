// Navigator 2.0 — ARSpaceManager.cs
// Coloca el planeta destino en su dirección real en el cielo (NASA Horizons + brújula)
// o en una dirección fija si no hay GPS. El viaje empieza en el origen (0,0,0).
// Guacalita Inc. — SAL / Claude

using UnityEngine;
using Navigator.Core;
using Navigator.Game;

namespace Navigator.AR
{
    public class ARSpaceManager : MonoBehaviour
    {
        [Header("Cámara")]
        public Camera ARCamera;

        [Header("Planet prefabs (Navigator → Assign Models)")]
        public GameObject PlanetMercury;
        public GameObject PlanetVenus;
        public GameObject PlanetMars;
        public GameObject PlanetJupiter;
        public GameObject PlanetSaturn;
        public GameObject PlanetUranus;
        public GameObject PlanetNeptune;
        public GameObject PlanetEarth;     // optional (Meshy PL-08_Earth) — flyby body
        public GameObject PlanetMoon;      // optional (Meshy PL-09_Moon)
        public GameObject PlanetPluto;     // optional (Meshy PL-10_Pluto)
        public GameObject PlanetEuropa;    // optional (Meshy PL-11_Europa)

        [Header("Landmarks to scan (optional Meshy models, NAVIGATOR_MESHY_ASSETS_V3.md)")]
        // Real probes waiting at their planet, moons, and telescopes near the launch point
        public GameObject ProbeMessenger, ProbeMagellan, ProbeMRO, ProbeJuno, ProbeCassini, ProbeVoyager;
        public GameObject Hubble, Jwst, Iss;
        public GameObject Dart, RocketSLS, RoverPerseverance;
        public GameObject MoonPhobos, MoonIo, MoonTitan, MoonEnceladus, MoonMiranda, MoonTriton;

        [Header("Escala del viaje (unidades de juego)")]
        public float BaseJourney      = 1200f;  // distancia mínima de viaje
        public float JourneyPerSqrtAU = 300f;   // más lejos en AU = viaje más largo
        public float PlanetDiameter   = 120f;
        public float SpinDegPerSec    = 4f;

        [HideInInspector] public Transform ShipAnchor;

        // Once the flight starts the route is fixed; late Horizons answers are ignored
        [HideInInspector] public bool Locked;

        public string  TargetPlanetId  { get; private set; }
        public Vector3 LaunchPoint     => Vector3.zero;
        public Vector3 TargetPosition  { get; private set; } = new(0f, 0f, 1400f);
        public float   JourneyDistance { get; private set; } = 1400f;
        public float   PlanetRadius    => PlanetDiameter * 0.5f;
        public bool    HasTarget       => _planet != null;

        private GameObject _planet;
        private GameObject _launchLandmarks;
        private float      _distanceAU = 1f;

        void Update()
        {
            if (_planet != null)
                _planet.transform.Rotate(Vector3.up, SpinDegPerSec * Time.deltaTime, Space.World);
        }

        // Horizons: azimuth clockwise from north (+Z), altitude above the horizon
        public void SetPlanetDirection(string planetId, float azimuthDeg, float altitudeDeg, float distanceAU)
        {
            float az  = azimuthDeg  * Mathf.Deg2Rad;
            float alt = altitudeDeg * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Sin(az) * Mathf.Cos(alt), Mathf.Sin(alt), Mathf.Cos(az) * Mathf.Cos(alt));
            Place(planetId, dir, distanceAU);
        }

        public void SetPlanetDirectionFallback(string planetId, Vector3 worldDir, float distanceAU)
        {
            Place(planetId, worldDir, distanceAU);
        }

        void Place(string planetId, Vector3 direction, float distanceAU)
        {
            if (Locked) return;
            if (direction.sqrMagnitude < 1e-6f) direction = Vector3.forward;

            TargetPlanetId  = planetId;
            _distanceAU     = Mathf.Max(0.1f, distanceAU);
            JourneyDistance = BaseJourney + JourneyPerSqrtAU * Mathf.Sqrt(_distanceAU);
            TargetPosition  = LaunchPoint + direction.normalized * JourneyDistance;

            if (_planet != null) Destroy(_planet);
            _planet = new GameObject($"Planet_{planetId}");
            _planet.transform.position = TargetPosition;
            Scannable.Tag(_planet, "planet", planetId, PlanetRadius);

            GameObject prefab = GetPlanetPrefab(planetId);
            GameObject model  = prefab != null
                ? Instantiate(prefab, _planet.transform)
                : FallbackSphere(planetId, _planet.transform);
            model.transform.localPosition = Vector3.zero;
            ModelFit.FitToSize(model, PlanetDiameter);
            PlaceLandmarks(planetId, direction.normalized);

            Debug.Log($"[ARSpace] {planetId} a {JourneyDistance:F0}u ({_distanceAU:F2} AU) dir {direction}");
        }

        // ── Landmarks: things worth scanning on the way ──────────────────────
        void PlaceLandmarks(string planetId, Vector3 dir)
        {
            Vector3 side = Vector3.Cross(dir, Vector3.up);
            if (side.sqrMagnitude < 0.01f) side = Vector3.right;
            side.Normalize();

            // The real spacecraft that explored this world, parked beside it
            var (probe, probeId) = planetId switch
            {
                "mercury" => (ProbeMessenger, "messenger"),
                "venus"   => (ProbeMagellan,  "magellan"),
                "mars"    => (ProbeMRO,       "mro"),
                "jupiter" => (ProbeJuno,      "juno"),
                "saturn"  => (ProbeCassini,   "cassini"),
                _         => (ProbeVoyager,   "voyager"),
            };
            Landmark(_planet.transform, probe, "probe", probeId, (side * 1.25f + Vector3.up * 0.35f) * PlanetDiameter, 12f, false);
            if (planetId == "mars")   // the rover that's exploring it right now
                Landmark(_planet.transform, RoverPerseverance, "probe", "perseverance", (-side * 0.9f - Vector3.up * 0.2f) * PlanetDiameter, 8f, false);

            // Its best-known moons (grey spheres until the Meshy models exist)
            (GameObject, string)[] moons = planetId switch
            {
                "mars"    => new[] { (MoonPhobos, "phobos") },
                "jupiter" => new[] { (MoonIo, "io"), (PlanetEuropa, "europa") },
                "saturn"  => new[] { (MoonTitan, "titan"), (MoonEnceladus, "enceladus") },
                "uranus"  => new[] { (MoonMiranda, "miranda") },
                "neptune" => new[] { (MoonTriton, "triton") },
                _         => new (GameObject, string)[0],
            };
            for (int i = 0; i < moons.Length; i++)
            {
                float a = (i + 1) * 2.1f;
                Vector3 off = (-side * Mathf.Cos(a) + Vector3.up * 0.25f + dir * Mathf.Sin(a) * 0.4f).normalized * PlanetDiameter * (1.4f + 0.5f * i);
                Landmark(_planet.transform, moons[i].Item1, "moon", moons[i].Item2, off, PlanetDiameter * (i == 0 ? 0.22f : 0.16f), true);
            }

            // Telescopes near the launch point (only with their models: a box isn't a telescope)
            if (_launchLandmarks != null) Destroy(_launchLandmarks);
            _launchLandmarks = new GameObject("LaunchLandmarks");
            _launchLandmarks.transform.position = LaunchPoint;
            Landmark(_launchLandmarks.transform, Hubble, "probe", "hubble", dir * 160f + side * 70f + Vector3.up * 25f, 14f, false);
            Landmark(_launchLandmarks.transform, Jwst,   "probe", "jwst",   dir * 320f - side * 90f + Vector3.up * 40f, 18f, false);
            Landmark(_launchLandmarks.transform, Iss,    "probe", "iss",    dir * 90f - side * 55f - Vector3.up * 15f, 16f, false);
            // The Moon rocket towering by the launch point, and DART out among the asteroids
            Landmark(_launchLandmarks.transform, RocketSLS, "probe", "sls",  -dir * 25f + side * 30f, 30f, false);
            Landmark(_launchLandmarks.transform, Dart,      "probe", "dart", dir * (JourneyDistance * 0.45f) + side * 55f + Vector3.up * 20f, 10f, false);
        }

        void Landmark(Transform parent, GameObject prefab, string kind, string id, Vector3 offset, float size, bool sphereIfMissing)
        {
            if (prefab == null && !sphereIfMissing) return;
            var root = new GameObject($"{kind}_{id}");
            root.transform.SetParent(parent, false);
            root.transform.position = parent.position + offset;
            var model = prefab != null ? Instantiate(prefab, root.transform) : FallbackSphere(id, root.transform);
            model.transform.localPosition = Vector3.zero;
            ModelFit.FitToSize(model, size);
            Scannable.Tag(root, kind, id, size * 0.6f);
        }

        static GameObject FallbackSphere(string planetId, Transform parent)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(s.GetComponent<Collider>());
            s.transform.SetParent(parent, false);
            var r = s.GetComponent<Renderer>();
            if (r != null) r.material.color = planetId switch
            {
                "mercury" => new Color(0.6f, 0.58f, 0.55f),
                "venus"   => new Color(0.9f, 0.75f, 0.45f),
                "mars"    => new Color(0.8f, 0.35f, 0.2f),
                "jupiter" => new Color(0.85f, 0.7f, 0.55f),
                "saturn"  => new Color(0.9f, 0.82f, 0.6f),
                "uranus"  => new Color(0.6f, 0.85f, 0.9f),
                "neptune" => new Color(0.3f, 0.45f, 0.9f),
                "moon"   => new Color(0.75f, 0.75f, 0.72f),
                "pluto"  => new Color(0.75f, 0.65f, 0.55f),
                "europa" => new Color(0.88f, 0.85f, 0.78f),
                "io"     => new Color(0.95f, 0.85f, 0.4f),
                "titan"  => new Color(0.9f, 0.65f, 0.3f),
                "enceladus" => new Color(0.95f, 0.97f, 1f),
                "triton" => new Color(0.85f, 0.75f, 0.75f),
                _         => Color.gray,
            };
            return s;
        }

        // Average Earth–planet distance, used when Horizons is unavailable
        public static float ApproxEarthDistanceAU(string planetId) =>
            MissionDesign.PlanetAU.TryGetValue(planetId, out var au)
                ? Mathf.Max(0.3f, Mathf.Abs(au - 1f))
                : 1f;

        public GameObject GetPlanetPrefab(string id) => id switch
        {
            "mercury" => PlanetMercury,
            "venus"   => PlanetVenus,
            "mars"    => PlanetMars,
            "jupiter" => PlanetJupiter,
            "saturn"  => PlanetSaturn,
            "uranus"  => PlanetUranus,
            "neptune" => PlanetNeptune,
            "earth"   => PlanetEarth,
            "moon"    => PlanetMoon,
            "pluto"   => PlanetPluto,
            "europa"  => PlanetEuropa,
            _         => null,
        };

        Vector3 From => ShipAnchor != null ? ShipAnchor.position : LaunchPoint;

        public Vector3 DirectionToTarget => (TargetPosition - From).normalized;

        // Remaining distance in AU, proportional to how much of the route is left
        public float DistanceToTarget =>
            _distanceAU * Mathf.Clamp01(Vector3.Distance(From, TargetPosition) / Mathf.Max(1f, JourneyDistance));

        public float Progress01 =>
            1f - Mathf.Clamp01(Vector3.Distance(From, TargetPosition) / Mathf.Max(1f, JourneyDistance));
    }
}
