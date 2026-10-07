using AuraKnight.UI;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>Re-bakes Prefabs/UI/VirtualControls.prefab with the themed TMP labels, Aura ring and settings styler.</summary>
    static class VirtualControlsPrefabBuilder
    {
        public static void Build()
        {
            var circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            var root = VirtualControlsBuilder.Build(circle);
            try
            {
                root.SetActive(true);
                PrefabUtility.SaveAsPrefabAsset(root, UiAssetPaths.VirtualControlsPrefab);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
