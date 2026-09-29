// Navigator 2.0 — WorldAssets.cs
// The models and materials the worlds use. Setup Scene fills the slots (materials are created
// as assets so web builds keep their shaders; models come from Models/Vehicles and Models/Props
// by their prefix). Every slot is optional: without a model the world uses a simple shape with
// the right colour, so it always plays.
// Guacalita Inc. — SAL / Claude

using System;
using UnityEngine;

namespace Navigator.Worlds
{
    [Serializable]
    public class WorldAssets
    {
        [Header("Materials (created by Setup Scene)")]
        public Material TerrainMaterial;   // URP Lit
        public Material PropMaterial;      // URP Lit, tinted per use
        public Material DustMaterial;      // particles, alpha blended
        public Material GlowMaterial;      // particles, additive (flames, beacons)

        [Header("Vehicles (Models/Vehicles, prefix VH-)")]
        public GameObject Lander;          // VH-01 Mercury, Triton
        public GameObject ThermalRover;    // VH-02 Mercury
        public GameObject EntryCapsule;    // VH-03 Mars, Venus, Titan
        public GameObject VenusLander;     // VH-04
        public GameObject SkyCrane;        // VH-05 Mars
        public GameObject AtmoProbe;       // VH-06 Jupiter, Uranus
        public GameObject Dragonfly;       // VH-07 Titan
        public GameObject UranusOrbiter;   // VH-08
        public GameObject Parachute;       // VH-09
        public GameObject Hopper;          // VH-10 Triton
        public GameObject Rover;           // OB-18 Perseverance (already in the project)

        [Header("Props (Models/Props, prefix PR-)")]
        public GameObject IceChunk;        // PR-01
        public GameObject MercuryBoulder;  // PR-02
        public GameObject VenusSlab;       // PR-03
        public GameObject MarsLayeredRock; // PR-04
        public GameObject SampleTube;      // PR-05
        public GameObject MarsBoulder;     // PR-06
        public GameObject TitanPebbles;    // PR-07
        public GameObject HeatShield;      // PR-08
        public GameObject GeyserVent;      // PR-09
        public GameObject TritonRock;      // PR-10
        public GameObject Beacon;          // OB-03 (objective markers)

        // Planet and moon models for the sky (from ARSpaceManager, set at runtime)
        [NonSerialized] public Func<string, GameObject> SkyBodyPrefab;

        public GameObject RockFor(string body) => body switch
        {
            "mercury" => MercuryBoulder,
            "venus"   => VenusSlab,
            "mars"    => MarsBoulder,
            "titan"   => TitanPebbles,
            "triton"  => TritonRock,
            _         => null,
        };

        public Material NewMaterial(Material template, Color color, float smoothness)
        {
            Material m = template != null ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            m.color = color;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            return m;
        }

        // A model if there is one, else a primitive of that colour; fitted to `size` metres
        public GameObject Spawn(GameObject model, PrimitiveType fallback, Vector3 fallbackScale, Color color,
                                float size, Transform parent, bool alignLong = false)
        {
            var root = new GameObject(model != null ? model.name : fallback.ToString());
            root.transform.SetParent(parent, false);
            if (model != null)
            {
                var m = UnityEngine.Object.Instantiate(model, root.transform);
                m.transform.localPosition = Vector3.zero;
                if (alignLong) Navigator.Core.ModelFit.AlignLongAxisToForward(m);
                Navigator.Core.ModelFit.FitToSize(m, size);
            }
            else
            {
                var mat = NewMaterial(PropMaterial, color, 0.35f);
                var p = Primitive(fallback, root.transform, mat);
                p.transform.localScale = fallbackScale;
            }
            return root;
        }

        public static GameObject Primitive(PrimitiveType type, Transform parent, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            var col = go.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.Destroy(col);   // worlds don't use physics colliders
            go.transform.SetParent(parent, false);
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        // Flat square on the XZ plane, facing up, white vertex colour
        public static Mesh QuadMesh(float size)
        {
            float h = size / 2f;
            var m = new Mesh { name = "WorldQuad" };
            m.vertices  = new[] { new Vector3(-h, 0, -h), new Vector3(h, 0, -h), new Vector3(-h, 0, h), new Vector3(h, 0, h) };
            m.uv        = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            m.colors    = new[] { Color.white, Color.white, Color.white, Color.white };
            m.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        // Soft blotchy noise for cloud layers
        public static Texture2D CloudTexture(int seed)
        {
            const int R = 128;
            var t = new Texture2D(R, R, TextureFormat.RGBA32, true) { name = "Cloud", wrapMode = TextureWrapMode.Repeat };
            var px = new Color[R * R];
            float o = (seed & 1023) * 0.37f;
            for (int j = 0; j < R; j++)
            for (int i = 0; i < R; i++)
            {
                // Tileable: sample noise on a torus
                float a = i / (float)R * Mathf.PI * 2f, b = j / (float)R * Mathf.PI * 2f;
                float n = Mathf.PerlinNoise(Mathf.Cos(a) * 1.5f + o, Mathf.Sin(a) * 1.5f + Mathf.Cos(b) * 1.5f)
                        * 0.65f + Mathf.PerlinNoise(Mathf.Sin(b) * 3f + o, Mathf.Cos(a + b) * 3f) * 0.35f;
                float alpha = Mathf.SmoothStep(0.35f, 0.75f, n);
                px[j * R + i] = new Color(1f, 1f, 1f, alpha);
            }
            t.SetPixels(px);
            t.Apply(true);
            return t;
        }
    }
}
