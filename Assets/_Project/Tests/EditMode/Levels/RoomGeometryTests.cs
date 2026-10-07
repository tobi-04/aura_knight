using System.Collections.Generic;
using AuraKnight.Editor;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Levels
{
    public sealed class RoomGeometryTests
    {
        [Test]
        public void MergedRectanglesCoverEachFilledCellExactlyOnce()
        {
            var room = LevelTestKit.Mini("##########", "#..#..#..#", "#.######.#", "##########");
            var rects = RoomGeometry.MergeRects(room.Width, room.Height, (x, y) => room.At(x, y) == '#');
            var covered = new HashSet<Vector2Int>();
            foreach (var r in rects)
                for (int x = r.xMin; x < r.xMax; x++)
                    for (int y = r.yMin; y < r.yMax; y++)
                        Assert.IsTrue(covered.Add(new Vector2Int(x, y)), $"cell {x},{y} covered twice");
            int filled = 0;
            for (int x = 0; x < room.Width; x++)
                for (int y = 0; y < room.Height; y++)
                    if (room.At(x, y) == '#') { filled++; Assert.IsTrue(covered.Contains(new Vector2Int(x, y)), $"cell {x},{y} not covered"); }
            Assert.AreEqual(filled, covered.Count);
        }

        [Test]
        public void ASolidBlockIsOneRectangle()
        {
            var rects = RoomGeometry.MergeRects(5, 4, (x, y) => true);
            Assert.AreEqual(1, rects.Count);
            Assert.AreEqual(new RectInt(0, 0, 5, 4), rects[0]);
        }

        [Test]
        public void NothingFilledGivesNoRectangles() => Assert.IsEmpty(RoomGeometry.MergeRects(5, 4, (x, y) => false));

        [Test]
        public void BoundsAndCentre()
        {
            var cells = new[] { new Vector2Int(3, 4), new Vector2Int(5, 4), new Vector2Int(4, 6) };
            var r = RoomGeometry.Bounds(cells);
            Assert.AreEqual(new RectInt(3, 4, 3, 3), r);
            Assert.AreEqual(new Vector2(4.5f, 5.5f), RoomGeometry.Center(r));
        }
    }
}
