// Navigator 2.0 — DeviceGyro.cs
// One gyroscope API for every platform. Natively it's Unity's Input.gyro; in a browser Unity has
// no gyro, so the phone's orientation comes from the DeviceOrientation event (NavP2P.jslib) and
// is turned into the same quaternion Android reports (device → east/north/up frame, right-handed),
// so GyroCameraRig's conversion works unchanged. W3C formula: R = Rz(α) · Rx(β) · Ry(γ).
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
    public static class DeviceGyro
    {
#if NAV_WEB
        [DllImport("__Internal")] static extern int    NavGyro_Supported();
        [DllImport("__Internal")] static extern void   NavGyro_Start();
        [DllImport("__Internal")] static extern int    NavGyro_HasData();
        [DllImport("__Internal")] static extern double NavGyro_Get(int axis);

        public static bool Supported => NavGyro_Supported() == 1;
        public static void Enable()  => NavGyro_Start();
        public static bool HasData   => NavGyro_HasData() == 1;

        public static Quaternion Attitude
        {
            get
            {
                double a = NavGyro_Get(0) * Mathf.Deg2Rad, b = NavGyro_Get(1) * Mathf.Deg2Rad, g = NavGyro_Get(2) * Mathf.Deg2Rad;
                return FromEuler(a, b, g);
            }
        }
#else
        public static bool Supported => SystemInfo.supportsGyroscope;
        public static void Enable()  => Input.gyro.enabled = true;
        public static bool HasData   => true;
        public static Quaternion Attitude => Input.gyro.attitude;
#endif

        // DeviceOrientation (alpha, beta, gamma in radians) → quaternion (x, y, z, w)
        public static Quaternion FromEuler(double alpha, double beta, double gamma)
        {
            double cX = System.Math.Cos(beta / 2),  sX = System.Math.Sin(beta / 2);
            double cY = System.Math.Cos(gamma / 2), sY = System.Math.Sin(gamma / 2);
            double cZ = System.Math.Cos(alpha / 2), sZ = System.Math.Sin(alpha / 2);
            double w = cX * cY * cZ - sX * sY * sZ;
            double x = sX * cY * cZ - cX * sY * sZ;
            double y = cX * sY * cZ + sX * cY * sZ;
            double z = cX * cY * sZ + sX * sY * cZ;
            return new Quaternion((float)x, (float)y, (float)z, (float)w);
        }
    }
}
