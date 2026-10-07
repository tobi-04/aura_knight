using AuraKnight.Core;
using AuraKnight.World.Pickups;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>Placeholder prefabs for the coin (gold disc-ish square) and the Light Drop heart pickup (pale green glow).</summary>
    static class EnemyPickupPrefabBuilder
    {
        static readonly Color CoinColor = new Color(1f, 0.78f, 0.34f);
        static readonly Color LightColor = new Color(0.65f, 1f, 0.8f);

        public static void Build(Sprite square)
        {
            BuildCoin(square);
            BuildLightDrop(square);
        }

        static void BuildCoin(Sprite square)
        {
            var root = new GameObject("CoinPickup");
            AuraPrefabParts.AddSprite(root.transform, "Visual", square, Vector2.zero, new Vector2(0.3f, 0.3f), CoinColor, 5);
            root.AddComponent<CoinPickup>();
            Save(root, EnemyAssetGenerator.CoinPrefabPath);
        }

        static void BuildLightDrop(Sprite square)
        {
            var root = new GameObject("LightDrop");
            PlayerGeneratorUtil.SetLayer(root, PhysicsLayers.Interactable);
            AuraPrefabParts.AddSprite(root.transform, "Visual", square, Vector2.zero, new Vector2(0.45f, 0.45f), LightColor, 5);
            var trigger = root.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.5f;
            root.AddComponent<LightDropPickup>();
            Save(root, EnemyAssetGenerator.LightDropPrefabPath);
        }

        static void Save(GameObject root, string path)
        {
            try { PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
