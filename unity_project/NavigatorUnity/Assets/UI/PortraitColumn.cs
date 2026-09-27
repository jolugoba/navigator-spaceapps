// Navigator 2.0 — PortraitColumn.cs
// Keeps a screen's layout area centred and no wider than MaxAspect × its height. The game is
// landscape: the builder uses a wide stage (2.1), so only ultra-wide windows get side margins.
// Guacalita Inc. — SAL / Claude

using UnityEngine;

namespace Navigator.UI
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class PortraitColumn : MonoBehaviour
    {
        [Tooltip("Maximum width / height of the column")]
        public float MaxAspect = 0.62f;

        void OnEnable() => Apply();
        void Update()   => Apply();

        void Apply()
        {
            var rt = (RectTransform)transform;
            if (rt.parent is not RectTransform parent) return;
            float width = Mathf.Min(parent.rect.width, parent.rect.height * MaxAspect);
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, 0f);
            rt.anchoredPosition = Vector2.zero;
        }
    }
}
