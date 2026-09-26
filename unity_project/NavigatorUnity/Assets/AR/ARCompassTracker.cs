// Navigator 2.0 — ARCompassTracker.cs
// GPS + brújula → dirección real al planeta objetivo en el espacio AR
// NASA Horizons da azimuth/altitud real según tu ubicación GPS
// Si no hay GPS o Horizons falla → dirección fija (siempre funciona en interiores)
// Guacalita Inc. — SAL / Claude

using System.Collections;
using UnityEngine;

namespace Navigator.AR
{
    public class ARCompassTracker : MonoBehaviour
    {
        [Header("Refs")]
        public ARSpaceManager       SpaceManager;
        public Navigator.Network.NasaHorizons Horizons;

        [Header("Planeta objetivo (de MissionDesign)")]
        public string TargetPlanetId = "mars";

        [Header("Fallback si no hay GPS (al frente y 20° arriba)")]
        public Vector3 FallbackDirection = new(0f, 0.36f, 1f);

        // ── Runtime ────────────────────────────────────────────────────────────
        private float _latitude  = 4.7f;    // Bogotá por defecto
        private float _longitude = -74.0f;
        private bool  _gpsReady  = false;

        public bool GpsReady => _gpsReady;
        private Coroutine _fetch;

        void Start()
        {
            StartCoroutine(InitGPS());
        }

        // ── GPS init ──────────────────────────────────────────────────────────

        IEnumerator InitGPS()
        {
            // Solicitar permiso de localización
            if (!Input.location.isEnabledByUser)
            {
                Debug.LogWarning("[Compass] GPS desactivado. Se usará dirección fallback.");
                yield break;
            }

            Input.location.Start(10f, 5f);  // 10m precisión, update cada 5m
            Input.compass.enabled = true;

            float timeout = 10f;
            while (Input.location.status == LocationServiceStatus.Initializing && timeout > 0f)
            {
                yield return new WaitForSeconds(0.5f);
                timeout -= 0.5f;
            }

            if (Input.location.status != LocationServiceStatus.Running)
            {
                Debug.LogWarning("[Compass] GPS timeout. Se usará dirección fallback.");
                yield break;
            }

            _latitude  = Input.location.lastData.latitude;
            _longitude = Input.location.lastData.longitude;
            _gpsReady  = true;
            Debug.Log($"[Compass] GPS listo: {_latitude:F4}, {_longitude:F4}");
        }

        // ── Consulta NASA Horizons ─────────────────────────────────────────────

        IEnumerator FetchPlanetDirection()
        {
            if (Horizons == null) yield break;

            yield return StartCoroutine(
                Horizons.GetPlanetPosition(
                    TargetPlanetId,
                    _latitude,
                    _longitude,
                    (azimuth, altitude, distAU) =>
                    {
                        SpaceManager?.SetPlanetDirection(TargetPlanetId, azimuth, altitude, distAU);
                        Debug.Log($"[Compass] {TargetPlanetId}: az={azimuth:F1}° alt={altitude:F1}° dist={distAU:F2}AU");
                    }));
        }

        // ── Fallback: planeta siempre al norte, 30° arriba ────────────────────
        // Funciona en interiores, aulas, eventos — no necesita GPS

        void ApplyFallback()
        {
            SpaceManager?.SetPlanetDirectionFallback(
                TargetPlanetId,
                FallbackDirection.normalized,
                ARSpaceManager.ApproxEarthDistanceAU(TargetPlanetId));
        }

        // ── Cambia el planeta objetivo en vuelo ───────────────────────────────

        // Places the planet right away (fallback) and refines it with Horizons if GPS is up.
        // Called at countdown start so Horizons has a few seconds before the route locks.
        public void SetTarget(string planetId)
        {
            TargetPlanetId = planetId;
            if (_fetch != null) StopCoroutine(_fetch);
            ApplyFallback();
            if (_gpsReady) _fetch = StartCoroutine(FetchPlanetDirection());
        }
    }
}
