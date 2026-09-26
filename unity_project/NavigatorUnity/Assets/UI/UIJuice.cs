// Navigator 2.0 — UIJuice.cs
// "Game feel" for the interface: every piece moves a little, so taps and alerts feel physical.
//   UIPress     — squash on touch, springy overshoot on release (a damped spring, not a tween)
//   UIPopIn     — alerts, toasts and cards pop in with a bounce each time they appear
//   UIPanelIntro— full screens fade and settle in
//   UIPulse     — warnings breathe (alpha)
//   UIGlitch    — the HUD jolts when the ship is hit
// All use unscaled time, so they work while the game is paused.
// Guacalita Inc. — SAL / Claude

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Navigator.UI
{
    // Damped spring on the scale: x'' = k·(target − x) − c·x'
    public class UIPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float Pressed   = 0.92f;
        public float Stiffness = 420f;
        public float Damping   = 20f;

        Vector3    _base = Vector3.one;
        float      _x = 1f, _v, _target = 1f;
        bool       _down;
        Selectable _sel;

        void Awake()
        {
            _base = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
            _sel  = GetComponent<Selectable>();
        }

        void OnDisable()
        {
            _x = _target = 1f; _v = 0f; _down = false;
            transform.localScale = _base;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (_sel != null && !_sel.IsInteractable()) return;
            _down = true; _target = Pressed;
        }

        public void OnPointerUp(PointerEventData e)   => Release();
        public void OnPointerExit(PointerEventData e) { if (_down) Release(); }

        void Release()
        {
            _down = false; _target = 1f;
            _v += 3f;   // a kick outward so it overshoots and settles
        }

        // Extra bounce from code (a shot fired, a pod collected…)
        public void Kick(float amount = 0.25f) => _v += amount * 12f;

        void Update()
        {
            if (Mathf.Abs(_x - _target) < 0.0005f && Mathf.Abs(_v) < 0.0005f)
            {
                if (_x != _target) { _x = _target; transform.localScale = _base * _x; }
                return;
            }
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            _v += ((_target - _x) * Stiffness - _v * Damping) * dt;
            _x += _v * dt;
            transform.localScale = _base * _x;
        }
    }

    // Pops in with a bounce every time it's enabled (or when Replay is called while visible)
    public class UIPopIn : MonoBehaviour
    {
        public float Duration = 0.35f;
        public float From     = 0.7f;

        float       _t = 1f;
        Vector3     _base = Vector3.one;
        CanvasGroup _cg;

        void Awake()
        {
            _base = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
            _cg = GetComponent<CanvasGroup>();
            if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
            _cg.blocksRaycasts = true;
        }

        void OnEnable() => Replay();

        public void Replay() { _t = 0f; Apply(0f); }

        void Update()
        {
            if (_t >= 1f) return;
            _t = Mathf.Min(1f, _t + Time.unscaledDeltaTime / Duration);
            Apply(_t);
        }

        void Apply(float t)
        {
            // easeOutBack: overshoots ~10 % then settles
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float e = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
            transform.localScale = _base * Mathf.LerpUnclamped(From, 1f, e);
            if (_cg != null) _cg.alpha = Mathf.Clamp01(t * 3f);
        }

        void OnDisable() => transform.localScale = _base;

        public static void Play(Component c)
        {
            if (c == null) return;
            var p = c.GetComponent<UIPopIn>();
            if (p == null) p = c.gameObject.AddComponent<UIPopIn>();
            if (p.isActiveAndEnabled) p.Replay();
        }
    }

    // Full screens: fade in and settle from 96 % scale
    public class UIPanelIntro : MonoBehaviour
    {
        public RectTransform Content;
        public float Duration = 0.28f;

        float       _t = 1f;
        CanvasGroup _cg;

        void Awake()
        {
            _cg = GetComponent<CanvasGroup>();
            if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
        }

        void OnEnable() { _t = 0f; Apply(0f); }

        void Update()
        {
            if (_t >= 1f) return;
            _t = Mathf.Min(1f, _t + Time.unscaledDeltaTime / Duration);
            Apply(_t);
        }

        void Apply(float t)
        {
            float e = 1f - Mathf.Pow(1f - t, 3f);   // easeOutCubic
            if (_cg != null) _cg.alpha = e;
            if (Content != null)
            {
                Content.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, e);
                Content.anchoredPosition = new Vector2(0f, Mathf.Lerp(-24f, 0f, e));
            }
        }
    }

    // Warnings breathe: the graphic's alpha swings between Min and Max
    public class UIPulse : MonoBehaviour
    {
        public float Min = 0.35f, Max = 1f, Speed = 4f;
        Graphic _g;
        float   _a0 = 1f;

        void Awake() { _g = GetComponent<Graphic>(); if (_g != null) _a0 = _g.color.a; }

        void Update()
        {
            if (_g == null) return;
            var c = _g.color;
            c.a = _a0 * Mathf.Lerp(Min, Max, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Speed));
            _g.color = c;
        }
    }

    // A hit jolts the whole HUD: a decaying random offset and a tiny tilt
    public class UIGlitch : MonoBehaviour
    {
        RectTransform _rt;
        Vector2 _home;
        float _amount;

        void Awake() { _rt = (RectTransform)transform; _home = _rt.anchoredPosition; }

        public void Hit(float amount) { if (Navigator.Core.FxPrefs.Motion) _amount = Mathf.Max(_amount, amount); }

        void Update()
        {
            if (_amount <= 0.001f)
            {
                if (_amount != 0f) { _amount = 0f; _rt.anchoredPosition = _home; _rt.localRotation = Quaternion.identity; }
                return;
            }
            _rt.anchoredPosition = _home + Random.insideUnitCircle * 26f * _amount;
            _rt.localRotation    = Quaternion.Euler(0f, 0f, Random.Range(-1.2f, 1.2f) * _amount);
            _amount = Mathf.MoveTowards(_amount, 0f, Time.unscaledDeltaTime * 2.2f);
        }
    }

    // Constant slow rotation (orbit rings on the world map)
    public class UISpin : MonoBehaviour
    {
        public float DegreesPerSecond = 12f;
        void Update() => transform.Rotate(0f, 0f, DegreesPerSecond * Time.unscaledDeltaTime);
    }
}
