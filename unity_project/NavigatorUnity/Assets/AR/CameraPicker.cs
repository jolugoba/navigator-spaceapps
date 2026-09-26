// Navigator 2.0 — CameraPicker.cs
// Picks the phone's back camera. On Android/iOS Unity says which camera faces front; in a browser
// it often can't, so the name ("back", "rear", "environment"…) decides, and phones list the back
// camera last. Also asks the browser/iOS for camera permission first.
// Guacalita Inc. — SAL / Claude

using System.Collections;
using UnityEngine;

namespace Navigator.AR
{
    public static class CameraPicker
    {
        public static string BackCamera()
        {
            var devices = WebCamTexture.devices;
            if (devices.Length == 0) return null;
            bool anyFront = false;
            foreach (var d in devices) anyFront |= d.isFrontFacing;
            if (anyFront)
                foreach (var d in devices) if (!d.isFrontFacing) return d.name;
            foreach (var d in devices)
            {
                string n = (d.name ?? "").ToLowerInvariant();
                if (n.Contains("back") || n.Contains("rear") || n.Contains("environment") || n.Contains("trase") || n.Contains("posterior"))
                    return d.name;
            }
            return devices[devices.Length - 1].name;
        }

        // Web and iOS need the user's OK before cameras are listed or opened
        public static IEnumerator RequestPermission()
        {
#if (UNITY_WEBGL || UNITY_IOS) && !UNITY_EDITOR
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
                yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
#endif
            yield break;
        }

#if (UNITY_WEBGL || UNITY_IOS) && !UNITY_EDITOR
        public static bool HasPermission => Application.HasUserAuthorization(UserAuthorization.WebCam);
#else
        public static bool HasPermission => true;
#endif
    }
}
