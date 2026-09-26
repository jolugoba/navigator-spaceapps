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

        [Header("Avisos de impacto")]
        public TMP_Text   ImpactLabel;       // "IMPACT · FUEL LEAK −10%" durante 2 s

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

        void OnDestroy() => Powerup.Collected -= OnPodCollected;

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
            _impactTimer = 2f;
            if (shake > 0f && _rig != null) _rig.Shake(shake);
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
            _fuel?.OnLowFuelWarning.AddListener(() => { if (LowFuelWarning != null) LowFuelWarning.SetActive(true); });
            _burnout?.OnTakedown.AddListener(_ => Flash("hud.takedown", UIStyle.Gold, 0.6f));
            _collision?.OnBlocked.AddListener(() => Flash("hud.hit.shielded", UIStyle.Accent, 0.3f));
            _collision?.OnDamaged.AddListener(sev => Flash(
                sev == CollisionSeverity.Heavy  ? "hud.hit.heavy"  :
                sev == CollisionSeverity.Medium ? "hud.hit.medium" : "hud.hit.light",
                sev == CollisionSeverity.Light ? UIStyle.Text : sev == CollisionSeverity.Medium ? UIStyle.Warn : UIStyle.Bad,
                sev == CollisionSeverity.Heavy ? 1.2f : sev == CollisionSeverity.Medium ? 0.7f : 0.3f));
            _rig = FindObjectOfType<GyroCameraRig>();
            _burnout?.OnBurnoutStart.AddListener(() => AudioFX.Play(Sfx.Burnout));
            _burnout?.OnNearMiss.AddListener(_ => { AudioFX.Play(Sfx.NearMiss, 0.7f); _rig?.Shake(0.15f); });
            _collision?.OnDamaged.AddListener(_ => AudioFX.Play(Sfx.Hit));
            Powerup.Collected += OnPodCollected;
            if (ImpactLabel != null) ImpactLabel.gameObject.SetActive(false);
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

            if (_impactTimer > 0f)
            {
                _impactTimer -= Time.deltaTime;
                if (_impactTimer <= 0f && ImpactLabel != null) ImpactLabel.gameObject.SetActive(false);
            }
        }

        void UpdateTime()
        {
            var gm = GameManager.Instance;
            if (TimeLabel == null || gm == null) return;
            float left = Mathf.Max(0f, gm.MaxFlightTimeS - gm.FlightTime);
            TimeLabel.text  = $"{(int)left / 60}:{(int)left % 60:00}";
            TimeLabel.color = left < 30f ? UIStyle.Bad : UIStyle.Text;
        }

        // ── Actualizar ────────────────────────────────────────────────────────

        void UpdateSpeed()
        {
            if (_rb == null || SpeedLabel == null) return;
            float speed = _rb.linearVelocity.magnitude;
            SpeedLabel.text = $"{speed:F0} m/s";
            if (SpeedGaugeFill != null)
                SpeedGaugeFill.fillAmount = Mathf.Clamp01(speed / 200f);
        }

        void UpdateFuel()
        {
            if (_fuel == null) return;
            float f = _fuel.FuelFraction;
            if (FuelBar   != null) FuelBar.fillAmount = f;
            if (FuelLabel != null) FuelLabel.text     = $"{f:P0}";
        }

        void UpdateDistance()
        {
            if (_arSpace == null) return;
            if (DistanceLabel != null)
            {
                float au = _arSpace.DistanceToTarget;
                DistanceLabel.text = au >= 1f
                    ? $"{au:F2} AU"
                    : $"{au * 149.6f:F0} M km";
            }

            if (ProgressBar != null) ProgressBar.fillAmount = _arSpace.Progress01;

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

            if (LivesLabel != null)
            {
                LivesLabel.text  = Loc.F("hud.hull", _collision.Lives);
                LivesLabel.color = _collision.Lives >= 3 ? UIStyle.Text
                                 : _collision.Lives == 2 ? UIStyle.Warn : UIStyle.Bad;
            }
        }

        void UpdateLowFuelFlash()
        {
            if (_fuel == null || LowFuelWarning == null) return;
            if (!_fuel.IsLow) return;
            var label = LowFuelWarning.GetComponent<TMP_Text>();
            if (label != null) label.text = Loc.T(_fuel.IsEmpty ? "hud.nofuel" : "hud.lowfuel");
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

        public void SetCommDelay(string delay)
        {
            if (CommDelayLabel != null)
                CommDelayLabel.text = Loc.F("hud.signal", delay);
        }

        public void SetRacePosition(int pos)
        {
            if (RacePositionLabel != null)
                RacePositionLabel.text = $"#{pos}";
        }
    }
}
