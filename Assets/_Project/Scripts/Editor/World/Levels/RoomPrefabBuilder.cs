using System.IO;
using AuraKnight.World;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Builds Prefabs/Rooms/&lt;Region&gt;/Room_&lt;id&gt;.prefab from a <see cref="RoomFile"/>: the Room_Template frame (Room component, bounds,
    /// containers), then terrain, hazards, props, doorways, enemies and the parallax backdrop. The prefab is rebuilt from scratch on
    /// every run, so it is safe to regenerate; never edit it by hand.
    /// </summary>
    static class RoomPrefabBuilder
    {
        public const string TemplatePath = LevelPaths.RoomsRoot + "/_Template/Room_Template.prefab";

        public static string Build(RoomFile file, LevelRegion region)
        {
            var template = PrefabKit.LoadPrefab(TemplatePath);
            var root = (GameObject)PrefabUtility.InstantiatePrefab(template);
            try
            {
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                root.name = $"Room_{file.Id}";
                var b = Frame(root, file, region);
                RoomTerrain.Build(b);
                RoomHazards.Build(b);
                RoomProps.Build(b);
                RoomEnemies.Build(b);
                RoomLinks.Build(b);
                RoomBackdrop.Add(root.transform, region, file.Width, file.Height);
                string path = LevelPaths.RoomPrefab(file, region);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                PrefabUtility.SaveAsPrefabAsset(root, path);
                return path;
            }
            finally { Object.DestroyImmediate(root); }
        }

        /// <summary>Sets the Room ids and bounds and returns the build context with the template's containers.</summary>
        static RoomBuild Frame(GameObject root, RoomFile file, LevelRegion region)
        {
            var room = root.GetComponent<Room>();
            PrefabKit.SetString(room, "roomId", file.Id);
            PrefabKit.SetString(room, "regionId", file.Region);
            PrefabKit.SetString(room, "preloadRegionId", file.PreloadRegion ?? "");
            var bounds = root.transform.Find("Bounds").GetComponent<PolygonCollider2D>();
            bounds.SetPath(0, new[] { Vector2.zero, new Vector2(file.Width, 0), new Vector2(file.Width, file.Height), new Vector2(0, file.Height) });
            var t = root.transform;
            return new RoomBuild
            {
                File = file, Region = region, Root = t, Grid = t.Find("Grid"), Exits = t.Find("Exits"), Spawns = t.Find("SpawnPoints"),
                Enemies = t.Find("Enemies"),
                Collision = PrefabKit.Child(t, "Collision").transform,
                Hazards = PrefabKit.Child(t, "Hazards").transform,
                Props = PrefabKit.Child(t, "Props").transform,
            };
        }
    }
}
