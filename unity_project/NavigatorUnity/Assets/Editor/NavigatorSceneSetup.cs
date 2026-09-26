// Navigator 2.0 — NavigatorSceneSetup.cs
// Un clic: crea, conecta y deja jugable toda la escena SpaceFlight
// Menu: Navigator → Setup Scene   (se puede ejecutar varias veces sin duplicar nada)
// AR sin ARCore/ARKit: cámara trasera de fondo + giroscopio (GyroCameraRig)
// Guacalita Inc. — SAL / Claude

#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Navigator.Core;
using Navigator.Game;
using Navigator.Network;
using Navigator.AR;
using Navigator.UI;

namespace Navigator.Editor
{
    public static class NavigatorSceneSetup
    {
        const string FxDir  = "Assets/Prefabs/FX";
        const string MatDir = "Assets/Materials";

        [MenuItem("Navigator/Setup Scene")]
        public static void SetupScene()
        {
            EnsureTag("LocalShip");
            EnsureTag("RemoteShip");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            // ── Luz ───────────────────────────────────────────────────────────
            var sun = Object.FindFirstObjectByType<Light>();
            if (sun == null || sun.type != LightType.Directional)
            {
                sun = new GameObject("Sun").AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.intensity = 1.3f;
            sun.transform.rotation = Quaternion.Euler(35f, -30f, 0f);

            // ── Cámara: giroscopio + persecución, video de fondo ──────────────
            var mainCamGO = GetOrCreate("Main Camera");
            mainCamGO.tag = "MainCamera";
            var cam = EnsureComp<Camera>(mainCamGO);
            EnsureComp<AudioListener>(mainCamGO);
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.nearClipPlane   = 0.3f;
            cam.farClipPlane    = 6000f;   // the planet sits up to ~3000 units away
            cam.fieldOfView     = 60f;
            var rig = EnsureComp<GyroCameraRig>(mainCamGO);
            EnsureComp<AudioFX>(mainCamGO);
            EnsureComp<SpaceDustFX>(mainCamGO).ParticleMaterial = ParticleMaterial();

            var feedGO = GetOrCreate("CameraFeed");
            var feed   = EnsureComp<CameraFeedBackground>(feedGO);
            feed.ARCamera = cam;

            // ── GameManager GO — red ──────────────────────────────────────────
            var gmGO   = GetOrCreate("GameManager");
            var senda  = EnsureComp<SendaBridge>(gmGO);
            var sync   = EnsureComp<ShipSyncService>(gmGO);
            var chat   = EnsureComp<P2PChatSystem>(gmGO);
            var remote = EnsureComp<RemoteShipManager>(gmGO);
            EnsureComp<ShipCatalog>(gmGO);   // hangar, filled by Assign Models

            // ── Nave local ────────────────────────────────────────────────────
            var ship = GetOrCreate("LocalShip");
            ship.tag = "LocalShip";
            ship.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            var rb = EnsureComp<Rigidbody>(ship);
            rb.useGravity     = false;
            rb.linearDamping  = 0.5f;
            rb.freezeRotation = true;
            rb.interpolation  = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var shipCol = EnsureComp<SphereCollider>(ship);
            shipCol.radius = 1.6f;

            var shipCtrl  = EnsureComp<ShipController>(ship);
            var physics   = EnsureComp<PhysicsEngine>(ship);
            EnsureComp<FuelSystem>(ship);
            EnsureComp<BurnoutSystem>(ship);
            var collision = EnsureComp<CollisionSystem>(ship);
            EnsureComp<ShipCustomizer>(ship);
            EnsureComp<ShipAnimator>(ship);
            var arShip    = EnsureComp<ARShipController>(ship);
            var thruster  = EnsureComp<ThrusterFX>(ship);
            thruster.ParticleMaterial = ParticleMaterial();
            thruster.ShipLength       = NavigatorAssignModels.ShipSize;
            EnsureComp<ShieldFX>(ship).BubbleMaterial = ParticleMaterial();
            EnsureComp<SpeedLines>(ship).ParticleMaterial = ParticleMaterial();   // streaks at speed
            var tools = EnsureComp<ShipTools>(ship);   // SCAN ("what is that?") and FIRE
            tools.GlowMaterial = ParticleMaterial();
            tools.ImpactFX     = PickupBurstPrefab();

            shipCtrl.Physics      = physics;
            shipCtrl.IsLocalPlayer = true;
            physics.ShipRb        = rb;
            arShip.Ship           = shipCtrl;
            arShip.ARCamera       = cam;
            arShip.Rig            = rig;
            rig.Target            = ship.transform;

            // ── Espacio: planeta, brújula, pods, asteroides ───────────────────
            var spaceMgrGO = GetOrCreate("ARSpaceManager");
            var spaceMgr   = EnsureComp<ARSpaceManager>(spaceMgrGO);
            spaceMgr.ARCamera   = cam;
            spaceMgr.ShipAnchor = ship.transform;

            var compassGO   = GetOrCreate("ARCompassTracker");
            var compass     = EnsureComp<ARCompassTracker>(compassGO);
            compass.SpaceManager = spaceMgr;
            compass.Horizons     = EnsureComp<NasaHorizons>(compassGO);
            // Old scenes serialized (0,0,1): straight ahead at eye level, hidden by the ship
            compass.FallbackDirection = new Vector3(0f, 0.36f, 1f);

            var arSessionGO = GetOrCreate("ARSessionManager");
            var arMgr       = EnsureComp<ARSessionManager>(arSessionGO);
            arMgr.ARCamera       = cam;
            arMgr.ShipController = arShip;
            arMgr.SpaceManager   = spaceMgr;

            var explosion = ExplosionPrefab();
            collision.ExplosionPrefab = explosion;

            var powerupGO  = GetOrCreate("PowerupSystem");
            var powerups   = EnsureComp<PowerupSystem>(powerupGO);
            powerups.ARCamera = cam;
            powerups.FuelPodPrefab          = PodPrefab(PowerupType.Fuel);
            powerups.BoostPodPrefab         = PodPrefab(PowerupType.Boost);
            powerups.ShieldPodPrefab        = PodPrefab(PowerupType.Shield);
            powerups.EMPPodPrefab           = PodPrefab(PowerupType.EMP);
            powerups.GravityAssistPodPrefab = PodPrefab(PowerupType.GravityAssist);

            var asteroidsGO = GetOrCreate("AsteroidField");
            var asteroids   = EnsureComp<AsteroidField>(asteroidsGO);
            asteroids.ExplosionPrefab = explosion;

            // Real flybys (MESSENGER, Galileo, Cassini, Voyager 2) and the painted sky
            var gravity = EnsureComp<GravityField>(GetOrCreate("GravityField"));
            gravity.GlowMaterial = ParticleMaterial();

            // Free flight: the whole Solar System of today, reusing the same planets, belt and pods
            var free = EnsureComp<FreeSpace>(GetOrCreate("FreeSpace"));
            free.Space        = spaceMgr;
            free.Gravity      = gravity;
            free.Asteroids    = asteroids;
            free.Pods         = powerups;
            free.SunMaterial  = GlowMaterial();
            free.GlowMaterial = ParticleMaterial();
            var sky = EnsureComp<CosmicSkyFX>(mainCamGO);
            sky.GlowMaterial     = ParticleMaterial();
            sky.NebulaTextures   = SkyTextures("nebula");
            sky.GalaxyTextures   = SkyTextures("galaxy");
            var holes            = SkyTextures("blackhole");
            sky.BlackHoleTexture = holes.Length > 0 ? holes[0] : null;

            var scoreGO = GetOrCreate("MissionScore");
            var score   = EnsureComp<MissionScore>(scoreGO);

            remote.ARCamera      = cam;
            remote.NameTagPrefab = NameTagPrefab();
            remote.ExhaustMaterial = ParticleMaterial();

            // ── Modelos GLB (requiere glTFast) ────────────────────────────────
            int models = NavigatorAssignModels.Assign();

            // ── GameManager — director de flujo ───────────────────────────────
            var gm = EnsureComp<GameManager>(gmGO);
            gm.Ship         = shipCtrl;
            gm.Physics      = physics;
            gm.ARShip       = arShip;
            gm.Senda        = senda;
            gm.SyncService  = sync;
            gm.Chat         = chat;
            gm.RemoteShips  = remote;
            gm.SpaceManager = spaceMgr;
            gm.Compass      = compass;
            gm.CameraFeed   = feed;
            gm.Rig          = rig;
            gm.Powerups     = powerups;
            gm.Asteroids    = asteroids;
            gm.Gravity      = gravity;
            gm.Free         = free;

            // Bridge to the Flutter app that embeds Unity (flutter_app/); no-op when run standalone
            EnsureComp<FlutterBridge>(GetOrCreate(FlutterBridge.ObjectName));
            gm.Score        = score;

            // ── UI completa ───────────────────────────────────────────────────
            bool uiBuilt = NavigatorUIBuilder.Build(gm) != null;

            EditorSceneManager.MarkAllScenesDirty();
            AddOpenSceneToBuild();

            Debug.Log($"[Navigator] Setup completo. Modelos asignados: {models}. UI: {uiBuilt}");
            if (!uiBuilt) return;   // the TMP importer dialog already explained what to do

            EditorUtility.DisplayDialog("Navigator Setup",
                "¡Escena lista para jugar!\n\n" +
                $"• Modelos GLB asignados: {models}" +
                (models == 0 ? "  ← instala glTFast (Package Manager) y vuelve a ejecutar\n" : "\n") +
                "• UI completa construida y conectada\n" +
                "• Pods, asteroides y explosiones creados\n\n" +
                "Guarda la escena (Ctrl+S) y pulsa Play.\n" +
                "Editor: clic izq/W = acelerar · Espacio = boost · clic der + arrastrar = mirar",
                "OK");
        }

        // ── Prefabs generados ─────────────────────────────────────────────────

        // Meshy models for the pods (NAVIGATOR_MESHY_ASSETS_V3.md); primitives until they exist
        static string PodModelPrefix(PowerupType type) => type switch
        {
            PowerupType.Fuel          => "PD-01",
            PowerupType.Boost         => "PD-02",
            PowerupType.Shield        => "PD-03",
            PowerupType.EMP           => "PD-04",
            PowerupType.GravityAssist => "PD-05",
            _                         => "PD-00",
        };

        const float PodSize = 6f;

        static GameObject PodPrefab(PowerupType type)
        {
            string path = $"{FxDir}/Pod_{type}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            // Generated assets: always rebuilt so fixes (script moves, new FX) reach old projects
            if (existing != null) AssetDatabase.DeleteAsset(path);

            Directory.CreateDirectory(FxDir);
            var pod = new GameObject($"Pod_{type}");
            var trigger = pod.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius    = PodSize * 0.6f;

            var p = pod.AddComponent<Powerup>();
            p.Type         = type;
            p.BobAmplitude = 1.5f;
            p.CollectFX    = PickupBurstPrefab();

            // Shape: the Meshy model if there is one, otherwise a primitive that differs per type,
            // so fuel, shield, boost… read at a glance even before the label
            var model = NavigatorAssignModels.LoadOptionalGLB(PodModelPrefix(type));
            GameObject visual;
            if (model != null)
            {
                visual = (GameObject)PrefabUtility.InstantiatePrefab(model, pod.transform);
                ModelFit.FitToSize(visual, PodSize);
                p.TintRenderer = false;
                p.PodRenderer  = visual.GetComponentInChildren<Renderer>();
            }
            else
            {
                visual = GameObject.CreatePrimitive(type switch
                {
                    PowerupType.Fuel   => PrimitiveType.Capsule,    // a fuel canister
                    PowerupType.Boost  => PrimitiveType.Cylinder,   // a flat booster disc
                    PowerupType.EMP    => PrimitiveType.Cube,       // a diamond
                    _                  => PrimitiveType.Sphere,     // shield bubble, gravity well
                });
                Object.DestroyImmediate(visual.GetComponent<Collider>());
                visual.transform.SetParent(pod.transform, false);
                visual.transform.localScale = type switch
                {
                    PowerupType.Fuel   => new Vector3(0.55f, 0.5f, 0.55f) * PodSize,
                    PowerupType.Boost  => new Vector3(1f, 0.18f, 1f) * PodSize,
                    PowerupType.EMP    => Vector3.one * PodSize * 0.6f,
                    PowerupType.GravityAssist => Vector3.one * PodSize * 0.7f,
                    _                  => Vector3.one * PodSize,
                };
                if (type == PowerupType.EMP) visual.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
                var r = visual.GetComponent<MeshRenderer>();
                r.sharedMaterial = GlowMaterial();
                p.PodRenderer = r;
            }
            visual.name = "Visual";
            p.Visual = visual.transform;

            // Soft halo in the pod's colour: visible from far away over the camera feed
            var halo = new GameObject("Halo");
            halo.transform.SetParent(pod.transform, false);
            var ps = halo.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.prewarm = true;
            main.startLifetime = 1.2f; main.startSpeed = 0f;
            main.startSize  = PodSize * 2.2f;
            main.startColor = new Color(p.TypeColor().r, p.TypeColor().g, p.TypeColor().b, 0.35f);
            main.maxParticles = 3;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var em = ps.emission; em.rateOverTime = 2f;
            var sh = ps.shape; sh.enabled = false;
            halo.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMaterial();

            // Name above it, in the player's language (set at runtime by Powerup)
            var label = new GameObject("Label").AddComponent<TextMeshPro>();
            label.transform.SetParent(pod.transform, false);
            label.transform.localPosition = new Vector3(0f, PodSize * 0.95f, 0f);
            label.text      = type.ToString();
            label.fontSize  = 14f;
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
            label.rectTransform.sizeDelta = new Vector2(30f, 5f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            p.LabelWorld = label;

            var prefab = PrefabUtility.SaveAsPrefabAsset(pod, path);
            Object.DestroyImmediate(pod);
            return prefab;
        }

        static GameObject PickupBurstPrefab()
        {
            string path = $"{FxDir}/PickupBurst.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            Directory.CreateDirectory(FxDir);
            var go = new GameObject("PickupBurst");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration        = 0.8f;
            main.loop            = false;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(6f, 18f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
            main.startColor      = new ParticleSystem.MinMaxGradient(new Color(0.4f, 0.9f, 1f), Color.white);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction      = ParticleSystemStopAction.Destroy;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 60) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 1f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleMaterial();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static Material GlowMaterial()
        {
            string path = $"{MatDir}/PodGlow.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;

            Directory.CreateDirectory(MatDir);
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.8f);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static GameObject ExplosionPrefab()
        {
            string path = $"{FxDir}/Explosion.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            Directory.CreateDirectory(FxDir);
            var go = new GameObject("Explosion");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration        = 1f;
            main.loop            = false;
            main.playOnAwake     = false;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(8f, 30f);
            main.startSize       = new ParticleSystem.MinMaxCurve(1f, 3.5f);
            main.startColor      = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.4f), new Color(1f, 0.35f, 0.05f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 90) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.5f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.4f, 0.1f), 0.4f),
                        new GradientColorKey(new Color(0.2f, 0.2f, 0.2f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.6f));

            var pr = go.GetComponent<ParticleSystemRenderer>();
            pr.sharedMaterial = ParticleMaterial();

            var light = new GameObject("Flash").AddComponent<Light>();
            light.transform.SetParent(go.transform, false);
            light.type      = LightType.Point;
            light.range     = 40f;
            light.color     = new Color(1f, 0.6f, 0.25f);
            light.intensity = 0f;

            var vfx = go.AddComponent<ExplosionVFX>();
            vfx.FlashLight = light;
            vfx.LifetimeS  = 2f;

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static Material ParticleMaterial()
        {
            string path = $"{MatDir}/ExplosionParticle.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;

            Directory.CreateDirectory(MatDir);
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                      ?? Shader.Find("Particles/Standard Unlit");
            mat = new Material(shader);
            var tex = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
            if (tex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                mat.mainTexture = tex;
            }
            // Additive blending: fiery glow over the camera feed
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend"))   mat.SetFloat("_Blend", 2f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        // Sky art dropped in Assets/Art/Sky (nebula_01.jpg, galaxy_01.png, blackhole_01.jpg…).
        // Real Hubble/JWST photos fill the frame edge to edge, which would show as squares in
        // the sky, so each one is baked once into Assets/Art/Sky/Baked: max 1024 px, the dark
        // background pulled to black, and an oval fade to transparent toward the edges.
        const string SkyDir = "Assets/Art/Sky", BakedDir = "Assets/Art/Sky/Baked";

        static Texture2D[] SkyTextures(string prefix)
        {
            var list = new System.Collections.Generic.List<Texture2D>();
            if (!AssetDatabase.IsValidFolder(SkyDir)) return list.ToArray();
            var sources = new System.Collections.Generic.List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { SkyDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetDirectoryName(path).Replace('\\', '/') != SkyDir) continue;   // skip Baked/
                if (!Path.GetFileName(path).StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase)) continue;
                sources.Add(path);
            }
            sources.Sort(System.StringComparer.OrdinalIgnoreCase);
            foreach (var path in sources)
            {
                var tex = BakeSkyTexture(path);
                if (tex != null) list.Add(tex);
            }
            return list.ToArray();
        }

        static Texture2D BakeSkyTexture(string srcPath)
        {
            string outPath = $"{BakedDir}/{Path.GetFileNameWithoutExtension(srcPath)}.png";
            if (File.Exists(outPath) && File.GetLastWriteTimeUtc(outPath) >= File.GetLastWriteTimeUtc(srcPath))
                return AssetDatabase.LoadAssetAtPath<Texture2D>(outPath);

            var src = AssetDatabase.LoadAssetAtPath<Texture2D>(srcPath);
            if (src == null) return null;

            const int MaxSize = 1024;
            float scale = Mathf.Min(1f, MaxSize / (float)Mathf.Max(src.width, src.height));
            int w = Mathf.Max(8, Mathf.RoundToInt(src.width * scale));
            int h = Mathf.Max(8, Mathf.RoundToInt(src.height * scale));

            // GPU copy: works whatever the importer's compression or read/write setting
            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var prev = RenderTexture.active;
            Graphics.Blit(src, rt);
            RenderTexture.active = rt;
            var outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            outTex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            outTex.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            var px = outTex.GetPixels();
            const float BlackPoint = 0.06f;   // sensor noise / sky glow → pure black
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w * 2f - 1f, v = (y + 0.5f) / h * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v);
                float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 0.98f, r));
                Color c = px[y * w + x];
                c.r = Mathf.Max(0f, c.r - BlackPoint) / (1f - BlackPoint);
                c.g = Mathf.Max(0f, c.g - BlackPoint) / (1f - BlackPoint);
                c.b = Mathf.Max(0f, c.b - BlackPoint) / (1f - BlackPoint);
                c.a = fade;
                px[y * w + x] = c;
            }
            outTex.SetPixels(px);
            outTex.Apply();

            Directory.CreateDirectory(BakedDir);
            File.WriteAllBytes(outPath, outTex.EncodeToPNG());
            Object.DestroyImmediate(outTex);
            AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);

            if (AssetImporter.GetAtPath(outPath) is TextureImporter ti)
            {
                ti.alphaIsTransparency = true;
                ti.wrapMode       = TextureWrapMode.Clamp;
                ti.maxTextureSize = MaxSize;
                ti.mipmapEnabled  = true;
                ti.SaveAndReimport();
            }
            Debug.Log($"[Navigator] Cielo: {Path.GetFileName(srcPath)} → {outPath} ({w}×{h}, bordes desvanecidos)");
            return AssetDatabase.LoadAssetAtPath<Texture2D>(outPath);
        }

        static GameObject NameTagPrefab()
        {
            string path = "Assets/Prefabs/UI/NameTag.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            Directory.CreateDirectory("Assets/Prefabs/UI");
            var go = new GameObject("NameTag");
            var label = new GameObject("Label").AddComponent<TextMeshPro>();
            label.transform.SetParent(go.transform, false);
            // LookAt points +Z at the camera, which would show the text mirrored
            label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            label.text      = "Rival";
            label.fontSize  = 8f;
            label.alignment = TextAlignmentOptions.Center;
            label.color     = UIStyle.Bad;
            label.fontStyle = FontStyles.Bold;
            label.rectTransform.sizeDelta = new Vector2(20f, 4f);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        static void AddOpenSceneToBuild()
        {
            string path = EditorSceneManager.GetActiveScene().path;
            if (string.IsNullOrEmpty(path)) return;
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == path);
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static GameObject GetOrCreate(string name)
        {
            var go = GameObject.Find(name);
            if (go == null) go = new GameObject(name);
            return go;
        }

        static T EnsureComp<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        static void EnsureTag(string tag)
        {
            var tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tagsProp = tagManager.FindProperty("tags");
            for (int i = 0; i < tagsProp.arraySize; i++)
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag) return;
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            tagManager.ApplyModifiedProperties();
        }
    }
}
#endif
