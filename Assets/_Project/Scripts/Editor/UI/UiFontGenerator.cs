using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Checks the TMP essentials (TMP Settings, default shaders) are present, then builds one Dynamic TMP font asset per Google
    /// font, pre-filled with ASCII + full Vietnamese so the first frame needs no glyph generation. Be Vietnam Pro is the
    /// fallback of every other font and the TMP default. Idempotent.
    /// </summary>
    public static class UiFontGenerator
    {
        public const string Display = "ChakraPetch-Bold", Mono = "IBMPlexMono-Bold", MonoRegular = "IBMPlexMono-Regular",
            Body = "BeVietnamPro-Regular", Number = "BarlowCondensed-Bold";

        const int SamplingSize = 64, Padding = 6, AtlasSize = 1024;
        const string VietnameseLower = "aàáảãạăằắẳẵặâầấẩẫậbcdđeèéẻẽẹêềếểễệfghiìíỉĩịjklmnoòóỏõọôồốổỗộơờớởỡợpqrstuùúủũụưừứửữựvwxyỳýỷỹỵ";

        public static string AssetPath(string fontName) => $"{UiAssetPaths.FontsDir}/{fontName} SDF.asset";

        /// <summary>Every character the game text may use: printable ASCII, Vietnamese letters (both cases), common punctuation.</summary>
        public static string CharacterSet()
        {
            var sb = new System.Text.StringBuilder();
            for (char c = ' '; c <= '~'; c++) sb.Append(c);
            sb.Append(VietnameseLower).Append(VietnameseLower.ToUpperInvariant());
            sb.Append("…–—‘’“”•·©°×÷");
            return sb.ToString();
        }

        /// <summary>TMP needs its Essential Resources (TMP Settings, shaders) in Assets/TextMesh Pro; they are committed with the project.</summary>
        public static void EnsureTmpEssentials()
        {
            if (AssetDatabase.FindAssets("t:TMP_Settings").Length > 0) return;
            throw new FileNotFoundException(
                "TMP Essential Resources are missing. Use Window > TextMeshPro > Import TMP Essential Resources, then rerun.");
        }

        public static void GenerateAll()
        {
            EnsureTmpEssentials();
            Directory.CreateDirectory(UiAssetPaths.FontsDir);
            var body = Ensure(Body, null);
            foreach (string name in new[] { Display, Mono, MonoRegular, Number }) Ensure(name, body);
            if (TMP_Settings.instance != null && TMP_Settings.defaultFontAsset != body)
            {
                TMP_Settings.defaultFontAsset = body;
                EditorUtility.SetDirty(TMP_Settings.instance);
            }
            AssetDatabase.SaveAssets();
        }

        static TMP_FontAsset Ensure(string fontName, TMP_FontAsset fallback)
        {
            string path = AssetPath(fontName);
            var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (asset != null && (asset.atlasTextures == null || asset.atlasTextures.Length == 0 || asset.atlasTextures[0] == null))
            {
                AssetDatabase.DeleteAsset(path); // half-written by an interrupted run
                asset = null;
            }
            if (asset == null)
            {
                var font = AssetDatabase.LoadAssetAtPath<Font>($"{UiAssetPaths.FontsDir}/{fontName}.ttf");
                if (font == null) throw new FileNotFoundException($"Font file missing: {fontName}.ttf");
                asset = TMP_FontAsset.CreateFontAsset(font, SamplingSize, Padding, GlyphRenderMode.SDFAA, AtlasSize, AtlasSize,
                    AtlasPopulationMode.Dynamic, true);
                asset.name = fontName + " SDF";
                AssetDatabase.CreateAsset(asset, path);
                foreach (var atlas in asset.atlasTextures)
                {
                    atlas.name = fontName + " Atlas";
                    AssetDatabase.AddObjectToAsset(atlas, asset);
                }
                asset.material.name = fontName + " Material";
                AssetDatabase.AddObjectToAsset(asset.material, asset);
            }
            asset.TryAddCharacters(CharacterSet(), out string missing);
            if (!string.IsNullOrEmpty(missing)) Debug.LogWarning($"[UiFontGenerator] {fontName}: glyphs not in the font: {missing}");
            asset.fallbackFontAssetTable ??= new System.Collections.Generic.List<TMP_FontAsset>();
            if (fallback != null && !asset.fallbackFontAssetTable.Contains(fallback)) asset.fallbackFontAssetTable.Add(fallback);
            EditorUtility.SetDirty(asset);
            return asset;
        }
    }
}
