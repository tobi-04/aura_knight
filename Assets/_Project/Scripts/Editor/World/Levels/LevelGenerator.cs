using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Entry point of the level content: Data/Levels/*.room.txt is the source of truth for every room prefab, the boss arena exits,
    /// the Region_* scene contents, the RegionGraph altars and the hazard prefabs. Idempotent; needs the art, bosses and progression
    /// steps to have run (Aura/Regenerate All orders them). Menu: Aura/Generate Levels.
    /// Batch: tools/unity-batch.sh exec AuraKnight.Editor.LevelGenerator.GenerateAll
    /// </summary>
    public static class LevelGenerator
    {
        [MenuItem("Aura/Generate Levels")]
        public static void GenerateAll()
        {
            var rooms = LevelCatalog.Load();
            if (rooms.Count == 0) throw new InvalidOperationException($"No room files under {LevelPaths.DataRoot}.");
            var problems = LevelValidator.ValidateFiles(rooms);
            if (problems.Count > 0) throw new InvalidOperationException($"{problems.Count} level data problem(s):\n{string.Join("\n", problems)}");

            TilesetArt.ClearCache();
            HazardPrefabs.BuildAll();
            foreach (var room in rooms) RoomPrefabBuilder.Build(room, LevelRegions.Get(room.Region));
            BossRoomLinker.LinkAll(rooms);
            WorldAssetGenerator.GenerateRegionGraph();
            AssetDatabase.SaveAssets();
            LevelSceneBuilder.BuildAll(rooms);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[LevelGenerator] {rooms.Count} room(s) + {CountBosses()} boss room(s) built.");
        }

        static int CountBosses()
        {
            int n = 0;
            foreach (var region in LevelRegions.All) if (region.HasBoss) n++;
            return n;
        }

        /// <summary>Validators step of Aura/Regenerate All: file rules, graph rules and the generated prefabs.</summary>
        public static List<string> Validate()
        {
            var rooms = LevelCatalog.Load();
            var problems = LevelValidator.ValidateFiles(rooms);
            problems.AddRange(LevelValidator.ValidatePrefabs(rooms));
            return problems;
        }
    }
}
