using AuraKnight.Aura;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Reproducible phase 5 generation: AuraDefinition assets, skill and interactable prefabs.
    /// Menu: Aura/Aura System/Generate Aura Assets. Batch: AuraKnight.Editor.AuraAssetGenerator.Generate
    /// The Player prefab picks the result up through <see cref="PlayerAssetGenerator"/>.
    /// </summary>
    public static class AuraAssetGenerator
    {
        [MenuItem("Aura/Aura System/Generate Aura Assets")]
        public static void Generate()
        {
            EnsureAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AuraAssetGenerator] Generated Aura definitions, skill prefabs and interactable prefabs.");
        }

        /// <summary>Builds everything and returns the definitions (indexed by AuraId order).</summary>
        internal static AuraDefinition[] EnsureAssets()
        {
            var square = PlayerGeneratorUtil.EnsureSolidSprite(PlayerAssetGenerator.SquareSpritePath, 32, 32);
            var skills = AuraSkillPrefabBuilder.BuildAll(square);
            AuraInteractablePrefabBuilder.BuildAll(square);
            return AuraDefinitionGenerator.Generate(skills);
        }
    }
}
