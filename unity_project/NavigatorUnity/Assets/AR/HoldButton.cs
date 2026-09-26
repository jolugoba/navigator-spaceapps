// Navigator 2.0 — HoldButton.cs
// On-screen button that reports whether it is being held (thrust, boost)
// Guacalita Inc. — SAL / Claude

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Navigator.AR
{
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Graphic Target;                 // brightens while held
        public bool Held { get; private set; }
        public bool PressedThisFrame => Held && _downFrame == Time.frameCount;

        private int   _downFrame = -1;
        private Color _idle;

        void Awake() { if (Target != null) _idle = Target.color; }

        // Counts fingers: lifting one of two fingers on the same button doesn't release it
        private readonly System.Collections.Generic.HashSet<int> _pointers = new();

        public void OnPointerDown(PointerEventData e)
        {
            if (_pointers.Count == 0) _downFrame = Time.frameCount;
            _pointers.Add(e.pointerId);
            Held = true; Tint(true);
        }
        public void OnPointerUp(PointerEventData e)   => Lift(e.pointerId);
        public void OnPointerExit(PointerEventData e) { if (_pointers.Contains(e.pointerId)) Lift(e.pointerId); }   // finger slid off
        void OnDisable() { _pointers.Clear(); Held = false; Tint(false); }

        void Lift(int id)
        {
            _pointers.Remove(id);
            Held = _pointers.Count > 0;
            if (!Held) Tint(false);
        }

        void Tint(bool on)
        {
            if (Target == null) return;
            var c = _idle;
            if (on) c = new Color(Mathf.Min(1f, c.r + 0.25f), Mathf.Min(1f, c.g + 0.25f), Mathf.Min(1f, c.b + 0.25f), Mathf.Min(1f, c.a + 0.3f));
            Target.color = c;
        }
    }
}
