using System.Collections.Generic;
using AuraKnight.Core;
using AuraKnight.Editor;
using AuraKnight.Progression;
using AuraKnight.UI;
using AuraKnight.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Tests.Progression
{
    /// <summary>Checks the generated assets (run <c>ProgressionAssetGenerator.GenerateAll</c> and the UI generator first).</summary>
    public sealed class GeneratedProgressionAssetsTests
    {
        static StringTable Strings()
        {
            Assert.IsTrue(StringTable.TryParse(Resources.Load<TextAsset>("Strings_vi").text, out var table));
            return table;
        }

        static List<ShopItem> Items()
        {
            var items = new List<ShopItem>();
            foreach (string guid in AssetDatabase.FindAssets("t:ShopItem", new[] { ProgressionAssetGenerator.ShopItemsDir }))
                items.Add(AssetDatabase.LoadAssetAtPath<ShopItem>(AssetDatabase.GUIDToAssetPath(guid)));
            return items;
        }

        static ShopItem Item(string id) => Items().Find(i => i.Id == id);

        [Test]
        public void ShopHasTheSevenGddItemsWithTheirPrices()
        {
            Assert.AreEqual(7, Items().Count);
            CollectionAssert.AreEqual(new[] { 100, 200, 300, 400 }, Item("heart").Prices);
            CollectionAssert.AreEqual(new[] { 120, 240, 360, 480 }, Item("energy").Prices);
            CollectionAssert.AreEqual(new[] { 300, 600 }, Item("sword").Prices);
            Assert.AreEqual(4, Item("heart").MaxPurchases);
            Assert.AreEqual(4, Item("energy").MaxPurchases);
            Assert.AreEqual(2, Item("sword").MaxPurchases);
            Assert.AreEqual(25, Item("energy").Amount);
            foreach (string region in new[] { "forest", "cave", "city", "castle" })
            {
                var map = Item(ShopItem.MapItemId(region));
                Assert.IsNotNull(map, region);
                Assert.AreEqual(ShopEffect.MapRegion, map.Effect);
                Assert.AreEqual(region, map.RegionId);
                CollectionAssert.AreEqual(new[] { 50 }, map.Prices);
                Assert.AreEqual(1, map.MaxPurchases);
            }
        }

        [Test]
        public void GeneratedAssetsReproduceTheGddEconomy()
        {
            int baseline = ShopRules.TotalCost(Item("heart"), 2) + ShopRules.TotalCost(Item("energy"), 2) + ShopRules.TotalCost(Item("sword"), 1);
            Assert.AreEqual(960, baseline);
            Assert.AreEqual(1000, ShopRules.TotalCost(Item("heart"), 4));
            Assert.AreEqual(1200, ShopRules.TotalCost(Item("energy"), 4));
            Assert.AreEqual(900, ShopRules.TotalCost(Item("sword"), 2));
        }

        [Test]
        public void ItemIdsAreUniqueAndEveryTextKeyExists()
        {
            var table = Strings();
            var ids = new HashSet<string>();
            foreach (var item in Items())
            {
                Assert.IsTrue(ids.Add(item.Id), "duplicate id " + item.Id);
                Assert.IsTrue(table.TryGet(item.NameKey, out _), item.NameKey);
                Assert.IsTrue(table.TryGet(item.DescriptionKey, out _), item.DescriptionKey);
            }
        }

        [Test]
        public void SolDialogueCoversTheGameAndItsLinesExist()
        {
            var sol = AssetDatabase.LoadAssetAtPath<DialogueByProgress>(ProgressionAssetGenerator.DialoguePath);
            Assert.IsNotNull(sol);
            var table = Strings();
            foreach (var entry in sol.Entries) Assert.IsTrue(table.TryGet(entry.lineKey, out _), entry.lineKey);
            Assert.AreEqual("dialogue.sol.start", sol.Select(GameState.NewGame()));
            var state = GameState.NewGame();
            state.unlockedAuras.Add("Wind");
            Assert.AreEqual("dialogue.sol.wind", sol.Select(state));
            Assert.IsTrue(table.TryGet("dialogue.sol.default", out _));
        }

        [Test]
        public void EveryRegionHasMapData()
        {
            var graph = AssetDatabase.LoadAssetAtPath<RegionGraph>(ProgressionAssetGenerator.RegionGraphPath);
            foreach (var region in graph.Regions)
            {
                var data = AssetDatabase.LoadAssetAtPath<RoomMapData>($"{ProgressionAssetGenerator.MapDir}/RoomMapData_{region.regionId}.asset");
                Assert.IsNotNull(data, region.regionId);
                Assert.AreEqual(region.regionId, data.RegionId);
                Assert.IsTrue(data.TryGetRoom($"{region.regionId}_01", out var room), "start room of " + region.regionId);
                Assert.Greater(room.cells.width, 0);
                bool hasAltar = false;
                foreach (var icon in data.Icons) hasAltar |= icon.kind == MapIconKind.Altar && icon.roomId == $"{region.regionId}_01";
                Assert.AreEqual(region.altarIds.Count > 0, hasAltar, "altar icon of " + region.regionId);
            }
        }

        [Test]
        public void MapDataRoomsNeverOverlapAcrossRegions()
        {
            var rects = new List<(string, RectInt)>();
            foreach (string guid in AssetDatabase.FindAssets("t:RoomMapData", new[] { ProgressionAssetGenerator.MapDir }))
            {
                var data = AssetDatabase.LoadAssetAtPath<RoomMapData>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (var room in data.Rooms) rects.Add((room.roomId, data.ToWorldCells(room.cells)));
            }
            for (int i = 0; i < rects.Count; i++)
                for (int j = i + 1; j < rects.Count; j++)
                    Assert.IsFalse(rects[i].Item2.Overlaps(rects[j].Item2), $"{rects[i].Item1} overlaps {rects[j].Item1}");
        }

        [Test]
        public void ChestAndNpcPrefabsAreWired()
        {
            var chest = AssetDatabase.LoadAssetAtPath<GameObject>(ProgressionAssetGenerator.ChestPrefabPath);
            Assert.IsNotNull(chest.GetComponent<TreasureChest>());
            Assert.IsNotNull(chest.GetComponent<PersistentId>());
            Assert.IsTrue(chest.GetComponent<Collider2D>().isTrigger);
            Assert.AreEqual(LayerMask.NameToLayer(PhysicsLayers.Interactable), chest.layer);
            Assert.AreEqual(100, chest.GetComponent<TreasureChest>().Reward.coinsMin);
            Assert.AreEqual(150, chest.GetComponent<TreasureChest>().Reward.coinsMax);

            var npc = AssetDatabase.LoadAssetAtPath<GameObject>(ProgressionAssetGenerator.NpcSolPrefabPath);
            Assert.IsNotNull(npc.GetComponent<NpcSol>());
            Assert.IsTrue(npc.GetComponent<Collider2D>().isTrigger);
            var so = new SerializedObject(npc.GetComponent<NpcSol>());
            Assert.IsNotNull(so.FindProperty("dialogue").objectReferenceValue);
            var prompt = so.FindProperty("prompt").objectReferenceValue as GameObject;
            Assert.IsNotNull(prompt);
            Assert.IsFalse(prompt.activeSelf, "prompt starts hidden");
            Assert.IsTrue(Strings().TryGet(prompt.GetComponent<ThemedText>().LocKey, out _));
        }

        [Test]
        public void GameScreensHoldTheShopMapAndLauncher()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiAssetPaths.GameScreensPrefab);
            Assert.IsNotNull(prefab.GetComponentInChildren<ShopScreen>(true));
            Assert.IsNotNull(prefab.GetComponentInChildren<MapScreen>(true));
            Assert.IsNotNull(prefab.GetComponent<ShopMapLauncher>());
            var table = Strings();
            foreach (var screen in new Component[] { prefab.GetComponentInChildren<ShopScreen>(true), prefab.GetComponentInChildren<MapScreen>(true) })
                foreach (var text in screen.GetComponentsInChildren<ThemedText>(true))
                    if (!string.IsNullOrEmpty(text.LocKey)) Assert.IsTrue(table.TryGet(text.LocKey, out _), $"{screen.name}/{text.name}: '{text.LocKey}'");
            foreach (var kind in System.Enum.GetNames(typeof(MapIconKind)))
                Assert.IsTrue(table.TryGet("map.icon." + kind.ToLowerInvariant(), out _), kind);
        }

        [Test]
        public void ShopScreenIsBoundToEverySevenItemsAndTheMapToEveryRegion()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiAssetPaths.GameScreensPrefab);
            var shop = new SerializedObject(prefab.GetComponentInChildren<ShopScreen>(true)).FindProperty("items");
            Assert.AreEqual(7, shop.arraySize);
            var map = new SerializedObject(prefab.GetComponentInChildren<MapScreen>(true)).FindProperty("regions");
            Assert.AreEqual(5, map.arraySize);
            for (int i = 0; i < map.arraySize; i++) Assert.IsNotNull(map.GetArrayElementAtIndex(i).objectReferenceValue);
        }

        [Test]
        public void CardAccentColoursFollowTheItemKind()
        {
            Assert.AreEqual(UIColorToken.Fire, ShopItemView.AccentFor(ProgressionTestUtil.Heart()));
            Assert.AreEqual(UIColorToken.Water, ShopItemView.AccentFor(ProgressionTestUtil.Energy()));
            Assert.AreEqual(UIColorToken.Gold, ShopItemView.AccentFor(ProgressionTestUtil.Sword()));
            Assert.AreEqual(UIColorToken.Wind, ShopItemView.AccentFor(ProgressionTestUtil.Map("forest")));
            Assert.AreEqual(UIColorToken.Water, ShopItemView.AccentFor(ProgressionTestUtil.Map("city")));
            Assert.AreEqual(UIColorToken.Fire, ShopItemView.AccentFor(ProgressionTestUtil.Map("castle")));
            Assert.AreEqual(UIColorToken.Gold, ShopItemView.AccentFor(ProgressionTestUtil.Map("cave")));
        }
    }
}
