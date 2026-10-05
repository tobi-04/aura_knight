using System.IO;
using AuraKnight.Core;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>Small helpers shared by the phase 3 asset and scene generators.</summary>
    static class PlayerGeneratorUtil
    {
        public static void EnsureFolder(string assetFolder)
        {
            if (!AssetDatabase.IsValidFolder(assetFolder))
                Directory.CreateDirectory(assetFolder);
        }

        /// <summary>Assigns an object reference to a (private) serialized field.</summary>
        public static void SetReference(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null) throw new System.InvalidOperationException($"{target.GetType().Name} has no serialized field '{field}'");
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Puts the object on a project physics layer (see PhysicsLayers).</summary>
        public static GameObject SetLayer(GameObject go, string layer)
        {
            PhysicsLayers.Apply(go, layer);
            return go;
        }

        /// <summary>Assigns a LayerMask field.</summary>
        public static void SetLayerMask(Object target, string field, int mask) =>
            Set(target, field, p => p.FindPropertyRelative("m_Bits").uintValue = (uint)mask);

        public static void SetString(Object target, string field, string value) => Set(target, field, p => p.stringValue = value);

        public static void SetInt(Object target, string field, int value) => Set(target, field, p => p.intValue = value);

        public static void SetFloat(Object target, string field, float value) => Set(target, field, p => p.floatValue = value);

        public static void SetBool(Object target, string field, bool value) => Set(target, field, p => p.boolValue = value);

        static void Set(Object target, string field, System.Action<SerializedProperty> assign)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null) throw new System.InvalidOperationException($"{target.GetType().Name} has no serialized field '{field}'");
            assign(property);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Writes a solid-colour PNG and imports it as a point-filtered 32 PPU sprite.</summary>
        public static Sprite EnsureSolidSprite(string path, int width, int height)
        {
            EnsureFolder(Path.GetDirectoryName(path));
            if (!File.Exists(path))
            {
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                var pixels = new Color32[width * height];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
                texture.SetPixels32(pixels);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        public static Material UnlitSpriteMaterial() =>
            AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
    }
}
