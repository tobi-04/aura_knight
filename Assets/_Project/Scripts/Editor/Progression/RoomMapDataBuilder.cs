using System.Collections.Generic;
using AuraKnight.Progression;
using AuraKnight.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Builds the RoomMapData assets from the rooms placed in the region scenes: every <see cref="Room"/>'s bounds polygon becomes grid
    /// cells (10 world units per cell), and altars, chests, shortcuts and boss rooms (room id contains "boss") become icons. Run it after
    /// placing or moving rooms; phase 9 rooms then appear on the map without hand-written data.
    /// Menu: Aura/Progression/Rebuild Map Data. Batch: AuraKnight.Editor.RoomMapDataBuilder.RebuildAll
    /// </summary>
    public static class RoomMapDataBuilder
    {
        const string ScenesDir = "Assets/_Project/Scenes";
        const float CellSize = 10f;

        [MenuItem("Aura/Progression/Rebuild Map Data")]
        public static void RebuildAll()
        {
            var graph = AssetDatabase.LoadAssetAtPath<RegionGraph>(ProgressionAssetGenerator.RegionGraphPath);
            if (graph == null)
            {
                Debug.LogError("[RoomMapDataBuilder] RegionGraph asset is missing; run Aura/Generate World Assets first.");
                return;
            }
            var scanned = new List<Scanned>();
            foreach (var region in graph.Regions) scanned.Add(Scan(region));

            var sizes = new Dictionary<string, Vector2Int>();
            foreach (var s in scanned) sizes[s.RegionId] = s.Size;
            sizes.TryGetValue("hub", out var hubSize);
            sizes.TryGetValue("cave", out var caveSize);
            int extra = 0;
            foreach (var s in scanned)
            {
                var offset = MapLayout.Offset(s.RegionId, s.Size, hubSize, caveSize, extra);
                if (s.RegionId != "hub" && s.RegionId != "forest" && s.RegionId != "cave" && s.RegionId != "city" && s.RegionId != "castle") extra++;
                var data = ProgressionAssetGenerator.LoadOrCreate<RoomMapData>($"{ProgressionAssetGenerator.MapDir}/RoomMapData_{s.RegionId}.asset");
                data.Configure(s.RegionId, CellSize, offset, s.Rooms, s.Icons);
                EditorUtility.SetDirty(data);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[RoomMapDataBuilder] Rebuilt {scanned.Count} region map(s).");
        }

        sealed class Scanned
        {
            public string RegionId;
            public Vector2Int Size;
            public List<MapRoom> Rooms = new();
            public List<MapIcon> Icons = new();
        }

        static Scanned Scan(RegionNode region)
        {
            var result = new Scanned { RegionId = region.regionId };
            string path = $"{ScenesDir}/{region.sceneName}.unity";
            var scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var rooms = new List<Room>();
                foreach (var root in scene.GetRootGameObjects()) rooms.AddRange(root.GetComponentsInChildren<Room>(true));
                var boxes = new Dictionary<Room, (Vector2 min, Vector2 max)>();
                Vector2Int origin = default;
                bool first = true;
                foreach (var room in rooms)
                {
                    if (!TryWorldBounds(room, out var min, out var max)) continue;
                    boxes[room] = (min, max);
                    var cell = new Vector2Int(Mathf.FloorToInt(min.x / CellSize), Mathf.FloorToInt(min.y / CellSize));
                    origin = first ? cell : new Vector2Int(Mathf.Min(origin.x, cell.x), Mathf.Min(origin.y, cell.y));
                    first = false;
                }
                int xMax = 0, yMax = 0;
                foreach (var pair in boxes)
                {
                    var cells = MapRules.CellsFromBounds(pair.Value.min, pair.Value.max, CellSize, origin);
                    result.Rooms.Add(new MapRoom { roomId = pair.Key.RoomId, cells = cells });
                    xMax = Mathf.Max(xMax, cells.xMax);
                    yMax = Mathf.Max(yMax, cells.yMax);
                    if (pair.Key.RoomId.Contains("boss"))
                        result.Icons.Add(IconAt(MapIconKind.Boss, pair.Key.RoomId, pair.Key.RoomId, (pair.Value.min + pair.Value.max) * 0.5f, origin));
                }
                result.Size = new Vector2Int(xMax, yMax);
                AddIcons<SunAltar>(scene, MapIconKind.Altar, a => a.AltarId, boxes, origin, result);
                AddIcons<TreasureChest>(scene, MapIconKind.Chest, c => c.ChestId, boxes, origin, result);
                AddIcons<Shortcut>(scene, MapIconKind.Shortcut, s => s.GetComponent<PersistentId>().Id, boxes, origin, result);
                result.Rooms.Sort((a, b) => string.CompareOrdinal(a.roomId, b.roomId));
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
            return result;
        }

        static void AddIcons<T>(Scene scene, MapIconKind kind, System.Func<T, string> idOf, Dictionary<Room, (Vector2 min, Vector2 max)> boxes,
            Vector2Int origin, Scanned result) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var item in root.GetComponentsInChildren<T>(true))
                {
                    var room = item.GetComponentInParent<Room>();
                    if (room == null || !boxes.ContainsKey(room)) continue;
                    result.Icons.Add(IconAt(kind, idOf(item), room.RoomId, item.transform.position, origin));
                }
            }
        }

        static MapIcon IconAt(MapIconKind kind, string id, string roomId, Vector2 world, Vector2Int origin) =>
            new MapIcon { kind = kind, id = id, roomId = roomId, cell = MapRules.CellOf(world, CellSize, origin) };

        /// <summary>World-space AABB of the room's bounds polygon, read from its points (colliders have no usable .bounds outside play mode).</summary>
        static bool TryWorldBounds(Room room, out Vector2 min, out Vector2 max)
        {
            min = max = default;
            if (room.Bounds == null || room.Bounds.pathCount == 0 || string.IsNullOrEmpty(room.RoomId)) return false;
            bool first = true;
            for (int p = 0; p < room.Bounds.pathCount; p++)
            {
                foreach (var point in room.Bounds.GetPath(p))
                {
                    Vector2 world = room.Bounds.transform.TransformPoint(point);
                    min = first ? world : Vector2.Min(min, world);
                    max = first ? world : Vector2.Max(max, world);
                    first = false;
                }
            }
            return !first;
        }
    }
}
