using System.IO;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// The project's pixel-art import settings (GDD section 10): PPU 32, point filter, no compression, no mipmaps, full rect mesh.
    /// <see cref="EnsurePreset"/> writes the same values to Sprite_Pixel32.preset for artists; the postprocessor applies them to
    /// new textures in the gameplay art folders so nobody has to remember.
    /// </summary>
    public static class Pixel32Importer
    {
        public const float PixelsPerUnit = 32f;
        const string TemplatePath = ArtPaths.Root + "/Presets/Sprite_Pixel32_Template.png";

        public static void Apply(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 4096;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
        }

        public static bool IsPixelArt(string assetPath)
        {
            if (!assetPath.EndsWith(".png")) return false;
            foreach (var folder in ArtPaths.PixelFolders)
                if (assetPath.StartsWith(folder + "/")) return true;
            return false;
        }

        /// <summary>Applies the settings to every PNG under the gameplay art folders (idempotent; only reimports changed files).</summary>
        public static int ApplyToAll()
        {
            int changed = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", ArtPaths.PixelFolders))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!IsPixelArt(path) || AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
                Apply(importer);
                if (EditorUtility.IsDirty(importer)) changed++;
                importer.SaveAndReimport();
            }
            return changed;
        }

        /// <summary>Creates (or refreshes) Sprite_Pixel32.preset from a tiny template texture configured with <see cref="Apply"/>.</summary>
        public static Preset EnsurePreset()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(TemplatePath));
            if (!File.Exists(TemplatePath))
            {
                var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                var pixels = new Color32[16];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
                texture.SetPixels32(pixels);
                File.WriteAllBytes(TemplatePath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(TemplatePath);
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(TemplatePath);
            Apply(importer);
            importer.SaveAndReimport();
            var preset = new Preset(importer);
            var existing = AssetDatabase.LoadAssetAtPath<Preset>(ArtPaths.PresetPath);
            if (existing == null) AssetDatabase.CreateAsset(preset, ArtPaths.PresetPath);
            else
            {
                EditorUtility.CopySerialized(preset, existing);
                EditorUtility.SetDirty(existing);
            }
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<Preset>(ArtPaths.PresetPath);
        }
    }

    /// <summary>Gives newly imported pixel-art textures the Pixel32 settings (existing, already configured assets are left alone).</summary>
    sealed class Pixel32Postprocessor : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!Pixel32Importer.IsPixelArt(assetPath) || assetImporter is not TextureImporter importer) return;
            if (importer.importSettingsMissing) Pixel32Importer.Apply(importer);
        }
    }
}
