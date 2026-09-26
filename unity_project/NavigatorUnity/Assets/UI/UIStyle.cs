// Navigator 2.0 — UIStyle.cs
// Paleta única del juego: cabina oscura con acentos cian, legible sobre la cámara AR
// Guacalita Inc. — SAL / Claude

using UnityEngine;

namespace Navigator.UI
{
    public static class UIStyle
    {
        public static readonly Color Backdrop   = new(0.02f, 0.04f, 0.09f, 0.94f);
        public static readonly Color Card       = new(0.07f, 0.10f, 0.18f, 0.96f);
        public static readonly Color HudCard    = new(0.02f, 0.05f, 0.11f, 0.62f);
        public static readonly Color ButtonIdle = new(0.12f, 0.16f, 0.27f, 1f);
        public static readonly Color Accent     = new(0.20f, 0.80f, 1.00f, 1f);
        public static readonly Color AccentDim  = new(0.20f, 0.80f, 1.00f, 0.35f);
        public static readonly Color Text       = new(0.90f, 0.94f, 1.00f, 1f);
        public static readonly Color Muted      = new(0.56f, 0.64f, 0.78f, 1f);
        public static readonly Color Ink        = new(0.02f, 0.05f, 0.10f, 1f);
        public static readonly Color Good       = new(0.32f, 0.92f, 0.55f, 1f);
        public static readonly Color Bad        = new(1.00f, 0.36f, 0.36f, 1f);
        public static readonly Color Warn       = new(1.00f, 0.72f, 0.22f, 1f);
        public static readonly Color Gold       = new(1.00f, 0.84f, 0.20f, 1f);
        public static readonly Color Burnout    = new(1.00f, 0.50f, 0.10f, 1f);
        public static readonly Color BarTrack   = new(1f, 1f, 1f, 0.10f);
    }
}
