// Navigator 2.0 — CockpitHUD.cs
// HUD en vuelo: velocidad, combustible, distancia, barra Burnout, estado nave
// Ported from Dart: navigator/lib/ui/hud_overlay.dart
// Guacalita Inc. — SAL / Claude

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Navigator.AR;
using Navigator.Core;
using Navigator.Game;

namespace Navigator.UI
{
    public class CockpitHUD : MonoBehaviour
    {
        // ── Referencias Inspector ─────────────────────────────────────────────
        [Header("Velocidad")]
        public TMP_Text   SpeedLabel;       // "1,240 m/s"
        public Image      SpeedGaugeFill;   // gauge circular

        [Header("Combustible")]
        public Image      FuelBar;          // barra horizontal
        public TMP_Text   FuelLabel;        // "72%"
        public GameObject LowFuelWarning;   // "⚠ COMBUSTIBLE BAJO" — parpadea

        [Header("Distancia al planeta")]
        public TMP_Text   DistanceLabel;    // "12.4 AU"
        public Image      ArrowToPlanet;    // flecha hacia el planeta en AR

        [Header("Barra Burnout")]
        public Image      BurnoutBar;               // barra de boost
        public GameObject BurnoutModeIndicator;     // "BURNOUT!" cuando activo
        public TMP_Text   NearMissComboLabel;        // "x3 NEAR MISS!"
        public GameObject NearMissComboObj;

        [Header("Estado nave (daño)")]
        public GameObject DamageIndicatorDamaged;   // panel naranja
        public GameObject DamageIndicatorCritical;  // panel rojo parpadeante
        public TMP_Text   LivesLabel;               // "♥♥♥"

        [Header("Retardo comunicación")]
        public TMP_Text   CommDelayLabel;   // "Retraso señal: 22 min"

        [Header("Posición en carrera")]
        public TMP_Text   RacePositionLabel; // "#1" "#2"

        [Header("Viñeta de pantalla (Burnout / casco crítico)")]
        public Image      Vignette;

        [Header("Controles")]
        public Button     GyroButton;        // apagar el giroscopio para jugar sentado o con control
        public TMP_Text   GyroLabel;

        [Header("Escáner y láser")]
        public Button        ScanButton;
        public Button        FireButton;
        public Image         FireCooldown;     // fills back up while the laser recharges
        public RectTransform Reticle;          // follows what's in your sights
        public TMP_Text      ReticleLabel;
        public GameObject    InfoCard;         // "what is that?" answer
        public TMP_Text      InfoTitle;
        public TMP_Text      InfoBody;

        [Header("Exploración libre")]
        public Button     ExitButton;        // only in free flight: back to the lobby

        [Header("Avisos de impacto")]
        public TMP_Text   ImpactLabel;       // "IMPACT · FUEL LEAK −10%" durante 2 s
        public GameObject ImpactBanner;      // plate behind the alert (pops in)
        public Image      ImpactRim;         // its glowing frame, tinted per alert
        public Image      HitFlash;          // whole-screen colour flash
        public UIGlitch[] Glitch;            // display parts that jolt on hits

        [Header("Progreso del viaje")]
        public Image      ProgressBar;       // 0 = salida, 1 = planeta
        public TMP_Text   TimeLabel;         // tiempo restante de misión
        public GameObject PlanetOnScreenMarker; // se oculta la flecha si el planeta está a la vista

        // ── Componentes de la nave ────────────────────────────────────────────
        private FuelSystem      _fuel;
        private BurnoutSystem   _burnout;
        private CollisionSystem _collision;
        private Rigidbody       _rb;
        private ARSpaceManager  _arSpace;

        private float _lowFuelFlash = 0f;
        private float _impactTimer  = 0f;
        private GyroCameraRig _rig;
        private ShipTools     _tools;
        private float         _infoTimer;
        private bool          _scanHintShown;
        const string ScanHintKey = "navigator.hint.scan";

        void OnDestroy()
        {
            Powerup.Collected -= OnPodCollected;
            Loc.Changed -= RefreshGyroLabel;
            ShipTools.Scanned -= OnScanned;
            ShipTools.FiredAt -= OnFiredAt;
            ShipTools.Fired   -= OnFired;
        }

        void RefreshGyroLabel()
        {
            if (GyroLabel != null && _rig != null) GyroLabel.text = Loc.T(_rig.GyroOn ? "hud.gyro.on" : "hud.gyro.off");
        }

        void OnPodCollected(PowerupType type) => Flash("pod." + type, type switch
        {
            PowerupType.Fuel          => UIStyle.Good,
            PowerupType.Boost         => UIStyle.Burnout,
            PowerupType.Shield        => UIStyle.Accent,
            PowerupType.EMP           => new Color(0.8f, 0.4f, 1f),
            _                         => UIStyle.Gold,
        }, 0f);

        void UpdateVignette()
        {
            if (Vignette == null) return;
            bool burn     = _burnout != null && _burnout.BurnoutActive;
            bool critical = _collision != null && _collision.Lives == 1;
            float pulse   = 0.5f + 0.5f * Mathf.Sin(Time.time * (burn ? 10f : 4f));
            Color c = burn ? UIStyle.Burnout : UIStyle.Bad;
            c.a = burn ? 0.35f + 0.25f * pulse : critical ? 0.2f + 0.2f * pulse : 0f;
            Vignette.color = c;
        }

        public void Flash(string key, Color color, float shake, string literal = null)
        {
            if (ImpactLabel != null)
            {
                ImpactLabel.text  = literal ?? Loc.T(key);
                ImpactLabel.color = color;
                ImpactLabel.gameObject.SetActive(true);
            }
            if (ImpactRim != null) ImpactRim.color = new Color(color.r, color.g, color.b, 0.95f);
            if (ImpactBanner != null)
            {
                if (ImpactBanner.activeSelf) UIPopIn.Play(ImpactBanner.transform);   // new alert: bounce again
                else ImpactBanner.SetActive(true);
            }
            _impactTimer = 2f;
            if (shake > 0f)
            {
                _rig?.Shake(shake);
                // The screen flashes the alert's colour and the HUD jolts, both scaled by the hit
                _flash = Mathf.Max(_flash, Mathf.Min(0.55f, 0.2f + shake * 0.3f) * (FxPrefs.Motion ? 1f : 0.35f));
                _flashColor = color;
                if (Glitch != null) foreach (var g in Glitch) if (g != null) g.Hit(Mathf.Min(1f, shake));
            }
        }

        private float _flash;
        private Color _flashColor;
        private float _reticleKick;

        void UpdateHitFlash()
        {
            if (HitFlash == null) return;
            _flash = Mathf.MoveTowards(_flash, 0f, Time.unscaledDeltaTime * 1.6f);
            var c = _flashColor; c.a = _flash;
            HitFlash.color = c;
        }

        // The trigger was pulled: button bounce, reticle kick, a small camera jolt
        void OnFired()
        {
            if (FireButton != null && FireButton.TryGetComponent<UIPress>(out var press)) press.Kick(0.3f);
            _reticleKick = 1f;
            _rig?.Shake(0.06f);
        }
        private float _comboTimer   = 0f;

        void Awake()
        {
            // Se obtienen desde la nave local (buscada por tag)
            var ship = GameObject.FindWithTag("LocalShip");
            if (ship != null)
            {
                _fuel      = ship.GetComponent<FuelSystem>();
                _burnout   = ship.GetComponent<BurnoutSystem>();
                _collision = ship.GetComponent<CollisionSystem>();
                _rb        = ship.GetComponent<Rigidbody>();
            }
            _arSpace = FindObjectOfType<ARSpaceManager>();

            // Suscribir eventos
            _burnout?.OnBurnoutStart.AddListener(OnBurnoutStart);
            _burnout?.OnBurnoutEnd.AddListener(OnBurnoutEnd);
            _burnout?.OnNearMiss.AddListener(OnNearMiss);
            _fuel?.OnLowFuelWarning.AddListener(() =>
            {
                if (LowFuelWarning != null) LowFuelWarning.SetActive(true);
                AudioFX.Play(Sfx.Alert, 0.6f);
            });
            _fuel?.OnFuelEmpty.AddListener(() => { AudioFX.Play(Sfx.Alert); Flash("hud.nofuel", UIStyle.Bad, 0.8f); });
            _burnout?.OnTakedown.AddListener(_ => Flash("hud.takedown", UIStyle.Gold, 0.6f));
            _collision?.OnBlocked.AddListener(() => Flash("hud.hit.shielded", UIStyle.Accent, 0.3f));
            _collision?.OnDamaged.AddListener(sev => Flash(
                sev == CollisionSeverity.Heavy  ? "hud.hit.heavy"  :
                sev == CollisionSeverity.Medium ? "hud.hit.medium" : "hud.hit.light",
                sev == CollisionSeverity.Light ? UIStyle.Text : sev == CollisionSeverity.Medium ? UIStyle.Warn : UIStyle.Bad,
                sev == CollisionSeverity.Heavy ? 1.2f : sev == CollisionSeverity.Medium ? 0.7f : 0.3f));
            _rig = FindObjectOfType<GyroCameraRig>();
            if (GyroButton != null)
            {
                bool available = _rig != null && _rig.GyroAvailable;
                GyroButton.gameObject.SetActive(available);
                GyroButton.onClick.AddListener(() => { _rig.SetGyro(!_rig.GyroOn); RefreshGyroLabel(); });
                RefreshGyroLabel();
                Loc.Changed += RefreshGyroLabel;
            }
            _burnout?.OnBurnoutStart.AddListener(() => AudioFX.Play(Sfx.Burnout));
            _burnout?.OnNearMiss.AddListener(_ => { AudioFX.Play(Sfx.NearMiss, 0.7f); _rig?.Shake(0.15f); });
            _collision?.OnDamaged.AddListener(_ => AudioFX.Play(Sfx.Hit));
            Powerup.Collected += OnPodCollected;
            if (ImpactLabel != null) ImpactLabel.gameObject.SetActive(false);

            var shipGo = GameObject.FindWithTag("LocalShip");
            _tools = shipGo != null ? shipGo.GetComponent<ShipTools>() : null;
            ScanButton?.onClick.AddListener(() => _tools?.Scan());
            FireButton?.onClick.AddListener(() =>
            {
                if (!ShipTools.WeaponsAllowed) { Flash("laser.off", UIStyle.Muted, 0f); return; }
                _tools?.Fire();
            });
            ShipTools.Scanned += OnScanned;
            ShipTools.FiredAt += OnFiredAt;
            ShipTools.Fired   += OnFired;
            if (ImpactBanner != null) ImpactBanner.SetActive(false);
            ExitButton?.onClick.AddListener(() => GameManager.Instance?.ExitFreeFlight());
            if (ExitButton != null) ExitButton.gameObject.SetActive(false);
            if (InfoCard != null) InfoCard.SetActive(false);
        }

        void Update()
        {
            UpdateSpeed();
            UpdateFuel();
            UpdateDistance();
            UpdateBurnoutBar();
            UpdateDamage();
            UpdateLowFuelFlash();
            UpdateNearMissCombo();
            UpdateTime();

            UpdateVignette();
            UpdateTools();

            if (_impactTimer > 0f)
            {
                _impactTimer -= Time.deltaTime;
                if (_impactTimer <= 0f)
                {
                    if (ImpactLabel != null)  ImpactLabel.gameObject.SetActive(false);
                    if (ImpactBanner != null) ImpactBanner.SetActive(false);
                }
            }
            UpdateHitFlash();
        }

        // ── Scanner / laser ───────────────────────────────────────────────────
        void OnScanned(ScanInfo info)
        {
            if (InfoCard == null) return;
            InfoCard.SetActive(true);
            _infoTimer = info.Kind == "none" ? 3f : 8f;
            if (InfoTitle != null) InfoTitle.text = info.Title;
            if (InfoBody != null)
                InfoBody.text = (info.Discovery
                    ? $"<color=#FFD633><b>{(ShipTools.FreeFlight ? Loc.T("scan.logbook") : Loc.F("scan.discovery", ShipTools.DiscoveryPoints))}</b></color>\n"
                    : "") + info.Body;
        }

        // A miss only has the reticle and the laser sound; it must not overwrite a banner that matters
        void OnFiredAt(string what)
        {
            if (what == null) return;
            Flash(null, UIStyle.Burnout, 0.15f, Loc.F("laser.hit", what));
        }

        void UpdateTools()
        {
            if (_infoTimer > 0f)
            {
                _infoTimer -= Time.deltaTime;
                if (_infoTimer <= 0f && InfoCard != null) InfoCard.SetActive(false);
            }
            if (FireCooldown != null && _tools != null) FireCooldown.fillAmount = 1f - _tools.Cooldown01;
            if (FireButton != null) FireButton.interactable = ShipTools.WeaponsAllowed;

            if (Reticle == null || _tools == null) return;

            // First time anything is in the sights: say what the SCAN button is for (once per install)
            if (_tools.Target != null && !_scanHintShown)
            {
                _scanHintShown = true;
                if (PlayerPrefs.GetInt(ScanHintKey, 0) == 0)
                {
                    PlayerPrefs.SetInt(ScanHintKey, 1);
                    PlayerPrefs.Save();
                    Flash("hint.scan", UIStyle.Accent, 0f);
                }
            }
            var cam = Camera.main;
            if (cam == null) return;

            // On the target when there is one; otherwise straight ahead of the ship
            bool locked = _tools.Target != null;
            Vector3 world = locked ? _tools.Target.transform.position
                                   : _tools.transform.position + _tools.transform.forward * 200f;
            Vector3 sp = cam.WorldToScreenPoint(world);
            bool visible = sp.z > 0f;
            if (Reticle.gameObject.activeSelf != visible) Reticle.gameObject.SetActive(visible);
            if (!visible) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)Reticle.parent, sp, null, out var local);
            Reticle.anchoredPosition = Vector2.Lerp(Reticle.anchoredPosition, local, 1f - Mathf.Exp(-18f * Time.deltaTime));
            var img = Reticle.GetComponent<Image>();
            if (img != null) img.color = locked ? new Color(1f, 0.84f, 0.3f, 0.9f) : new Color(1f, 1f, 1f, 0.35f);
            _reticleKick = Mathf.MoveTowards(_reticleKick, 0f, Time.deltaTime * 5f);
            Reticle.localScale = Vector3.one * ((locked ? 1f + 0.08f * Mathf.Sin(Time.time * 8f) : 0.8f) + 0.35f * _reticleKick);
            if (ReticleLabel != null) ReticleLabel.text = locked ? _tools.TargetName : "";
        }

        void UpdateTime()
        {
            var gm = GameManager.Instance;
            if (TimeLabel == null || gm == null) return;
            if (gm.FreeMode)
            {
                TimeLabel.text  = Loc.T("free.time");
                TimeLabel.color = UIStyle.Accent;
                return;
            }
            float left = Mathf.Max(0f, gm.MaxFlightTimeS - gm.FlightTime);
            TimeLabel.text  = $"{(int)left / 60}:{(int)left % 60:00}";
            TimeLabel.color = left < 30f ? UIStyle.Bad : UIStyle.Text;
        }

        // ── Actualizar ────────────────────────────────────────────────────────

        void UpdateSpeed()
        {
            if (_rb == null || SpeedLabel == null) return;
            float speed = _rb.linearVelocity.magnitude;
            // The text only changes when the shown number does (no string rebuild and mesh update per frame)
            int shown = Mathf.RoundToInt(speed);
            if (shown != _lastSpeed) { _lastSpeed = shown; SpeedLabel.text = shown + " m/s"; }
            if (SpeedGaugeFill != null)
                SpeedGaugeFill.fillAmount = Mathf.Clamp01(speed / 200f);
        }

        void UpdateFuel()
        {
            if (_fuel == null) return;
            float f = _fuel.FuelFraction;
            if (FuelBar   != null) FuelBar.fillAmount = f;
            int pct = Mathf.RoundToInt(f * 100f);
            if (FuelLabel != null && pct != _lastFuelPct) { _lastFuelPct = pct; FuelLabel.text = pct + " %"; }
        }

        float _freeRefresh;

        // Free flight: nearest world, explorer's logbook, arrow to the next thing to discover
        void UpdateFree(GameManager gm)
        {
            var free = gm.Free;
            if (free == null || _rb == null) return;
            if (ExitButton != null && !ExitButton.gameObject.activeSelf) ExitButton.gameObject.SetActive(true);
            if (RacePositionLabel != null) RacePositionLabel.text = "";

            _freeRefresh -= Time.deltaTime;
            if (_freeRefresh <= 0f)
            {
                _freeRefresh = 0.5f;
                var (id, dist) = free.Nearest(_rb.position);
                if (DistanceLabel != null)
                    DistanceLabel.text = Loc.F("free.near", id == "sun" ? Loc.T("scan.sun.t") : Loc.Planet(id));
                int found = ShipTools.FoundNow(), total = Mathf.Max(1, ShipTools.DiscoverableNow());
                if (CommDelayLabel != null) CommDelayLabel.text = Loc.F("free.logbook", found, total);
                if (ProgressBar != null) ProgressBar.fillAmount = found / (float)total;
                _freeTarget = free.NextToDiscover(_rb.position);
            }
            PointArrow(_freeTarget != null ? _freeTarget.position : (Vector3?)null);
        }

        Transform _freeTarget;

        void PointArrow(Vector3? target)
        {
            var cam = Camera.main;
            if (ArrowToPlanet == null || cam == null) return;
            if (target == null) { ArrowToPlanet.gameObject.SetActive(false); return; }
            Vector3 sp = cam.WorldToScreenPoint(target.Value);
            bool behind = sp.z < 0f;
            Vector2 d = (Vector2)sp - new Vector2(Screen.width / 2f, Screen.height / 2f);
            if (behind) d = -d;
            bool onScreen = !behind && sp.x > 0 && sp.x < Screen.width && sp.y > 0 && sp.y < Screen.height;
            ArrowToPlanet.gameObject.SetActive(!onScreen);
            if (!onScreen)
                ArrowToPlanet.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f);
        }

        void UpdateDistance()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.FreeMode) { UpdateFree(gm); return; }
            if (ExitButton != null && ExitButton.gameObject.activeSelf) ExitButton.gameObject.SetActive(false);
            if (_arSpace == null) return;
            if (DistanceLabel != null)
            {
                float au = _arSpace.DistanceToTarget;
                DistanceLabel.text = au >= 1f
                    ? $"{au:F2} AU"
                    : $"{au * 149.6f:F0} M km";
            }

            if (ProgressBar != null) ProgressBar.fillAmount = _arSpace.Progress01;
            UpdateCalendar(gm, _arSpace.Progress01);

            // Flecha apunta al planeta; cuando está detrás, WorldToScreenPoint se invierte
            var cam = Camera.main;
            if (ArrowToPlanet != null && cam != null)
            {
                Vector3 sp = cam.WorldToScreenPoint(_arSpace.TargetPosition);
                bool behind = sp.z < 0f;
                Vector2 center = new(Screen.width / 2f, Screen.height / 2f);
                Vector2 d = (Vector2)sp - center;
                if (behind) d = -d;   // projection mirrors through the center when behind
                bool onScreen = !behind && sp.x > 0 && sp.x < Screen.width && sp.y > 0 && sp.y < Screen.height;

                ArrowToPlanet.gameObject.SetActive(!onScreen);
                if (PlanetOnScreenMarker != null) PlanetOnScreenMarker.SetActive(onScreen);
                if (!onScreen)
                {
                    float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                    ArrowToPlanet.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
                }
            }
        }

        void UpdateBurnoutBar()
        {
            if (_burnout == null || BurnoutBar == null) return;
            BurnoutBar.fillAmount = _burnout.BurnoutBar;

            // Color: blanco cuando Burnout activo, cyan→naranja según nivel
            BurnoutBar.color = _burnout.BurnoutActive
                ? Color.white
                : Color.Lerp(UIStyle.Accent, UIStyle.Burnout, _burnout.BurnoutBar);
        }

        void UpdateDamage()
        {
            if (_collision == null) return;
            var state = _collision.DamageLevel;
            if (DamageIndicatorDamaged != null) DamageIndicatorDamaged.SetActive(state == DamageState.Damaged);
            if (DamageIndicatorCritical != null) DamageIndicatorCritical.SetActive(state == DamageState.Critical);

            if (LivesLabel != null && _collision.Lives != _lastLives)
            {
                _lastLives = _collision.Lives;
                LivesLabel.text  = Loc.F("hud.hull", _collision.Lives);
                LivesLabel.color = _collision.Lives >= 3 ? UIStyle.Text
                                 : _collision.Lives == 2 ? UIStyle.Warn : UIStyle.Bad;
            }
        }

        void UpdateLowFuelFlash()
        {
            if (_fuel == null || LowFuelWarning == null) return;
            // After a refuel the warning has to go away again
            if (!_fuel.IsLow) { if (LowFuelWarning.activeSelf) LowFuelWarning.SetActive(false); return; }
            if (_lowFuelLabel == null) _lowFuelLabel = LowFuelWarning.GetComponent<TMP_Text>();
            // Set the warning text only when it changes between "low" and "empty"
            int state = (_fuel.IsEmpty ? 1 : 0) + (Loc.Current == Lang.Spanish ? 2 : 0);   // also follows the language
            if (_lowFuelLabel != null && state != _lowFuelState)
            {
                _lowFuelState = state;
                _lowFuelLabel.text = Loc.T(_fuel.IsEmpty ? "hud.nofuel" : "hud.lowfuel");
            }
            if (_fuel.IsEmpty) { LowFuelWarning.SetActive(true); return; }
            _lowFuelFlash += Time.deltaTime * 3.5f;
            LowFuelWarning.SetActive(Mathf.Sin(_lowFuelFlash) > 0f);
        }

        void UpdateNearMissCombo()
        {
            if (_burnout == null || NearMissComboObj == null) return;
            _comboTimer -= Time.deltaTime;
            NearMissComboObj.SetActive(_comboTimer > 0f && _burnout.NearMissCombo >= 3);
        }

        // ── Eventos ───────────────────────────────────────────────────────────

        void OnBurnoutStart()
        {
            if (BurnoutModeIndicator != null) BurnoutModeIndicator.SetActive(true);
        }

        void OnBurnoutEnd()
        {
            if (BurnoutModeIndicator != null) BurnoutModeIndicator.SetActive(false);
        }

        void OnNearMiss(int combo)
        {
            _comboTimer = 2.5f;
            if (NearMissComboLabel != null)
            {
                string label = combo >= 10 ? "UNTOUCHABLE!" :
                               combo >= 5  ? $"x3 NEAR MISS!" :
                               combo >= 3  ? $"x{combo} NEAR MISS" :
                               "NEAR MISS";
                NearMissComboLabel.text = label;
            }
        }

        // ── Setters externos ──────────────────────────────────────────────────

        private string _signal;
        // Last values shown, so the text is only rebuilt when they change (see the Update* methods)
        int _lastSpeed = int.MinValue, _lastFuelPct = int.MinValue, _lastLives = int.MinValue;
        int _calDays = int.MinValue;
        string _calSignal;
        TMP_Text _lowFuelLabel;
        int _lowFuelState = -1;   // -1: never written (the scene text is English only)

        public void SetCommDelay(string delay)
        {
            _signal = delay;
            if (CommDelayLabel != null)
                CommDelayLabel.text = Loc.F("hud.signal", delay);
        }

        // Mission calendar: the real date along the Lambert transfer (launch → arrival) as you fly
        void UpdateCalendar(GameManager gm, float progress)
        {
            var m = gm != null ? gm.Mission : null;
            if (CommDelayLabel == null || m == null || m.TransferDays <= 0f) return;
            // The text only changes when the day shown changes (this runs every frame in flight)
            int days = Mathf.RoundToInt(m.TransferDays * Mathf.Clamp01(progress));
            if (days == _calDays && _signal == _calSignal) return;
            if (!System.DateTime.TryParse(m.LaunchDateIso, System.Globalization.CultureInfo.InvariantCulture,
                                          System.Globalization.DateTimeStyles.None, out var launch)) return;
            _calDays = days; _calSignal = _signal;
            var today = launch.AddDays(m.TransferDays * Mathf.Clamp01(progress));
            string date = today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            CommDelayLabel.text = (_signal != null ? Loc.F("hud.signal", _signal) + "   ·   " : "") +
                                  Loc.F("hud.date", date, days);
        }

        public void SetRacePosition(int pos)
        {
            if (RacePositionLabel != null)
                RacePositionLabel.text = $"#{pos}";
        }
    }
}
