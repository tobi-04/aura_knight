using System.Collections.Generic;
using AuraKnight.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Puts the generated rooms into the Region_* scenes at their slot positions (this layout is also what the map screen draws) and
    /// adds each scene's Global Light 2D with its <see cref="RegionLighting"/> (GDD 10 intensities). The scene objects it owns
    /// ("Levels", "RegionLighting", the old "StartRoom") are rebuilt on every run.
    /// </summary>
    static class LevelSceneBuilder
    {
        public const string LevelsRoot = "Levels";
        public const string LightingName = "RegionLighting";
        static readonly string[] OwnedRoots = { LevelsRoot, LightingName, "StartRoom" };

        public static void BuildAll(IReadOnlyList<RoomFile> rooms)
        {
            foreach (var region in LevelRegions.All) Build(region, LevelCatalog.InRegion(rooms, region.Id));
        }

        static void Build(LevelRegion region, List<RoomFile> rooms)
        {
            var scene = EditorSceneManager.OpenScene(region.ScenePath, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
                if (System.Array.IndexOf(OwnedRoots, root.name) >= 0) Object.DestroyImmediate(root);
            var levels = new GameObject(LevelsRoot);
            foreach (var room in rooms)
                Place(levels.transform, LevelPaths.RoomPrefab(room, region), $"Room_{room.Id}", LevelRegions.WorldPosition(room));
            if (region.HasBoss)
                Place(levels.transform, region.BossPrefabPath, $"Room_{region.BossRoomId}", LevelRegions.WorldPosition(region, region.BossSlot, RoomFile.Slot));
            AddLighting(region);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, region.ScenePath);
        }

        static void Place(Transform parent, string prefabPath, string name, Vector2 position)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(PrefabKit.LoadPrefab(prefabPath), parent);
            instance.name = name;
            instance.transform.position = new Vector3(position.x, position.y, 0f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        }

        static void AddLighting(LevelRegion region)
        {
            var go = new GameObject(LightingName);
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = RegionLightingTable.IntensityOf(region.Id);
            light.color = Color.white;
            var lighting = go.AddComponent<RegionLighting>();
            PrefabKit.SetString(lighting, "regionId", region.Id);
            PrefabKit.SetRef(lighting, "globalLight", light);
        }
    }
}
