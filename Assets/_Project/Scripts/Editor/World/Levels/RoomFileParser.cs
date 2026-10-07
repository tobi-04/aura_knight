using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Reads and validates the text of a .room.txt file. Everything that comes from the file is checked here (sizes, legend,
    /// door placement, id counts), so the rest of the level tooling can trust a <see cref="RoomFile"/>. Bad input throws
    /// <see cref="FormatException"/> naming the file and the problem.
    /// </summary>
    public static class RoomFileParser
    {
        /// <summary>Every character a room grid may contain (docs/level-map.md section "Legend").</summary>
        public const string Legend = ".#W=^KcshPaMf~YACTZGQkjNbmBSpzng123456789";
        /// <summary>Markers that need an id in the "ids" line (their state is saved or looked up by it).</summary>
        public const string IdMarkers = "CTZGQkjf";

        public static RoomFile Parse(string text, string source)
        {
            if (string.IsNullOrWhiteSpace(text)) throw Fail(source, "the file is empty");
            var lines = text.Replace("\r", "").Split('\n');
            var file = new RoomFile { Source = source };
            int i = ReadHeader(file, lines, source);
            ReadGrid(file, lines, i, source);
            ValidateDoors(file, source);
            ValidateIds(file, source);
            return file;
        }

        static int ReadHeader(RoomFile file, string[] lines, string source)
        {
            int i = 0;
            for (; i < lines.Length && lines[i].Trim() != "---"; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                int colon = line.IndexOf(':');
                if (colon <= 0) throw Fail(source, $"header line '{line}' is not 'key: value'");
                ApplyKey(file, line.Substring(0, colon).Trim(), line.Substring(colon + 1).Trim(), source);
            }
            if (i >= lines.Length) throw Fail(source, "missing the '---' line before the grid");
            foreach (var (name, value) in new[] { ("id", file.Id), ("region", file.Region), ("title", file.Title) })
                if (string.IsNullOrEmpty(value)) throw Fail(source, $"missing header '{name}'");
            if (file.Width <= 0 || file.Height <= 0) throw Fail(source, "missing header 'size: WxH'");
            return i + 1;
        }

        static void ApplyKey(RoomFile file, string key, string value, string source)
        {
            switch (key)
            {
                case "id": file.Id = value; break;
                case "region": file.Region = value; break;
                case "title": file.Title = value; break;
                case "altar": file.AltarId = value; break;
                case "preload": file.PreloadRegion = value; break;
                case "slot": file.SlotCell = RoomHeaderFields.ParseVector(value, ',', source, key); break;
                case "size": { var v = RoomHeaderFields.ParseVector(value, 'x', source, key); file.Width = v.x; file.Height = v.y; break; }
                case "doors": RoomHeaderFields.ParseDoors(file, value, source); break;
                case "ids": RoomHeaderFields.ParseIds(file, value, source); break;
                case "rewards": RoomHeaderFields.ParsePairs(value, source, key, (id, effect) => file.Rewards[id] = effect); break;
                case "requires": RoomHeaderFields.ParsePairs(value, source, key, (target, needs) => file.Requires.Add(RoomHeaderFields.ParseRequirement(target, needs, source))); break;
                case "zones": RoomHeaderFields.ParseZones(file, value, source); break;
                default: throw Fail(source, $"unknown header '{key}'");
            }
        }

        static void ReadGrid(RoomFile file, string[] lines, int start, string source)
        {
            var rows = new List<string>();
            for (int i = start; i < lines.Length; i++)
            {
                string row = lines[i];
                if (row.Length == 0) { if (i == lines.Length - 1) break; throw Fail(source, $"blank line {i + 1} inside the grid"); }
                rows.Add(row);
            }
            if (rows.Count != file.Height) throw Fail(source, $"the grid has {rows.Count} rows but size says {file.Height}");
            for (int r = 0; r < rows.Count; r++)
            {
                if (rows[r].Length != file.Width) throw Fail(source, $"grid row {r + 1} has {rows[r].Length} cells, expected {file.Width}");
                foreach (char c in rows[r])
                    if (Legend.IndexOf(c) < 0) throw Fail(source, $"grid row {r + 1} contains '{c}', which is not in the legend");
            }
            file.Rows = rows.ToArray();
        }

        static void ValidateDoors(RoomFile file, string source)
        {
            var digits = new HashSet<char>();
            foreach (var door in file.Doors)
            {
                var cells = file.Cells(door.Digit);
                if (cells.Count == 0) throw Fail(source, $"door {door.Digit} is declared but not drawn");
                int x = cells[0].x;
                if (x != 0 && x != file.Width - 1) throw Fail(source, $"door {door.Digit} must sit on the left or right wall column");
                int min = int.MaxValue, max = int.MinValue;
                foreach (var c in cells)
                {
                    if (c.x != x) throw Fail(source, $"door {door.Digit} spans two wall columns");
                    min = Mathf.Min(min, c.y);
                    max = Mathf.Max(max, c.y);
                }
                if (max - min + 1 != cells.Count) throw Fail(source, $"door {door.Digit} cells are not one vertical strip");
                if (cells.Count < 3) throw Fail(source, $"door {door.Digit} must be at least 3 cells tall");
                door.X = x; door.YMin = min; door.YMax = max; door.OnLeft = x == 0;
                digits.Add(door.Digit);
            }
            for (char d = '1'; d <= '9'; d++)
                if (file.Cells(d).Count > 0 && !digits.Contains(d)) throw Fail(source, $"door {d} is drawn but has no target in 'doors'");
            var targets = new HashSet<string>();
            foreach (var door in file.Doors)
                if (!targets.Add(door.Target)) throw Fail(source, $"two doors lead to '{door.Target}' (spawn names would clash)");
        }

        static void ValidateIds(RoomFile file, string source)
        {
            foreach (char marker in IdMarkers)
            {
                int groups = file.Groups(marker).Count;
                int ids = file.IdsOf(marker).Count;
                if (groups != ids) throw Fail(source, $"'{marker}' has {groups} group(s) in the grid but {ids} id(s) in 'ids'");
            }
            int altars = file.Cells('A').Count;
            if (altars > 1) throw Fail(source, "a room holds at most one altar");
            if (altars == 1 && string.IsNullOrEmpty(file.AltarId)) throw Fail(source, "the grid has an altar but there is no 'altar' header");
            if (altars == 0 && !string.IsNullOrEmpty(file.AltarId)) throw Fail(source, "'altar' header without an 'A' in the grid");
            foreach (var id in file.Rewards.Keys)
                if (!file.Ids.TryGetValue('C', out var chests) || !chests.Contains(id)) throw Fail(source, $"reward for unknown chest '{id}'");
        }

        internal static FormatException Fail(string source, string message) => new FormatException($"{source}: {message}");
    }
}
