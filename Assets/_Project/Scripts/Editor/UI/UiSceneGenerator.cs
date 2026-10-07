using AuraKnight.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Puts the UI into scenes. Core gets one GameObject, "UI_Root" (the only object this generator touches: the world
    /// generator owns the rest); MainMenu gets the menu screens. Both are rebuilt from prefabs, so reruns are idempotent.
    /// </summary>
    public static class UiSceneGenerator
    {
        public const string RootName = "UI_Root";

        public static void PopulateCore()
        {
            var scene = EditorSceneManager.OpenScene(UiAssetPaths.CoreScene, OpenSceneMode.Single);
            DestroyAll(RootName);
            var root = new GameObject(RootName);
            var controls = Instantiate(UiAssetPaths.VirtualControlsPrefab, root.transform);
            var hud = Instantiate(UiAssetPaths.HudPrefab, root.transform);
            Instantiate(UiAssetPaths.GameScreensPrefab, root.transform);
            VirtualControlsBuilder.CreateEventSystem().transform.SetParent(root.transform, false);

            // Only the two canvas groups: the boss bar's own group is driven by BossHealthBarView.
            var groups = new[] { hud.transform.Find("HudStatic").GetComponent<CanvasGroup>(), hud.transform.Find("HudDynamic").GetComponent<CanvasGroup>() };
            root.AddComponent<HudController>().Bind(groups, controls.GetComponent<CanvasGroup>());
            root.AddComponent<CoreLauncher>();
            root.AddComponent<AudioListenerGuard>();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[UiSceneGenerator] Core UI_Root populated.");
        }

        public static void PopulateMainMenu()
        {
            var scene = EditorSceneManager.OpenScene(UiAssetPaths.MainMenuScene, OpenSceneMode.Single);
            DestroyAll("MenuScreens");
            DestroyAll("EventSystem");
            var screens = Instantiate(UiAssetPaths.MenuScreensPrefab, null);
            VirtualControlsBuilder.CreateEventSystem();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[UiSceneGenerator] MainMenu populated ({screens.name}).");
        }

        static GameObject Instantiate(string prefabPath, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (parent != null) instance.transform.SetParent(parent, false);
            return instance;
        }

        static void DestroyAll(string name)
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
                if (go != null && go.name == name && go.transform.parent == null) Object.DestroyImmediate(go);
        }
    }
}
