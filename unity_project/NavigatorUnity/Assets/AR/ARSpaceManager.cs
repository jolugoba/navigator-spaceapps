// Navigator 2.0 — ARSpaceManager.cs
// Places planets in real-world direction based on GPS + NASA Horizons data
// Works in ANY location — indoors, outdoors, classroom, event
// No AR Foundation required — uses gyroscope + Unity Camera directly
// Guacalita Inc. — SAL / Claude

using System.Collections;
using UnityEngine;

namespace Navigator.AR
{
    public enum ARMode { Immersive, Portal, Table }

    public class ARSpaceManager : MonoBehaviour
    {
        [Header("AR Mode")]
        public ARMode Mode = ARMode.Immersive;

        [Header("References")]
        public GameObject Session;       // placeholder (no AR Foundation)
        public Camera     CameraManager; // main camera
        public Camera     ARCamera;

        [Header("Planet prefabs (assign in Inspector)")]
        public GameObject PlanetMercury;
        public GameObject PlanetVenus;
        public GameObject PlanetMars;
        public GameObject PlanetJupiter;
        public GameObject PlanetSaturn;
        public GameObject PlanetUranus;
        public GameObject PlanetNeptune;

        [Header("Scale in AR (meters per AU)")]
        public float MetersPerAU = 0.5f;

        private Vector3 _targetPlanetDir;
        private string  _targetPlanetId;
        private float   _targetPlanetAU;

        [HideInInspector] public Transform ShipAnchor;

        void Start()
        {
            StartCoroutine(InitAR());
        }

        IEnumerator InitAR()
        {
            yield return new WaitUntil(() => Input.location.isEnabledByUser);
            Input.location.Start();
            yield return new WaitForSeconds(1f);

            float lat = Input.location.lastData.latitude;
            float lon = Input.location.lastData.longitude;
            Debug.Log($"[ARSpace] GPS: {lat:F4}, {lon:F4}");
        }

        public void SetPlanetDirection(string planetId, float azimuthDeg, float altitudeDeg, float distanceAU)
        {
            _targetPlanetId = planetId;
            _targetPlanetAU = distanceAU;

            float az  = azimuthDeg  * Mathf.Deg2Rad;
            float alt = altitudeDeg * Mathf.Deg2Rad;
            _targetPlanetDir = new Vector3(
                Mathf.Sin(az) * Mathf.Cos(alt),
                Mathf.Sin(alt),
                Mathf.Cos(az) * Mathf.Cos(alt)
            ).normalized;

            PlacePlanetInAR(planetId, _targetPlanetDir, distanceAU);
        }

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
                : 3f;

            Camera cam = ARCamera != null ? ARCamera : Camera.main;
            Vector3 worldPos = cam.transform.position + direction * dist3D;

            var instance = Instantiate(prefab, worldPos, Quaternion.identity);
            instance.name = $"Planet_{planetId}";

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

        public Vector3 DirectionToTarget => _targetPlanetDir;
        public float   DistanceToTarget  => _targetPlanetAU;
    }
}
