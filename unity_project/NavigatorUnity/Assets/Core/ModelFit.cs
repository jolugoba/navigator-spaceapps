// Navigator 2.0 — ModelFit.cs
// Los GLB de Meshy vienen a escalas arbitrarias: esto los ajusta a un tamaño conocido
// Guacalita Inc. — SAL / Claude

using UnityEngine;

namespace Navigator.Core
{
    public static class ModelFit
    {
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
