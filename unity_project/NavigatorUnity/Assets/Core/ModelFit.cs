// Navigator 2.0 — ModelFit.cs
// Los GLB de Meshy vienen a escalas arbitrarias: esto los ajusta a un tamaño conocido
// Guacalita Inc. — SAL / Claude

using UnityEngine;

namespace Navigator.Core
{
    public static class ModelFit
    {
        // Meshy exports face arbitrary directions. A ship is longest nose-to-tail, so turn the
        // model until its longest horizontal side lies along +Z. `extraYaw` (0 or 180) picks
        // which end is the nose. Returns the yaw applied. Call before FitToSize.
        public static float AlignLongAxisToForward(GameObject model, float extraYaw = 0f)
        {
            model.transform.localRotation = Quaternion.identity;
            var renderers = model.GetComponentsInChildren<Renderer>();
            float yaw = 0f;
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                if (b.size.x > b.size.z * 1.1f) yaw = 90f;
            }
            yaw += extraYaw;
            model.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return yaw;
        }

        // Scales `model` uniformly so its largest renderer-bounds side equals `size`
        // and recenters it on its parent. Returns the applied scale factor.
        public static float FitToSize(GameObject model, float size)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return 1f;

            model.transform.localScale = Vector3.one;
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

            float largest = Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (largest < 1e-5f) return 1f;

            float scale = size / largest;
            model.transform.localScale = Vector3.one * scale;

            // Bounds are world-space; shift so the visual center sits on the transform's origin
            Vector3 centerOffset = (b.center - model.transform.position) * scale;
            model.transform.position -= centerOffset;
            return scale;
        }
    }
}
