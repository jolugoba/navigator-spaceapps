// Navigator 2.0 — ARSpaceManager.cs
// Coloca el planeta destino en su dirección real en el cielo (NASA Horizons + brújula)
// o en una dirección fija si no hay GPS. El viaje empieza en el origen (0,0,0).
// Guacalita Inc. — SAL / Claude

using UnityEngine;
using Navigator.Core;

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

            GameObject prefab = GetPlanetPrefab(planetId);
            GameObject model  = prefab != null
                ? Instantiate(prefab, _planet.transform)
                : FallbackSphere(planetId, _planet.transform);
            model.transform.localPosition = Vector3.zero;
            ModelFit.FitToSize(model, PlanetDiameter);

            Debug.Log($"[ARSpace] {planetId} a {JourneyDistance:F0}u ({_distanceAU:F2} AU) dir {direction}");
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
