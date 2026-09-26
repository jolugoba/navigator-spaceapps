// Navigator 2.0 — NavigatorAssignModels.cs
// Asigna los GLB de Meshy a la escena (nave, rival, planetas, asteroides)
// Menu: Navigator → Assign Models  (Setup Scene también lo ejecuta)
// Requiere glTFast (com.unity.cloud.gltfast) para que Unity importe los .glb
// Guacalita Inc. — SAL / Claude

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Navigator.Core;
using Navigator.AR;
using Navigator.Game;

namespace Navigator.Editor
{
    public static class NavigatorAssignModels
    {
        public const float ShipSize = 3f;   // largest side of the ship model, in game units

        [MenuItem("Navigator/Assign Models")]
        public static void AssignMenu()
        {
            int n = Assign();
            EditorUtility.DisplayDialog("Navigator — Modelos",
                n > 0 ? $"{n} modelos asignados. Guarda la escena (Ctrl+S)."
                      : "No se encontró ningún modelo.\n¿Está instalado glTFast (com.unity.cloud.gltfast)?\n" +
                        "Sin él Unity no importa los .glb.",
                "OK");
        }

        public static int Assign()
        {
            int assigned = 0;

            // ── Nave local → NV-01 como hijo, tamaño normalizado ─────────────
            var localShip = GameObject.Find("LocalShip");
            var nv01 = LoadGLB("NV-01");
            if (localShip != null && nv01 != null)
            {
                var old = localShip.transform.Find("Model");
                if (old != null) Object.DestroyImmediate(old.gameObject);

                var inst = (GameObject)PrefabUtility.InstantiatePrefab(nv01, localShip.transform);
                inst.name = "Model";
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                ModelFit.FitToSize(inst, ShipSize);
                assigned++;
            }

            // ── Rival → NV-02 ─────────────────────────────────────────────────
            var rsm = Object.FindFirstObjectByType<RemoteShipManager>();
            var nv02 = LoadGLB("NV-02");
            if (rsm != null && nv02 != null)
            {
                rsm.RemoteShipPrefab = nv02;
                rsm.ShipSize = ShipSize;
                EditorUtility.SetDirty(rsm);
                assigned++;
            }

            // ── Planetas ──────────────────────────────────────────────────────
            var sm = Object.FindFirstObjectByType<ARSpaceManager>();
            if (sm != null)
            {
                assigned += Set(ref sm.PlanetMercury, "PL-01");
                assigned += Set(ref sm.PlanetVenus,   "PL-02");
                assigned += Set(ref sm.PlanetMars,    "PL-03");
                assigned += Set(ref sm.PlanetJupiter, "PL-04");
                assigned += Set(ref sm.PlanetSaturn,  "PL-05");
                assigned += Set(ref sm.PlanetUranus,  "PL-06");
                assigned += Set(ref sm.PlanetNeptune, "PL-07");
                EditorUtility.SetDirty(sm);
            }

            // ── Asteroides ────────────────────────────────────────────────────
            var field = Object.FindFirstObjectByType<AsteroidField>();
            if (field != null)
            {
                assigned += Set(ref field.AsteroidPrefab, "OB-01");
                EditorUtility.SetDirty(field);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            return assigned;
        }

        static int Set(ref GameObject slot, string prefix)
        {
            var go = LoadGLB(prefix);
            if (go == null) return 0;
            slot = go;
            return 1;
        }

        // Prefers the "_FINAL" (PBR-textured) export when both exist
        static GameObject LoadGLB(string namePrefix)
        {
            GameObject fallback = null;
            foreach (var guid in AssetDatabase.FindAssets(namePrefix, new[] { "Assets/Models" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = System.IO.Path.GetFileNameWithoutExtension(path);
                if (!path.EndsWith(".glb", System.StringComparison.OrdinalIgnoreCase)) continue;
                if (!file.StartsWith(namePrefix, System.StringComparison.OrdinalIgnoreCase)) continue;

                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) continue;
                if (file.EndsWith("_FINAL", System.StringComparison.OrdinalIgnoreCase)) return go;
                fallback ??= go;
            }
            if (fallback == null)
                Debug.LogWarning($"[Navigator] No se pudo cargar {namePrefix}*.glb — ¿glTFast instalado?");
            return fallback;
        }
    }
}
#endif
