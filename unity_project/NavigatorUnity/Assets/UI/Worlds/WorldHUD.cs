// Navigator 2.0 — WorldHUD.cs
// The screen while you are in a world (built by NavigatorUIBuilder). Same layout as the flight
// HUD: two sticks, three round buttons whose labels change with the phase, meters at the top
// left, an arrow to the next objective, and the tools each phase needs: a timing bar (parachute,
// probe release), the entry circle, the touchdown indicator and the "data sent" bar.
// Guacalita Inc. — SAL / Claude

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Navigator.AR;
using Navigator.UI;

namespace Navigator.Worlds
{
    public enum BtnState { Hidden, Off, On, Pulse }

    [Serializable]
    public class ActionButton
    {
        public GameObject Root;
        public HoldButton Hold;
        public Image      Face;
        public TMP_Text   Label;
        public Image      Fill;       // radial ring for hold actions
        [NonSerialized] public BtnState State;
        [NonSerialized] public string   Reason;   // why it's off ("no drill on board")
        [NonSerialized] public Color    BaseColor;
    }

    [Serializable]
    public class Meter
    {
        public GameObject Root;
        public TMP_Text   Label, Value;
        public Image      Fill;
    }

    public class WorldHUD : MonoBehaviour
    {
        public GameObject Panel;
        public TMP_Text   Title, Objective, Checklist, Prompt;
        public GameObject BannerBox;
        public TMP_Text   Banner;
        public Meter[]    Meters = new Meter[3];

        public RectTransform Arrow;
        public TMP_Text      ArrowLabel;

        public GameObject    WindowBox;
        public RectTransform WindowZone, WindowNeedle;
        public TMP_Text      WindowLabel;

        public GameObject    EntryBox;
        public RectTransform EntryDot;
        public Image         EntryHeat;
        public TMP_Text      EntryLabel;

        public GameObject LandingBox;
        public TMP_Text   LandV, LandH, LandTilt;

        public GameObject DataBox;
        public Image      DataFill;
        public TMP_Text   DataLabel;

        public ActionButton BtnA, BtnB, BtnC;
        public VirtualJoystick MoveStick, LookStick;
        public Button ExitButton;
        public Image  FlashImage;

        public GameObject ResultPanel;
        public TMP_Text   ResultTitle, ResultBody, ResultStars;
        public Button     RetryButton, ContinueButton;

        public Camera Cam;

        float _bannerUntil, _promptUntil, _flash;
        Color _flashColor;
        string _promptSticky = "";

        public void Show(bool on)
        {
            if (Panel != null) Panel.SetActive(on);
            if (!on) return;
            // Remember each button's own colour once (later it may be greyed out)
            foreach (var b in new[] { BtnA, BtnB, BtnC }) if (b?.Face != null && b.BaseColor.a <= 0f) b.BaseColor = b.Face.color;
            ResetAll();
        }

        // Letterbox bars for the cinematic moments (entry, touchdown); created on first use
        RectTransform _barTop, _barBottom;
        float _cine, _cineTarget;

        public void SetCinematic(bool on) => _cineTarget = on ? 1f : 0f;

        void EnsureBars()
        {
            if (_barTop != null || Panel == null) return;
            _barTop = Bar("CineTop", 1f);
            _barBottom = Bar("CineBottom", 0f);
        }

        RectTransform Bar(string name, float y)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(Panel.transform, false);
            rt.SetSiblingIndex(1);   // behind the controls
            rt.anchorMin = new Vector2(0f, y); rt.anchorMax = new Vector2(1f, y);
            rt.pivot = new Vector2(0.5f, y);
            rt.sizeDelta = new Vector2(0f, 0f);
            var img = go.GetComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = false;
            return rt;
        }

        public void ResetAll()
        {
            EnsureBars();
            _cine = _cineTarget = 0f;
            SetTitle("", "");
            SetChecklist("");
            for (int i = 0; i < Meters.Length; i++) HideMeter(i);
            SetArrow(null, null);
            SetWindow(false, 0, 0, 0, "");
            SetEntry(false, Vector2.zero, 0f);
            SetLanding(false, 0, 0, 0);
            SetData(false, 0, 0);
            SetButton(BtnA, null, BtnState.Hidden);
            SetButton(BtnB, null, BtnState.Hidden);
            SetButton(BtnC, null, BtnState.Hidden);
            if (ResultPanel != null) ResultPanel.SetActive(false);
            if (BannerBox != null) BannerBox.SetActive(false);
            _promptSticky = "";
            if (Prompt != null) Prompt.text = "";
        }

        // ── Texts ────────────────────────────────────────────────────────────
        public void SetTitle(string title, string objective)
        {
            if (Title != null) Title.text = title;
            if (Objective != null) Objective.text = objective;
        }

        public void SetChecklist(string text) { if (Checklist != null) Checklist.text = text; }

        // A short hint above the buttons; sticky until replaced (seconds > 0 = temporary)
        public void SetPrompt(string text, float seconds = 0f)
        {
            if (seconds > 0f) { _promptUntil = Time.time + seconds; if (Prompt != null) Prompt.text = text; }
            else { _promptSticky = text ?? ""; if (Time.time >= _promptUntil && Prompt != null) Prompt.text = _promptSticky; }
        }

        public void ShowBanner(string text, Color color, float seconds = 2.5f)
        {
            if (BannerBox == null || Banner == null) return;
            Banner.text = text;
            Banner.color = color;
            BannerBox.SetActive(false);   // re-enable replays the pop-in
            BannerBox.SetActive(true);
            _bannerUntil = Time.time + seconds;
        }

        public void Flash(Color c, float amount = 0.6f) { _flashColor = c; _flash = Mathf.Max(_flash, amount); }

        // ── Meters (top left) ────────────────────────────────────────────────
        public void SetMeter(int i, string label, string value, float fill, Color color)
        {
            if (i < 0 || i >= Meters.Length || Meters[i]?.Root == null) return;
            var m = Meters[i];
            if (!m.Root.activeSelf) m.Root.SetActive(true);
            if (m.Label != null) m.Label.text = label;
            if (m.Value != null) { m.Value.text = value; m.Value.color = color; }
            if (m.Fill != null)  { m.Fill.fillAmount = Mathf.Clamp01(fill); m.Fill.color = color; }
        }

        public void HideMeter(int i)
        {
            if (i >= 0 && i < Meters.Length && Meters[i]?.Root != null) Meters[i].Root.SetActive(false);
        }

        // ── Objective arrow: around the screen centre toward the target ─────
        public void SetArrow(Vector3? target, string label)
        {
            if (Arrow == null) return;
            bool show = target.HasValue && Cam != null;
            if (Arrow.gameObject.activeSelf != show) Arrow.gameObject.SetActive(show);
            if (!show) return;
            Vector3 sp = Cam.WorldToScreenPoint(target.Value);
            bool behind = sp.z < 0f;
            var parent = (RectTransform)Arrow.parent;
            Vector2 center = new(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 d = (Vector2)sp - center;
            if (behind) d = -d;
            bool onScreen = !behind && sp.x > 60 && sp.x < Screen.width - 60 && sp.y > 60 && sp.y < Screen.height - 60;
            Vector2 at;
            if (onScreen) at = (Vector2)sp + new Vector2(0f, 70f);   // just above the target, pointing down
            else
            {
                float k = Mathf.Min((Screen.width * 0.42f) / Mathf.Max(1f, Mathf.Abs(d.x)), (Screen.height * 0.36f) / Mathf.Max(1f, Mathf.Abs(d.y)));
                at = center + d * k;
            }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, at, null, out var local);
            Arrow.anchoredPosition = local;
            float angle = onScreen ? 180f : Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f;
            Arrow.localRotation = Quaternion.Euler(0f, 0f, angle);
            if (ArrowLabel != null)
            {
                ArrowLabel.text = label;
                ArrowLabel.rectTransform.rotation = Quaternion.identity;   // text stays upright
            }
        }

        // ── Timing bar: press when the needle is in the green zone ──────────
        public void SetWindow(bool show, float zoneMin, float zoneMax, float needle, string label)
        {
            if (WindowBox == null) return;
            if (WindowBox.activeSelf != show) WindowBox.SetActive(show);
            if (!show) return;
            if (WindowZone != null) { WindowZone.anchorMin = new Vector2(Mathf.Clamp01(zoneMin), 0f); WindowZone.anchorMax = new Vector2(Mathf.Clamp01(zoneMax), 1f); }
            if (WindowNeedle != null) { WindowNeedle.anchorMin = WindowNeedle.anchorMax = new Vector2(Mathf.Clamp01(needle), 0.5f); WindowNeedle.anchoredPosition = Vector2.zero; }
            if (WindowLabel != null) WindowLabel.text = label;
        }

        // ── Entry: keep the dot inside the circle ────────────────────────────
        public void SetEntry(bool show, Vector2 dot, float heat01)
        {
            if (EntryBox == null) return;
            if (EntryBox.activeSelf != show) EntryBox.SetActive(show);
            if (!show) return;
            if (EntryDot != null) EntryDot.anchoredPosition = dot * 150f;
            if (EntryHeat != null) { EntryHeat.fillAmount = heat01; EntryHeat.color = Color.Lerp(UIStyle.Warn, UIStyle.Bad, heat01); }
        }

        // ── Touchdown: vertical speed, sideways speed, tilt (green/amber/red) ─
        public void SetLanding(bool show, float vs, float hs, float tilt, float vsMax = 3f, float hsMax = 1.5f, float tiltMax = 15f)
        {
            if (LandingBox == null) return;
            if (LandingBox.activeSelf != show) LandingBox.SetActive(show);
            if (!show) return;
            Put(LandV, Loc2("w.land.v", vs), Mathf.Abs(vs), vsMax);
            Put(LandH, Loc2("w.land.h", hs), Mathf.Abs(hs), hsMax);
            Put(LandTilt, Loc2("w.land.tilt", tilt), Mathf.Abs(tilt), tiltMax);
        }

        static string Loc2(string key, float v) => Navigator.Core.Loc.F(key, v);

        static void Put(TMP_Text t, string text, float v, float max)
        {
            if (t == null) return;
            t.text = text;
            t.color = v <= max ? UIStyle.Good : v <= max * 1.6f ? UIStyle.Warn : UIStyle.Bad;
        }

        public void SetData(bool show, float sent, float total)
        {
            if (DataBox == null) return;
            if (DataBox.activeSelf != show) DataBox.SetActive(show);
            if (!show) return;
            if (DataFill != null) DataFill.fillAmount = total > 0f ? sent / total : 0f;
            if (DataLabel != null) DataLabel.text = Navigator.Core.Loc.F("w.data", sent, total);
        }

        // ── Buttons ──────────────────────────────────────────────────────────
        public void SetButton(ActionButton b, string label, BtnState state, float fill = 0f, string reason = null)
        {
            if (b?.Root == null) return;
            b.State  = state;
            b.Reason = reason;
            bool show = state != BtnState.Hidden;
            if (b.Root.activeSelf != show) b.Root.SetActive(show);
            if (!show) return;
            if (b.Label != null && label != null) b.Label.text = label;
            if (b.Face != null)
            {
                var c = b.BaseColor.a > 0f ? b.BaseColor : b.Face.color;
                if (state == BtnState.Off) c = new Color(0.35f, 0.38f, 0.45f, 0.6f);
                b.Face.color = c;
            }
            if (b.Fill != null) b.Fill.fillAmount = Mathf.Clamp01(fill);
        }

        public void SetFill(ActionButton b, float fill) { if (b?.Fill != null) b.Fill.fillAmount = Mathf.Clamp01(fill); }

        // ── Result ───────────────────────────────────────────────────────────
        public void ShowResult(bool success, string title, string body, int stars, bool canRetry)
        {
            if (ResultPanel == null) return;
            ResultPanel.SetActive(true);
            if (ResultTitle != null) { ResultTitle.text = title; ResultTitle.color = success ? UIStyle.Good : UIStyle.Bad; }
            if (ResultBody != null) ResultBody.text = body;
            if (ResultStars != null)
            {
                ResultStars.text = new string('○', 3);
                StopAllCoroutines();
                StartCoroutine(RevealStars(stars));
            }
            if (RetryButton != null) RetryButton.gameObject.SetActive(canRetry);
        }

        // The stars light up one by one, each with a pop and a chime
        System.Collections.IEnumerator RevealStars(int stars)
        {
            yield return new WaitForSecondsRealtime(0.4f);
            for (int i = 1; i <= stars; i++)
            {
                ResultStars.text = new string('●', i) + new string('○', 3 - i);
                ResultStars.transform.localScale = Vector3.one * 1.35f;
                Navigator.Game.AudioFX.Play(Navigator.Game.Sfx.Pickup, 0.7f + 0.15f * i);
                float t = 0f;
                while (t < 0.3f)
                {
                    t += Time.unscaledDeltaTime;
                    ResultStars.transform.localScale = Vector3.one * Mathf.Lerp(1.35f, 1f, t / 0.3f);
                    yield return null;
                }
                yield return new WaitForSecondsRealtime(0.15f);
            }
        }

        void Update()
        {
            _cine = Mathf.MoveTowards(_cine, _cineTarget, Time.unscaledDeltaTime * 2.5f);
            if (_barTop != null)
            {
                float h = Mathf.SmoothStep(0f, 1f, _cine) * 90f;
                _barTop.sizeDelta = new Vector2(0f, h);
                _barBottom.sizeDelta = new Vector2(0f, h);
            }
            if (BannerBox != null && BannerBox.activeSelf && Time.time > _bannerUntil) BannerBox.SetActive(false);
            if (Prompt != null && _promptUntil > 0f && Time.time >= _promptUntil) { _promptUntil = 0f; Prompt.text = _promptSticky; }

            // Pulsing buttons ask to be pressed; "off" buttons explain why when tapped
            foreach (var b in new[] { BtnA, BtnB, BtnC })
            {
                if (b?.Root == null || !b.Root.activeSelf) continue;
                float s = b.State == BtnState.Pulse ? 1f + 0.07f * Mathf.Sin(Time.unscaledTime * 9f) : 1f;
                b.Root.transform.localScale = Vector3.one * s;
                if (b.State == BtnState.Off && b.Hold != null && b.Hold.PressedThisFrame && !string.IsNullOrEmpty(b.Reason))
                    SetPrompt(b.Reason, 3f);
            }

            if (FlashImage != null)
            {
                _flash = Mathf.MoveTowards(_flash, 0f, Time.unscaledDeltaTime * 2.5f);
                var c = _flashColor; c.a = _flash * 0.5f;
                FlashImage.color = c;
            }
        }

        // Buttons that are "off" don't act (the input layer asks this)
        public static bool Active(ActionButton b) => b != null && (b.State == BtnState.On || b.State == BtnState.Pulse);
    }
}
