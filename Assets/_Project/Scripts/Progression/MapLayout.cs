using UnityEngine;

namespace AuraKnight.Progression
{
    /// <summary>Where each region's local grid sits on the world map (GDD §7.1 diagram): hub in the middle, forest left, cave right, city below, castle above.</summary>
    public static class MapLayout
    {
        public const int Gap = 2;

        /// <summary>
        /// Grid offset of a region given its local size and the hub's size. Unknown regions go right of the cave column, in a row.
        /// <paramref name="extraIndex"/> counts those unknown regions so far (0 for the first).
        /// </summary>
        public static Vector2Int Offset(string regionId, Vector2Int size, Vector2Int hubSize, Vector2Int caveSize, int extraIndex = 0)
        {
            switch (regionId)
            {
                case "hub": return Vector2Int.zero;
                case "forest": return new Vector2Int(-(size.x + Gap), 0);
                case "cave": return new Vector2Int(hubSize.x + Gap, 0);
                case "city": return new Vector2Int(0, -(size.y + Gap));
                case "castle": return new Vector2Int(0, hubSize.y + Gap);
                default: return new Vector2Int(hubSize.x + caveSize.x + 2 * Gap + extraIndex * (size.x + Gap), 0);
            }
        }
    }
}
