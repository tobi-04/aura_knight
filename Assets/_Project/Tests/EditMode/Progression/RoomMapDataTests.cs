using AuraKnight.Progression;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Progression
{
    public sealed class RoomMapDataTests
    {
        static RoomMapData Region(string id, Vector2Int offset, params RectInt[] rects)
        {
            var rooms = new MapRoom[rects.Length];
            for (int i = 0; i < rects.Length; i++) rooms[i] = new MapRoom { roomId = $"{id}_{i + 1:00}", cells = rects[i] };
            return ScriptableObject.CreateInstance<RoomMapData>().Configure(id, 10f, offset, rooms, null);
        }

        [Test]
        public void RoomsAreFoundById()
        {
            var data = Region("forest", Vector2Int.zero, new RectInt(0, 0, 4, 3), new RectInt(4, 0, 4, 3));
            Assert.IsTrue(data.TryGetRoom("forest_02", out var room));
            Assert.AreEqual(new RectInt(4, 0, 4, 3), room.cells);
            Assert.IsFalse(data.TryGetRoom("nope", out _));
        }

        [Test]
        public void UnionCoversEveryRegionAtItsOffset()
        {
            var hub = Region("hub", Vector2Int.zero, new RectInt(0, 0, 4, 3));
            var forest = Region("forest", new Vector2Int(-6, 0), new RectInt(0, 0, 4, 3), new RectInt(4, 0, 4, 3));
            var union = RoomMapData.Union(new[] { hub, forest, null });
            Assert.AreEqual(new RectInt(-6, 0, 10, 3), union);
        }

        [Test]
        public void UnionOfNothingIsEmpty()
        {
            Assert.AreEqual(0, RoomMapData.Union(new RoomMapData[0]).width);
            Assert.AreEqual(0, RoomMapData.Union(new[] { Region("hub", Vector2Int.zero) }).width);
        }

        [Test]
        public void LayoutPutsRegionsWhereTheGddDiagramDoes()
        {
            var hub = new Vector2Int(12, 3);
            var cave = new Vector2Int(20, 6);
            Assert.AreEqual(Vector2Int.zero, MapLayout.Offset("hub", hub, hub, cave));
            Assert.AreEqual(new Vector2Int(-(10 + MapLayout.Gap), 0), MapLayout.Offset("forest", new Vector2Int(10, 5), hub, cave));
            Assert.AreEqual(new Vector2Int(12 + MapLayout.Gap, 0), MapLayout.Offset("cave", cave, hub, cave));
            Assert.AreEqual(new Vector2Int(0, -(7 + MapLayout.Gap)), MapLayout.Offset("city", new Vector2Int(8, 7), hub, cave));
            Assert.AreEqual(new Vector2Int(0, 3 + MapLayout.Gap), MapLayout.Offset("castle", new Vector2Int(8, 7), hub, cave));
        }

        [Test]
        public void LayoutRegionsDoNotOverlapTheHubOrEachOther()
        {
            var hubSize = new Vector2Int(12, 3);
            var caveSize = new Vector2Int(20, 6);
            var rects = new[]
            {
                new RectInt(Vector2Int.zero, hubSize),
                new RectInt(MapLayout.Offset("forest", new Vector2Int(10, 5), hubSize, caveSize), new Vector2Int(10, 5)),
                new RectInt(MapLayout.Offset("cave", caveSize, hubSize, caveSize), caveSize),
                new RectInt(MapLayout.Offset("city", new Vector2Int(8, 7), hubSize, caveSize), new Vector2Int(8, 7)),
                new RectInt(MapLayout.Offset("castle", new Vector2Int(8, 7), hubSize, caveSize), new Vector2Int(8, 7)),
            };
            for (int i = 0; i < rects.Length; i++)
                for (int j = i + 1; j < rects.Length; j++)
                    Assert.IsFalse(rects[i].Overlaps(rects[j]), $"regions {i} and {j} overlap");
        }
    }
}
