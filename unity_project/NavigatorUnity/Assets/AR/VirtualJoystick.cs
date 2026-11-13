// Navigator 2.0 — VirtualJoystick.cs
// On-screen stick for phones and tablets (no keyboard): drag to steer.
// Floating: the stick appears where the thumb lands inside its zone, so a touch never starts
// at full deflection. Only the finger that grabbed it moves it (a second finger can't steal it),
// and the output is lightly smoothed so touch-screen noise doesn't make the ship wobble.
// Guacalita Inc. — SAL / Claude

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Navigator.AR
{
    // Runs before the ship reads it, so a stick value is never one frame old
    [DefaultExecutionOrder(-50)]
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform Base;   // the ring that follows the thumb (child of this zone)
        public RectTransform Knob;   // child of Base
        [Tooltip("Knob travel in UI units")]
        public float Range = 80f;
        [Range(0f, 0.5f)] public float DeadZone  = 0.08f;
        [Range(1f, 3f)]   public float Exponent  = 1.2f;    // lower = small pushes already move the ship
        [Range(0f, 0.2f)] public float Smoothing = 0.035f;  // seconds

        [Header("Look")]
        public Graphic BaseGraphic;   // brightens while held
        public Graphic KnobGraphic;
        public Color   IdleColor   = new(0.4f, 0.85f, 1f, 0.35f);
        public Color   ActiveColor = new(0.4f, 0.9f, 1f, 0.95f);
        private float  _glow;

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
            // The ring starts under the thumb, exactly (no clamp: clamping would start the stick already deflected)
            _origin = Local(e);
            if (Base != null) Base.anchoredPosition = _origin;
            OnDrag(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != _pointerId) return;
            Vector2 v = Vector2.ClampMagnitude(Local(e) - _origin, Range);
            // The ring turns to face the push, so its arrow shows the direction you steer
            if (Base != null && v.sqrMagnitude > 25f)
                Base.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg - 90f);
            if (Knob != null)
            {
                // The knob is a child of the ring: undo the ring's turn so it sits under the thumb
                Vector2 local = Base != null ? (Vector2)(Quaternion.Inverse(Base.localRotation) * v) : v;
                Knob.anchoredPosition = local;
                Knob.rotation = Quaternion.identity;
            }

            // Dead zone so a resting thumb doesn't drift, then a soft curve for fine aiming
            Vector2 n = v / Range;
            float m = n.magnitude;
            _raw = m <= DeadZone ? Vector2.zero
                 : n / m * Mathf.Pow((m - DeadZone) / (1f - DeadZone), Exponent);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId == _pointerId) Release();
        }

        void OnDisable() => Release();

        // A call, a notification or switching apps can swallow the finger-up: never keep steering after it
        void OnApplicationPause(bool paused) { if (paused) Release(); }
        void OnApplicationFocus(bool focused) { if (!focused) Release(); }

        void Update()
        {
            // Light smoothing; releasing snaps to zero at once so the ship stops turning
            Value = _pointerId == NoPointer || Smoothing <= 0f
                ? _raw
                : Vector2.SmoothDamp(Value, _raw, ref _velocity, Smoothing, Mathf.Infinity, Time.unscaledDeltaTime);

            // Visuals: glow while held; on release the knob and ring glide home instead of snapping
            float dt = Time.unscaledDeltaTime;
            _glow = Mathf.MoveTowards(_glow, Active ? 1f : 0f, dt * 6f);
            if (BaseGraphic != null) BaseGraphic.color = Color.Lerp(IdleColor, ActiveColor, _glow);
            if (KnobGraphic != null) KnobGraphic.color = Color.Lerp(IdleColor, Color.white, _glow);
            if (Knob != null) Knob.localScale = Vector3.one * (1f + 0.15f * _glow);
            if (!Active)
            {
                float k = 1f - Mathf.Exp(-18f * dt);
                if (Knob != null) Knob.anchoredPosition = Vector2.Lerp(Knob.anchoredPosition, Vector2.zero, k);
                if (Base != null)
                {
                    Base.anchoredPosition = Vector2.Lerp(Base.anchoredPosition, _restPos, k);
                    Base.localRotation    = Quaternion.Slerp(Base.localRotation, Quaternion.identity, k);
                }
            }
        }

        Vector2 Local(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rt, e.position, e.pressEventCamera, out var p);
            return p - _rt.rect.center;   // from the middle of the zone, whatever the pivot
        }

        void Release()
        {
            _pointerId = NoPointer;
            _raw = Value = _velocity = Vector2.zero;   // steering stops at once; the knob glides back in Update
        }
    }
}
