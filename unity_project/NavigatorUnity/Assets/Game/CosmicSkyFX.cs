// Navigator 2.0 — CosmicSkyFX.cs
// The universe painted over your room: glowing nebulae, a spiral galaxy, a distant black hole
// with its accretion disk, a faint star field and shooting stars (plus full meteor showers).
// Everything sits "at infinity": it follows the camera's position, never its rotation, so it
// stays fixed in the sky while you turn the phone.
// Textures from Meshy (Assets/Art/Sky) are used when present; otherwise they're generated.
// Guacalita Inc. — SAL / Claude

using System.Collections.Generic;
using UnityEngine;

namespace Navigator.Game
{
    [RequireComponent(typeof(Camera))]
    public class CosmicSkyFX : MonoBehaviour
    {
        public Material  GlowMaterial;                // additive particle material
        public Texture2D[] NebulaTextures;             // optional (Meshy): transparent PNGs
        public Texture2D[] GalaxyTextures;
        public Texture2D   BlackHoleTexture;

        [Range(0f, 1f)] public float Intensity = 0.55f;   // keep the room visible behind the sky
        public float SkyDistance     = 5000f;             // in front of the camera feed (far plane × 0.95)
        public float MeteorsPerMinute = 10f;
        public int   Seed = 2026;

        public static CosmicSkyFX Instance { get; private set; }

        private Transform _root;
        private readonly List<Transform> _billboards = new();
        private ParticleSystem _meteors;
        private float _showerUntil, _nextMeteor;

        void Awake() => Instance = this;

        void Start()
        {
            _root = new GameObject("CosmicSky").transform;
            Random.State saved = Random.state;
            Random.InitState(Seed);

            BuildStars();
            // Every real image gets a place in the sky (up to 6); procedural ones fill in otherwise
            int nebulae = Mathf.Clamp(NebulaTextures != null ? NebulaTextures.Length : 0, 4, 6);
            for (int i = 0; i < nebulae; i++)
            {
                var art = Pick(NebulaTextures, i);   // Meshy art keeps its own colours
                Billboard("Nebula", art ?? NebulaTexture(i), RandomSkyDir(), Random.Range(2200f, 3400f),
                          art != null ? Color.white : NebulaTint(i), additive: true);
            }
            int galaxies = Mathf.Clamp(GalaxyTextures != null ? GalaxyTextures.Length : 0, 1, 2);
            for (int i = 0; i < galaxies; i++)
                Billboard("Galaxy", Pick(GalaxyTextures, i) ?? GalaxyTexture(), RandomSkyDir(), i == 0 ? 1300f : 900f,
                          new Color(1f, 0.95f, 0.9f), additive: true);
            // Real images (EHT ring on black) glow additively; only the procedural one has a true
            // opaque shadow and needs alpha blending to darken the room behind it
            Billboard("BlackHole", BlackHoleTexture != null ? BlackHoleTexture : BlackHoleTex(), RandomSkyDir(), 1100f,
                      Color.white, additive: BlackHoleTexture != null);
            BuildMeteors();

            Random.state = saved;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_root != null) Destroy(_root.gameObject);
        }

        // The Sky Finder shows the real sky: hide the painted one meanwhile
        public static void SetVisible(bool on)
        {
            if (Instance != null && Instance._root != null) Instance._root.gameObject.SetActive(on);
        }

        // A burst of shooting stars (arrival, launch, events)
        public static void MeteorShower(float seconds)
        {
            if (Instance != null) Instance._showerUntil = Time.time + seconds;
        }

        void LateUpdate()
        {
            if (_root == null) return;
            _root.position = transform.position;
            foreach (var b in _billboards)
                b.rotation = Quaternion.LookRotation(b.position - transform.position);

            float perMinute = Time.time < _showerUntil ? MeteorsPerMinute * 12f : MeteorsPerMinute;
            if (Time.time >= _nextMeteor)
            {
                EmitMeteor();
                _nextMeteor = Time.time + Random.Range(0.3f, 1.7f) * 60f / Mathf.Max(1f, perMinute);
            }
        }

        // ── Pieces ────────────────────────────────────────────────────────────
        static T Pick<T>(T[] arr, int i) where T : class =>
            arr != null && arr.Length > 0 ? arr[i % arr.Length] : null;

        static Vector3 RandomSkyDir()
        {
            // Mostly around and above the horizon, where people look
            Vector3 d = Random.onUnitSphere;
            d.y = Mathf.Abs(d.y) * 0.8f - 0.1f;
            return d.normalized;
        }

        static Color NebulaTint(int i) => (i % 4) switch
        {
            0 => new Color(0.55f, 0.35f, 1f),    // violet
            1 => new Color(1f, 0.35f, 0.55f),    // hydrogen pink
            2 => new Color(0.25f, 0.75f, 1f),    // oxygen teal
            _ => new Color(1f, 0.6f, 0.3f),      // dusty orange
        };

        static Mesh _quad;
        static Mesh QuadMesh
        {
            get
            {
                if (_quad != null) return _quad;
                _quad = new Mesh { name = "SkyQuad" };
                _quad.vertices  = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f) };
                _quad.uv        = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
                _quad.triangles = new[] { 0, 3, 1, 3, 0, 2 };   // same winding as Unity's built-in quad (faces −Z)
                _quad.RecalculateNormals();
                _quad.RecalculateBounds();
                return _quad;
            }
        }

        void Billboard(string name, Texture tex, Vector3 dir, float size, Color tint, bool additive)
        {
            if (GlowMaterial == null) return;
            // A plain quad: CreatePrimitive would add a MeshCollider, which web builds strip
            var go = new GameObject("Quad", typeof(MeshFilter), typeof(MeshRenderer));
            go.GetComponent<MeshFilter>().sharedMesh = QuadMesh;
            go.name = name;
            // Real photos are scanned by their file name (nebula_01 = Orion…); generated ones by kind
            string id = tex != null && !string.IsNullOrEmpty(tex.name) && tex.name.Contains("_") ? tex.name : name.ToLowerInvariant();
            Scannable.Tag(go, "sky", id, size * 0.5f, atInfinity: true);
            go.transform.SetParent(_root, false);
            go.transform.localPosition = dir * SkyDistance;
            // Keep the picture's proportions: the longer side is `size`
            float aspect = tex != null && tex.height > 0 ? tex.width / (float)tex.height : 1f;
            go.transform.localScale = aspect >= 1f ? new Vector3(size, size / aspect, 1f)
                                                   : new Vector3(size * aspect, size, 1f);

            // Additive glows can only brighten; the black hole's shadow has to darken, so it blends
            var mat = additive ? new Material(GlowMaterial) : AlphaBlend(new Material(GlowMaterial));
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            mat.mainTexture = tex;
            Color c = tint; c.a = additive ? Intensity : Mathf.Clamp01(Intensity + 0.35f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            mat.color = c;

            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows    = false;
            r.sortingOrder      = -500;   // after the camera feed (-1000), before everything else
            _billboards.Add(go.transform);
        }

        static Material AlphaBlend(Material m)
        {
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);
            return m;
        }

        void BuildStars()
        {
            var go = new GameObject("Stars");
            go.transform.SetParent(_root, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false; main.playOnAwake = false;
            main.startLifetime = 1e6f;
            main.startSpeed = 0f;
            main.maxParticles = 1400;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var em = ps.emission; em.enabled = false;
            var shape = ps.shape; shape.enabled = false;
            var r = go.GetComponent<ParticleSystemRenderer>();
            if (GlowMaterial != null) r.sharedMaterial = GlowMaterial;
            r.sortingOrder = -490;

            var stars = new ParticleSystem.Particle[main.maxParticles];
            for (int i = 0; i < stars.Length; i++)
            {
                float temp = Random.value;   // blue-white hot stars to orange cool ones
                Color c = temp < 0.2f ? new Color(0.7f, 0.8f, 1f) : temp < 0.85f ? Color.white : new Color(1f, 0.8f, 0.6f);
                c.a = Random.Range(0.25f, 0.9f) * Intensity;
                stars[i].position   = Random.onUnitSphere * SkyDistance * 0.98f;
                stars[i].startSize  = Random.Range(4f, 14f) * (Random.value < 0.03f ? 2.5f : 1f);
                stars[i].startColor = c;
                stars[i].remainingLifetime = 1e6f;
                stars[i].startLifetime     = 1e6f;
            }
            ps.Play();   // emission is off: playing only keeps the hand-placed stars alive and drawn
            ps.SetParticles(stars, stars.Length);
        }

        void BuildMeteors()
        {
            var go = new GameObject("Meteors");
            go.transform.SetParent(_root, false);
            _meteors = go.AddComponent<ParticleSystem>();
            _meteors.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _meteors.main;
            main.loop = false; main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSize     = new ParticleSystem.MinMaxCurve(6f, 12f);
            main.maxParticles  = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var em = _meteors.emission; em.enabled = false;
            var shape = _meteors.shape; shape.enabled = false;

            var col = _meteors.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.6f, 0.85f, 1f), 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var trails = _meteors.trails;
            trails.enabled = true;
            trails.lifetime = 0.25f;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            trails.dieWithParticles = true;
            trails.inheritParticleColor = true;

            var r = go.GetComponent<ParticleSystemRenderer>();
            if (GlowMaterial != null) { r.sharedMaterial = GlowMaterial; r.trailMaterial = GlowMaterial; }
            r.sortingOrder = -480;
            _meteors.Play();
        }

        void EmitMeteor()
        {
            if (_meteors == null) return;
            Vector3 start = RandomSkyDir() * SkyDistance * 0.6f;
            Vector3 tangent = Vector3.Cross(start.normalized, Random.onUnitSphere).normalized;
            tangent.y = -Mathf.Abs(tangent.y) - 0.2f;   // they fall
            var p = new ParticleSystem.EmitParams
            {
                position = start,
                velocity = tangent.normalized * Random.Range(1400f, 2400f),
                applyShapeToPosition = false,
            };
            _meteors.Emit(p, 1);
        }

        // ── Procedural textures (used until Meshy art is dropped in) ──────────
        static float Hash(int x, int y, int s)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + s * 982451653;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xffff) / 65535f;
            }
        }

        static float ValueNoise(float x, float y, int s)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float xf = x - xi, yf = y - yi;
            float u = xf * xf * (3f - 2f * xf), v = yf * yf * (3f - 2f * yf);
            float a = Hash(xi, yi, s), b = Hash(xi + 1, yi, s), c = Hash(xi, yi + 1, s), d = Hash(xi + 1, yi + 1, s);
            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }

        static float Fbm(float x, float y, int s)
        {
            float sum = 0f, amp = 0.5f;
            for (int o = 0; o < 5; o++) { sum += amp * ValueNoise(x, y, s + o * 31); x *= 2f; y *= 2f; amp *= 0.5f; }
            return sum;
        }

        static Texture2D NewTex(int size)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            return t;
        }

        static Texture2D NebulaTexture(int seed)
        {
            const int N = 192;
            var t = NewTex(N);
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float u = x / (float)N * 2f - 1f, v = y / (float)N * 2f - 1f;
                float fall = Mathf.Clamp01(1f - Mathf.Sqrt(u * u + v * v));
                float n  = Fbm(x * 0.03f, y * 0.03f, seed * 7 + 1);
                float n2 = Fbm(x * 0.06f + 13f, y * 0.06f, seed * 7 + 3);
                float cloud = Mathf.Clamp01((n * 1.4f - 0.35f)) * fall * fall;
                float core  = Mathf.Pow(Mathf.Clamp01(n2), 3f) * fall;
                float a = Mathf.Clamp01(cloud * 1.6f + core * 0.8f);
                px[y * N + x] = new Color(1f, 0.85f + 0.15f * core, 0.9f, a);
            }
            t.SetPixels(px); t.Apply(true, true);
            return t;
        }

        static Texture2D GalaxyTexture()
        {
            const int N = 192;
            var t = NewTex(N);
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float u = x / (float)N * 2f - 1f, v = (y / (float)N * 2f - 1f) * 1.8f;   // tilted disc
                float r = Mathf.Sqrt(u * u + v * v);
                float ang = Mathf.Atan2(v, u);
                float arms = Mathf.Pow(0.5f + 0.5f * Mathf.Cos(2f * ang - 9f * Mathf.Log(r + 0.05f)), 3f);
                float disc = Mathf.Exp(-r * 3.2f);
                float bulge = Mathf.Exp(-r * r * 60f);
                float grain = 0.7f + 0.3f * Fbm(x * 0.12f, y * 0.12f, 77);
                float a = Mathf.Clamp01((arms * disc * 1.3f + bulge * 1.5f) * grain);
                Color c = Color.Lerp(new Color(0.6f, 0.75f, 1f), new Color(1f, 0.9f, 0.7f), bulge * 2f + (1f - r));
                c.a = a;
                px[y * N + x] = c;
            }
            t.SetPixels(px); t.Apply(true, true);
            return t;
        }

        // Black hole seen slightly edge-on: dark shadow, thin photon ring, a Doppler-bright disk
        static Texture2D BlackHoleTex()
        {
            const int N = 256;
            var t = NewTex(N);
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float u = x / (float)N * 2f - 1f, v = y / (float)N * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v);
                const float shadow = 0.22f;

                // Accretion disk: flattened ellipse, brighter on the side moving toward us
                float dv = v * 4.2f;
                float dr = Mathf.Sqrt(u * u + dv * dv);
                float disk = Mathf.Exp(-Mathf.Pow((dr - 0.55f) / 0.22f, 2f));
                float doppler = 0.55f + 0.45f * Mathf.Clamp(-u / Mathf.Max(0.1f, dr), -1f, 1f);
                disk *= doppler * (0.75f + 0.25f * Fbm(Mathf.Atan2(dv, u) * 6f + dr * 12f, dr * 20f, 5));

                // Lensed image of the far side of the disk arching over the shadow
                float halo = Mathf.Exp(-Mathf.Pow((r - 0.3f) / 0.05f, 2f)) * (v > 0f ? 0.9f : 0.5f);
                float photon = Mathf.Exp(-Mathf.Pow((r - shadow - 0.015f) / 0.012f, 2f));

                float glow = Mathf.Clamp01(disk + halo + photon);
                bool front = Mathf.Abs(v) < 0.06f && dr > shadow;   // disk passes in front of the shadow
                if (r < shadow && !front)
                {
                    px[y * N + x] = new Color(0f, 0f, 0f, 1f);
                    continue;
                }
                Color hot = Color.Lerp(new Color(1f, 0.45f, 0.1f), new Color(1f, 0.95f, 0.8f), glow);
                float edge = Mathf.Clamp01(1f - r);
                hot.a = Mathf.Clamp01(glow * 1.2f) * edge;
                px[y * N + x] = hot;
            }
            t.SetPixels(px); t.Apply(true, true);
            return t;
        }
    }
}
