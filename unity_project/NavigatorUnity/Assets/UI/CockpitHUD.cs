// Navigator 2.0 â€” CockpitHUD.cs
// HUD en vuelo: velocidad, combustible, distancia, barra Burnout, estado nave
// Ported from Dart: navigator/lib/ui/hud_overlay.dart
// Guacalita Inc. â€” SAL / Claude

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
        // â”€â”€ Referencias Inspector â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        [Header("Velocidad")]
        public TMP_Text   SpeedLabel;       // "1,240 m/s"
        public Image      SpeedGaugeFill;   // gauge circular

        [Header("Combustible")]
        public Image      FuelBar;          // barra horizontal
        public TMP_Text   FuelLabel;        // "72%"
        public GameObject LowFuelWarning;   // "âš  COMBUSTIBLE BAJO" â€” parpadea

        [Header("Distancia al planeta")]
        public TMP_Text   DistanceLabel;    // "12.4 AU"
        public Image      ArrowToPlanet;    // flecha hacia el planeta en AR

        [Header("Barra Burnout")]
        public Image      BurnoutBar;               // barra de boost
        public GameObject BurnoutModeIndicator;     // "BURNOUT!" cuando activo
        public TMP_Text   NearMissComboLabel;        // "x3 NEAR MISS!"
        public GameObject NearMissComboObj;

        [Header("Estado nave (daÃ±o)")]
        public GameObject DamageIndicatorDamaged;   // panel naranja
        public GameObject DamageIndicatorCritical;  // panel rojo parpadeante
        public TMP_Text   LivesLabel;               // "â™¥â™¥â™¥"

        [Header("Retardo comunicaciÃ³n")]
        public TMP_Text   CommDelayLabel;   // "Retraso seÃ±al: 22 min"

        [Header("PosiciÃ³n en carrera")]
        public TMP_Text   RacePositionLabel; // "#1" "#2"

        // â”€â”€ Componentes de la nave â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private FuelSystem      _fuel;
        private BurnoutSystem   _burnout;
        private CollisionSystem _collision;
        private Rigidbody       _rb;
        private ARSpaceManager  _arSpace;

        private float _lowFuelFlash = 0f;
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
        }

        // â”€â”€ Actualizar â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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
            if (_arSpace == null || DistanceLabel == null) return;
            float au = _arSpace.DistanceToTarget;
            DistanceLabel.text = au >= 1f
                ? $"{au:F2} AU"
                : $"{au * 149.6f:F0} M km";

            // Flecha apunta al planeta
            if (ArrowToPlanet != null)
            {
                Vector3 screenDir = Camera.main.WorldToScreenPoint(
                    Camera.main.transform.position + _arSpace.DirectionToTarget * 100f);
                Vector3 center    = new(Screen.width / 2f, Screen.height / 2f, 0f);
                float   angle     = Mathf.Atan2(screenDir.y - center.y, screenDir.x - center.x) * Mathf.Rad2Deg;
                ArrowToPlanet.rectTransform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
            }
        }

        void UpdateBurnoutBar()
        {
            if (_burnout == null || BurnoutBar == null) return;
            BurnoutBar.fillAmount = _burnout.BurnoutBar;

            // Color: blanco cuando Burnout activo, cyanâ†’naranja segÃºn nivel
            BurnoutBar.color = _burnout.BurnoutActive
                ? Color.white
                : Color.Lerp(Color.cyan, new Color(1f, 0.5f, 0f), _burnout.BurnoutBar);
        }

        void UpdateDamage()
        {
            if (_collision == null) return;
            var state = _collision.DamageLevel;
            if (DamageIndicatorDamaged != null) DamageIndicatorDamaged.SetActive(state == DamageState.Damaged);
            if (DamageIndicatorCritical != null) DamageIndicatorCritical.SetActive(state == DamageState.Critical);

            if (LivesLabel != null)
            {
                string hearts = "";
                for (int i = 0; i < _collision.Lives; i++) hearts += "â™¥";
                LivesLabel.text = hearts;
            }
        }

        void UpdateLowFuelFlash()
        {
            if (_fuel == null || LowFuelWarning == null) return;
            if (!_fuel.IsLow) return;
            _lowFuelFlash += Time.deltaTime * 3.5f;
            LowFuelWarning.SetActive(Mathf.Sin(_lowFuelFlash) > 0f);
        }

        void UpdateNearMissCombo()
        {
            if (_burnout == null || NearMissComboObj == null) return;
            _comboTimer -= Time.deltaTime;
            NearMissComboObj.SetActive(_comboTimer > 0f && _burnout.NearMissCombo >= 3);
        }

        // â”€â”€ Eventos â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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

        // â”€â”€ Setters externos â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        public void SetCommDelay(string delay)
        {
            if (CommDelayLabel != null)
                CommDelayLabel.text = $"SeÃ±al: {delay}";
        }

        public void SetRacePosition(int pos)
        {
            if (RacePositionLabel != null)
                RacePositionLabel.text = $"#{pos}";
        }
    }
}
