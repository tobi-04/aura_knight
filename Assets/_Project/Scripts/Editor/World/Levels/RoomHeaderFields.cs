using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>Parsers of the single header lines of a room file (doors, ids, rewards, requirements, zones, "a,b" vectors).</summary>
    static class RoomHeaderFields
    {
        internal static void ParseDoors(RoomFile file, string value, string source)
        {
            foreach (var token in Split(value))
            {
                int eq = token.IndexOf('=');
                if (eq != 1 || token[0] < '1' || token[0] > '9' || eq == token.Length - 1)
                    throw RoomFileParser.Fail(source, $"door '{token}' is not 'digit=roomId'");
                file.Doors.Add(new RoomDoor { Digit = token[0], Target = token.Substring(2) });
            }
        }

        internal static void ParseIds(RoomFile file, string value, string source)
        {
            foreach (var token in Split(value))
            {
                int eq = token.IndexOf('=');
                if (eq != 1 || RoomFileParser.IdMarkers.IndexOf(token[0]) < 0 || eq == token.Length - 1)
                    throw RoomFileParser.Fail(source, $"ids entry '{token}' is not 'marker=id[,id]' with a marker from {RoomFileParser.IdMarkers}");
                file.Ids[token[0]] = new List<string>(token.Substring(2).Split(','));
            }
        }

        internal static void ParsePairs(string value, string source, string key, Action<string, string> add)
        {
            foreach (var token in Split(value))
            {
                int eq = token.IndexOf('=');
                if (eq <= 0 || eq == token.Length - 1) throw RoomFileParser.Fail(source, $"{key} entry '{token}' is not 'name=value'");
                add(token.Substring(0, eq), token.Substring(eq + 1));
            }
        }

        internal static RoomRequirement ParseRequirement(string target, string needs, string source)
        {
            var ability = LevelAbilities.None;
            foreach (var name in needs.Split('+'))
            {
                if (!LevelAbilityNames.TryParse(name, out var one)) throw RoomFileParser.Fail(source, $"unknown ability '{name}' in requires");
                ability |= one;
            }
            return new RoomRequirement { Target = target, Needs = ability };
        }

        internal static void ParseZones(RoomFile file, string value, string source)
        {
            foreach (var token in Split(value))
            {
                var parts = token.Split('@');
                var box = parts.Length == 2 ? parts[1].Split(',') : null;
                if (box == null || box.Length != 4 || parts[0].Length == 0) throw RoomFileParser.Fail(source, $"zone '{token}' is not 'region@x,y,w,h'");
                var n = new int[4];
                for (int k = 0; k < 4; k++)
                    if (!int.TryParse(box[k], NumberStyles.Integer, CultureInfo.InvariantCulture, out n[k])) throw RoomFileParser.Fail(source, $"zone '{token}' has a non-number");
                file.Zones.Add(new RoomZone { Region = parts[0], X = n[0], Y = n[1], Width = n[2], Height = n[3] });
            }
        }

        internal static Vector2Int ParseVector(string value, char separator, string source, string key)
        {
            var parts = value.Split(separator);
            if (parts.Length != 2 || !int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int x)
                || !int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int y))
                throw RoomFileParser.Fail(source, $"'{key}: {value}' is not two integers separated by '{separator}'");
            return new Vector2Int(x, y);
        }

        internal static string[] Split(string value) => value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    }
}
