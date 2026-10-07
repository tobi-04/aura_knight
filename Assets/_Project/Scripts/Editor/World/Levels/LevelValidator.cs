using System.Collections.Generic;
using AuraKnight.World;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Rules over the room files: ids and sizes, doors that match on both sides, markers standing on something, at most 6 enemies,
    /// every door/altar/chest reachable, every gate unreachable without its Aura (generous movement model), the shortcut closed from
    /// outside, and a whole-game walk from the hub altar to the last boss. Returns human-readable problems (empty = valid).
    /// </summary>
    public static class LevelValidator
    {
        public const int MaxEnemiesPerRoom = 6;

        public static List<string> ValidateFiles(IReadOnlyList<RoomFile> rooms)
        {
            var problems = new List<string>();
            var byId = new Dictionary<string, RoomFile>();
            foreach (var room in rooms) byId[room.Id] = room;
            foreach (var room in rooms)
            {
                Basics(room, problems);
                Doors(room, byId, problems);
                Markers(room, problems);
                SpawnSafety(room, problems);
                Reachability(room, problems);
                Requirements(room, problems);
                ShortcutLock(room, problems);
            }
            var all = LevelProgression.Simulate(rooms);
            problems.AddRange(all.Problems);
            foreach (var room in rooms)
                if (!all.Reached(room.Id)) problems.Add($"{room.Id}: not reachable from {LevelProgression.StartRoom} by the main path");
            foreach (var region in LevelRegions.All)
                if (region.HasBoss && !all.BossRooms.Contains(region.BossRoomId)) problems.Add($"{region.BossRoomId}: not reachable by the main path");
            return problems;
        }

        static void Basics(RoomFile room, List<string> problems)
        {
            var region = LevelRegions.Get(room.Region);
            if (region == null) { problems.Add($"{room.Id}: unknown region '{room.Region}'"); return; }
            if (!RoomValidator.IsValidId(room.Id) || !room.Id.StartsWith(room.Region + "_")) problems.Add($"{room.Id}: bad id for region {room.Region}");
            bool standard = room.Width == 40 && room.Height == 22, vertical = room.Width == 22 && room.Height == 44;
            if (!standard && !vertical) problems.Add($"{room.Id}: size {room.Width}x{room.Height} is neither 40x22 nor 22x44");
            if (RoomEnemies.Count(room) > MaxEnemiesPerRoom) problems.Add($"{room.Id}: more than {MaxEnemiesPerRoom} enemies");
            if (!string.IsNullOrEmpty(room.PreloadRegion) && LevelRegions.Get(room.PreloadRegion) == null) problems.Add($"{room.Id}: unknown preload region");
        }

        static void Doors(RoomFile room, Dictionary<string, RoomFile> byId, List<string> problems)
        {
            var region = LevelRegions.Get(room.Region);
            foreach (var door in room.Doors)
            {
                if (region != null && door.Target == region.BossRoomId)
                {
                    if (region.BossPreviousRoom != room.Id) problems.Add($"{room.Id}: door {door.Digit} enters the boss arena but {region.BossPreviousRoom} is its antechamber");
                    continue;
                }
                if (!byId.TryGetValue(door.Target, out var target)) { problems.Add($"{room.Id}: door {door.Digit} leads to missing room {door.Target}"); continue; }
                var back = target.DoorTo(room.Id);
                if (back == null) problems.Add($"{room.Id}: {door.Target} has no door back");
                else
                {
                    var arrival = target.SpawnCell(back);
                    if (!LevelReachability.IsStandable(target, arrival.x, arrival.y, LevelAbilities.All))
                        problems.Add($"{target.Id}: the arrival spot from {room.Id} is not a standing cell");
                }
                if (target.Region != room.Region && room.Region != "hub" && target.Region != "hub") problems.Add($"{room.Id}: door to another region that is not the hub");
            }
        }

        static void Markers(RoomFile room, List<string> problems)
        {
            foreach (char marker in "bmpnzANCQh")
                foreach (var c in room.Cells(marker))
                    if (!Supported(room, c) || Solid(room, c) || Solid(room, c + Vector2Int.up))
                        problems.Add($"{room.Id}: '{marker}' at {c} needs free space above a floor");
            foreach (char marker in "Bg")
                foreach (var c in room.Cells(marker))
                    if (Solid(room, c)) problems.Add($"{room.Id}: '{marker}' at {c} is inside a wall");
            foreach (var c in room.Cells('^'))
                if (!Supported(room, c)) problems.Add($"{room.Id}: spikes at {c} float");
            foreach (char marker in "sP")
                foreach (var c in room.Cells(marker))
                    if (room.At(c.x, c.y + 1) != '#') problems.Add($"{room.Id}: '{marker}' at {c} needs a ceiling cell above");
            foreach (var group in room.Groups('S'))
            {
                var r = RoomGeometry.Bounds(group);
                if (r.height != 1 || r.width < 3) problems.Add($"{room.Id}: a spider run must be one row and at least 3 cells");
            }
            if (room.Groups('G').Count > 0 && room.Groups('Q').Count != SealRules.SealAuras.Length)
                problems.Add($"{room.Id}: the seal gate needs {SealRules.SealAuras.Length} seals (Q)");
            foreach (var id in room.IdsOf('Q'))
                try { RoomProps.AuraOfSeal(id, room); } catch (System.FormatException e) { problems.Add(e.Message); }
        }

        /// <summary>Cells an arriving Leo must stay clear of: no enemy within 7 columns and 6 rows (he arrives with no time to react), no spikes or pit within 2.</summary>
        static void SpawnSafety(RoomFile room, List<string> problems)
        {
            foreach (var door in room.Doors)
            {
                var spawn = room.SpawnCell(door);
                foreach (char marker in "bmpnzBgS")
                    foreach (var c in room.Cells(marker))
                        if (Mathf.Abs(c.x - spawn.x) <= 7 && Mathf.Abs(c.y - spawn.y) <= 6)
                            problems.Add($"{room.Id}: enemy '{marker}' at {c} is too close to the arrival spot of door {door.Digit} {spawn}");
                foreach (char marker in "^KsaM")
                    foreach (var c in room.Cells(marker))
                        if (Mathf.Abs(c.x - spawn.x) <= 2 && Mathf.Abs(c.y - spawn.y) <= 3)
                            problems.Add($"{room.Id}: hazard '{marker}' at {c} is too close to the arrival spot of door {door.Digit} {spawn}");
            }
        }

        static bool Solid(RoomFile room, Vector2Int c) => room.At(c.x, c.y) == '#' || room.At(c.x, c.y) == 'W';

        static bool Supported(RoomFile room, Vector2Int c)
        {
            char below = room.At(c.x, c.y - 1);
            return below == '#' || below == 'W' || below == '=' || below == 'c';
        }

        static void Reachability(RoomFile room, List<string> problems)
        {
            foreach (var from in room.Doors)
            {
                var start = room.SpawnCell(from);
                if (!LevelReachability.IsStandable(room, start.x, start.y, LevelAbilities.All))
                { problems.Add($"{room.Id}: spawn of door {from.Digit} {start} is not a standing cell"); continue; }
                var states = LevelReachability.Reach(room, start, LevelAbilities.All, ReachMode.Safe);
                foreach (var door in room.Doors)
                    if (!LevelReachability.ReachesAny(states, room.Cells(door.Digit))) problems.Add($"{room.Id}: door {door.Digit} cannot be reached from door {from.Digit}");
                foreach (char marker in "AC")
                    foreach (var cell in room.Cells(marker))
                        if (!LevelReachability.ReachesCell(states, cell)) problems.Add($"{room.Id}: '{marker}' at {cell} cannot be reached from door {from.Digit}");
            }
        }

        static void Requirements(RoomFile room, List<string> problems)
        {
            var main = room.DoorByDigit('1');
            if (main == null || room.Requires.Count == 0) return;
            var start = room.SpawnCell(main);
            foreach (var req in room.Requires)
            {
                var cells = req.Target.Length == 1 && char.IsDigit(req.Target[0]) ? room.Cells(req.Target[0]) : room.CellsOfId(req.Target);
                if (cells.Count == 0) { problems.Add($"{room.Id}: requirement target '{req.Target}' does not exist"); continue; }
                if (!LevelReachability.ReachesAny(LevelReachability.Reach(room, start, LevelAbilities.All, ReachMode.Safe), cells))
                    problems.Add($"{room.Id}: {req.Target} cannot be reached even with every Aura");
                foreach (var need in new[] { LevelAbilities.Wind, LevelAbilities.Fire, LevelAbilities.Water, LevelAbilities.Seals })
                {
                    if ((req.Needs & need) == 0) continue;
                    var without = LevelReachability.Reach(room, start, LevelAbilities.All & ~need, ReachMode.Max);
                    if (LevelReachability.ReachesAny(without, cells)) problems.Add($"{room.Id}: {req.Target} can be reached without {need} (gate can be bypassed)");
                }
                var bare = LevelReachability.Reach(room, start, LevelAbilities.ShortcutOpen, ReachMode.Max);
                if (LevelReachability.ReachesAny(bare, cells)) problems.Add($"{room.Id}: {req.Target} can be reached with no Aura at all");
            }
        }

        static void ShortcutLock(RoomFile room, List<string> problems)
        {
            var region = LevelRegions.Get(room.Region);
            bool entry = region != null && region.HasBoss && room.Id == region.Id + "_01";
            int doors = room.Groups('k').Count + room.Groups('j').Count;
            if (entry && doors != 1) problems.Add($"{room.Id}: the entry room needs exactly one shortcut door (k or j), found {doors}");
            if (doors == 0) return;
            var door = room.DoorByDigit('7');
            var main = room.DoorByDigit('1');
            if (door == null || main == null) { problems.Add($"{room.Id}: a room with a shortcut door needs doors 1 and 7"); return; }
            var closed = LevelReachability.Reach(room, room.SpawnCell(main), LevelAbilities.All & ~LevelAbilities.ShortcutOpen, ReachMode.Max);
            if (LevelReachability.ReachesAny(closed, room.Cells('7'))) problems.Add($"{room.Id}: the shortcut pocket can be entered from outside while its door is closed");
        }

        public static List<string> ValidatePrefabs(IReadOnlyList<RoomFile> rooms) => LevelPrefabValidator.Validate(rooms);
    }
}
