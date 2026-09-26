// Navigator 2.0 — VirtualJoystick.cs
// On-screen stick for phones and tablets (no keyboard): drag to steer
// Guacalita Inc. — SAL / Claude

using UnityEngine;
using UnityEngine.EventSystems;

namespace Navigator.AR
{
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform Knob;
        [Tooltip("Knob travel in UI units")]
        public float Range = 110f;

        // -1..1 on each axis; x = turn, y = climb
        public Vector2 Value { get; private set; }

        private RectTransform _rt;

        void Awake() => _rt = (RectTransform)transform;

        public void OnPointerDown(PointerEventData e) => OnDrag(e);

        public void OnDrag(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rt, e.position, e.pressEventCamera, out var p);
            Vector2 v = Vector2.ClampMagnitude(p, Range);
            if (Knob != null) Knob.anchoredPosition = v;
            Value = v / Range;
        }

        public void OnPointerUp(PointerEventData e) => Release();

        void OnDisable() => Release();

        void Release()
        {
            Value = Vector2.zero;
            if (Knob != null) Knob.anchoredPosition = Vector2.zero;
        }
    }
}
