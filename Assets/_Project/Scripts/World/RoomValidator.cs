using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AuraKnight.World
{
    /// <summary>Editor-independent snapshot of one room prefab, used by validation rules.</summary>
    public sealed class RoomInfo
    {
        public readonly string Id;
        public readonly string RegionId;
        public readonly string Source;
        public readonly IReadOnlyList<string> ExitTargets;
        public readonly IReadOnlyList<string> PersistentIds;
        /// <summary>Sun altar ids placed inside the room.</summary>
        public readonly IReadOnlyList<string> AltarIds;
        /// <summary>Names of Burn/Extinguish gates that have no PersistentId (their open state could not be saved).</summary>
        public readonly IReadOnlyList<string> GatesWithoutPersistentId;
        /// <summary>Names of objects carrying a Hitbox and a Hurtbox of different teams (one GameObject has one layer, so one of them would be on the wrong layer).</summary>
        public readonly IReadOnlyList<string> MixedTeamCombatObjects;

        public RoomInfo(string id, string regionId, string source,
                        IReadOnlyList<string> exitTargets = null, IReadOnlyList<string> persistentIds = null,
                        IReadOnlyList<string> altarIds = null, IReadOnlyList<string> gatesWithoutPersistentId = null,
                        IReadOnlyList<string> mixedTeamCombatObjects = null)
        {
            Id = id;
            RegionId = regionId;
            Source = source;
            ExitTargets = exitTargets ?? new string[0];
            PersistentIds = persistentIds ?? new string[0];
            AltarIds = altarIds ?? new string[0];
            GatesWithoutPersistentId = gatesWithoutPersistentId ?? new string[0];
            MixedTeamCombatObjects = mixedTeamCombatObjects ?? new string[0];
        }
    }

    /// <summary>Pure validation rules for room ids, exits and persistent ids.</summary>
    public static class RoomValidator
    {
        static readonly Regex IdPattern = new(@"^[a-z0-9]+(_[a-z0-9]+)+$", RegexOptions.Compiled);

        /// <summary>Lowercase segments joined by underscores, e.g. forest_03 or forest_boss.</summary>
        public static bool IsValidId(string id) => !string.IsNullOrEmpty(id) && IdPattern.IsMatch(id);

        /// <summary>Room rules plus, when the region list is given, the altar directory rules for every placed altar.</summary>
        public static List<string> Validate(IEnumerable<RoomInfo> rooms, IReadOnlyList<RegionNode> regions)
        {
            var list = new List<RoomInfo>(rooms);
            var errors = Validate(list);
            if (regions == null) return errors;
            foreach (var room in list)
                errors.AddRange(AltarValidator.ValidatePlacement(regions, room.RegionId, room.AltarIds,
                    $"room '{room.Id}' ({room.Source})", requireAllListed: false));
            return errors;
        }

        public static List<string> Validate(IEnumerable<RoomInfo> rooms)
        {
            var errors = new List<string>();
            var list = new List<RoomInfo>(rooms);
            var ids = new Dictionary<string, RoomInfo>();
            var persistent = new Dictionary<string, RoomInfo>();

            foreach (var room in list)
            {
                if (!IsValidId(room.Id))
                    errors.Add($"Invalid room id '{room.Id}' in {room.Source} (expected lowercase like forest_03).");
                else if (!string.IsNullOrEmpty(room.RegionId) && !room.Id.StartsWith(room.RegionId + "_"))
                    errors.Add($"Room '{room.Id}' in {room.Source} does not start with its region '{room.RegionId}_'.");

                if (!string.IsNullOrEmpty(room.Id))
                {
                    if (ids.TryGetValue(room.Id, out var first))
                        errors.Add($"Duplicate room id '{room.Id}' in {room.Source} and {first.Source}.");
                    else ids[room.Id] = room;
                }

                foreach (var gate in room.GatesWithoutPersistentId)
                    errors.Add($"Room '{room.Id}' ({room.Source}): one-time gate '{gate}' (Burn/Extinguish) needs a PersistentId, " +
                               "otherwise its open state is lost on reload.");

                foreach (var name in room.MixedTeamCombatObjects)
                    errors.Add($"Room '{room.Id}' ({room.Source}): '{name}' has a Hitbox and a Hurtbox of different teams; " +
                               "put them on separate child objects so each sits on its own physics layer.");

                foreach (var pid in room.PersistentIds)
                {
                    if (string.IsNullOrEmpty(pid))
                        errors.Add($"Room '{room.Id}' ({room.Source}) has an empty persistent id.");
                    else if (persistent.TryGetValue(pid, out var other))
                        errors.Add($"Duplicate persistent id '{pid}' in {room.Source} and {other.Source}.");
                    else persistent[pid] = room;
                }
            }

            foreach (var room in list)
            foreach (var target in room.ExitTargets)
            {
                if (string.IsNullOrEmpty(target))
                    errors.Add($"Room '{room.Id}' ({room.Source}) has an exit with an empty target.");
                else if (!ids.ContainsKey(target))
                    errors.Add($"Room '{room.Id}' ({room.Source}) has an exit to missing room '{target}'.");
            }
            return errors;
        }
    }
}
