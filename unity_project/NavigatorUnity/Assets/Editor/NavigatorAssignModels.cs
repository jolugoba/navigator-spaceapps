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
                ModelFit.AlignLongAxisToForward(inst, NoseFlip("NV-01"));
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
                var probe = (GameObject)PrefabUtility.InstantiatePrefab(nv02);
                rsm.ModelYaw = ModelFit.AlignLongAxisToForward(probe, NoseFlip("NV-02"));
                Object.DestroyImmediate(probe);
                EditorUtility.SetDirty(rsm);
                assigned++;
            }

            // ── Hangar: every NV-xx ship the player can pick ──────────────────
            var catalog = Object.FindFirstObjectByType<ShipCatalog>();
            if (catalog != null)
            {
                var models = new System.Collections.Generic.List<GameObject>();
                var yaws   = new System.Collections.Generic.List<float>();
                for (int i = 1; i <= MaxShips; i++)
                {
                    string id = $"NV-{i:00}";
                    var glb = LoadOptionalGLB(id);
                    if (glb == null) break;   // the hangar is NV-01 … the last consecutive one
                    var probe = (GameObject)PrefabUtility.InstantiatePrefab(glb);
                    yaws.Add(ModelFit.AlignLongAxisToForward(probe, NoseFlip(id)));
                    Object.DestroyImmediate(probe);
                    models.Add(glb);
                }
                catalog.Models   = models.ToArray();
                catalog.Yaw      = yaws.ToArray();
                catalog.ShipSize = ShipSize;
                EditorUtility.SetDirty(catalog);
                assigned += models.Count;
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
                // Optional bodies — skipped quietly when GLB is absent
                if (AssetDatabase.FindAssets("PL-08", new[] { "Assets/Models" }).Length > 0)
                    assigned += Set(ref sm.PlanetEarth, "PL-08");
                if (AssetDatabase.FindAssets("PL-09", new[] { "Assets/Models" }).Length > 0)
                    assigned += Set(ref sm.PlanetMoon, "PL-09");
                if (AssetDatabase.FindAssets("PL-10", new[] { "Assets/Models" }).Length > 0)
                    assigned += Set(ref sm.PlanetPluto, "PL-10");
                if (AssetDatabase.FindAssets("PL-11", new[] { "Assets/Models" }).Length > 0)
                    assigned += Set(ref sm.PlanetEuropa, "PL-11");
                // Landmarks to scan: skipped quietly until each model exists
                assigned += Opt(ref sm.ProbeMessenger, "OB-11");
                assigned += Opt(ref sm.ProbeMagellan,  "OB-12");
                assigned += Opt(ref sm.ProbeMRO,       "OB-13");
                assigned += Opt(ref sm.ProbeJuno,      "OB-08");
                assigned += Opt(ref sm.ProbeCassini,   "OB-07");
                assigned += Opt(ref sm.ProbeVoyager,   "OB-06");
                assigned += Opt(ref sm.Hubble,         "OB-14");
                assigned += Opt(ref sm.Jwst,           "OB-15");
                assigned += Opt(ref sm.Iss,            "OB-16");
                assigned += Opt(ref sm.Dart,           "OB-17");
                assigned += Opt(ref sm.RocketSLS,      "LV-01");
                assigned += Opt(ref sm.RoverPerseverance, "OB-18");
                assigned += Opt(ref sm.MoonPhobos,     "MN-01");
                assigned += Opt(ref sm.MoonIo,         "MN-02");
                assigned += Opt(ref sm.MoonTitan,      "MN-03");
                assigned += Opt(ref sm.MoonEnceladus,  "MN-04");
                assigned += Opt(ref sm.MoonMiranda,    "MN-05");
                assigned += Opt(ref sm.MoonTriton,     "MN-06");
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

        // Vehicles and props of the 7 worlds (NAVIGATOR_MESHY_MUNDOS.md); all optional
        public static int AssignWorlds(Navigator.Worlds.WorldAssets a)
        {
            if (a == null) return 0;
            int n = 0;
            n += Opt(ref a.Lander,          "VH-01");
            n += Opt(ref a.ThermalRover,    "VH-02");
            n += Opt(ref a.EntryCapsule,    "VH-03");
            n += Opt(ref a.VenusLander,     "VH-04");
            n += Opt(ref a.SkyCrane,        "VH-05");
            n += Opt(ref a.AtmoProbe,       "VH-06");
            n += Opt(ref a.Dragonfly,       "VH-07");
            n += Opt(ref a.UranusOrbiter,   "VH-08");
            n += Opt(ref a.Parachute,       "VH-09");
            n += Opt(ref a.Hopper,          "VH-10");
            n += Opt(ref a.Rover,           "OB-18");
            n += Opt(ref a.IceChunk,        "PR-01");
            n += Opt(ref a.MercuryBoulder,  "PR-02");
            n += Opt(ref a.VenusSlab,       "PR-03");
            n += Opt(ref a.MarsLayeredRock, "PR-04");
            n += Opt(ref a.SampleTube,      "PR-05");
            n += Opt(ref a.MarsBoulder,     "PR-06");
            n += Opt(ref a.TitanPebbles,    "PR-07");
            n += Opt(ref a.HeatShield,      "PR-08");
            n += Opt(ref a.GeyserVent,      "PR-09");
            n += Opt(ref a.TritonRock,      "PR-10");
            n += Opt(ref a.Beacon,          "OB-03");
            a.MercuryRocks = LoadAllGLB("PR-02");
            a.VenusRocks   = LoadAllGLB("PR-03");
            a.MarsRocks    = LoadAllGLB("PR-06", "PR-04");
            a.TitanRocks   = LoadAllGLB("PR-07");
            a.TritonRocks  = LoadAllGLB("PR-10");
            return n;
        }

        // Which end is the nose can't be measured; "Flip Ship Nose" toggles it and remembers
        static float NoseFlip(string model) => EditorPrefs.GetBool("Navigator.NoseFlip." + model, false) ? 180f : 0f;

        const int MaxShips = 20;

        // A hangar ship flying backwards (engine glow in front): pick it in the game's hangar,
        // then use this menu; it remembers the flip for that model only
        [MenuItem("Navigator/Flip Nose of Ship Selected in Hangar")]
        public static void FlipSelectedShip()
        {
            int index = PlayerPrefs.GetInt("navigator.ship", 0);
            string id = $"NV-{index + 1:00}";
            EditorPrefs.SetBool("Navigator.NoseFlip." + id, !EditorPrefs.GetBool("Navigator.NoseFlip." + id, false));
            int n = Assign();
            Debug.Log($"[Navigator] Nariz de {id} girada 180° ({n} modelos reasignados). Guarda la escena.");
        }

        [MenuItem("Navigator/Flip Ship Nose 180°")]
        public static void FlipNose()
        {
            foreach (var m in new[] { "NV-01", "NV-02" })
                EditorPrefs.SetBool("Navigator.NoseFlip." + m, !EditorPrefs.GetBool("Navigator.NoseFlip." + m, false));
            int n = Assign();
            Debug.Log($"[Navigator] Nariz de las naves girada 180° ({n} modelos reasignados). Guarda la escena.");
        }

        static int Opt(ref GameObject slot, string prefix)
        {
            var go = LoadOptionalGLB(prefix);
            if (go == null) return 0;
            slot = go;
            return 1;
        }

        static int Set(ref GameObject slot, string prefix)
        {
            var go = LoadGLB(prefix);
            if (go == null) return 0;
            slot = go;
            return 1;
        }

        // Optional models: null (and no warning) when nothing with that prefix was dropped in
        public static GameObject LoadOptionalGLB(string namePrefix) =>
            AssetDatabase.FindAssets(namePrefix, new[] { "Assets/Models" }).Length > 0 ? LoadGLB(namePrefix) : null;

        // Every "_FINAL" variant of these codes (e.g. PR-02_MercuryBoulder_FINAL, PR-02_..._b_FINAL);
        // a code with no _FINAL contributes its one fallback model, if any
        public static GameObject[] LoadAllGLB(params string[] prefixes)
        {
            var list = new System.Collections.Generic.List<GameObject>();
            foreach (var prefix in prefixes)
            {
                var paths = new System.Collections.Generic.List<string>();
                foreach (var guid in AssetDatabase.FindAssets(prefix, new[] { "Assets/Models" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    string file = System.IO.Path.GetFileNameWithoutExtension(path);
                    if (!path.EndsWith(".glb", System.StringComparison.OrdinalIgnoreCase)) continue;
                    if (!file.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase)) continue;
                    if (!file.EndsWith("_FINAL", System.StringComparison.OrdinalIgnoreCase)) continue;
                    if (!paths.Contains(path)) paths.Add(path);
                }
                paths.Sort(System.StringComparer.OrdinalIgnoreCase);
                int before = list.Count;
                foreach (var path in paths)
                {
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (go != null) list.Add(go);
                }
                if (list.Count == before)
                {
                    var one = LoadOptionalGLB(prefix);
                    if (one != null) list.Add(one);
                }
            }
            return list.ToArray();
        }

        // Prefers the "_FINAL" (PBR-textured) export when both exist
        static GameObject LoadGLB(string namePrefix)
        {
            GameObject fallback = null;
            string broken = null;
            foreach (var guid in AssetDatabase.FindAssets(namePrefix, new[] { "Assets/Models" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = System.IO.Path.GetFileNameWithoutExtension(path);
                if (!path.EndsWith(".glb", System.StringComparison.OrdinalIgnoreCase)) continue;
                if (!file.StartsWith(namePrefix, System.StringComparison.OrdinalIgnoreCase)) continue;

                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) { broken = path + " — " + GlbProblem(path); continue; }
                if (file.EndsWith("_FINAL", System.StringComparison.OrdinalIgnoreCase)) return go;
                fallback ??= go;
            }
            if (fallback == null)
                Debug.LogWarning(broken != null
                    ? $"[Navigator] {broken}. Se usa una forma simple en su lugar."
                    : $"[Navigator] No se pudo cargar {namePrefix}*.glb — ¿glTFast instalado?");
            return fallback;
        }

        // Why a .glb didn't import: Git LFS pointer, truncated download, or something else
        static string GlbProblem(string path)
        {
            try
            {
                var info = new System.IO.FileInfo(path);
                using var fs = System.IO.File.OpenRead(path);
                var head = new byte[12];
                int n = fs.Read(head, 0, 12);
                string magic = n >= 4 ? System.Text.Encoding.ASCII.GetString(head, 0, 4) : "";
                if (magic == "vers") return "es solo un puntero de Git LFS: ejecuta 'git lfs pull'";
                if (magic != "glTF") return "no es un archivo GLB válido";
                uint declared = System.BitConverter.ToUInt32(head, 8);
                if (declared > info.Length)
                    return $"archivo incompleto ({info.Length / 1048576f:F1} MB de {declared / 1048576f:F1} MB): vuelve a descargarlo de Meshy";
                return "glTFast no pudo importarlo (revisa la consola del importador)";
            }
            catch (System.Exception e) { return e.Message; }
        }
    }
}
#endif
