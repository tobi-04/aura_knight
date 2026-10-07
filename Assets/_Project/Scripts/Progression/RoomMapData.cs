using System;
using System.Collections.Generic;
using UnityEngine;

namespace AuraKnight.Progression
{
    public enum MapIconKind
    {
        Altar = 0,
        Boss = 1,
        Chest = 2,
        Shortcut = 3,
    }

    /// <summary>A room on the map: its id and the grid cells it covers (x/y from the region's own bottom-left, y up).</summary>
    [Serializable]
    public struct MapRoom
    {
        public string roomId;
        public RectInt cells;
    }

    /// <summary>An icon on a room. <see cref="id"/> is the PersistentId/altar id (a chest icon hides once that chest is opened).</summary>
    [Serializable]
    public struct MapIcon
    {
        public MapIconKind kind;
        public string id;
        public string roomId;
        public Vector2Int cell;
    }

    /// <summary>
    /// Map layout of one region: room id to grid rect plus icons. Filled by the editor tool
    /// (<c>Aura/Progression/Rebuild Map Data</c>) from the rooms placed in the region scenes; never edited by hand.
    /// </summary>
    [CreateAssetMenu(menuName = "Aura/Room Map Data", fileName = "RoomMapData")]
    public sealed class RoomMapData : ScriptableObject
    {
        [SerializeField] string regionId;
        [Tooltip("World units per map cell (rooms are 40 x 22 units, so 10 gives about 4 x 2 cells).")]
        [SerializeField, Min(1f)] float cellWorldSize = 10f;
        [Tooltip("Where this region's local grid sits on the world map (hub at 0,0; forest left, cave right, city below, castle above).")]
        [SerializeField] Vector2Int gridOffset;
        [SerializeField] List<MapRoom> rooms = new();
        [SerializeField] List<MapIcon> icons = new();

        public string RegionId => regionId;
        public float CellWorldSize => cellWorldSize;
        public Vector2Int GridOffset => gridOffset;
        public IReadOnlyList<MapRoom> Rooms => rooms;
        public IReadOnlyList<MapIcon> Icons => icons;

        public RoomMapData Configure(string region, float cellSize, Vector2Int offset, IEnumerable<MapRoom> mapRooms, IEnumerable<MapIcon> mapIcons)
        {
            regionId = region;
            cellWorldSize = Mathf.Max(1f, cellSize);
            gridOffset = offset;
            rooms = new List<MapRoom>(mapRooms ?? Array.Empty<MapRoom>());
            icons = new List<MapIcon>(mapIcons ?? Array.Empty<MapIcon>());
            return this;
        }

        public bool TryGetRoom(string roomId, out MapRoom room)
        {
            foreach (var r in rooms)
            {
                if (r.roomId != roomId) continue;
                room = r;
                return true;
            }
            room = default;
            return false;
        }

        /// <summary>A local cell rect moved to world-map grid coordinates.</summary>
        public RectInt ToWorldCells(RectInt local) => new RectInt(local.x + gridOffset.x, local.y + gridOffset.y, local.width, local.height);

        /// <summary>Smallest rect (world-map cells) holding every room of every region; zero size when there are none.</summary>
        public static RectInt Union(IEnumerable<RoomMapData> regions)
        {
            bool any = false;
            int xMin = 0, yMin = 0, xMax = 0, yMax = 0;
            foreach (var data in regions)
            {
                if (data == null) continue;
                foreach (var room in data.rooms)
                {
                    var r = data.ToWorldCells(room.cells);
                    xMin = any ? Mathf.Min(xMin, r.xMin) : r.xMin;
                    yMin = any ? Mathf.Min(yMin, r.yMin) : r.yMin;
                    xMax = any ? Mathf.Max(xMax, r.xMax) : r.xMax;
                    yMax = any ? Mathf.Max(yMax, r.yMax) : r.yMax;
                    any = true;
                }
            }
            return any ? new RectInt(xMin, yMin, xMax - xMin, yMax - yMin) : new RectInt(0, 0, 0, 0);
        }
    }
}
