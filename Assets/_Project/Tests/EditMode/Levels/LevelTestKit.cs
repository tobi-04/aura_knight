using System;
using System.Collections.Generic;
using System.IO;
using AuraKnight.Editor;

namespace AuraKnight.Tests.Levels
{
    /// <summary>Shared helpers: the real catalog, tiny hand-written rooms, and a way to edit a real room file's text and re-parse it.</summary>
    static class LevelTestKit
    {
        static List<RoomFile> _catalog;

        public static IReadOnlyList<RoomFile> Catalog => _catalog ??= LevelCatalog.Load();

        public static RoomFile Room(string id)
        {
            foreach (var room in Catalog)
                if (room.Id == id) return room;
            throw new InvalidOperationException($"no room file {id}");
        }

        /// <summary>The catalog with one room replaced by an edited copy of its file text (to prove the validators catch a broken room).</summary>
        public static List<RoomFile> WithEdited(string id, Func<string, string> edit)
        {
            var list = new List<RoomFile>(Catalog);
            int index = list.FindIndex(r => r.Id == id);
            var original = list[index];
            string edited = edit(File.ReadAllText(original.Source));
            list[index] = RoomFileParser.Parse(edited, original.Source + " (edited)");
            return list;
        }

        /// <summary>Header + grid for a small test room (no doors, no altar). Rows are given top first.</summary>
        public static RoomFile Mini(params string[] rows) =>
            RoomFileParser.Parse($"id: test_01\nregion: forest\nslot: 0,0\nsize: {rows[0].Length}x{rows.Length}\ntitle: t\n---\n{string.Join("\n", rows)}\n", "mini");

        /// <summary>Like <see cref="Mini"/> with extra header lines (for markers that need ids, e.g. "ids: Z=gate_a").</summary>
        public static RoomFile MiniWith(string extraHeader, params string[] rows) =>
            RoomFileParser.Parse($"id: test_01\nregion: forest\nslot: 0,0\nsize: {rows[0].Length}x{rows.Length}\ntitle: t\n{extraHeader}\n---\n{string.Join("\n", rows)}\n", "mini");

        public static string Repeat(char c, int n) => new string(c, n);
    }
}
