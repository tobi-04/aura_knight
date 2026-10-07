using AuraKnight.Core;
using AuraKnight.Progression;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Progression
{
    public sealed class MapRulesTests
    {
        static GameState State(bool visited, bool mapBought)
        {
            var state = GameState.NewGame();
            if (visited) state.MarkRoomVisited("forest_01");
            if (mapBought) state.AddPurchase(ShopItem.MapItemId("forest"));
            return state;
        }

        [Test]
        public void VisitedRoomsAlwaysShow()
        {
            Assert.AreEqual(RoomVisibility.Visited, MapRules.Visibility(State(true, false), "forest", "forest_01"));
            Assert.AreEqual(RoomVisibility.Visited, MapRules.Visibility(State(true, true), "forest", "forest_01"));
        }

        [Test]
        public void UnvisitedRoomsAreHiddenUntilTheRegionMapIsBought()
        {
            Assert.AreEqual(RoomVisibility.Hidden, MapRules.Visibility(State(false, false), "forest", "forest_02"));
            Assert.AreEqual(RoomVisibility.Dim, MapRules.Visibility(State(false, true), "forest", "forest_02"));
        }

        [Test]
        public void ABoughtMapOnlyRevealsItsOwnRegion()
        {
            var state = State(false, true);
            Assert.AreEqual(RoomVisibility.Hidden, MapRules.Visibility(state, "cave", "cave_02"));
            Assert.AreEqual(RoomVisibility.Hidden, MapRules.Visibility(null, "forest", "forest_02"));
        }

        [Test]
        public void IconsShowOnlyOnVisitedRoomsAndOpenedChestsDisappear()
        {
            var altar = new MapIcon { kind = MapIconKind.Altar, id = "a", roomId = "forest_01" };
            var chest = new MapIcon { kind = MapIconKind.Chest, id = "chest_forest_01", roomId = "forest_01" };
            var unseen = new MapIcon { kind = MapIconKind.Boss, id = "forest_boss", roomId = "forest_09" };
            var state = State(true, true);
            Assert.IsTrue(MapRules.IconVisible(state, "forest", altar));
            Assert.IsTrue(MapRules.IconVisible(state, "forest", chest));
            Assert.IsFalse(MapRules.IconVisible(state, "forest", unseen), "dim room: no icons");
            state.openedChests.Add("chest_forest_01");
            Assert.IsFalse(MapRules.IconVisible(state, "forest", chest));
            Assert.IsTrue(MapRules.IconVisible(state, "forest", altar));
        }

        [Test]
        public void StandardRoomBoundsBecomeFourByThreeCells()
        {
            var cells = MapRules.CellsFromBounds(new Vector2(0f, 0f), new Vector2(40f, 22f), 10f, Vector2Int.zero);
            Assert.AreEqual(new RectInt(0, 0, 4, 3), cells);
        }

        [Test]
        public void CellsAreRelativeToTheRegionOriginAndSnapOutward()
        {
            var cells = MapRules.CellsFromBounds(new Vector2(105f, -3f), new Vector2(145f, 19f), 10f, new Vector2Int(10, -1));
            Assert.AreEqual(new RectInt(0, 0, 5, 3), cells, "105..145 covers cells 10..14, -3..19 covers -1..1");
            Assert.AreEqual(new RectInt(0, 0, 1, 1), MapRules.CellsFromBounds(Vector2.zero, Vector2.zero, 10f, Vector2Int.zero), "never empty");
        }

        [Test]
        public void CellOfFloorsNegativeCoordinatesToo()
        {
            Assert.AreEqual(new Vector2Int(2, 1), MapRules.CellOf(new Vector2(25f, 10.5f), 10f, Vector2Int.zero));
            Assert.AreEqual(new Vector2Int(-1, -1), MapRules.CellOf(new Vector2(-0.1f, -9.9f), 10f, Vector2Int.zero));
            Assert.AreEqual(new Vector2Int(1, 0), MapRules.CellOf(new Vector2(25f, 10.5f), 10f, new Vector2Int(1, 1)));
        }

        [Test]
        public void NormalizeClampsAndSurvivesDegenerateBoxes()
        {
            Assert.AreEqual(new Vector2(0.25f, 0.5f), MapRules.Normalize(new Vector2(10f, 11f), Vector2.zero, new Vector2(40f, 22f)));
            Assert.AreEqual(new Vector2(1f, 0f), MapRules.Normalize(new Vector2(99f, -5f), Vector2.zero, new Vector2(40f, 22f)));
            Assert.AreEqual(new Vector2(0.5f, 0.5f), MapRules.Normalize(Vector2.one, Vector2.zero, Vector2.zero));
        }
    }
}
