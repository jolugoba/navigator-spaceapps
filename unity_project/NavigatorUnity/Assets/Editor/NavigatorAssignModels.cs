// Navigator 2.0 — NavigatorAssignModels.cs
// Asigna automáticamente todos los GLBs a sus componentes en la escena
// Menu: Navigator → Assign Models
// Guacalita Inc. — SAL / Claude

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Navigator.AR;
using Navigator.Game;
using Navigator.Network;

namespace Navigator.Editor
{
    public static class NavigatorAssignModels
    {
        [MenuItem("Navigator/Assign Models")]
        public static void AssignModels()
        {
            int assigned = 0;

            // ── LocalShip → NV-01 como hijo ──────────────────────────────────
            var localShip = GameObject.Find("LocalShip");
            if (localShip != null)
            {
                var nv01 = LoadGLB("NV-01_FINAL");
                if (nv01 != null && localShip.transform.Find("NV-01_FINAL") == null)
                {
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(nv01, localShip.transform);
                    inst.transform.localPosition = Vector3.zero;
                    inst.transform.localRotation = Quaternion.identity;
                    inst.transform.localScale    = Vector3.one * 0.5f;
                    assigned++;
                    Debug.Log("[Navigator] NV-01 asignado a LocalShip");
                }
            }

            // ── GameManager → RemoteShipManager.RemoteShipPrefab = NV-02 ─────
            var gmGO = GameObject.Find("GameManager");
            if (gmGO != null)
            {
                var rsm = gmGO.GetComponent<RemoteShipManager>();
                if (rsm != null)
                {
                    var nv02 = LoadGLB("NV-02_FINAL");
                    if (nv02 != null) { rsm.RemoteShipPrefab = nv02; assigned++; }
                }
            }

            // ── ARSpaceManager → planetas ─────────────────────────────────────
            var spaceMgrGO = GameObject.Find("ARSpaceManager");
            if (spaceMgrGO != null)
            {
                var sm = spaceMgrGO.GetComponent<ARSpaceManager>();
                if (sm != null)
                {
                    sm.PlanetMercury = LoadGLB("PL-01"); assigned++;
                    sm.PlanetVenus   = LoadGLB("PL-02"); assigned++;
                    sm.PlanetMars    = LoadGLB("PL-03"); assigned++;
                    sm.PlanetJupiter = LoadGLB("PL-04"); assigned++;
                    sm.PlanetSaturn  = LoadGLB("PL-05"); assigned++;
                    sm.PlanetUranus  = LoadGLB("PL-06"); assigned++;
                    sm.PlanetNeptune = LoadGLB("PL-07"); assigned++;
                    Debug.Log("[Navigator] Planetas asignados a ARSpaceManager");
                }
            }

            // ── ARShipController → ARCamera ───────────────────────────────────
            var shipGO = GameObject.Find("LocalShip");
            var arShip = shipGO?.GetComponent<ARShipController>();
            var mainCam = GameObject.Find("Main Camera")?.GetComponent<Camera>();
            if (arShip != null && mainCam != null) { arShip.ARCamera = mainCam; assigned++; }

            // ── PowerupSystem → ARCamera ──────────────────────────────────────
            var pwGO = GameObject.Find("PowerupSystem");
            var pw   = pwGO?.GetComponent<PowerupSystem>();
            if (pw != null && mainCam != null) { pw.ARCamera = mainCam; assigned++; }

            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();

            EditorUtility.DisplayDialog(
                "Navigator — Modelos asignados",
                $"{assigned} asignaciones completadas.\n\n" +
                "Revisa en el Inspector:\n" +
                "• LocalShip → NV-01 como hijo\n" +
                "• GameManager (RemoteShipManager) → NV-02\n" +
                "• ARSpaceManager → PL-01…PL-07\n\n" +
                "Guarda la escena: Ctrl+S",
                "OK");
        }

        static GameObject LoadGLB(string namePrefix)
        {
            string[] guids = AssetDatabase.FindAssets(namePrefix, new[] { "Assets/Models" });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".glb", System.StringComparison.OrdinalIgnoreCase))
                {
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (go != null) return go;
                }
            }
            Debug.LogWarning($"[Navigator] No encontrado: {namePrefix}.glb en Assets/Models/");
            return null;
        }
    }
}
#endif
