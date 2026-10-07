using System;
using System.Collections.Generic;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>A doorway on the left or right wall: the cells carrying <see cref="Digit"/> become a RoomExit to <see cref="Target"/>.</summary>
    public sealed class RoomDoor
    {
        public char Digit;
        public string Target;
        /// <summary>Column of the doorway cells (0 or width - 1).</summary>
        public int X;
        /// <summary>Lowest and highest row of the doorway cells.</summary>
        public int YMin, YMax;
        public bool OnLeft;
        public int Height => YMax - YMin + 1;
    }

    /// <summary>"The cell or id `Target` is only reachable with <see cref="Needs"/>" (a door digit, or a chest / seal id).</summary>
    public sealed class RoomRequirement
    {
        public string Target;
        public LevelAbilities Needs;
    }

    /// <summary>An extra region to keep preloaded while Leo stands in the cell rectangle.</summary>
    public sealed class RoomZone
    {
        public string Region;
        public int X, Y, Width, Height;
    }

    /// <summary>
    /// One room of the game as written in a .room.txt file (see docs/level-map.md for the legend). Cells are addressed with
    /// y pointing up (row 0 is the floor row, the last text line); the text lists the top row first.
    /// </summary>
    public sealed class RoomFile
    {
        /// <summary>Spawn point of a Leo that comes through a door: this many cells inside the room.</summary>
        public const int SpawnInset = 3;
        public const int Slot = 40;

        public string Id, Region, Title, AltarId, PreloadRegion, Source;
        public Vector2Int SlotCell;
        public int Width, Height;
        public string[] Rows;
        public readonly List<RoomDoor> Doors = new List<RoomDoor>();
        public readonly Dictionary<char, List<string>> Ids = new Dictionary<char, List<string>>();
        public readonly Dictionary<string, string> Rewards = new Dictionary<string, string>();
        public readonly List<RoomRequirement> Requires = new List<RoomRequirement>();
        public readonly List<RoomZone> Zones = new List<RoomZone>();

        public bool IsVertical => Height > Width;

        /// <summary>The cell character; anything outside the room counts as solid ground.</summary>
        public char At(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return '#';
            return Rows[Height - 1 - y][x];
        }

        public List<Vector2Int> Cells(char marker)
        {
            var list = new List<Vector2Int>();
            for (int y = Height - 1; y >= 0; y--)
                for (int x = 0; x < Width; x++)
                    if (At(x, y) == marker) list.Add(new Vector2Int(x, y));
            return list;
        }

        /// <summary>4-connected groups of the marker, in reading order (top to bottom, left to right).</summary>
        public List<List<Vector2Int>> Groups(char marker)
        {
            var groups = new List<List<Vector2Int>>();
            var seen = new HashSet<Vector2Int>();
            foreach (var start in Cells(marker))
            {
                if (!seen.Add(start)) continue;
                var group = new List<Vector2Int> { start };
                for (int i = 0; i < group.Count; i++)
                {
                    var c = group[i];
                    foreach (var n in new[] { c + Vector2Int.right, c + Vector2Int.left, c + Vector2Int.up, c + Vector2Int.down })
                        if (At(n.x, n.y) == marker && seen.Add(n)) group.Add(n);
                }
                groups.Add(group);
            }
            return groups;
        }

        public RoomDoor DoorTo(string target) => Doors.Find(d => d.Target == target);

        public RoomDoor DoorByDigit(char digit) => Doors.Find(d => d.Digit == digit);

        /// <summary>Spawn cell (standing cell) of a Leo arriving through the doorway.</summary>
        public Vector2Int SpawnCell(RoomDoor door) =>
            new Vector2Int(door.OnLeft ? SpawnInset : Width - 1 - SpawnInset, door.YMin);

        /// <summary>Name of the spawn point other rooms send Leo to when they come from <paramref name="sourceRoomId"/>.</summary>
        public static string SpawnName(string sourceRoomId) => "from_" + sourceRoomId;

        /// <summary>Spawn name a RoomExit must use to enter <paramref name="targetRoomId"/> from <paramref name="sourceRoomId"/> (boss arenas have only "default").</summary>
        public static string EntrySpawnFor(string targetRoomId, string sourceRoomId) =>
            targetRoomId.EndsWith("_boss", StringComparison.Ordinal) ? "default" : SpawnName(sourceRoomId);

        /// <summary>Ids of a marker (chests, gates, seals...) in reading order of their groups.</summary>
        public IReadOnlyList<string> IdsOf(char marker) =>
            Ids.TryGetValue(marker, out var list) ? list : (IReadOnlyList<string>)Array.Empty<string>();

        /// <summary>The cells of the marker group carrying <paramref name="id"/>, or an empty list.</summary>
        public List<Vector2Int> CellsOfId(string id)
        {
            foreach (var pair in Ids)
            {
                int index = pair.Value.IndexOf(id);
                if (index < 0) continue;
                var groups = Groups(pair.Key);
                return index < groups.Count ? groups[index] : new List<Vector2Int>();
            }
            return new List<Vector2Int>();
        }
    }
}
