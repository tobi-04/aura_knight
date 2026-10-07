using System.Collections.Generic;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>What a simulated playthrough reached.</summary>
    public sealed class ProgressionResult
    {
        public readonly HashSet<string> Rooms = new HashSet<string>();
        public readonly HashSet<string> BossRooms = new HashSet<string>();
        public readonly HashSet<string> OpenedShortcuts = new HashSet<string>();
        public readonly List<string> Problems = new List<string>();
        public LevelAbilities Abilities;

        public bool Reached(string roomId) => Rooms.Contains(roomId) || BossRooms.Contains(roomId);
    }

    /// <summary>
    /// Plays the whole game on paper: start at the hub altar, walk every reachable door of every room (grid reachability with the
    /// current abilities), and gain an Aura each time a boss room is reached (Forest Wind, Cave Fire, City Water), until nothing new
    /// opens. Proves the main path from the start to Malakor exists and that each region is closed until its Aura is held.
    /// </summary>
    public static class LevelProgression
    {
        public const string StartRoom = "hub_01";

        /// <param name="grantAuras">When false the bosses give nothing, to ask "what is open with exactly these abilities?".</param>
        public static ProgressionResult Simulate(IReadOnlyList<RoomFile> rooms, LevelAbilities start = LevelAbilities.None,
                                                 bool grantAuras = true, ReachMode mode = ReachMode.Safe)
        {
            var result = new ProgressionResult { Abilities = start };
            var byId = new Dictionary<string, RoomFile>();
            foreach (var room in rooms) byId[room.Id] = room;
            if (!byId.TryGetValue(StartRoom, out var first) || first.Cells('A').Count == 0)
            {
                result.Problems.Add($"{StartRoom} with its altar is missing");
                return result;
            }
            var cache = new Dictionary<string, HashSet<Vector2Int>>();
            int before;
            do
            {
                before = result.Rooms.Count + result.BossRooms.Count + result.OpenedShortcuts.Count + (int)result.Abilities;
                Walk(byId, first, result, mode, cache);
                if (grantAuras) GrantRewards(result);
            }
            while (before != result.Rooms.Count + result.BossRooms.Count + result.OpenedShortcuts.Count + (int)result.Abilities);
            return result;
        }

        static void Walk(Dictionary<string, RoomFile> byId, RoomFile first, ProgressionResult result, ReachMode mode,
                         Dictionary<string, HashSet<Vector2Int>> cache)
        {
            var queue = new Queue<(RoomFile room, Vector2Int cell)>();
            var seen = new HashSet<string>();
            queue.Enqueue((first, first.Cells('A')[0]));
            while (queue.Count > 0)
            {
                var (room, cell) = queue.Dequeue();
                if (!seen.Add($"{room.Id}@{cell.x},{cell.y}")) continue;
                result.Rooms.Add(room.Id);
                var ab = result.Abilities;
                if (result.OpenedShortcuts.Contains(room.Id)) ab |= LevelAbilities.ShortcutOpen;
                string key = $"{room.Id}@{cell.x},{cell.y}/{(int)ab}/{(int)mode}";
                if (!cache.TryGetValue(key, out var reach)) cache[key] = reach = LevelReachability.Reach(room, cell, ab, mode);
                foreach (var door in room.Doors)
                {
                    if (!LevelReachability.ReachesAny(reach, room.Cells(door.Digit))) continue;
                    Cross(byId, room, door, result, queue);
                }
            }
        }

        static void Cross(Dictionary<string, RoomFile> byId, RoomFile room, RoomDoor door, ProgressionResult result,
                          Queue<(RoomFile room, Vector2Int cell)> queue)
        {
            var region = LevelRegions.Get(room.Region);
            if (region != null && door.Target == region.BossRoomId) { result.BossRooms.Add(door.Target); return; }
            if (!byId.TryGetValue(door.Target, out var target)) { result.Problems.Add($"{room.Id} door {door.Digit} leads to missing room {door.Target}"); return; }
            var back = target.DoorTo(room.Id);
            if (back == null) { result.Problems.Add($"{target.Id} has no door back to {room.Id}"); return; }
            if (back.Digit == '7' && (target.Cells('k').Count > 0 || target.Cells('j').Count > 0)) result.OpenedShortcuts.Add(target.Id);
            queue.Enqueue((target, target.SpawnCell(back)));
        }

        static void GrantRewards(ProgressionResult result)
        {
            foreach (var region in LevelRegions.All)
            {
                if (!region.HasBoss || region.BossReward == null || !result.BossRooms.Contains(region.BossRoomId)) continue;
                if (LevelAbilityNames.TryParse(region.BossReward.ToLowerInvariant(), out var aura)) result.Abilities |= aura;
            }
            bool all = (result.Abilities & (LevelAbilities.Wind | LevelAbilities.Fire | LevelAbilities.Water))
                       == (LevelAbilities.Wind | LevelAbilities.Fire | LevelAbilities.Water);
            if (all) result.Abilities |= LevelAbilities.Seals;
        }
    }
}
