// Navigator 2.0 — ARSpaceManager.cs
// Places planets in real-world AR direction based on GPS + NASA Horizons data
// Works in ANY location — indoors, outdoors, classroom, event
// Guacalita Inc. — SAL / Claude

using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Navigator.AR
{
    public enum ARMode { Immersive, Portal, Table }

    public class ARSpaceManager : MonoBehaviour
    {
        [Header("AR Mode")]
        public ARMode Mode = ARMode.Immersive;

        [Header("References")]
        public ARSession       Session;
        public ARCameraManager CameraManager;
        public Camera          ARCamera;

        [Header("Planet prefabs (assign in Inspector)")]
        public GameObject PlanetMercury;
        public GameObject PlanetVenus;
        public GameObject PlanetMars;
        public GameObject PlanetJupiter;
        public GameObject PlanetSaturn;
        public GameObject PlanetUranus;
        public GameObject PlanetNeptune;

        [Header("Scale in AR (meters per AU)")]
        public float MetersPerAU = 0.5f;   // 0.5m = 1AU in table mode

        // Planet positions relative to camera (set by NasaHorizons)
        private Vector3 _targetPlanetDir;   // normalized direction to target planet
        private string  _targetPlanetId;
        private float   _targetPlanetAU;

        // Ship AR anchor
        [HideInInspector] public Transform ShipAnchor;

        void Start()
        {
            StartCoroutine(InitAR());
        }

        IEnumerator InitAR()
        {
            // Wait for GPS permission
            yield return new WaitUntil(() => Input.location.isEnabledByUser);
            Input.location.Start();
            yield return new WaitForSeconds(1f);

            float lat = Input.location.lastData.latitude;
            float lon = Input.location.lastData.longitude;

            Debug.Log($"[ARSpace] GPS: {lat:F4}, {lon:F4}");
            // NasaHorizons.cs will call SetPlanetDirection() once data arrives
        }

        // Called by NasaHorizons when planet azimuth/altitude is known
        public void SetPlanetDirection(string planetId, float azimuthDeg, float altitudeDeg, float distanceAU)
        {
            _targetPlanetId = planetId;
            _targetPlanetAU = distanceAU;

            // Convert azimuth (N=0, E=90) + altitude to Unity world direction
            float az  = azimuthDeg * Mathf.Deg2Rad;
            float alt = altitudeDeg * Mathf.Deg2Rad;
            _targetPlanetDir = new Vector3(
                Mathf.Sin(az) * Mathf.Cos(alt),
                Mathf.Sin(alt),
                Mathf.Cos(az) * Mathf.Cos(alt)
            ).normalized;

            PlacePlanetInAR(planetId, _targetPlanetDir, distanceAU);
        }

        // Called by fallback when GPS/Horizons not available
        public void SetPlanetDirectionFallback(string planetId, Vector3 worldDir, float distanceAU)
        {
            _targetPlanetId = planetId;
            _targetPlanetAU = distanceAU;
            _targetPlanetDir = worldDir.normalized;
            PlacePlanetInAR(planetId, worldDir.normalized, distanceAU);
        }

        private void PlacePlanetInAR(string planetId, Vector3 direction, float distanceAU)
        {
            GameObject prefab = GetPlanetPrefab(planetId);
            if (prefab == null) return;

            float dist3D = Mode == ARMode.Table
                ? distanceAU * MetersPerAU
                : 3f;  // Fixed 3m ahead in immersive/portal mode

            Vector3 worldPos = ARCamera.transform.position + direction * dist3D;

            if (ShipAnchor == null)
            {
                var instance = Instantiate(prefab, worldPos, Quaternion.identity);
                instance.name = $"Planet_{planetId}";
            }

            Debug.Log($"[ARSpace] {planetId} placed at {worldPos} ({distanceAU:F2} AU away)");
        }

        private GameObject GetPlanetPrefab(string id) => id switch
        {
            "mercury" => PlanetMercury,
            "venus"   => PlanetVenus,
            "mars"    => PlanetMars,
            "jupiter" => PlanetJupiter,
            "saturn"  => PlanetSaturn,
            "uranus"  => PlanetUranus,
            "neptune" => PlanetNeptune,
            _         => null,
        };

        // Direction from ship to target planet (used by HUD arrow)
        public Vector3 DirectionToTarget => _targetPlanetDir;
        public float   DistanceToTarget  => _targetPlanetAU;
    }
}
