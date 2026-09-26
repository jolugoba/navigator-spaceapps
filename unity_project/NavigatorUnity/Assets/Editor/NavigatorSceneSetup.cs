// Navigator 2.0 — NavigatorSceneSetup.cs
// Editor utility: crea y conecta toda la escena SpaceFlight con un clic
// Menu: Navigator → Setup Scene
// Sin AR Foundation — usa cámara estándar + giroscopio
// Guacalita Inc. — SAL / Claude

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
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
        [MenuItem("Navigator/Setup Scene")]
        public static void SetupScene()
        {
            // ── Main Camera ───────────────────────────────────────────────────
            var mainCamGO = GetOrCreate("Main Camera");
            mainCamGO.tag = "MainCamera";
            var cam = EnsureComp<Camera>(mainCamGO);
            cam.clearFlags = CameraClearFlags.Skybox;

            // ── GameManager GO — todos los scripts de red van aquí ───────────
            var gmGO   = GetOrCreate("GameManager");
            var senda  = EnsureComp<SendaBridge>(gmGO);
            var sync   = EnsureComp<ShipSyncService>(gmGO);
            var chat   = EnsureComp<P2PChatSystem>(gmGO);
            var remote = EnsureComp<RemoteShipManager>(gmGO);

            // ── LocalShip ─────────────────────────────────────────────────────
            var ship = GetOrCreate("LocalShip");
            EnsureTag("LocalShip");
            ship.tag = "LocalShip";

            var rb = EnsureComp<Rigidbody>(ship);
            rb.useGravity = false;
            rb.linearDamping = 0.02f;

            var shipCtrl  = EnsureComp<ShipController>(ship);
            var physics   = EnsureComp<PhysicsEngine>(ship);
            var fuel      = EnsureComp<FuelSystem>(ship);
            var burnout   = EnsureComp<BurnoutSystem>(ship);
            var collision = EnsureComp<CollisionSystem>(ship);
            EnsureComp<ShipCustomizer>(ship);
            EnsureComp<ShipAnimator>(ship);

            var arShip = EnsureComp<ARShipController>(ship);
            arShip.ARCamera = cam;

            // Wire ShipController.Physics
            shipCtrl.Physics = physics;
            physics.ShipRb   = rb;

            // ── ARCompassTracker ──────────────────────────────────────────────
            var compass     = GetOrCreate("ARCompassTracker");
            var compassComp = EnsureComp<ARCompassTracker>(compass);

            // ── ARSpaceManager ────────────────────────────────────────────────
            var spaceMgrGO = GetOrCreate("ARSpaceManager");
            var spaceMgr   = EnsureComp<ARSpaceManager>(spaceMgrGO);
            spaceMgr.ARCamera = cam;

            // ── ARSessionManager ──────────────────────────────────────────────
            var arSessionMgrGO = GetOrCreate("ARSessionManager");
            var arMgr          = EnsureComp<ARSessionManager>(arSessionMgrGO);
            arMgr.ARCamera       = cam;
            arMgr.ShipController = arShip;
            arMgr.SpaceManager   = spaceMgr;

            // ── ARCompassTracker refs ─────────────────────────────────────────
            compassComp.SpaceManager = spaceMgr;

            // ── RemoteShipManager camera ref ──────────────────────────────────
            remote.ARCamera = cam;

            // ── CockpitHUD Canvas ─────────────────────────────────────────────
            var hudGO = GetOrCreate("CockpitHUD");
            if (hudGO.GetComponent<Canvas>() == null)
            {
                var canvas = hudGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                hudGO.AddComponent<CanvasScaler>();
                hudGO.AddComponent<GraphicRaycaster>();
            }
            EnsureComp<CockpitHUD>(hudGO);

            // ── PowerupSystem ─────────────────────────────────────────────────
            var powerupGO  = GetOrCreate("PowerupSystem");
            var powerupSys = EnsureComp<PowerupSystem>(powerupGO);
            powerupSys.ARCamera = cam;

            // ── MissionScore ──────────────────────────────────────────────────
            var scorerGO = GetOrCreate("MissionScore");
            EnsureComp<MissionScore>(scorerGO);

            // ── GameManager — director de flujo ───────────────────────────────
            var gameManager      = EnsureComp<GameManager>(gmGO);
            gameManager.Ship         = ship.GetComponent<ShipController>();
            gameManager.Physics      = ship.GetComponent<PhysicsEngine>();
            gameManager.ARShip       = ship.GetComponent<ARShipController>();
            gameManager.Senda        = senda;
            gameManager.SyncService  = sync;
            gameManager.Chat         = chat;
            gameManager.SpaceManager = spaceMgr;
            gameManager.Compass      = compassComp;
            gameManager.HUD          = hudGO.GetComponent<CockpitHUD>();

            var mduiGO = GetOrCreate("MissionDesignUI");
            gameManager.MissionDesignUI = EnsureComp<MissionDesignUI>(mduiGO);
            var sbGO = GetOrCreate("ScoreboardUI");
            gameManager.ScoreboardUI = EnsureComp<ScoreboardUI>(sbGO);
            var sciGO = GetOrCreate("ScienceOpsUI");
            gameManager.ScienceOpsUI = EnsureComp<ScienceOpsUI>(sciGO);

            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();

            Debug.Log("[Navigator] Scene setup completo.");
            EditorUtility.DisplayDialog(
                "Navigator Setup",
                "Escena configurada!\n\n" +
                "Pasos siguientes en el Inspector:\n" +
                "1. LocalShip → asigna NV-01_FINAL.glb como hijo\n" +
                "2. RemoteShipManager → RemoteShipPrefab = NV-02_FINAL.glb\n" +
                "3. ARSpaceManager → asigna planetas PL-01 a PL-07\n" +
                "4. PowerupSystem → asigna prefabs de pods\n" +
                "5. File → Save As → SpaceFlight.unity",
                "OK");
        }

        static GameObject GetOrCreate(string name)
        {
            var go = GameObject.Find(name);
            if (go == null) go = new GameObject(name);
            return go;
        }

        static GameObject GetOrCreateChild(GameObject parent, string name)
        {
            var t = parent.transform.Find(name);
            if (t != null) return t.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        static T EnsureComp<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        static void EnsureTag(string tag)
        {
            var tagManager = new UnityEditor.SerializedObject(
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
