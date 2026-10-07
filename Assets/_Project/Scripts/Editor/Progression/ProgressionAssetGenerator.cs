using System.Collections.Generic;
using System.IO;
using AuraKnight.Progression;
using AuraKnight.World;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Idempotent generator for phase 12 data: the seven ShopItem assets (GDD §8 prices), Sol's DialogueByProgress, the five RoomMapData
    /// assets (filled from the region scenes) and the TreasureChest / NpcSol prefabs. Existing assets are updated in place (GUIDs stay).
    /// Menu: Aura/Progression/Generate All. Batch: AuraKnight.Editor.ProgressionAssetGenerator.GenerateAll (then the UI generator).
    /// </summary>
    public static class ProgressionAssetGenerator
    {
        /// <summary>Same file the world generator writes (its class lives in another editor assembly).</summary>
        public const string RegionGraphPath = "Assets/_Project/Data/World/RegionGraph.asset";
        public const string ShopItemsDir = "Assets/_Project/Data/ShopItems";
        public const string DialoguePath = "Assets/_Project/Data/Dialogue/SolDialogue.asset";
        public const string MapDir = "Assets/_Project/Data/Map";
        public const string ChestPrefabPath = "Assets/_Project/Prefabs/Interactables/TreasureChest.prefab";
        public const string NpcSolPrefabPath = "Assets/_Project/Prefabs/Interactables/NpcSol.prefab";

        static readonly int[] HeartPrices = { 100, 200, 300, 400 };
        static readonly int[] EnergyPrices = { 120, 240, 360, 480 };
        static readonly int[] SwordPrices = { 300, 600 };
        static readonly int[] MapPrices = { 50 };
        static readonly string[] MapRegions = { "forest", "cave", "city", "castle" };

        [MenuItem("Aura/Progression/Generate All")]
        public static void GenerateAll()
        {
            GenerateShopItems();
            GenerateDialogue();
            InteractablePrefabBuilder.BuildAll();
            RoomMapDataBuilder.RebuildAll();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ProgressionAssetGenerator] Done.");
        }

        public static void GenerateShopItems()
        {
            Item("01_Heart", "heart", ShopEffect.Heart, 1, HeartPrices, 4, null);
            Item("02_Energy", "energy", ShopEffect.Energy, 25, EnergyPrices, 4, null);
            Item("03_Sword", "sword", ShopEffect.Sword, 1, SwordPrices, 2, null);
            for (int i = 0; i < MapRegions.Length; i++)
                Item($"{04 + i:00}_Map_{MapRegions[i]}", ShopItem.MapItemId(MapRegions[i]), ShopEffect.MapRegion, 1, MapPrices, 1, MapRegions[i]);
        }

        public static void GenerateDialogue()
        {
            var dialogue = LoadOrCreate<DialogueByProgress>(DialoguePath);
            dialogue.SetEntries(new List<DialogueEntry>
            {
                Line("dialogue.sol.start"),
                Line("dialogue.sol.wind", "Wind"),
                Line("dialogue.sol.fire", "Wind", "Fire"),
                Line("dialogue.sol.water", "Wind", "Fire", "Water"),
                Line("dialogue.sol.end", "Wind", "Fire", "Water").WithBoss("malakor"),
            });
            EditorUtility.SetDirty(dialogue);
        }

        static void Item(string file, string id, ShopEffect effect, int amount, int[] prices, int max, string region)
        {
            var item = LoadOrCreate<ShopItem>($"{ShopItemsDir}/{file}.asset");
            string key = $"shop.item.{id}";
            string desc = effect == ShopEffect.MapRegion ? "shop.item.map.desc" : key + ".desc";
            item.Configure(id, key + ".name", desc, effect, amount, prices, max, region);
            EditorUtility.SetDirty(item);
        }

        static DialogueEntry Line(string key, params string[] auras) =>
            new DialogueEntry { lineKey = key, requiredAuras = new List<string>(auras) };

        static DialogueEntry WithBoss(this DialogueEntry entry, string bossId)
        {
            entry.requiredBoss = bossId;
            return entry;
        }

        internal static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
