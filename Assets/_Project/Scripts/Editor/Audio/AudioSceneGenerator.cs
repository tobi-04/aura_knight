using AuraKnight.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Adds or updates the "Audio" object (AudioManager, AudioEventListener, MusicLayerController) in Core.unity and touches
    /// nothing else in the scene. Idempotent; the scene is only saved when something changed.
    /// </summary>
    public static class AudioSceneGenerator
    {
        const string CoreScenePath = "Assets/_Project/Scenes/Core.unity";
        const string ObjectName = "Audio";

        [MenuItem("Aura/Audio/Populate Core Audio Object")]
        public static void PopulateCore()
        {
            // Opening a scene unloads unreferenced assets, so everything is loaded after the scene is open.
            var scene = EditorSceneManager.OpenScene(CoreScenePath, OpenSceneMode.Single);
            var mixer = AudioAssetGenerator.LoadMixer();
            var library = AssetDatabase.LoadAssetAtPath<SfxLibrary>(AudioAssetGenerator.LibraryPath);
            if (mixer == null || library == null)
                throw new System.InvalidOperationException("Run Aura/Audio/Generate Audio Assets first: mixer or SfxLibrary is missing.");

            var go = FindRoot(scene, ObjectName);
            if (go == null)
            {
                go = new GameObject(ObjectName);
                Undo.RegisterCreatedObjectUndo(go, "Create Audio");
            }
            var manager = EnsureComponent<AudioManager>(go);
            EnsureComponent<AudioEventListener>(go);
            var music = EnsureComponent<MusicLayerController>(go);

            SetRef(manager, "mixer", mixer);
            SetRef(manager, "library", library);
            var tracks = new SerializedObject(music);
            var list = tracks.FindProperty("tracks");
            var assets = AudioAssetGenerator.LoadAllRegionMusic();
            list.arraySize = assets.Length;
            for (int i = 0; i < assets.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = assets[i];
            tracks.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[AudioSceneGenerator] Core 'Audio' object is up to date.");
        }

        static GameObject FindRoot(UnityEngine.SceneManagement.Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == name) return root;
            return null;
        }

        static T EnsureComponent<T>(GameObject go) where T : Component =>
            go.TryGetComponent<T>(out var existing) ? existing : go.AddComponent<T>();

        static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
