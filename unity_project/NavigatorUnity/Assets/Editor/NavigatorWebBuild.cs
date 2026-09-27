// Navigator 2.0 — NavigatorWebBuild.cs
// Menu Navigator → Web: the game as a web page that opens from a link and keeps working offline.
//   · Prepare: PWA template (installable, service worker caches the game → plays offline),
//     gzip with decompression fallback (works on GitHub Pages / itch.io / any static host
//     without special headers), data caching, and the native P2P library kept out of the
//     web build (the browser uses WebRTC: Plugins/WebGL/NavP2P.*).
//   · Build: Prepare + build to <project>/WebBuild (upload that folder as it is).
// Camera and gyroscope need HTTPS (every host above serves it) or localhost.
// Guacalita Inc. — SAL / Claude

using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Navigator.Editor
{
    public static class NavigatorWebBuild
    {
        const string NativeP2P = "Assets/Plugins/senda_core.dll";
        public const string OutputDir = "WebBuild";

        [MenuItem("Navigator/Web/Prepare (settings only)")]
        public static void Prepare()
        {
            PlayerSettings.WebGL.template              = "APPLICATION:PWA";
            PlayerSettings.WebGL.compressionFormat     = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching           = true;
            PlayerSettings.WebGL.nameFilesAsHashes     = true;   // every build has new file names: never mixed with a cached one
            PlayerSettings.WebGL.exceptionSupport      = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.powerPreference       = WebGLPowerPreference.HighPerformance;
            PlayerSettings.runInBackground             = true;   // keep the P2P link alive if the tab loses focus

            // The Iroh library is a Windows DLL: editor and desktop only, never the browser
            if (AssetImporter.GetAtPath(NativeP2P) is PluginImporter native)
            {
                native.SetCompatibleWithAnyPlatform(false);
                native.SetCompatibleWithEditor(true);
                native.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, true);
                native.SetCompatibleWithPlatform(BuildTarget.WebGL, false);
                native.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Navigator] Web ready: PWA (offline), gzip + fallback, WebRTC P2P. " +
                      "Build with Navigator → Web → Build, or your 'Web - Mobile - Release' profile.");
        }

        [MenuItem("Navigator/Web/Build")]
        public static void Build()
        {
            Prepare();
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                EditorUtility.DisplayDialog("Navigator", "No scenes in the build list. Run Navigator → Setup Scene first.", "OK");
                return;
            }
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputDir));
            // The offline cache is named after the version: a new one per build makes phones pick up
            // the update instead of mixing old and new files. The Android version is restored after.
            string version = PlayerSettings.bundleVersion;
            PlayerSettings.bundleVersion = $"{version}-web{System.DateTime.Now:yyyyMMddHHmm}";
            UnityEditor.Build.Reporting.BuildReport report;
            try
            {
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes           = scenes,
                    locationPathName = output,
                    target           = BuildTarget.WebGL,
                    options          = BuildOptions.None,
                });
            }
            finally { PlayerSettings.bundleVersion = version; }
            var summary = report.summary;
            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"[Navigator] Web build OK: {output} ({summary.totalSize / 1048576f:F0} MB). " +
                          "Upload the whole folder to GitHub Pages or itch.io (HTML5).");
                EditorUtility.RevealInFinder(output);
            }
            else Debug.LogError($"[Navigator] Web build failed: {summary.result} ({summary.totalErrors} errors). See the Console.");
        }
    }
}
