using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>The shared lit sprite material: URP 2D Sprite-Lit-Default, so Light2D lights it and a MaterialPropertyBlock _Color tints it.</summary>
    public static class ArtMaterials
    {
        const string ShaderName = "Universal Render Pipeline/2D/Sprite-Lit-Default";

        public static Material EnsureSpriteLit()
        {
            var shader = Shader.Find(ShaderName) ?? throw new InvalidOperationException($"Shader '{ShaderName}' not found; is URP installed?");
            Directory.CreateDirectory(Path.GetDirectoryName(ArtPaths.MaterialPath));
            var material = AssetDatabase.LoadAssetAtPath<Material>(ArtPaths.MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "Mat_SpriteLit" };
                AssetDatabase.CreateAsset(material, ArtPaths.MaterialPath);
            }
            else material.shader = shader;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }
    }
}
