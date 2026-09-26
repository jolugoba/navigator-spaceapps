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

            var feedGO = GetOrCreate("CameraFeed");
            var feed   = EnsureComp<CameraFeedBackground>(feedGO);
            feed.ARCamera = cam;

            // ── GameManager GO — red ──────────────────────────────────────────
            var gmGO   = GetOrCreate("GameManager");
            var senda  = EnsureComp<SendaBridge>(gmGO);
            var sync   = EnsureComp<ShipSyncService>(gmGO);
            var chat   = EnsureComp<P2PChatSystem>(gmGO);
            var remote = EnsureComp<RemoteShipManager>(gmGO);

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

            var scoreGO = GetOrCreate("MissionScore");
            var score   = EnsureComp<MissionScore>(scoreGO);

            remote.ARCamera      = cam;
            remote.NameTagPrefab = NameTagPrefab();

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

        static GameObject PodPrefab(PowerupType type)
        {
            string path = $"{FxDir}/Pod_{type}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            Directory.CreateDirectory(FxDir);
            var pod = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pod.name = $"Pod_{type}";
            pod.transform.localScale = Vector3.one * 6f;
            var r = pod.GetComponent<MeshRenderer>();
            r.sharedMaterial = GlowMaterial();
            var p = pod.AddComponent<Powerup>();
            p.Type        = type;
            p.PodRenderer = r;
            p.BobAmplitude = 1.5f;

            var prefab = PrefabUtility.SaveAsPrefabAsset(pod, path);
            Object.DestroyImmediate(pod);
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
