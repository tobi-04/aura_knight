using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// One entry point for everything the UI phase generates, in dependency order. Idempotent.
    /// Menu: Aura/UI/Generate All. Batch: -executeMethod AuraKnight.Editor.UiGenerator.GenerateAll
    /// </summary>
    public static class UiGenerator
    {
        [MenuItem("Aura/UI/Generate All")]
        public static void GenerateAll()
        {
            GenerateFoundation();
            CreditsTextBuilder.Build();
            VirtualControlsPrefabBuilder.Build();
            HudPrefabBuilder.Build();
            ScreensPrefabBuilder.BuildGameScreens();
            ScreensPrefabBuilder.BuildMenuScreens();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UiSceneGenerator.PopulateCore();
            UiSceneGenerator.PopulateMainMenu();
            Debug.Log("[UiGenerator] UI generated.");
        }

        [MenuItem("Aura/UI/Generate Fonts and Theme")]
        public static void GenerateFoundation()
        {
            UiFontGenerator.GenerateAll();
            UiSpriteGenerator.GenerateAll();
            UiThemeGenerator.Generate();
            Debug.Log("[UiGenerator] Fonts, sprites and theme generated.");
        }
    }
}
