using System.Collections.Generic;
using System.IO;
using AuraKnight.Core;
using AuraKnight.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Finishes the four boss arenas the boss generator builds: removes the greybox left wall and puts a RoomExit there that leads
    /// back to the room before the arena (the altar room), swaps the greybox look for the region tiles, and adds the parallax backdrop.
    /// The way in is a door of the previous room (spawn "default" at x 3.5). Idempotent: its own objects are rebuilt each run.
    /// </summary>
    static class BossRoomLinker
    {
        public const string BackExitName = "Exit_back";
        /// <summary>Centre of the way back: its 1-wide trigger spans 0.1 to 1.1, so it never touches the previous room's door trigger (which ends where this room begins).</summary>
        public const float BackExitX = 0.6f;
        static readonly string[] HiddenGreybox = { "Floor", "Ceiling", "WallRight" };

        public static void LinkAll(IReadOnlyList<RoomFile> rooms)
        {
            foreach (var region in LevelRegions.All)
                if (region.HasBoss) Link(region, rooms);
        }

        static void Link(LevelRegion region, IReadOnlyList<RoomFile> rooms)
        {
            RoomFile previous = null;
            foreach (var room in rooms)
                if (room.Id == region.BossPreviousRoom) previous = room;
            if (previous == null) throw new System.InvalidOperationException($"Room {region.BossPreviousRoom} (before the {region.Id} boss) has no room file.");
            if (previous.DoorTo(region.BossRoomId) == null) throw new System.InvalidOperationException($"{previous.Id} has no door to {region.BossRoomId}.");
            if (!File.Exists(region.BossPrefabPath)) throw new System.InvalidOperationException($"{region.BossPrefabPath} is missing; run the boss generator first.");

            var root = PrefabUtility.LoadPrefabContents(region.BossPrefabPath);
            try
            {
                var wall = root.transform.Find("Greybox/WallLeft");
                if (wall != null) Object.DestroyImmediate(wall.gameObject);
                var exits = root.transform.Find("Exits");
                var old = exits.Find(BackExitName);
                if (old != null) Object.DestroyImmediate(old.gameObject);
                var exit = BackExit(exits, previous.Id, RoomFile.SpawnName(region.BossRoomId));
                PrefabKit.SetRefs(root.GetComponent<Room>(), "exits", new Object[] { exit });
                Retile(root.transform, region);
                RoomBackdrop.Add(root.transform, region, RoomFile.Slot, 22);
                PrefabUtility.SaveAsPrefabAsset(root, region.BossPrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static RoomExit BackExit(Transform parent, string target, string spawn)
        {
            var go = PrefabKit.OnLayer(PrefabKit.Child(parent, BackExitName, new Vector3(BackExitX, 3f, 0f)), PhysicsLayers.Interactable);
            PrefabKit.Box(go, new Vector2(1f, 4f), Vector2.zero, true);
            var exit = go.AddComponent<RoomExit>();
            PrefabKit.SetString(exit, "targetRoomId", target);
            PrefabKit.SetString(exit, "targetSpawnName", spawn);
            return exit;
        }

        /// <summary>Region tiles over the greybox floor, ceiling and right wall (the greybox colliders stay; their sprites are hidden).</summary>
        static void Retile(Transform root, LevelRegion region)
        {
            var ground = root.Find("Grid/Ground");
            var tile = TilesetArt.Tile(region.Folder, "Ground");
            if (ground == null || tile == null) return;
            var collider = ground.GetComponent<TilemapCollider2D>();
            if (collider != null) Object.DestroyImmediate(collider);
            var lit = AssetDatabase.LoadAssetAtPath<Material>(LevelPaths.LitMaterial);
            if (lit != null && ground.TryGetComponent<TilemapRenderer>(out var renderer)) renderer.sharedMaterial = lit;
            var map = ground.GetComponent<Tilemap>();
            map.ClearAllTiles();
            for (int x = 0; x <= RoomFile.Slot; x++)
            {
                map.SetTile(new Vector3Int(x, 0, 0), tile);
                map.SetTile(new Vector3Int(x, 22, 0), tile);
            }
            for (int y = 1; y < 22; y++) map.SetTile(new Vector3Int(RoomFile.Slot, y, 0), tile);
            foreach (var name in HiddenGreybox)
            {
                var block = root.Find("Greybox/" + name);
                if (block != null && block.TryGetComponent<SpriteRenderer>(out var sr)) sr.enabled = false;
            }
        }
    }
}
