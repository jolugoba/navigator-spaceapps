// Navigator 2.0 — ShipCustomizer.cs
// Runtime color customization via MaterialPropertyBlock — 4 UV zones
// Colors sync to other players via P2P (ShipSyncService)
// From NAVIGATOR_MESHY_ASSETS.md — 4 separate materials required in Meshy model
// Guacalita Inc. — SAL / Claude

using UnityEngine;

namespace Navigator.Game
{
    [System.Serializable]
    public class ShipColors
    {
        public Color Primary   = new Color(0.9f, 0.9f, 0.9f); // fuselage
        public Color Secondary = new Color(0f, 0.9f, 1f);     // wings/details
        public Color Engine    = new Color(0f, 0.9f, 1f);     // nozzle glow
        public string ShipName = "NAVIGATOR-01";
    }

    // Meshy model must have 4 named materials:
    //   Material index 0 → fuselage
    //   Material index 1 → wings and details
    //   Material index 2 → engine nozzles (emission)
    //   Material index 3 → cockpit glass (always dark translucent)
    public class ShipCustomizer : MonoBehaviour
    {
        [Header("Renderer with 4 materials from Meshy")]
        public Renderer ShipRenderer;

        [Header("Current colors")]
        public ShipColors Colors = new();

        private static readonly int _colorId    = Shader.PropertyToID("_BaseColor");
        private static readonly int _emissionId = Shader.PropertyToID("_EmissionColor");

        private MaterialPropertyBlock _block;

        void Awake() => _block = new MaterialPropertyBlock();

        public void Apply(ShipColors colors)
        {
            Colors = colors;
            ApplyToRenderer();
        }

        public void SetPrimary(Color c)   { Colors.Primary   = c; ApplyToRenderer(); }
        public void SetSecondary(Color c) { Colors.Secondary = c; ApplyToRenderer(); }
        public void SetEngine(Color c)    { Colors.Engine    = c; ApplyToRenderer(); }

        private void ApplyToRenderer()
        {
            if (ShipRenderer == null) return;

            // Material 0 — fuselage
            ShipRenderer.GetPropertyBlock(_block, 0);
            _block.SetColor(_colorId, Colors.Primary);
            ShipRenderer.SetPropertyBlock(_block, 0);

            // Material 1 — wings/details
            ShipRenderer.GetPropertyBlock(_block, 1);
            _block.SetColor(_colorId, Colors.Secondary);
            ShipRenderer.SetPropertyBlock(_block, 1);

            // Material 2 — engine nozzles (emission)
            ShipRenderer.GetPropertyBlock(_block, 2);
            _block.SetColor(_colorId,    Colors.Engine);
            _block.SetColor(_emissionId, Colors.Engine * 2.5f); // HDR glow
            ShipRenderer.SetPropertyBlock(_block, 2);

            // Material 3 — cockpit glass stays dark translucent (no change)
        }

        // Serialize to JSON for P2P sync
        public string ToJson() => JsonUtility.ToJson(Colors);

        public void FromJson(string json)
        {
            var c = JsonUtility.FromJson<ShipColors>(json);
            if (c != null) Apply(c);
        }
    }
}
