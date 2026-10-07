using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using AuraKnight.UI;

namespace AuraKnight.Editor
{
    /// <summary>Creates (or refreshes the font links of) Data/UI/Resources/UITheme.asset. Colour tokens keep any tuned values.</summary>
    public static class UiThemeGenerator
    {
        public static UITheme Generate()
        {
            Directory.CreateDirectory(UiAssetPaths.ResourcesDir);
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(UiAssetPaths.Theme);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<UITheme>();
                AssetDatabase.CreateAsset(theme, UiAssetPaths.Theme);
            }
            var so = new SerializedObject(theme);
            Link(so, "display", UiFontGenerator.Display);
            Link(so, "mono", UiFontGenerator.Mono);
            Link(so, "monoRegular", UiFontGenerator.MonoRegular);
            Link(so, "body", UiFontGenerator.Body);
            Link(so, "number", UiFontGenerator.Number);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            return theme;
        }

        static void Link(SerializedObject so, string property, string fontName) =>
            so.FindProperty(property).objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiFontGenerator.AssetPath(fontName));
    }
}
