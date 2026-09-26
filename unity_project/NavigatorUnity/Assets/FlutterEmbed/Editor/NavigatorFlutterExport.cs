// Navigator 2.0 — NavigatorFlutterExport.cs
// One click: configure the Player for "Unity as a Library" and export straight into the
// Flutter app of this repo (flutter_app/android/unityLibrary or flutter_app/ios/unityLibrary).
// Lives next to the flutter_embed_unity exporter so it can reuse it (same assembly).
// Menu: Navigator → Flutter → Export Android / Export iOS
// Guacalita Inc. — SAL / Claude

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

internal static class NavigatorFlutterExport
{
    // <repo>/unity_project/NavigatorUnity/Assets → <repo>/flutter_app
    static string FlutterApp => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", "flutter_app"));

    [MenuItem("Navigator/Flutter/Export Android → flutter_app", false, 100)]
    static void ExportAndroid()
    {
        if (!CheckFlutterApp()) return;
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            if (!EditorUtility.DisplayDialog("Navigator → Flutter",
                    "Hay que cambiar la plataforma a Android (tarda unos minutos la primera vez).\n" +
                    "Cuando termine, vuelve a elegir este mismo menú.", "Cambiar a Android", "Cancelar")) return;
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            return;
        }

        ConfigureCommon(NamedBuildTarget.Android);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
#if UNITY_6000_0_OR_NEWER
        PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;   // the plugin needs Activity, not GameActivity
#endif
        if ((int)PlayerSettings.Android.minSdkVersion < 26) PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)26;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = true;

        string exportPath = Path.Combine(FlutterApp, "android", "unityLibrary");
        if (!PrepareFolder(exportPath)) return;

#if UNITY_ANDROID
        new ProjectExporterAndroid().Export(Options(exportPath, BuildTarget.Android), new List<string>());
        if (File.Exists(Path.Combine(exportPath, "build.gradle")))
        {
            CopyStreamingAssetsProperty(exportPath);
            SyncMinSdk();
            EditorUtility.DisplayDialog("Navigator → Flutter",
                "Listo: Unity quedó exportado en\n" + exportPath + "\n\nAhora, en la carpeta flutter_app:\n" +
                "  flutter pub get\n  flutter run", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Navigator → Flutter", "La exportación falló. Revisa la Console.", "OK");
        }
#endif
    }

    [MenuItem("Navigator/Flutter/Export iOS → flutter_app", false, 101)]
    static void ExportIos()
    {
        if (!CheckFlutterApp()) return;
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
        {
            if (!EditorUtility.DisplayDialog("Navigator → Flutter",
                    "Hay que cambiar la plataforma a iOS (requiere el módulo iOS de Unity).\n" +
                    "Cuando termine, vuelve a elegir este mismo menú.", "Cambiar a iOS", "Cancelar")) return;
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
            return;
        }

        ConfigureCommon(NamedBuildTarget.iOS);
        string exportPath = Path.Combine(FlutterApp, "ios", "unityLibrary");
        if (!PrepareFolder(exportPath)) return;

#if UNITY_IOS
        new ProjectExporterIos().Export(Options(exportPath, BuildTarget.iOS), new List<string>());
        EditorUtility.DisplayDialog("Navigator → Flutter",
            "Unity quedó exportado en\n" + exportPath + "\n\nSigue los pasos de iOS en flutter_app/README.md (Xcode).", "OK");
#endif
    }

    static void ConfigureCommon(NamedBuildTarget target)
    {
        PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetIl2CppCodeGeneration(target, Il2CppCodeGeneration.OptimizeSpeed);
    }

    static bool CheckFlutterApp()
    {
        if (File.Exists(Path.Combine(FlutterApp, "pubspec.yaml"))) return true;
        EditorUtility.DisplayDialog("Navigator → Flutter", "No se encontró la app Flutter en\n" + FlutterApp, "OK");
        return false;
    }

    static bool PrepareFolder(string path)
    {
        if (Directory.Exists(path) && Directory.EnumerateFileSystemEntries(path).Any())
        {
            if (!EditorUtility.DisplayDialog("Navigator → Flutter",
                    "Se reemplazará la exportación anterior en\n" + path, "Reemplazar", "Cancelar")) return false;
            Directory.Delete(path, true);
        }
        Directory.CreateDirectory(path);
        return true;
    }

    static BuildPlayerOptions Options(string path, BuildTarget target) => new BuildPlayerOptions
    {
        scenes           = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
        locationPathName = path,
        target           = target,
        options          = BuildOptions.None,
    };

    // The app's minSdk can't be lower than the Unity library's ("Minimum API Level")
    static void SyncMinSdk()
    {
        string gradle = Path.Combine(FlutterApp, "android", "app", "build.gradle.kts");
        if (!File.Exists(gradle)) return;
        int unityMin = (int)PlayerSettings.Android.minSdkVersion;
        string text = File.ReadAllText(gradle);
        var m = Regex.Match(text, @"minSdk\s*=\s*(\d+)");
        if (!m.Success || int.Parse(m.Groups[1].Value) >= unityMin) return;
        File.WriteAllText(gradle, text.Substring(0, m.Groups[1].Index) + unityMin + text.Substring(m.Groups[1].Index + m.Groups[1].Length));
        Debug.Log($"[Navigator] flutter_app minSdk → {unityMin} (same as Unity)");
    }

    // The exported library reads unityStreamingAssets from the host app's gradle.properties
    static void CopyStreamingAssetsProperty(string exportPath)
    {
        string exported = Path.Combine(exportPath, "gradle.properties");
        string host     = Path.Combine(FlutterApp, "android", "gradle.properties");
        if (!File.Exists(exported) || !File.Exists(host)) return;

        var m = Regex.Match(File.ReadAllText(exported), @"^unityStreamingAssets=(.*)$", RegexOptions.Multiline);
        string line = "unityStreamingAssets=" + (m.Success ? m.Groups[1].Value.Trim() : "");
        string text = File.ReadAllText(host);
        text = Regex.IsMatch(text, @"^unityStreamingAssets=.*$", RegexOptions.Multiline)
            ? Regex.Replace(text, @"^unityStreamingAssets=.*$", line, RegexOptions.Multiline)
            : text.TrimEnd() + "\n" + line + "\n";
        File.WriteAllText(host, text);
        Debug.Log("[Navigator] " + line + " → " + host);
    }
}
