using AuraKnight.Core;
using UnityEngine;

namespace AuraKnight.Progression
{
    public enum RoomVisibility
    {
        /// <summary>Not drawn at all.</summary>
        Hidden,
        /// <summary>Drawn faint: unvisited room of a region whose map was bought.</summary>
        Dim,
        /// <summary>Drawn in the region colour: Leo has been there.</summary>
        Visited,
    }

    /// <summary>Pure map rules: which rooms and icons show, and grid maths for the editor tool and Leo's marker.</summary>
    public static class MapRules
    {
        public static bool OwnsRegionMap(GameState state, string regionId) =>
            state != null && !string.IsNullOrEmpty(regionId) && state.GetPurchaseCount(ShopItem.MapItemId(regionId)) > 0;

        public static RoomVisibility Visibility(GameState state, string regionId, string roomId)
        {
            if (state == null) return RoomVisibility.Hidden;
            if (state.visitedRooms.Contains(roomId)) return RoomVisibility.Visited;
            return OwnsRegionMap(state, regionId) ? RoomVisibility.Dim : RoomVisibility.Hidden;
        }

        /// <summary>Icons only show on rooms Leo has seen; an opened chest no longer shows.</summary>
        public static bool IconVisible(GameState state, string regionId, MapIcon icon)
        {
            if (Visibility(state, regionId, icon.roomId) != RoomVisibility.Visited) return false;
            return icon.kind != MapIconKind.Chest || !ChestLogic.IsOpen(state, icon.id);
        }

        /// <summary>Cells covered by a world-space box (floor/ceil so the room is always covered), relative to <paramref name="origin"/>; at least 1 x 1.</summary>
        public static RectInt CellsFromBounds(Vector2 min, Vector2 max, float cellSize, Vector2Int origin)
        {
            float cell = Mathf.Max(0.0001f, cellSize);
            int x0 = Mathf.FloorToInt(min.x / cell + 0.001f);
            int y0 = Mathf.FloorToInt(min.y / cell + 0.001f);
            int x1 = Mathf.CeilToInt(max.x / cell - 0.001f);
            int y1 = Mathf.CeilToInt(max.y / cell - 0.001f);
            return new RectInt(x0 - origin.x, y0 - origin.y, Mathf.Max(1, x1 - x0), Mathf.Max(1, y1 - y0));
        }

        public static Vector2Int CellOf(Vector2 point, float cellSize, Vector2Int origin)
        {
            float cell = Mathf.Max(0.0001f, cellSize);
            return new Vector2Int(Mathf.FloorToInt(point.x / cell) - origin.x, Mathf.FloorToInt(point.y / cell) - origin.y);
        }

        /// <summary>Position inside a box as 0..1 on each axis (clamped; a degenerate box gives the centre).</summary>
        public static Vector2 Normalize(Vector2 point, Vector2 min, Vector2 max)
        {
            var size = max - min;
            float x = size.x > 0.0001f ? Mathf.Clamp01((point.x - min.x) / size.x) : 0.5f;
            float y = size.y > 0.0001f ? Mathf.Clamp01((point.y - min.y) / size.y) : 0.5f;
            return new Vector2(x, y);
        }
    }
}
