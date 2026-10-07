using System;
using System.Collections.Generic;
using System.IO;

namespace AuraKnight.Editor
{
    /// <summary>All room files under Data/Levels, parsed and validated. The files are the source of truth for every room prefab, scene and the map doc.</summary>
    public static class LevelCatalog
    {
        public const string Extension = ".room.txt";

        /// <summary>Loads every room file (sorted by id). Throws when a file is malformed or two files share an id.</summary>
        public static List<RoomFile> Load(string root = LevelPaths.DataRoot)
        {
            var rooms = new List<RoomFile>();
            if (!Directory.Exists(root)) return rooms;
            var ids = new Dictionary<string, string>();
            foreach (var path in Directory.GetFiles(root, "*" + Extension, SearchOption.AllDirectories))
            {
                var room = RoomFileParser.Parse(File.ReadAllText(path), path.Replace('\\', '/'));
                if (ids.TryGetValue(room.Id, out var first)) throw new FormatException($"{path}: duplicate room id '{room.Id}' (also in {first})");
                ids[room.Id] = path;
                rooms.Add(room);
            }
            rooms.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            return rooms;
        }

        public static bool HasRooms(string regionId, string root = LevelPaths.DataRoot)
        {
            foreach (var room in Load(root))
                if (room.Region == regionId) return true;
            return false;
        }

        public static List<RoomFile> InRegion(IEnumerable<RoomFile> rooms, string regionId)
        {
            var list = new List<RoomFile>();
            foreach (var room in rooms)
                if (room.Region == regionId) list.Add(room);
            return list;
        }

        /// <summary>Altar ids of a region in room-id order (the RegionGraph lists them).</summary>
        public static List<string> AltarIds(IEnumerable<RoomFile> rooms, string regionId)
        {
            var ids = new List<string>();
            foreach (var room in InRegion(rooms, regionId))
                if (!string.IsNullOrEmpty(room.AltarId)) ids.Add(room.AltarId);
            return ids;
        }
    }
}
