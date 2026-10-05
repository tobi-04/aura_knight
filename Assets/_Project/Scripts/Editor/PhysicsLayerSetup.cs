using AuraKnight.Core;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Defines the project physics layers (ids 8+ in TagManager) and sets the 2D collision matrix: among the project layers
    /// only the pairs in <see cref="PhysicsLayers.CollidingPairs"/> interact. Idempotent; part of Aura/Setup Project.
    /// </summary>
    static class PhysicsLayerSetup
    {
        const string TagManagerPath = "ProjectSettings/TagManager.asset";
        const int FirstUserLayer = 8;

        public static void Apply()
        {
            DefineLayers();
            SetCollisionMatrix();
        }

        static void DefineLayers()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(TagManagerPath);
            if (assets == null || assets.Length == 0) throw new System.InvalidOperationException("TagManager.asset not found");
            var tagManager = new SerializedObject(assets[0]);
            var layers = tagManager.FindProperty("layers");
            foreach (var name in PhysicsLayers.All)
            {
                if (IndexOf(layers, name) >= 0) continue;
                int free = FreeSlot(layers);
                if (free < 0) throw new System.InvalidOperationException($"No free physics layer slot for '{name}'");
                layers.GetArrayElementAtIndex(free).stringValue = name;
            }
            tagManager.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        static void SetCollisionMatrix()
        {
            var all = PhysicsLayers.All;
            for (int i = 0; i < all.Length; i++)
            for (int j = i; j < all.Length; j++)
            {
                int a = LayerMask.NameToLayer(all[i]), b = LayerMask.NameToLayer(all[j]);
                if (a < 0 || b < 0) throw new System.InvalidOperationException($"Layer '{all[i]}' or '{all[j]}' is not defined");
                Physics2D.IgnoreLayerCollision(a, b, !Collides(all[i], all[j]));
            }
            AssetDatabase.SaveAssets();
        }

        static bool Collides(string a, string b)
        {
            foreach (var pair in PhysicsLayers.CollidingPairs)
                if ((pair[0] == a && pair[1] == b) || (pair[0] == b && pair[1] == a)) return true;
            return false;
        }

        static int IndexOf(SerializedProperty layers, string name)
        {
            for (int i = FirstUserLayer; i < layers.arraySize; i++)
                if (layers.GetArrayElementAtIndex(i).stringValue == name) return i;
            return -1;
        }

        static int FreeSlot(SerializedProperty layers)
        {
            for (int i = FirstUserLayer; i < layers.arraySize; i++)
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) return i;
            return -1;
        }
    }
}
