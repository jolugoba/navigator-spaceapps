// Navigator 2.0 — WorldTerrain.cs
// The ground of a world, generated from its profile: rolling relief, craters with rims, dune
// ridges, Venus' broken lava plates, flat landing pads, lake basins. Vehicles don't use physics
// colliders: they ask Height()/Normal() here, which works the same in every build (web too).
// Colour comes from a painted texture (base colours, slopes, crater rims, zones like ice or sand).
// Guacalita Inc. — SAL / Claude

using System.Collections.Generic;
using UnityEngine;

namespace Navigator.Worlds
{
    public class WorldTerrain
    {
        public const float Size = 480f;     // metres per side
        public const int   N    = 121;      // vertices per side
        const int MapRes = 256;             // colour texture

        public Vector3 Center { get; private set; }
        public float   WaterLevel = float.NegativeInfinity;   // Titan's lake surface (world y) — only inside the lake
        public bool    HasLake => _hasLake;
        public Vector3 LakeCenter => new(Center.x + _lake.x, WaterLevel, Center.z + _lake.y);
        public float   LakeRadius => _lakeR * 1.3f;              // the basin reaches this far

        readonly float[,] _h = new float[N, N];
        float Step => Size / (N - 1);
        WorldProfile _p;

        struct Crater { public Vector2 C; public float R, Depth; }
        struct Flat   { public Vector2 C; public float R; public float Level; public bool Fixed; }
        struct Paint  { public Vector2 C; public float R; public Color Col; public float Strength; }

        readonly List<Crater> _craters = new();
        readonly List<Flat>   _flats   = new();
        readonly List<Flat>   _basins  = new();
        readonly List<Paint>  _paints  = new();
        public readonly List<Vector3> Rocks = new();    // (x, z, radius) in world space, for collisions

        // ── Shape the world before Generate() ────────────────────────────────
        // Positions are local: (0,0) is the centre of the map, in metres.
        public void AddCrater(Vector2 at, float radius, float depth) => _craters.Add(new Crater { C = at, R = radius, Depth = depth });
        public void AddFlat(Vector2 at, float radius) => _flats.Add(new Flat { C = at, R = radius });
        public void AddBasin(Vector2 at, float radius, float depth) => _basins.Add(new Flat { C = at, R = radius, Level = depth });
        public void AddPaint(Vector2 at, float radius, Color c, float strength = 1f) => _paints.Add(new Paint { C = at, R = radius, Col = c, Strength = strength });

        // A basin that fills with liquid (Titan's methane lakes): the surface settles after Generate
        public void AddLake(Vector2 at, float radius, float depth)
        {
            AddBasin(at, radius, depth);
            _lake = at; _lakeR = radius; _lakeDepth = depth; _hasLake = true;
        }
        Vector2 _lake; float _lakeR, _lakeDepth; bool _hasLake;

        // Keep rocks off this circle (landing sites, samples, geysers…)
        public void AddClear(Vector2 at, float radius) => _clear.Add(new Flat { C = at, R = radius });
        readonly List<Flat> _clear = new();

        public void Generate(WorldProfile p, Vector3 center)
        {
            _p = p;
            Center = center;
            var rnd = new System.Random(p.Seed);
            for (int i = 0; i < p.Craters; i++)
            {
                var c = new Vector2((float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f) * Size * 0.95f;
                float r = Mathf.Lerp(6f, 32f, Mathf.Pow((float)rnd.NextDouble(), 2f));
                _craters.Add(new Crater { C = c, R = r, Depth = r * 0.22f });
            }
            // Flat pads take the natural height at their centre
            for (int i = 0; i < _flats.Count; i++)
            {
                var f = _flats[i];
                f.Level = RawHeight(f.C.x, f.C.y, false);
                _flats[i] = f;
            }
            for (int j = 0; j < N; j++)
            for (int i = 0; i < N; i++)
                _h[i, j] = ShapedHeight(-Size / 2f + i * Step, -Size / 2f + j * Step);
            if (_hasLake) WaterLevel = Height(center.x + _lake.x, center.z + _lake.y) + _lakeDepth * 0.6f;

            // Rocks: scattered, never on pads
            int rocks = p.Slabs > 0.5f ? 25 : 70;
            for (int k = 0; k < rocks; k++)
            {
                var c = new Vector2((float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f) * Size * 0.9f;
                if (NearFlat(c, 6f) || InLakeArea(c.x, c.y)) continue;
                float r = Mathf.Lerp(0.4f, 2.2f, Mathf.Pow((float)rnd.NextDouble(), 3f));
                Rocks.Add(new Vector3(c.x + center.x, c.y + center.z, r));
            }
        }

        bool NearFlat(Vector2 c, float extra)
        {
            foreach (var f in _flats) if ((c - f.C).magnitude < f.R + extra) return true;
            foreach (var f in _clear) if ((c - f.C).magnitude < f.R + extra) return true;
            return false;
        }

        // Local coords: is this spot inside the lake's basin?
        bool InLakeArea(float x, float z) => _hasLake && (new Vector2(x, z) - _lake).magnitude < _lakeR * 1.3f;

        // Natural ground (relief + dunes + plates + craters), local coords
        float RawHeight(float x, float z, bool craters = true)
        {
            float s = _p.Seed * 13.37f;
            float h = _p.Relief * (Fbm(x * 0.008f + s, z * 0.008f - s, 4) - 0.5f) * 2f;
            if (_p.Dunes > 0f)
            {
                float warp = Mathf.PerlinNoise(x * 0.01f + 7f, z * 0.01f + 3f) * 6f;
                float ridge = Mathf.Sin((x * 0.8f + z * 0.6f) * 0.08f + warp);
                h += _p.Dunes * 2.4f * ridge * ridge * Mathf.PerlinNoise(x * 0.004f + 1f, z * 0.004f);
            }
            if (_p.Slabs > 0f)
            {
                float n = Mathf.PerlinNoise(x * 0.035f + s, z * 0.035f);
                h += _p.Slabs * Mathf.Floor(n * 5f) / 5f * 1.4f;
            }
            if (craters)
                foreach (var c in _craters)
                {
                    float d = (new Vector2(x, z) - c.C).magnitude / c.R;
                    if (d > 1.8f) continue;
                    float bowl = d < 1f ? -c.Depth * (1f - d * d) : 0f;
                    float rim  = c.Depth * 0.35f * Mathf.Exp(-Mathf.Pow((d - 1f) / 0.28f, 2f));
                    h += bowl + rim;
                }
            return h;
        }

        float ShapedHeight(float x, float z)
        {
            float h = RawHeight(x, z);
            var p = new Vector2(x, z);
            foreach (var b in _basins)
            {
                float d = (p - b.C).magnitude / b.R;
                if (d < 1.3f) h -= b.Level * Mathf.SmoothStep(1f, 0f, d / 1.3f);
            }
            foreach (var f in _flats)
            {
                float d = (p - f.C).magnitude;
                if (d < f.R * 1.8f)
                {
                    float t = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(f.R, f.R * 1.8f, d));
                    h = Mathf.Lerp(h, f.Level, t);
                }
            }
            return h;
        }

        static float Fbm(float x, float y, int octaves)
        {
            float sum = 0f, amp = 0.5f, freq = 1f, norm = 0f;
            for (int o = 0; o < octaves; o++)
            {
                sum  += amp * Mathf.PerlinNoise(x * freq, y * freq);
                norm += amp;
                amp  *= 0.5f; freq *= 2.03f;
            }
            return sum / norm;
        }

        // ── Queries (world space) ────────────────────────────────────────────
        public float Height(float wx, float wz)
        {
            float fx = Mathf.Clamp((wx - Center.x + Size / 2f) / Step, 0f, N - 1.001f);
            float fz = Mathf.Clamp((wz - Center.z + Size / 2f) / Step, 0f, N - 1.001f);
            int i = (int)fx, j = (int)fz;
            float tx = fx - i, tz = fz - j;
            float a = Mathf.Lerp(_h[i, j],     _h[i + 1, j],     tx);
            float b = Mathf.Lerp(_h[i, j + 1], _h[i + 1, j + 1], tx);
            return Center.y + Mathf.Lerp(a, b, tz);
        }

        public float Height(Vector3 w) => Height(w.x, w.z);

        public Vector3 Normal(float wx, float wz)
        {
            const float e = 1.2f;
            float hl = Height(wx - e, wz), hr = Height(wx + e, wz);
            float hd = Height(wx, wz - e), hu = Height(wx, wz + e);
            return new Vector3(hl - hr, 2f * e, hd - hu).normalized;
        }

        public bool Inside(Vector3 w, float margin = 12f) =>
            Mathf.Abs(w.x - Center.x) < Size / 2f - margin && Mathf.Abs(w.z - Center.z) < Size / 2f - margin;

        public Vector3 Clamp(Vector3 w, float margin = 12f)
        {
            float half = Size / 2f - margin;
            w.x = Mathf.Clamp(w.x, Center.x - half, Center.x + half);
            w.z = Mathf.Clamp(w.z, Center.z - half, Center.z + half);
            return w;
        }

        public Vector3 Surface(Vector2 local) => new(Center.x + local.x, Height(Center.x + local.x, Center.z + local.y), Center.z + local.y);

        // Liquid surface under w (only the lake has one; everywhere else it's -infinity)
        public float WaterY(Vector3 w) => InLakeArea(w.x - Center.x, w.z - Center.z) ? WaterLevel : float.NegativeInfinity;

        // What a vehicle rests on: the ground, or the lake's surface over it
        public float SurfaceY(Vector3 w) => Mathf.Max(Height(w), WaterY(w));

        public bool InWater(Vector3 w) => Height(w) < WaterY(w) - 0.05f;

        // Nearest rock the circle (w, radius) overlaps; returns the push-out vector (xz)
        public bool HitsRock(Vector3 w, float radius, out Vector3 push)
        {
            push = Vector3.zero;
            foreach (var r in Rocks)
            {
                var d = new Vector2(w.x - r.x, w.z - r.y);
                float min = r.z + radius;
                if (d.sqrMagnitude < min * min && d.sqrMagnitude > 1e-6f)
                {
                    float m = d.magnitude;
                    push = new Vector3(d.x / m, 0f, d.y / m) * (min - m);
                    return true;
                }
            }
            return false;
        }

        // ── Mesh and colour ─────────────────────────────────────────────────
        public Mesh BuildMesh()
        {
            var verts = new Vector3[N * N];
            var uvs   = new Vector2[N * N];
            for (int j = 0; j < N; j++)
            for (int i = 0; i < N; i++)
            {
                verts[j * N + i] = new Vector3(-Size / 2f + i * Step, _h[i, j], -Size / 2f + j * Step);
                uvs[j * N + i]   = new Vector2(i / (float)(N - 1), j / (float)(N - 1));
            }
            var tris = new int[(N - 1) * (N - 1) * 6];
            int t = 0;
            for (int j = 0; j < N - 1; j++)
            for (int i = 0; i < N - 1; i++)
            {
                int a = j * N + i, b = a + 1, c = a + N, d = c + 1;
                tris[t++] = a; tris[t++] = c; tris[t++] = b;
                tris[t++] = b; tris[t++] = c; tris[t++] = d;
            }
            var mesh = new Mesh { name = "WorldGround" };
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public Texture2D BuildColorMap()
        {
            var tex = new Texture2D(MapRes, MapRes, TextureFormat.RGBA32, true) { name = "WorldColor", wrapMode = TextureWrapMode.Clamp };
            var px = new Color[MapRes * MapRes];
            float s = _p.Seed * 3.1f;
            for (int j = 0; j < MapRes; j++)
            for (int i = 0; i < MapRes; i++)
            {
                float x = -Size / 2f + (i + 0.5f) / MapRes * Size;
                float z = -Size / 2f + (j + 0.5f) / MapRes * Size;
                float wx = Center.x + x, wz = Center.z + z;
                var n = Normal(wx, wz);
                float slope = 1f - n.y;
                float blend = Fbm(x * 0.02f + s, z * 0.02f - s, 3);
                var c = Color.Lerp(_p.GroundA, _p.GroundB, Mathf.SmoothStep(0.25f, 0.75f, blend));
                c = Color.Lerp(c, _p.Rock, Mathf.Clamp01((slope - 0.12f) * 4f));
                if (_p.Slabs > 0f)   // dark cracks between the plates
                {
                    float plate = Mathf.PerlinNoise(x * 0.035f + _p.Seed * 13.37f, z * 0.035f) * 5f;
                    float edge = Mathf.Abs(plate - Mathf.Round(plate));
                    if (edge < 0.06f) c = Color.Lerp(c, _p.Rock, 0.8f);
                }
                foreach (var cr in _craters)   // brighter rims, like fresh ejecta
                {
                    float d = (new Vector2(x, z) - cr.C).magnitude / cr.R;
                    if (d > 0.85f && d < 1.4f) c = Color.Lerp(c, Color.Lerp(_p.GroundA, Color.white, 0.25f), 0.25f * (1f - Mathf.Abs(d - 1.1f) / 0.3f));
                }
                foreach (var pa in _paints)
                {
                    float d = (new Vector2(x, z) - pa.C).magnitude / pa.R;
                    if (d < 1f) c = Color.Lerp(c, pa.Col, pa.Strength * Mathf.SmoothStep(1f, 0f, d));
                }
                if (InLakeArea(x, z) && Center.y + Height01(i, j) < WaterLevel) c = _p.Accent;   // lake
                float grain = 0.92f + 0.16f * Mathf.PerlinNoise(x * 0.9f, z * 0.9f);
                c.r *= grain; c.g *= grain; c.b *= grain; c.a = 1f;
                px[j * MapRes + i] = c;
            }
            tex.SetPixels(px);
            tex.Apply(true);
            return tex;
        }

        float Height01(int ti, int tj)
        {
            int i = Mathf.Clamp(Mathf.RoundToInt(ti / (float)(MapRes - 1) * (N - 1)), 0, N - 1);
            int j = Mathf.Clamp(Mathf.RoundToInt(tj / (float)(MapRes - 1) * (N - 1)), 0, N - 1);
            return _h[i, j];
        }
    }
}
