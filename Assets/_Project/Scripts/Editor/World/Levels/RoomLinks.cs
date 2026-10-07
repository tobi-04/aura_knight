using System.Collections.Generic;
using AuraKnight.Core;
using AuraKnight.World;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Doorways of a room: one RoomExit per door strip (sent to the neighbour's "from_&lt;this room&gt;" spawn, or "default" for a
    /// boss arena), one spawn point per door (3 cells inside) plus "default", and the extra preload zones.
    /// </summary>
    static class RoomLinks
    {
        public static void Build(RoomBuild b)
        {
            var f = b.File;
            var spawns = new List<Object>();
            var exits = new List<Object>();
            foreach (var door in f.Doors)
            {
                exits.Add(Exit(b, door));
                spawns.Add(Spawn(b, RoomFile.SpawnName(door.Target), f.SpawnCell(door)));
            }
            Transform defaultSpawn = b.Spawns.Find("default");
            var first = f.DoorByDigit('1') ?? f.Doors[0];
            defaultSpawn.localPosition = SpawnPosition(f.SpawnCell(first));
            spawns.Insert(0, defaultSpawn);
            var room = b.Root.GetComponent<Room>();
            PrefabKit.SetRefs(room, "spawnPoints", spawns.ToArray());
            PrefabKit.SetRefs(room, "exits", exits.ToArray());
            foreach (var zone in f.Zones) Zone(b, zone);
        }

        /// <summary>Leo's centre when standing on the floor of the spawn cell.</summary>
        public static Vector3 SpawnPosition(Vector2Int cell) => new Vector3(cell.x + 0.5f, cell.y + 1f, 0f);

        static Object Spawn(RoomBuild b, string name, Vector2Int cell) =>
            PrefabKit.Child(b.Spawns, name, SpawnPosition(cell)).transform;

        static Object Exit(RoomBuild b, RoomDoor door)
        {
            var center = new Vector3(door.X + 0.5f, door.YMin + door.Height * 0.5f, 0f);
            var go = PrefabKit.OnLayer(PrefabKit.Child(b.Exits, $"Exit_{door.Digit}_{door.Target}", center), PhysicsLayers.Interactable);
            PrefabKit.Box(go, new Vector2(1f, door.Height), Vector2.zero, true);
            var exit = go.AddComponent<RoomExit>();
            PrefabKit.SetString(exit, "targetRoomId", door.Target);
            PrefabKit.SetString(exit, "targetSpawnName", RoomFile.EntrySpawnFor(door.Target, b.File.Id));
            return exit;
        }

        static void Zone(RoomBuild b, RoomZone zone)
        {
            var go = PrefabKit.OnLayer(PrefabKit.Child(b.Exits, $"Preload_{zone.Region}",
                new Vector3(zone.X + zone.Width * 0.5f, zone.Y + zone.Height * 0.5f, 0f)), PhysicsLayers.Interactable);
            PrefabKit.Box(go, new Vector2(zone.Width, zone.Height), Vector2.zero, true);
            PrefabKit.SetString(go.AddComponent<RegionPreloadZone>(), "regionId", zone.Region);
        }
    }
}
