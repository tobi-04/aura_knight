using System;
using System.Collections.Generic;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>Pure grid helpers for the room builder.</summary>
    public static class RoomGeometry
    {
        /// <summary>Covers every filled cell with as few axis-aligned rectangles as a greedy row-major sweep finds (one collider per rectangle).</summary>
        public static List<RectInt> MergeRects(int width, int height, Func<int, int, bool> filled)
        {
            var rects = new List<RectInt>();
            var used = new bool[width, height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (used[x, y] || !filled(x, y)) continue;
                    int x2 = x;
                    while (x2 + 1 < width && !used[x2 + 1, y] && filled(x2 + 1, y)) x2++;
                    int y2 = y;
                    while (y2 + 1 < height && RowFree(used, filled, x, x2, y2 + 1)) y2++;
                    for (int yy = y; yy <= y2; yy++)
                        for (int xx = x; xx <= x2; xx++) used[xx, yy] = true;
                    rects.Add(new RectInt(x, y, x2 - x + 1, y2 - y + 1));
                }
            }
            return rects;
        }

        static bool RowFree(bool[,] used, Func<int, int, bool> filled, int x0, int x1, int y)
        {
            for (int x = x0; x <= x1; x++)
                if (used[x, y] || !filled(x, y)) return false;
            return true;
        }

        /// <summary>Bounding box of a group of cells as a rectangle in cell units.</summary>
        public static RectInt Bounds(IReadOnlyList<Vector2Int> cells)
        {
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var c in cells)
            {
                minX = Mathf.Min(minX, c.x); maxX = Mathf.Max(maxX, c.x);
                minY = Mathf.Min(minY, c.y); maxY = Mathf.Max(maxY, c.y);
            }
            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        /// <summary>Centre of a cell rectangle in room units.</summary>
        public static Vector2 Center(RectInt r) => new Vector2(r.x + r.width * 0.5f, r.y + r.height * 0.5f);
    }
}
