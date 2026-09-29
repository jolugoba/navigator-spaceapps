// Navigator 2.0 — WorldEnvironment.cs
// Builds (and later removes) everything you see in a world: sky gradient, sun, ambient light,
// fog, the ground, rocks, weather particles and the planet or moon hanging in the sky.
// It changes the scene's global lighting while the world is open and puts it all back after.
// The sky is a screen-space gradient behind everything (the UI shader ignores fog), so the
// fogged horizon blends into it; worlds without air get a black sky with the real stars.
// Guacalita Inc. — SAL / Claude

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Navigator.Worlds
{
    public class WorldEnvironment
    {
        public GameObject Root { get; private set; }
        public WorldTerrain Terrain { get; private set; }
        public Transform SkyBody { get; private set; }

        readonly Camera _cam;
        readonly Light  _sun;
        readonly WorldAssets _assets;
        WorldProfile _p;

        // What we change and must restore
        CameraClearFlags _camClear; Color _camBg; float _camFar;
        Quaternion _sunRot; Color _sunColor; float _sunIntensity; LightShadows _sunShadows;
        UnityEngine.Rendering.AmbientMode _ambMode; Color _ambColor;
        bool _fog; FogMode _fogMode; Color _fogColor; float _fogDensity;

        RawImage _sky; Texture2D _skyTex; Canvas _skyCanvas;
        readonly List<Object> _owned = new();
        ParticleSystem _weather;

        public WorldEnvironment(Camera cam, Light sun, WorldAssets assets)
        {
            _cam = cam; _sun = sun; _assets = assets;
        }

        public void Build(WorldProfile p, Vector3 center, System.Action<WorldTerrain> shape)
        {
            _p = p;
            Root = new GameObject($"World_{p.Body}");
            Root.transform.position = center;
            SaveScene();
            ApplyLighting(p);

            if (p.HasGround)
            {
                Terrain = new WorldTerrain();
                shape?.Invoke(Terrain);   // the mission adds its craters, pads, basins, paint
                Terrain.Generate(p, center);
                BuildGround();
                BuildRocks();
                if (!float.IsNegativeInfinity(Terrain.WaterLevel)) BuildWater();
            }
            BuildSky(p);
            BuildSunGlow(p);
            BuildWeather(p.Weather);
            if (!string.IsNullOrEmpty(p.SkyBody)) BuildSkyBody(p);
            Navigator.Game.CosmicSkyFX.SetVisible(p.StarsVisible);
        }

        // ── Scene state ──────────────────────────────────────────────────────
        void SaveScene()
        {
            if (_cam != null) { _camClear = _cam.clearFlags; _camBg = _cam.backgroundColor; _camFar = _cam.farClipPlane; }
            if (_sun != null) { _sunRot = _sun.transform.rotation; _sunColor = _sun.color; _sunIntensity = _sun.intensity; _sunShadows = _sun.shadows; }
            _ambMode = RenderSettings.ambientMode; _ambColor = RenderSettings.ambientLight;
            _fog = RenderSettings.fog; _fogMode = RenderSettings.fogMode; _fogColor = RenderSettings.fogColor; _fogDensity = RenderSettings.fogDensity;
        }

        public void Teardown()
        {
            if (_cam != null) { _cam.clearFlags = _camClear; _cam.backgroundColor = _camBg; _cam.farClipPlane = _camFar; }
            if (_sun != null) { _sun.transform.rotation = _sunRot; _sun.color = _sunColor; _sun.intensity = _sunIntensity; _sun.shadows = _sunShadows; }
            RenderSettings.ambientMode = _ambMode; RenderSettings.ambientLight = _ambColor;
            RenderSettings.fog = _fog; RenderSettings.fogMode = _fogMode; RenderSettings.fogColor = _fogColor; RenderSettings.fogDensity = _fogDensity;
            if (_skyCanvas != null) Object.Destroy(_skyCanvas.gameObject);
            if (Root != null) Object.Destroy(Root);
            foreach (var o in _owned) if (o != null) Object.Destroy(o);
            _owned.Clear();
            Navigator.Game.CosmicSkyFX.SetVisible(true);
        }

        void ApplyLighting(WorldProfile p)
        {
            if (_sun != null)
            {
                _sun.transform.rotation = Quaternion.Euler(p.SunElevation, p.SunAzimuth + 180f, 0f);
                _sun.color = p.SunColor;
                _sun.intensity = p.SunIntensity;
                _sun.shadows = LightShadows.Soft;
            }
            RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = p.Ambient;
            SetFog(p.Fog, p.FogDensity);
            if (_cam != null)
            {
                _cam.clearFlags = CameraClearFlags.SolidColor;
                _cam.backgroundColor = p.StarsVisible ? Color.black : p.SkyHorizon;
            }
        }

        // Missions change the air as they descend (Jupiter's layers, Venus' clouds)
        public void SetFog(Color c, float density)
        {
            RenderSettings.fog = density > 0f;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = c;
            RenderSettings.fogDensity = density;
        }

        public void SetSky(Color top, Color horizon)
        {
            if (_skyTex == null) return;
            PaintSky(top, horizon, RenderSettings.fog ? RenderSettings.fogColor : horizon);
            if (_cam != null && !_p.StarsVisible) _cam.backgroundColor = horizon;
        }

        // ── Sky: a gradient drawn behind everything, kept level with the horizon ─
        void BuildSky(WorldProfile p)
        {
            if (p.StarsVisible || _cam == null) return;   // black sky: stars instead
            var go = new GameObject("WorldSky");
            _skyCanvas = go.AddComponent<Canvas>();
            _skyCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            _skyCanvas.worldCamera = _cam;
            _skyCanvas.planeDistance = _cam.farClipPlane * 0.92f;
            _skyCanvas.sortingOrder = -900;
            var img = new GameObject("Gradient").AddComponent<RawImage>();
            img.transform.SetParent(go.transform, false);
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            _skyTex = new Texture2D(1, 256, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "WorldSkyGradient" };
            _owned.Add(_skyTex);
            PaintSky(p.SkyTop, p.SkyHorizon, p.FogDensity > 0f ? p.Fog : p.SkyHorizon);
            img.texture = _skyTex;
            _sky = img;
        }

        // 256 rows: 0..127 below the horizon (fog colour), 128..255 horizon → zenith
        void PaintSky(Color top, Color horizon, Color below)
        {
            var px = new Color[256];
            for (int i = 0; i < 256; i++)
            {
                float t = (i - 128) / 127f;
                px[i] = t < 0f ? Color.Lerp(horizon, below, Mathf.Clamp01(-t * 4f))
                               : Color.Lerp(horizon, top, Mathf.Pow(Mathf.Clamp01(t), 0.6f));
            }
            _skyTex.SetPixels(px);
            _skyTex.Apply(false);
        }

        // Called every frame: slide the gradient so its horizon row sits on the real horizon
        public void Update()
        {
            if (_sky != null && _cam != null)
            {
                float pitch = -Mathf.DeltaAngle(0f, _cam.transform.eulerAngles.x) * Mathf.Deg2Rad;   // + looking up
                float halfV = _cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
                // Screen y (0 bottom … 1 top) where the horizon is
                float horizonY = 0.5f - Mathf.Tan(pitch) / Mathf.Tan(halfV) * 0.5f;
                // The texture's middle (v = 0.5) must land on horizonY; the screen spans h in v
                float h = 1f;   // one screen height = the whole texture (128 rows up, 128 down)
                _sky.uvRect = new Rect(0f, 0.5f - horizonY * h, 1f, h);
            }
            if (_weather != null && _cam != null) _weather.transform.position = _cam.transform.position + Vector3.up * 6f;
            if (SkyBody != null && _cam != null)
                SkyBody.position = _cam.transform.position + _skyDir * SkyDistance;
            if (_sunGlow != null && _cam != null)
            {
                _sunGlow.position = _cam.transform.position + _sunDir * SkyDistance;
                _sunGlow.rotation = Quaternion.LookRotation(_sunGlow.position - _cam.transform.position);
                float flicker = 1f + 0.03f * Mathf.Sin(Time.time * 7f);
                _sunGlow.localScale = _sunGlowScale * flicker;
            }
            if (_lightning > 0f)
            {
                _lightning = Mathf.MoveTowards(_lightning, 0f, Time.deltaTime * 4f);
                RenderSettings.ambientLight = Color.Lerp(_p.Ambient, Color.white, _lightning);
            }
        }

        // ── Ground ───────────────────────────────────────────────────────────
        void BuildGround()
        {
            var go = new GameObject("Ground", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(Root.transform, false);
            var mesh = Terrain.BuildMesh();
            _owned.Add(mesh);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.GetComponent<MeshRenderer>();
            var mat = _assets.NewMaterial(_assets.TerrainMaterial, Color.white, 0.08f);
            var tex = Terrain.BuildColorMap();
            _owned.Add(tex); _owned.Add(mat);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            mat.mainTexture = tex;
            mr.sharedMaterial = mat;
            mr.receiveShadows = true;
        }

        void BuildRocks()
        {
            var rockMat = _assets.NewMaterial(_assets.PropMaterial, _p.Rock, 0.15f);
            _owned.Add(rockMat);
            var prefab = _assets.RockFor(_p.Body);
            foreach (var r in Terrain.Rocks)
            {
                var pos = new Vector3(r.x, Terrain.Height(r.x, r.y) - r.z * 0.25f, r.y);
                GameObject go;
                if (prefab != null)
                {
                    go = Object.Instantiate(prefab, Root.transform);
                    Navigator.Core.ModelFit.FitToSize(go, r.z * 2.2f);
                    go.transform.position = pos;
                }
                else
                {
                    go = WorldAssets.Primitive(PrimitiveType.Sphere, Root.transform, rockMat);
                    go.transform.position = pos;
                    go.transform.localScale = new Vector3(r.z * 2f, r.z * 1.2f, r.z * 1.7f);
                }
                go.transform.rotation = Quaternion.Euler(0f, (r.x * 37f + r.y * 11f) % 360f, 0f);
                go.name = "Rock";
            }
        }

        void BuildWater()
        {
            var go = new GameObject("Lake", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(Root.transform, false);
            var mesh = WorldAssets.QuadMesh(WorldTerrain.Size);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.transform.position = new Vector3(Terrain.Center.x, Terrain.WaterLevel, Terrain.Center.z);
            var mat = _assets.NewMaterial(_assets.PropMaterial, _p.Accent, 0.97f);
            _owned.Add(mat);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        // ── Weather ──────────────────────────────────────────────────────────
        void BuildWeather(Weather w)
        {
            if (w == Weather.None || _assets.DustMaterial == null) return;
            var go = new GameObject("Weather");
            go.transform.SetParent(Root.transform, false);
            _weather = go.AddComponent<ParticleSystem>();
            _weather.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _weather.main;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 600;
            var shape = _weather.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(70f, 10f, 70f);
            var emission = _weather.emission;
            var vel = _weather.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            Color c;
            switch (w)
            {
                case Weather.Dust:
                    c = new Color(0.85f, 0.6f, 0.4f, 0.35f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
                    main.startLifetime = 5f;
                    emission.rateOverTime = 50f;
                    vel.x = new ParticleSystem.MinMaxCurve(2f, 5f); vel.y = new ParticleSystem.MinMaxCurve(-0.4f, 0.2f); vel.z = new ParticleSystem.MinMaxCurve(-1f, 1f);
                    break;
                case Weather.AcidHaze:
                    c = new Color(0.95f, 0.85f, 0.5f, 0.25f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 1.4f);
                    main.startLifetime = 6f;
                    emission.rateOverTime = 40f;
                    vel.x = new ParticleSystem.MinMaxCurve(0.2f, 1f); vel.y = new ParticleSystem.MinMaxCurve(-0.1f, 0.3f); vel.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
                    break;
                case Weather.MethaneRain:   // big drops that fall slowly (low gravity, thick air)
                    c = new Color(0.9f, 0.75f, 0.5f, 0.45f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
                    main.startLifetime = 6f;
                    emission.rateOverTime = 0f;   // the rain event turns it on
                    vel.x = new ParticleSystem.MinMaxCurve(0.2f, 0.5f); vel.y = new ParticleSystem.MinMaxCurve(-1.8f, -1.4f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
                    var r = _weather.GetComponent<ParticleSystemRenderer>();
                    r.renderMode = ParticleSystemRenderMode.Stretch;
                    r.lengthScale = 6f;
                    break;
                default:   // Frost: a few glints drifting down
                    c = new Color(0.9f, 0.95f, 1f, 0.6f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
                    main.startLifetime = 8f;
                    emission.rateOverTime = 12f;
                    vel.x = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f); vel.y = new ParticleSystem.MinMaxCurve(-0.3f, -0.1f); vel.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
                    break;
            }
            main.startColor = c;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = _assets.DustMaterial;
            _weather.Play();
        }

        // Rain / storm events change the amount (0 = off, 1 = normal, 3 = storm)
        public void SetWeatherAmount(float k)
        {
            if (_weather == null) return;
            var e = _weather.emission;
            float baseRate = _p.Weather == Weather.MethaneRain ? 220f : _p.Weather == Weather.Dust ? 50f : 40f;
            e.rateOverTime = baseRate * k;
        }

        // ── Planet or moon in the sky ────────────────────────────────────────
        const float SkyDistance = 3000f;
        Vector3 _skyDir;

        void BuildSkyBody(WorldProfile p)
        {
            var prefab = _assets.SkyBodyPrefab?.Invoke(p.SkyBody);
            GameObject go;
            float size = 2f * SkyDistance * Mathf.Tan(p.SkyBodyDeg * 0.5f * Mathf.Deg2Rad);
            if (prefab != null)
            {
                go = Object.Instantiate(prefab, Root.transform);
                Navigator.Core.ModelFit.FitToSize(go, size);
            }
            else
            {
                var mat = _assets.NewMaterial(_assets.PropMaterial, p.SkyBody == "neptune" ? new Color(0.25f, 0.4f, 0.8f) : new Color(0.45f, 0.4f, 0.37f), 0.3f);
                _owned.Add(mat);
                go = WorldAssets.Primitive(PrimitiveType.Sphere, Root.transform, mat);
                go.transform.localScale = Vector3.one * size;
            }
            go.name = "SkyBody";
            // Opposite the sun's azimuth, low over the horizon
            float az = (p.SunAzimuth + 150f) * Mathf.Deg2Rad, el = p.SkyBodyElevation * Mathf.Deg2Rad;
            _skyDir = new Vector3(Mathf.Sin(az) * Mathf.Cos(el), Mathf.Sin(el), Mathf.Cos(az) * Mathf.Cos(el));
            SkyBody = go.transform;
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // ── The Sun's glare: a soft glowing disc where the Sun really is ──────
        Transform _sunGlow; Vector3 _sunDir, _sunGlowScale;

        void BuildSunGlow(WorldProfile p)
        {
            if (p.SunGlowDeg <= 0f || _assets.GlowMaterial == null) return;
            var go = new GameObject("SunGlow", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(Root.transform, false);
            var m = WorldAssets.QuadMesh(1f);
            _owned.Add(m);
            // The quad lies on XZ: stand it up so it faces the camera along its forward axis
            var verts = m.vertices;
            for (int i = 0; i < verts.Length; i++) verts[i] = new Vector3(verts[i].x, verts[i].z, 0f);
            m.vertices = verts;
            m.RecalculateBounds();
            go.GetComponent<MeshFilter>().sharedMesh = m;
            var mat = new Material(_assets.GlowMaterial);
            var c = p.SunColor; c.a = 1f;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            mat.color = c;
            _owned.Add(mat);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _sunDir = -(Quaternion.Euler(p.SunElevation, p.SunAzimuth + 180f, 0f) * Vector3.forward);
            float size = 2f * SkyDistance * Mathf.Tan(p.SunGlowDeg * 0.5f * Mathf.Deg2Rad) * 3f;   // glare, not just the disc
            _sunGlowScale = Vector3.one * size;
            _sunGlow = go.transform;
        }

        // Lightning: the whole scene lights up for a moment (Jupiter's water clouds, Venus)
        float _lightning;
        public void Lightning() => _lightning = 1f;

        // ── Cloud layers for the giants (no ground) ─────────────────────────
        public Transform AddCloudLayer(float y, Color color, float alpha)
        {
            if (_assets.DustMaterial == null) return null;
            var go = new GameObject("CloudLayer", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(Root.transform, false);
            go.transform.position = new Vector3(Root.transform.position.x, y, Root.transform.position.z);
            go.GetComponent<MeshFilter>().sharedMesh = WorldAssets.QuadMesh(2400f);
            var mat = new Material(_assets.DustMaterial);
            var tex = WorldAssets.CloudTexture(color.GetHashCode());
            _owned.Add(mat); _owned.Add(tex);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            mat.mainTexture = tex;
            var c = color; c.a = alpha;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            mat.color = c;
            mat.mainTextureScale = new Vector2(6f, 6f);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            // Two faces, so the layer is seen from above and from below
            var back = Object.Instantiate(go, Root.transform);
            back.transform.position = go.transform.position;
            back.transform.rotation = Quaternion.Euler(180f, 0f, 0f);
            return go.transform;
        }
    }
}
