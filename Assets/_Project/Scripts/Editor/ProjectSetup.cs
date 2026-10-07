using System.IO;
using AuraKnight.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Idempotent project configuration (GDD §1.1, plan phase 01).
    /// Menu: Aura/Setup Project. Batch: -executeMethod AuraKnight.Editor.ProjectSetup.Run
    /// </summary>
    public static class ProjectSetup
    {
        const string ScenesDir = "Assets/_Project/Scenes";
        const string AndroidId = "com.aurastudio.auraknight";
        /// <summary>Release identity (plan phase 13): version 1.0.0, Android version code 1.</summary>
        public const string Version = "1.0.0";
        public const int BundleVersionCode = 1;

        // Order defines build indices; Boot must stay first.
        static readonly string[] BuildScenes =
        {
            "Boot", "MainMenu", "Core",
            "Region_Hub", "Region_Forest", "Region_Cave", "Region_City", "Region_Castle"
        };

        [MenuItem("Aura/Setup Project")]
        public static void Run()
        {
            ConfigurePlayer();
            ConfigureEditor();
            PhysicsLayerSetup.Apply();
            EnsureScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectSetup] Done.");
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Aura Studio";
            PlayerSettings.productName = "Aura Knight";
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.Android.bundleVersionCode = BundleVersionCode;
            EditorUserBuildSettings.development = false; // Development Build is off; BuildScript turns it on for the dev APK only

            var android = NamedBuildTarget.Android;
            PlayerSettings.SetApplicationIdentifier(android, AndroidId);
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;

            ConfigureQuality();
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        }

        /// <summary>Frame pacing comes from Application.targetFrameRate, which Android ignores while vsync is on: every level has vsync off.</summary>
        static void ConfigureQuality()
        {
            int current = QualitySettings.GetQualityLevel();
            for (int level = 0; level < QualitySettings.names.Length; level++)
            {
                QualitySettings.SetQualityLevel(level, false);
                QualitySettings.vSyncCount = 0;
            }
            QualitySettings.SetQualityLevel(current, false);
        }

        static void ConfigureEditor()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            VersionControlSettings.mode = "Visible Meta Files";
        }

        static void EnsureScenes()
        {
            Directory.CreateDirectory(ScenesDir);
            var entries = new EditorBuildSettingsScene[BuildScenes.Length];
            for (int i = 0; i < BuildScenes.Length; i++)
            {
                string path = $"{ScenesDir}/{BuildScenes[i]}.unity";
                if (!File.Exists(path)) CreateScene(path, BuildScenes[i]);
                entries[i] = new EditorBuildSettingsScene(path, true);
            }
            EditorBuildSettings.scenes = entries;
        }

        static void CreateScene(string path, string sceneName)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (sceneName == "Boot")
                new GameObject("Bootstrapper").AddComponent<Bootstrapper>();
            if (sceneName == "Boot" || sceneName == "MainMenu")
                CreateCamera();
            EditorSceneManager.SaveScene(scene, path);
        }

        static void CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.625f; // 360px / 32ppu / 2
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x07, 0x0D, 0x1F, 0xFF); // bg/night
            go.transform.position = new Vector3(0, 0, -10);
        }
    }
}
