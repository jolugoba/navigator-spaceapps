// Navigator 2.0 — VirtualJoystick.cs
// On-screen stick for phones and tablets (no keyboard): drag to steer.
// Floating: the stick appears where the thumb lands inside its zone, so a touch never starts
// at full deflection. Only the finger that grabbed it moves it (a second finger can't steal it),
// and the output is lightly smoothed so touch-screen noise doesn't make the ship wobble.
// Guacalita Inc. — SAL / Claude

using UnityEngine;
using UnityEngine.EventSystems;

namespace Navigator.AR
{
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform Base;   // the ring that follows the thumb (child of this zone)
        public RectTransform Knob;   // child of Base
        [Tooltip("Knob travel in UI units")]
        public float Range = 80f;
        [Range(0f, 0.5f)] public float DeadZone  = 0.12f;
        [Range(1f, 3f)]   public float Exponent  = 1.5f;
        [Range(0f, 0.2f)] public float Smoothing = 0.05f;   // seconds

        // -1..1 on each axis; x = turn, y = climb
        public Vector2 Value { get; private set; }
        public bool    Active => _pointerId != NoPointer;

        const int NoPointer = int.MinValue;
        private RectTransform _rt;
        private int     _pointerId = NoPointer;
        private Vector2 _origin, _raw, _velocity, _restPos;

        void Awake()
        {
            _rt = (RectTransform)transform;
            if (Base != null) _restPos = Base.anchoredPosition;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (_pointerId != NoPointer) return;   // already held by another finger
            _pointerId = e.pointerId;
            _origin = Local(e);
            if (Base != null)
            {
                // Keep the whole ring inside the zone
                Vector2 half = _rt.rect.size * 0.5f - Base.rect.size * 0.5f;
                _origin = new Vector2(Mathf.Clamp(_origin.x, -half.x, half.x), Mathf.Clamp(_origin.y, -half.y, half.y));
                Base.anchoredPosition = _origin;
            }
            OnDrag(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != _pointerId) return;
            Vector2 v = Vector2.ClampMagnitude(Local(e) - _origin, Range);
            if (Knob != null) Knob.anchoredPosition = v;

            // Dead zone so a resting thumb doesn't drift, then a soft curve for fine aiming
            Vector2 n = v / Range;
            float m = n.magnitude;
            _raw = m < DeadZone ? Vector2.zero
                 : n / m * Mathf.Pow((m - DeadZone) / (1f - DeadZone), Exponent);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId == _pointerId) Release();
        }

        void OnDisable() => Release();

        void Update()
        {
            // Light smoothing; releasing snaps to zero at once so the ship stops turning
            Value = _pointerId == NoPointer || Smoothing <= 0f
                ? _raw
                : Vector2.SmoothDamp(Value, _raw, ref _velocity, Smoothing, Mathf.Infinity, Time.unscaledDeltaTime);
        }

        Vector2 Local(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rt, e.position, e.pressEventCamera, out var p);
            return p - _rt.rect.center;   // from the middle of the zone, whatever the pivot
        }

        void Release()
        {
            _pointerId = NoPointer;
            _raw = Value = _velocity = Vector2.zero;
            if (Knob != null) Knob.anchoredPosition = Vector2.zero;
            if (Base != null) Base.anchoredPosition = _restPos;
        }
    }
}
