// Navigator 2.0 — SkyFinderUI.cs
// "Where is it tonight?": uses GPS + compass + NASA JPL Horizons to point the phone at the
// real planet in the sky, right now, from where the player stands. Go outside and look.
// Guacalita Inc. — SAL / Claude

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Navigator.Core;
using Navigator.Network;
using Navigator.AR;

namespace Navigator.UI
{
    public class SkyFinderUI : MonoBehaviour
    {
        public GameObject Panel;
        public TMP_Text   PlanetLabel;
        public TMP_Text   StatusLabel;
        public TMP_Text   DetailLabel;
        public Image      Arrow;           // points to the planet while it's off screen
        public RectTransform Reticle;      // circles the planet while it's on screen
        public Button     CloseButton;
        public Button[]   PlanetButtons;   // mercury … neptune

        [Header("Refs")]
        public NasaHorizons   Horizons;
        public ARSpaceManager SpaceManager;
        public Camera         ARCamera;

        const float MarkerDistance = 500f;
        static readonly string[] Planets = { "mercury", "venus", "mars", "jupiter", "saturn", "uranus", "neptune" };

        private string      _planet = "mars";
        private SkyPosition _sky;
        private bool        _located;
        private GameObject  _marker;
        private Coroutine   _locate;

        void Awake()
        {
            CloseButton?.onClick.AddListener(() => GameManager.Instance?.CloseSkyFinder());
            for (int i = 0; i < PlanetButtons.Length && i < Planets.Length; i++)
            {
                string id = Planets[i];
                PlanetButtons[i]?.onClick.AddListener(() => Locate(id));
            }
            if (Panel != null) Panel.SetActive(false);
        }

        public void Show(string planetId)
        {
            if (Panel != null) Panel.SetActive(true);
            Navigator.Game.CosmicSkyFX.SetVisible(false);
            Locate(planetId);
        }

        public void Hide()
        {
            if (_locate != null) StopCoroutine(_locate);
            if (_marker != null) Destroy(_marker);
            if (Panel != null) Panel.SetActive(false);
            Navigator.Game.CosmicSkyFX.SetVisible(true);
        }

        void Locate(string planetId)
        {
            _planet  = planetId;
            _located = false;
            if (_marker != null) Destroy(_marker);
            if (PlanetLabel != null) PlanetLabel.text = Loc.Planet(planetId).ToUpper();
            if (DetailLabel != null) DetailLabel.text = "";
            for (int i = 0; i < PlanetButtons.Length && i < Planets.Length; i++)
                if (PlanetButtons[i] != null)
                    PlanetButtons[i].GetComponent<Image>().color = Planets[i] == planetId ? UIStyle.Accent : UIStyle.ButtonIdle;

            if (_locate != null) StopCoroutine(_locate);
            _locate = StartCoroutine(LocateRoutine(planetId));
        }

        IEnumerator LocateRoutine(string planetId)
        {
            SetStatus(Loc.T("sky.gps"), UIStyle.Muted);

            if (Input.location.status != LocationServiceStatus.Running)
            {
                if (!Input.location.isEnabledByUser && !Application.isEditor)
                {
                    SetStatus(Loc.T("sky.nogps"), UIStyle.Bad);
                    yield break;
                }
                Input.location.Start(50f, 10f);
                float wait = 15f;
                while (Input.location.status == LocationServiceStatus.Initializing && wait > 0f)
                {
                    wait -= Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            Input.compass.enabled = true;

            float lat, lon;
            if (Input.location.status == LocationServiceStatus.Running)
            {
                lat = Input.location.lastData.latitude;
                lon = Input.location.lastData.longitude;
            }
            else if (Application.isEditor)
            {
                lat = 4.711f; lon = -74.072f;   // Editor has no GPS: Bogotá, for testing
            }
            else
            {
                SetStatus(Loc.T("sky.nogps"), UIStyle.Bad);
                yield break;
            }

            // Let the compass settle, then make "forward" true north so the sky lines up
            yield return new WaitForSeconds(0.8f);
            var rig = ARCamera != null ? ARCamera.GetComponent<GyroCameraRig>() : null;
            if (rig != null) rig.Calibrate(true);

            SetStatus(Loc.T("sky.asking"), UIStyle.Muted);
            SkyPosition sky = default;
            if (Horizons != null) yield return Horizons.GetSky(planetId, lat, lon, p => sky = p);
            if (!sky.Ok)
            {
                SetStatus(Loc.T("sky.offline"), UIStyle.Bad);
                yield break;
            }

            _sky = sky;
            _located = true;
            PlaceMarker(planetId, sky);
            DescribeSky(planetId, sky);
        }

        static Vector3 Direction(SkyPosition s)
        {
            float az = s.Azimuth * Mathf.Deg2Rad, el = s.Altitude * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(az) * Mathf.Cos(el), Mathf.Sin(el), Mathf.Cos(az) * Mathf.Cos(el));
        }

        void PlaceMarker(string planetId, SkyPosition sky)
        {
            var cam = ARCamera != null ? ARCamera : Camera.main;
            if (cam == null) return;
            _marker = new GameObject($"Sky_{planetId}");
            _marker.transform.position = cam.transform.position + Direction(sky) * MarkerDistance;
            var prefab = SpaceManager != null ? SpaceManager.GetPlanetPrefab(planetId) : null;
            if (prefab != null)
            {
                var model = Instantiate(prefab, _marker.transform);
                ModelFit.FitToSize(model, 36f);
            }
        }

        void DescribeSky(string planetId, SkyPosition sky)
        {
            if (sky.IsDaytime && sky.AboveHorizon) SetStatus(Loc.T("sky.daytime"), UIStyle.Warn);
            else if (sky.AboveHorizon)             SetStatus(Loc.F("sky.up", Loc.Planet(planetId)), UIStyle.Good);
            else                                   SetStatus(Loc.T("sky.below"), UIStyle.Warn);

            if (DetailLabel != null)
                DetailLabel.text = Loc.F("sky.detail", sky.Azimuth, Compass(sky.Azimuth), sky.Altitude,
                                         sky.DistanceAU, sky.DistanceAU * 8.317f) +
                                   "\n" + Loc.T("sky.eye." + planetId);
        }

        static string Compass(float az)
        {
            string[] en = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            string[] es = { "N", "NE", "E", "SE", "S", "SO", "O", "NO" };
            int i = Mathf.RoundToInt(Mathf.Repeat(az, 360f) / 45f) % 8;
            return Loc.Current == Lang.Spanish ? es[i] : en[i];
        }

        void SetStatus(string text, Color color)
        {
            if (StatusLabel == null) return;
            StatusLabel.text  = text;
            StatusLabel.color = color;
        }

        void Update()
        {
            if (Panel == null || !Panel.activeSelf || !_located) { Toggle(false, false); return; }
            var cam = ARCamera != null ? ARCamera : Camera.main;
            if (cam == null) return;

            Vector3 world = cam.transform.position + Direction(_sky) * MarkerDistance;
            if (_marker != null) _marker.transform.position = world;   // follows the camera, stays in its sky spot

            Vector3 sp = cam.WorldToScreenPoint(world);
            bool behind = sp.z < 0f;
            bool onScreen = !behind && sp.x > 0 && sp.x < Screen.width && sp.y > 0 && sp.y < Screen.height;
            Toggle(!onScreen, onScreen);

            if (onScreen && Reticle != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)Reticle.parent, sp, null, out var local);
                Reticle.anchoredPosition = local;
                Reticle.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(Time.time * 4f));
            }
            else if (Arrow != null)
            {
                Vector2 d = (Vector2)sp - new Vector2(Screen.width / 2f, Screen.height / 2f);
                if (behind) d = -d;
                Arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f);
            }
        }

        void Toggle(bool arrow, bool reticle)
        {
            if (Arrow   != null && Arrow.gameObject.activeSelf   != arrow)   Arrow.gameObject.SetActive(arrow);
            if (Reticle != null && Reticle.gameObject.activeSelf != reticle) Reticle.gameObject.SetActive(reticle);
        }
    }
}
