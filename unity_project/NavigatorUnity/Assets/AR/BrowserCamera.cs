// Navigator 2.0 — BrowserCamera.cs
// The phone's camera in the web build (NavCam.jspre / NavCam.jslib). Unity's WebCamTexture breaks
// in browsers (it floods "glCopySubTextureCHROMIUM: Offset overflows texture dimensions", shows
// nothing and can't be reopened after Stop), so the browser opens the camera itself and hands
// over small RGBA frames that are uploaded into a normal Texture2D.
// Rows arrive top first: draw the texture with uvRect (0, 1, 1, −1).
// Also the browser's QR scanner (BarcodeDetector or jsQR), its GPS and its compass.
// Guacalita Inc. — SAL / Claude

#if UNITY_WEBGL && !UNITY_EDITOR
#define NAV_WEB
#endif

using UnityEngine;
#if NAV_WEB
using System.Runtime.InteropServices;
#endif

namespace Navigator.AR
{
    public sealed class BrowserCamera
    {
#if NAV_WEB
        [DllImport("__Internal")] static extern void NavCam_Start();
        [DllImport("__Internal")] static extern void NavCam_Stop();
        [DllImport("__Internal")] static extern int  NavCam_State();
        [DllImport("__Internal")] static extern int  NavCam_Size(int maxSide);
        [DllImport("__Internal")] static extern int  NavCam_Frame(byte[] buf, int len, int maxSide);
        [DllImport("__Internal")] static extern void NavCam_ScanStart(string gameObject);
        [DllImport("__Internal")] static extern void NavCam_ScanStop();
        public static bool Available => true;
#else
        static void NavCam_Start() { }
        static void NavCam_Stop() { }
        static int  NavCam_State() => 3;
        static int  NavCam_Size(int maxSide) => 0;
        static int  NavCam_Frame(byte[] buf, int len, int maxSide) => 0;
        static void NavCam_ScanStart(string gameObject) { }
        static void NavCam_ScanStop() { }
        public static bool Available => false;
#endif

        public enum CamState { Idle = 0, Opening = 1, Live = 2, Failed = 3 }

        public Texture2D Texture { get; private set; }
        public bool      Running { get; private set; }
        public CamState  State   => Running ? (CamState)NavCam_State() : CamState.Idle;
        public bool      HasFrame { get; private set; }

        readonly int _maxSide;
        byte[] _buf;

        // maxSide: longest side of the frames handed to Unity (the background doesn't need full HD)
        public BrowserCamera(int maxSide = 720) => _maxSide = maxSide;

        public void Start()
        {
            if (Running) return;
            Running = true;
            HasFrame = false;
            NavCam_Start();   // shared stream: the browser opens the camera once for everyone
        }

        public void Stop()
        {
            if (!Running) return;
            Running = false;
            HasFrame = false;
            NavCam_Stop();
        }

        // Call once per frame; true when the texture got a new picture
        public bool Update()
        {
            if (!Running) return false;
            int packed = NavCam_Size(_maxSide);
            if (packed <= 0) return false;
            int w = packed >> 16, h = packed & 0xFFFF;
            if (Texture == null || Texture.width != w || Texture.height != h)
            {
                if (Texture != null) Object.Destroy(Texture);
                Texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
                {
                    name = "BrowserCamera", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
                };
                _buf = new byte[w * h * 4];
            }
            if (NavCam_Frame(_buf, _buf.Length, _maxSide) != 1) return false;
            Texture.LoadRawTextureData(_buf);
            Texture.Apply(false);
            HasFrame = true;
            return true;
        }

        public void Dispose()
        {
            Stop();
            if (Texture != null) Object.Destroy(Texture);
            Texture = null;
            _buf = null;
        }

        // The browser scans the camera for a QR and calls gameObject.OnWebScan(text) once
        public static void ScanStart(string gameObject) => NavCam_ScanStart(gameObject);
        public static void ScanStop() => NavCam_ScanStop();
    }
}
