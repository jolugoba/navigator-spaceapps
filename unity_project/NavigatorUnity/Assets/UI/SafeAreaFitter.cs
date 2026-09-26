// Navigator 2.0 — SafeAreaFitter.cs
// Mantiene la UI fuera del notch y de la barra de gestos del celular
// Guacalita Inc. — SAL / Claude

using UnityEngine;

namespace Navigator.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private Rect       _applied;
        private Vector2Int _screen;

        void OnEnable() => Apply();

        void Update()
        {
            if (Screen.safeArea != _applied || _screen.x != Screen.width || _screen.y != Screen.height)
                Apply();
        }

        void Apply()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            Rect safe = Screen.safeArea;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            rt.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            _applied = safe;
            _screen  = new Vector2Int(Screen.width, Screen.height);
        }
    }
}
