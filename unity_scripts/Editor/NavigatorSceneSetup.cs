// Navigator 2.0 — NavigatorSceneSetup.cs
// Editor utility: crea y conecta toda la escena SpaceFlight con un clic
// Menu: Navigator → Setup Scene
// Guacalita Inc. — SAL / Claude

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.XR.ARFoundation;
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
            // ── GameManager GO — todos los Network scripts van aquí ───────────
            var gmGO   = GetOrCreate("GameManager");
            var senda  = EnsureComp<SendaBridge>(gmGO);
            var sync   = EnsureComp<ShipSyncService>(gmGO);
            var chat   = EnsureComp<P2PChatSystem>(gmGO);
            var remote = EnsureComp<RemoteShipManager>(gmGO);

            // ── AR Session ────────────────────────────────────────────────────
            var arSession = GetOrCreate("AR Session");
            EnsureComp<ARSession>(arSession);

            // ── XR Origin ─────────────────────────────────────────────────────
            var xrOrigin = GetOrCreate("XR Origin");
            var camOffset = GetOrCreateChild(xrOrigin, "Camera Offset");
            var mainCamGO = GetOrCreateChild(camOffset, "Main Camera");
            mainCamGO.tag = "MainCamera";
            var cam = EnsureComp<Camera>(mainCamGO);
            EnsureComp<ARCameraManager>(mainCamGO);
            EnsureComp<ARCameraBackground>(mainCamGO);

            // ── LocalShip ─────────────────────────────────────────────────────
            var ship = GetOrCreate("LocalShip");
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

            // Wire ShipController.Physics
            shipCtrl.Physics = physics;
            physics.ShipRb   = rb;

            // ── ARCompassTracker ──────────────────────────────────────────────
            var compass = GetOrCreate("ARCompassTracker");
            var compassComp = EnsureComp<ARCompassTracker>(compass);

            // ── ARSpaceManager ────────────────────────────────────────────────
            var spaceMgrGO = GetOrCreate("ARSpaceManager");
            var spaceMgr   = EnsureComp<ARSpaceManager>(spaceMgrGO);

            // ── ARSessionManager ──────────────────────────────────────────────
            var arSessionMgr = GetOrCreate("ARSessionManager");
            var arMgr = EnsureComp<ARSessionManager>(arSessionMgr);
            arMgr.Session        = arSession.GetComponent<ARSession>();
            arMgr.CameraManager  = mainCamGO.GetComponent<ARCameraManager>();
            arMgr.ShipController = arShip;
            arMgr.SpaceManager   = spaceMgr;

            // ── ARShipController camera ref ───────────────────────────────────
            arShip.ARCamera = cam;

            // ── RemoteShipManager camera ref ──────────────────────────────────
            remote.ARCamera = cam;

            // ── ARCompassTracker refs ─────────────────────────────────────────
            compassComp.SpaceManager = spaceMgr;

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
            var powerupGO = GetOrCreate("PowerupSystem");
            var powerupSys = EnsureComp<PowerupSystem>(powerupGO);
            powerupSys.ARCamera = cam;

            // ── MissionScore ──────────────────────────────────────────────────
            var scorerGO = GetOrCreate("MissionScore");
            EnsureComp<MissionScore>(scorerGO);

            // ── GameManager — director de flujo (mismo GO que los Network scripts)
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

            // Canvas UI del GameManager
            var mduiGO = GetOrCreate("MissionDesignUI");
            gameManager.MissionDesignUI = EnsureComp<MissionDesignUI>(mduiGO);
            var sbGO = GetOrCreate("ScoreboardUI");
            gameManager.ScoreboardUI = EnsureComp<ScoreboardUI>(sbGO);
            var sciGO = GetOrCreate("ScienceOpsUI");
            gameManager.ScienceOpsUI = EnsureComp<ScienceOpsUI>(sciGO);

            // ── Mark dirty ────────────────────────────────────────────────────
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();

            Debug.Log("[Navigator] Scene setup completo. Asigna los prefabs en el Inspector y guarda la escena.");
            EditorUtility.DisplayDialog(
                "Navigator Setup",
                "¡Escena configurada!\n\n" +
                "Pasos restantes en el Inspector:\n" +
                "1. Asigna RemoteShipPrefab (NV-02) en RemoteShipManager\n" +
                "2. Asigna modelos/sub-meshes en ShipAnimator\n" +
                "3. Asigna planetas en ARSpaceManager\n" +
                "4. Asigna prefabs de pods en PowerupSystem\n" +
                "5. Copia senda_core.dll a Assets/Plugins/\n" +
                "6. File → Save As → SpaceFlight.unity",
                "OK");
        }

        // ── Helpers ────────────────────────────────────────────────────────────

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
    }
}
#endif
