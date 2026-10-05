using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// APK builds from the menu or batch mode (plan phase 13).
    /// Batch: tools/unity-batch.sh exec AuraKnight.Editor.BuildScript.BuildDevelopmentApk
    /// Release signing is configured locally in Player Settings; the keystore never lives in the repo.
    /// </summary>
    public static class BuildScript
    {
        const string OutputDir = "Builds/Android";

        [MenuItem("Aura/Build/Development APK")]
        public static void BuildDevelopmentApk() => Build($"{OutputDir}/AuraKnight-dev.apk", BuildOptions.Development);

        [MenuItem("Aura/Build/Release APK")]
        public static void BuildReleaseApk()
        {
            if (!IsReleaseSigningConfigured(out string problem))
            {
                // Without a keystore Unity silently signs with the debug key: refuse instead of shipping that.
                Debug.LogError($"[BuildScript] Release build refused: {problem}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }
            Build($"{OutputDir}/AuraKnight.apk", BuildOptions.None);
        }

        /// <summary>True when Player Settings carry a custom keystore (file present, passwords and alias set).</summary>
        public static bool IsReleaseSigningConfigured(out string problem)
        {
            problem = null;
            if (!PlayerSettings.Android.useCustomKeystore) problem = "no custom keystore selected in Player Settings > Publishing Settings.";
            else if (string.IsNullOrEmpty(PlayerSettings.Android.keystoreName) || !File.Exists(PlayerSettings.Android.keystoreName))
                problem = $"keystore file '{PlayerSettings.Android.keystoreName}' does not exist.";
            else if (string.IsNullOrEmpty(PlayerSettings.Android.keystorePass)) problem = "keystore password is empty.";
            else if (string.IsNullOrEmpty(PlayerSettings.Android.keyaliasName) || string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass))
                problem = "key alias or alias password is empty.";
            return problem == null;
        }

        static void Build(string path, BuildOptions options)
        {
            EditorUserBuildSettings.buildAppBundle = false;
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = path,
                target = BuildTarget.Android,
                options = options
            });

            var summary = report.summary;
            Debug.Log($"[BuildScript] {summary.result}: {path} ({ApkSizeMb(path):F1} MB, " +
                      $"{summary.totalErrors} errors, {summary.totalTime})");
            if (Application.isBatchMode && summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        // BuildSummary.totalSize counts uncompressed build data, not the APK on disk.
        static float ApkSizeMb(string path) => File.Exists(path) ? new FileInfo(path).Length / (1024f * 1024f) : 0f;
    }
}
