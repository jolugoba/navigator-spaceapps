// Navigator 2.0 — DeviceLocation.cs
// GPS and compass for every platform. Natively it's Unity's Input.location / Input.compass; in a
// browser those never ask for permission and never start, so the web build uses
// navigator.geolocation (the browser shows "allow location?") and the absolute orientation sensor
// (NavCam.jspre).
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
    public static class DeviceLocation
    {
        public enum Status { Idle, Asking, Running, Denied, Unavailable }

#if NAV_WEB
        [DllImport("__Internal")] static extern void   NavGeo_Start();
        [DllImport("__Internal")] static extern int    NavGeo_State();
        [DllImport("__Internal")] static extern double NavGeo_Get(int axis);
        [DllImport("__Internal")] static extern void   NavCompass_Start();
        [DllImport("__Internal")] static extern int    NavCompass_Kind();
        [DllImport("__Internal")] static extern double NavCompass_Get(int axis);

        // The browser asks the player; the answer arrives later (poll State)
        public static void Start()
        {
            NavGeo_Start();
            NavCompass_Start();
        }

        public static Status State => (Status)NavGeo_State();
        public static float Latitude  => (float)NavGeo_Get(0);
        public static float Longitude => (float)NavGeo_Get(1);

        // Camera heading from north in degrees (clockwise), given the rig's device → Unity conversion
        public static bool TryHeading(System.Func<Quaternion, Quaternion> toUnity, out float heading)
        {
            heading = 0f;
            switch (NavCompass_Kind())
            {
                case 1:   // absolute orientation: same maths as the gyro, but alpha is measured from north
                    var q = DeviceGyro.FromEuler(NavCompass_Get(0) * Mathf.Deg2Rad,
                                                 NavCompass_Get(1) * Mathf.Deg2Rad,
                                                 NavCompass_Get(2) * Mathf.Deg2Rad);
                    heading = toUnity(q).eulerAngles.y;
                    return true;
                case 2:   // iPhone: Safari already gives the heading
                    heading = (float)NavCompass_Get(3);
                    return true;
                default:
                    return false;
            }
        }
#else
        public static void Start()
        {
            if (Input.location.status == LocationServiceStatus.Stopped && (Input.location.isEnabledByUser || Application.isEditor))
                Input.location.Start(50f, 10f);
            Input.compass.enabled = true;
        }

        public static Status State
        {
            get
            {
                switch (Input.location.status)
                {
                    case LocationServiceStatus.Running:      return Status.Running;
                    case LocationServiceStatus.Initializing: return Status.Asking;
                    case LocationServiceStatus.Failed:       return Status.Denied;
                    default: return Input.location.isEnabledByUser ? Status.Idle : Status.Unavailable;
                }
            }
        }

        public static float Latitude  => Input.location.lastData.latitude;
        public static float Longitude => Input.location.lastData.longitude;

        public static bool TryHeading(System.Func<Quaternion, Quaternion> toUnity, out float heading)
        {
            heading = Input.compass.trueHeading;   // clockwise from north, same as Unity yaw
            return Input.compass.enabled && Input.compass.timestamp > 0;
        }
#endif
    }
}
