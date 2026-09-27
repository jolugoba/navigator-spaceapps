// Navigator 2.0 — CameraFeedBackground.cs
// Muestra la cámara trasera del celular detrás de la escena 3D (AR sin ARCore/ARKit)
// En la versión web la cámara la abre el navegador (BrowserCamera): WebCamTexture no funciona ahí.
// Guacalita Inc. — SAL / Claude

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace Navigator.AR
{
    public class CameraFeedBackground : MonoBehaviour
    {
        public Camera ARCamera;
        public int    RequestedWidth  = 1280;
        public int    RequestedHeight = 720;
        public int    RequestedFPS    = 30;

        public bool IsPlaying => _web != null ? _web.Running : _tex != null && _tex.isPlaying;

        private WebCamTexture      _tex;
        private BrowserCamera      _web;
        private bool               _webShown;
        private RawImage           _image;
        private Canvas             _canvas;

        void Awake()
        {
            if (ARCamera == null) ARCamera = Camera.main;
            if (BrowserCamera.Available) _web = new BrowserCamera(720);
            BuildCanvas();
        }

        void BuildCanvas()
        {
            var canvasGO = new GameObject("CameraFeedCanvas");
            canvasGO.transform.SetParent(transform, false);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode    = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera   = ARCamera;
            _canvas.sortingOrder  = -1000;
            // Right in front of the far plane so every 3D object draws on top
            _canvas.planeDistance = ARCamera != null ? ARCamera.farClipPlane * 0.95f : 900f;

            var imgGO = new GameObject("Feed");
            imgGO.transform.SetParent(canvasGO.transform, false);
            _image = imgGO.AddComponent<RawImage>();
            _image.raycastTarget = false;
            _image.color = Color.black;
            var rt = _image.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);

            canvasGO.SetActive(false);
        }

        public void Play()
        {
            if (IsPlaying) return;
            if (_web != null)
            {
                // The browser asks for permission and opens the camera; frames arrive in Update
                _webShown = false;
                _web.Start();
                return;
            }
            StartCoroutine(PlayRoutine());
        }

        public void Stop()
        {
            StopAllCoroutines();
            if (_web != null) { _web.Stop(); _webShown = false; }
            if (_tex != null) _tex.Stop();
            if (_canvas != null) _canvas.gameObject.SetActive(false);
        }

        IEnumerator PlayRoutine()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Permission.RequestUserPermission(Permission.Camera);
                float wait = 10f;
                while (!Permission.HasUserAuthorizedPermission(Permission.Camera) && wait > 0f)
                {
                    wait -= Time.unscaledDeltaTime;
                    yield return null;
                }
                if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
                {
                    Debug.LogWarning("[CameraFeed] Permiso de cámara denegado — fondo espacial.");
                    yield break;
                }
            }
#else
            yield return CameraPicker.RequestPermission();
            if (!CameraPicker.HasPermission) yield break;
#endif
            if (_tex == null)
            {
                string device = CameraPicker.BackCamera();
                if (device == null)
                {
                    Debug.LogWarning("[CameraFeed] No hay cámara disponible.");
                    yield break;
                }
                _tex = new WebCamTexture(device, RequestedWidth, RequestedHeight, RequestedFPS);
            }

            _tex.Play();
            // WebCamTexture reports 16x16 until the first real frame arrives
            while (_tex.isPlaying && _tex.width <= 16) yield return null;

            if (ARCamera != null)
            {
                ARCamera.clearFlags      = CameraClearFlags.SolidColor;
                ARCamera.backgroundColor = Color.black;
            }
            _image.texture = _tex;
            _image.color   = Color.white;
            _canvas.gameObject.SetActive(true);
        }

        void Update()
        {
            if (_web != null) { UpdateWeb(); return; }
            if (!IsPlaying || _tex.width <= 16) return;

            int angle = _tex.videoRotationAngle;
            var rt = _image.rectTransform;
            rt.localEulerAngles = new Vector3(0f, 0f, -angle);

            // Cover the screen; when rotated 90°, the texture's width lies along screen height
            bool rotated = angle % 180 != 0;
            Vector2 screen = ((RectTransform)rt.parent).rect.size;
            float w = rotated ? _tex.height : _tex.width;
            float h = rotated ? _tex.width  : _tex.height;
            float scale = Mathf.Max(screen.x / w, screen.y / h);
            rt.sizeDelta = new Vector2(_tex.width * scale, _tex.height * scale);

            _image.uvRect = _tex.videoVerticallyMirrored
                ? new Rect(0f, 1f, 1f, -1f)
                : new Rect(0f, 0f, 1f, 1f);
        }

        // Web: the browser already turns the picture upright; rows arrive top first
        void UpdateWeb()
        {
            if (!_web.Running || !_web.Update()) return;
            var tex = _web.Texture;
            if (!_webShown)
            {
                _webShown = true;
                if (ARCamera != null)
                {
                    ARCamera.clearFlags      = CameraClearFlags.SolidColor;
                    ARCamera.backgroundColor = Color.black;
                }
                _image.texture = tex;
                _image.color   = Color.white;
                _image.uvRect  = new Rect(0f, 1f, 1f, -1f);
                _image.rectTransform.localEulerAngles = Vector3.zero;
                _canvas.gameObject.SetActive(true);
            }
            else if (_image.texture != tex) _image.texture = tex;   // new size (phone rotated)

            Vector2 screen = ((RectTransform)_image.rectTransform.parent).rect.size;
            float scale = Mathf.Max(screen.x / tex.width, screen.y / tex.height);
            _image.rectTransform.sizeDelta = new Vector2(tex.width * scale, tex.height * scale);
        }

        void OnDestroy()
        {
            if (_web != null) _web.Dispose();
            if (_tex != null) { _tex.Stop(); Destroy(_tex); }
        }
    }
}
