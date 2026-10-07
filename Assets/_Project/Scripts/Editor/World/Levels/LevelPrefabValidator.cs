using System.Collections.Generic;
using AuraKnight.Enemies;
using AuraKnight.World;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Checks the generated room prefabs against their files: ids, doorways (exit targets and the spawn names they use), altars,
    /// persistent ids unique across the game, at most 6 enemies, and the four boss arenas' way back. Exits and spawns are the part
    /// a typo in a room file would turn into a softlock, so every RoomExit must resolve to a real spawn point.
    /// </summary>
    static class LevelPrefabValidator
    {
        public static List<string> Validate(IReadOnlyList<RoomFile> rooms)
        {
            var problems = new List<string>();
            var prefabs = new Dictionary<string, GameObject>();
            foreach (var room in rooms)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LevelPaths.RoomPrefab(room, LevelRegions.Get(room.Region)));
                if (prefab == null) problems.Add($"{room.Id}: prefab is missing (run Aura/Generate Levels)");
                else prefabs[room.Id] = prefab;
            }
            foreach (var region in LevelRegions.All)
            {
                if (!region.HasBoss) continue;
                var boss = AssetDatabase.LoadAssetAtPath<GameObject>(region.BossPrefabPath);
                if (boss == null) problems.Add($"{region.BossRoomId}: prefab is missing");
                else prefabs[region.BossRoomId] = boss;
            }
            var persistent = new Dictionary<string, string>();
            foreach (var pair in prefabs)
            {
                var info = pair.Value.GetComponent<Room>();
                if (info == null || info.RoomId != pair.Key) { problems.Add($"{pair.Key}: prefab has no matching Room component"); continue; }
                Exits(pair.Key, pair.Value, prefabs, problems);
                Ids(pair.Key, pair.Value, persistent, problems);
                int enemies = pair.Value.GetComponentsInChildren<EnemyBase>(true).Length;
                if (enemies > LevelValidator.MaxEnemiesPerRoom) problems.Add($"{pair.Key}: {enemies} enemies (max {LevelValidator.MaxEnemiesPerRoom})");
            }
            foreach (var room in rooms)
                if (prefabs.TryGetValue(room.Id, out var prefab)) Props(room, prefab, problems);
            BossWayBack(prefabs, problems);
            return problems;
        }

        static void Exits(string id, GameObject prefab, Dictionary<string, GameObject> prefabs, List<string> problems)
        {
            foreach (var exit in prefab.GetComponentsInChildren<RoomExit>(true))
            {
                if (!prefabs.TryGetValue(exit.TargetRoomId, out var target)) { problems.Add($"{id}: exit to missing room '{exit.TargetRoomId}'"); continue; }
                if (!target.GetComponent<Room>().TryGetSpawn(exit.TargetSpawnName, out _))
                    problems.Add($"{id}: exit to {exit.TargetRoomId} uses spawn '{exit.TargetSpawnName}', which does not exist there");
            }
        }

        static void Ids(string id, GameObject prefab, Dictionary<string, string> seen, List<string> problems)
        {
            foreach (var pid in prefab.GetComponentsInChildren<PersistentId>(true))
            {
                if (string.IsNullOrEmpty(pid.Id)) problems.Add($"{id}: {pid.name} has an empty persistent id");
                else if (seen.TryGetValue(pid.Id, out var other)) problems.Add($"{id}: persistent id '{pid.Id}' is also used in {other}");
                else seen[pid.Id] = id;
            }
        }

        static void Props(RoomFile room, GameObject prefab, List<string> problems)
        {
            var altars = prefab.GetComponentsInChildren<SunAltar>(true);
            if (altars.Length != room.Cells('A').Count || (altars.Length == 1 && altars[0].AltarId != room.AltarId))
                problems.Add($"{room.Id}: altar in the prefab does not match the file");
            var exits = new List<string>();
            foreach (var exit in prefab.GetComponentsInChildren<RoomExit>(true)) exits.Add(exit.TargetRoomId);
            foreach (var door in room.Doors)
                if (!exits.Contains(door.Target)) problems.Add($"{room.Id}: no RoomExit to {door.Target}");
            if (exits.Count != room.Doors.Count) problems.Add($"{room.Id}: {exits.Count} RoomExit(s) for {room.Doors.Count} door(s)");
        }

        static void BossWayBack(Dictionary<string, GameObject> prefabs, List<string> problems)
        {
            foreach (var region in LevelRegions.All)
            {
                if (!region.HasBoss || !prefabs.TryGetValue(region.BossRoomId, out var boss)) continue;
                if (boss.transform.Find("Greybox/WallLeft") != null) problems.Add($"{region.BossRoomId}: the left greybox wall still blocks the way back");
                bool back = false;
                foreach (var exit in boss.GetComponentsInChildren<RoomExit>(true)) back |= exit.TargetRoomId == region.BossPreviousRoom;
                if (!back) problems.Add($"{region.BossRoomId}: no exit back to {region.BossPreviousRoom}");
            }
        }
    }
}
