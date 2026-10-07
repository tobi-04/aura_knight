using System.Collections.Generic;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>One region as the level tools see it: scene, art set, where its rooms sit in the world, its boss room.</summary>
    public sealed class LevelRegion
    {
        public string Id;
        public string Scene;
        /// <summary>Folder and file-name part of the art / prefab set ("Forest").</summary>
        public string Folder;
        /// <summary>World position of slot (0, 0) of the region: regions are far apart so room bounds never overlap across scenes.</summary>
        public Vector2 Origin;
        /// <summary>Boss room slot and the room the boss room connects back to (null for the hub).</summary>
        public Vector2Int BossSlot;
        public string BossPreviousRoom;
        public string BossReward;
        public bool HasBoss => BossPreviousRoom != null;

        public string BossRoomId => Id + "_boss";
        public string BossPrefabPath => $"{LevelPaths.RoomsRoot}/{Folder}/Room_Boss_{Folder}.prefab";
        public string ScenePath => $"{LevelPaths.ScenesRoot}/{Scene}.unity";
    }

    /// <summary>Asset locations shared by the level tools.</summary>
    public static class LevelPaths
    {
        public const string DataRoot = "Assets/_Project/Data/Levels";
        public const string RoomsRoot = "Assets/_Project/Prefabs/Rooms";
        public const string ScenesRoot = "Assets/_Project/Scenes";
        public const string HazardsRoot = "Assets/_Project/Prefabs/Hazards";
        public const string InteractablesRoot = "Assets/_Project/Prefabs/Interactables";
        public const string EnemiesRoot = "Assets/_Project/Prefabs/Enemies";
        public const string TilesetsRoot = "Assets/_Project/Art/Tilesets";
        public const string BackgroundsRoot = "Assets/_Project/Art/Backgrounds";
        public const string LitMaterial = "Assets/_Project/Art/Materials/Mat_SpriteLit.mat";

        public static string RoomPrefab(RoomFile room, LevelRegion region) => $"{RoomsRoot}/{region.Folder}/Room_{room.Id}.prefab";
    }

    /// <summary>The five regions: the hub in the middle, Forest to its west, Cave east, City south, Castle north (GDD 7.1).</summary>
    public static class LevelRegions
    {
        /// <summary>Distance between two stacked slots: a 22-tall room plus a gap, so map cells (10 units) never overlap.</summary>
        public const int SlotHeight = 30;

        public static readonly IReadOnlyList<LevelRegion> All = new[]
        {
            new LevelRegion { Id = "hub", Scene = "Region_Hub", Folder = "Hub", Origin = new Vector2(0, 0) },
            new LevelRegion { Id = "forest", Scene = "Region_Forest", Folder = "Forest", Origin = new Vector2(-2000, 0),
                BossSlot = new Vector2Int(-7, 0), BossPreviousRoom = "forest_07", BossReward = "Wind" },
            new LevelRegion { Id = "cave", Scene = "Region_Cave", Folder = "Cave", Origin = new Vector2(2000, 0),
                BossSlot = new Vector2Int(7, 0), BossPreviousRoom = "cave_07", BossReward = "Fire" },
            new LevelRegion { Id = "city", Scene = "Region_City", Folder = "City", Origin = new Vector2(0, -2000),
                BossSlot = new Vector2Int(7, 0), BossPreviousRoom = "city_07", BossReward = "Water" },
            new LevelRegion { Id = "castle", Scene = "Region_Castle", Folder = "Castle", Origin = new Vector2(0, 2000),
                BossSlot = new Vector2Int(6, 0), BossPreviousRoom = "castle_06", BossReward = null },
        };

        public static LevelRegion Get(string id)
        {
            foreach (var region in All)
                if (region.Id == id) return region;
            return null;
        }

        /// <summary>World position of a room's bottom-left corner: its slot, centred horizontally when narrower than a slot.</summary>
        public static Vector2 WorldPosition(LevelRegion region, Vector2Int slot, int roomWidth) =>
            region.Origin + new Vector2(slot.x * RoomFile.Slot + (RoomFile.Slot - roomWidth) * 0.5f, slot.y * SlotHeight);

        public static Vector2 WorldPosition(RoomFile room) => WorldPosition(Get(room.Region), room.SlotCell, room.Width);
    }
}
