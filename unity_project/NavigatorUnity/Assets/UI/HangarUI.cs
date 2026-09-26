// Navigator 2.0 — HangarUI.cs
// Lobby → SHIP: pick the hull you fly. A small studio far below the scene renders the ship
// turning slowly into a texture shown on the screen. The pick is saved and sent to rivals.
// Guacalita Inc. — SAL / Claude

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Navigator.Core;
using Navigator.Game;

namespace Navigator.UI
{
    public class HangarUI : MonoBehaviour
    {
        public GameObject Panel;
        public Button     OpenButton;     // lobby "SHIP"
        public Button     PrevButton;
        public Button     NextButton;
        public Button     ChooseButton;
        public Button     BackButton;
        public RawImage   Preview;
        public TMP_Text   NameLabel;
        public TMP_Text   DescLabel;
        public TMP_Text   CounterLabel;

        static readonly Vector3 StudioPos = new(0f, -20000f, 0f);   // far from everything in the scene

        private int          _index;
        private Camera       _cam;
        private Transform    _turntable;
        private RenderTexture _rt;

        void Awake()
        {
            OpenButton?.onClick.AddListener(Open);
            BackButton?.onClick.AddListener(Close);
            PrevButton?.onClick.AddListener(() => Show(_index - 1));
            NextButton?.onClick.AddListener(() => Show(_index + 1));
            ChooseButton?.onClick.AddListener(Choose);
            if (Panel != null) Panel.SetActive(false);
        }

        void OnEnable()  => Loc.Changed += RefreshTexts;
        void OnDisable() => Loc.Changed -= RefreshTexts;

        void OnDestroy()
        {
            if (_rt != null) _rt.Release();
            if (_turntable != null) Destroy(_turntable.parent.gameObject);
        }

        void Open()
        {
            var catalog = ShipCatalog.Instance;
            if (catalog == null || catalog.Count == 0) return;
            BuildStudio();
            if (Panel != null) Panel.SetActive(true);
            _cam.enabled = true;
            Show(ShipCatalog.Selected);
        }

        void Close()
        {
            if (Panel != null) Panel.SetActive(false);
            if (_cam != null) _cam.enabled = false;
        }

        void Choose()
        {
            ShipCatalog.Selected = _index;
            AudioFX.Play(Sfx.Go, 0.6f);
            Close();
        }

        void Show(int index)
        {
            var catalog = ShipCatalog.Instance;
            if (catalog == null || catalog.Count == 0) return;
            _index = ((index % catalog.Count) + catalog.Count) % catalog.Count;

            // Rebuild the model on the turntable
            for (int i = _turntable.childCount - 1; i >= 0; i--) Destroy(_turntable.GetChild(i).gameObject);
            catalog.Build(_index, _turntable);
            RefreshTexts();
        }

        void RefreshTexts()
        {
            var catalog = ShipCatalog.Instance;
            if (catalog == null) return;
            int n = _index + 1;
            if (NameLabel != null)    NameLabel.text    = $"NV-{n:00} · {Loc.T($"ship.{n}.name")}";
            if (DescLabel != null)    DescLabel.text    = Loc.T($"ship.{n}.desc") + "\n<size=80%><color=#8FA3C7>" + Loc.T("hangar.note") + "</color></size>";
            if (CounterLabel != null) CounterLabel.text = $"{n} / {catalog.Count}" + (_index == ShipCatalog.Selected ? "  ·  " + Loc.T("hangar.current") : "");
        }

        void Update()
        {
            if (Panel == null || !Panel.activeSelf || _turntable == null) return;
            _turntable.Rotate(0f, 35f * Time.unscaledDeltaTime, 0f, Space.World);
        }

        // Camera + light + turntable, rendering into the Preview image only
        void BuildStudio()
        {
            if (_cam != null) return;
            var studio = new GameObject("HangarStudio").transform;
            studio.position = StudioPos;

            _turntable = new GameObject("Turntable").transform;
            _turntable.SetParent(studio, false);
            _turntable.localRotation = Quaternion.Euler(0f, 30f, 0f);

            _rt = new RenderTexture(768, 768, 16, RenderTextureFormat.ARGB32) { name = "HangarPreview" };
            _cam = new GameObject("HangarCamera").AddComponent<Camera>();
            _cam.transform.SetParent(studio, false);
            _cam.transform.localPosition = new Vector3(0f, 1.6f, -5.2f);
            _cam.transform.LookAt(studio.position);
            _cam.clearFlags      = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0.03f, 0.05f, 0.12f, 1f);
            _cam.fieldOfView     = 40f;
            _cam.nearClipPlane   = 0.1f;
            _cam.farClipPlane    = 40f;   // sees only the studio
            _cam.targetTexture   = _rt;
            _cam.enabled         = false;

            var key = new GameObject("KeyLight").AddComponent<Light>();
            key.transform.SetParent(studio, false);
            key.type  = LightType.Point;
            key.range = 20f;
            key.intensity = 3f;
            key.transform.localPosition = new Vector3(2.5f, 3f, -3f);

            if (Preview != null) Preview.texture = _rt;
        }
    }
}
