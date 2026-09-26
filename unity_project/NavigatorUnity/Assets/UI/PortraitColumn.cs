// Navigator 2.0 — PortraitColumn.cs
// Keeps a screen laid out as a phone-shaped column, centred, whatever the window shape.
// Portrait phones use the full width; landscape windows (Editor, tablets) get a centred column.
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
