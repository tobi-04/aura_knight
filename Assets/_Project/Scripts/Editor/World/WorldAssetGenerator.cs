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
    /// Idempotent generator for world assets: RegionGraph, Room_Template, SunAltar and Shortcut prefabs.
    /// Menu: Aura/Generate World Assets. Batch: -executeMethod AuraKnight.Editor.WorldAssetGenerator.GenerateAll
    /// </summary>
    public static class WorldAssetGenerator
    {
        public const string RegionGraphPath = "Assets/_Project/Data/World/RegionGraph.asset";
        public const string RoomTemplatePath = "Assets/_Project/Prefabs/Rooms/_Template/Room_Template.prefab";
        public const string SunAltarPath = "Assets/_Project/Prefabs/Interactables/SunAltar.prefab";
        public const string ShortcutPath = "Assets/_Project/Prefabs/Interactables/Shortcut.prefab";
        const float RoomWidth = 40f, RoomHeight = 22f;

        [MenuItem("Aura/Generate World Assets")]
        public static void GenerateAll()
        {
            GenerateRegionGraph();
            GenerateRoomTemplate();
            GenerateSunAltar();
            GenerateShortcut();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[WorldAssetGenerator] Done.");
        }

        public static RegionGraph GenerateRegionGraph()
        {
            EnsureFolder(RegionGraphPath);
            var graph = AssetDatabase.LoadAssetAtPath<RegionGraph>(RegionGraphPath);
            if (graph == null)
            {
                graph = ScriptableObject.CreateInstance<RegionGraph>();
                AssetDatabase.CreateAsset(graph, RegionGraphPath);
            }
            var rooms = LevelCatalog.Load();
            graph.SetRegions(new List<RegionNode>
            {
                new RegionNode("hub", "Region_Hub", "forest", "cave", "city", "castle").WithAltars(AltarsOf(rooms, "hub", GameState.StartAltarId)),
                new RegionNode("forest", "Region_Forest", "hub").WithAltars(AltarsOf(rooms, "forest", "forest_altar_01")),
                new RegionNode("cave", "Region_Cave", "hub").WithAltars(AltarsOf(rooms, "cave", "cave_altar_01")),
                new RegionNode("city", "Region_City", "hub").WithAltars(AltarsOf(rooms, "city", "city_altar_01")),
                new RegionNode("castle", "Region_Castle", "hub").WithAltars(AltarsOf(rooms, "castle", "castle_altar_01")),
            });
            EditorUtility.SetDirty(graph);
            return graph;
        }

        /// <summary>The altars of a region come from the room files once they exist (every altar must exist in its scene); before that the single start altar.</summary>
        static string[] AltarsOf(List<RoomFile> rooms, string region, string fallback)
        {
            var ids = LevelCatalog.AltarIds(rooms, region);
            return ids.Count > 0 ? ids.ToArray() : new[] { fallback };
        }

        public static void GenerateRoomTemplate()
        {
            var root = new GameObject("Room_Template");
            var grid = new GameObject("Grid", typeof(Grid));
            grid.transform.SetParent(root.transform, false);
            var ground = AddTilemap(grid, "Ground", 0);
            PhysicsLayers.Apply(ground, PhysicsLayers.Ground);
            ground.AddComponent<TilemapCollider2D>();
            AddTilemap(grid, "Decor", -1);
            AddTilemap(grid, "Foreground", 10);

            var bounds = Child(root, "Bounds").AddComponent<PolygonCollider2D>();
            bounds.isTrigger = true;
            bounds.SetPath(0, new[]
            {
                Vector2.zero, new Vector2(RoomWidth, 0), new Vector2(RoomWidth, RoomHeight), new Vector2(0, RoomHeight),
            });
            Child(root, "Exits");
            var spawns = Child(root, "SpawnPoints");
            Child(spawns, "default").transform.localPosition = new Vector3(2f, 2f, 0f);
            var enemies = Child(root, "Enemies");

            var so = new SerializedObject(root.AddComponent<Room>());
            so.FindProperty("roomId").stringValue = "template_00";
            so.FindProperty("regionId").stringValue = "template";
            so.FindProperty("bounds").objectReferenceValue = bounds;
            so.FindProperty("enemiesContainer").objectReferenceValue = enemies;
            var spawnList = so.FindProperty("spawnPoints");
            spawnList.arraySize = 1;
            spawnList.GetArrayElementAtIndex(0).objectReferenceValue = spawns.transform.GetChild(0);
            so.ApplyModifiedPropertiesWithoutUndo();
            Save(root, RoomTemplatePath);
        }

        public static void GenerateSunAltar()
        {
            var root = new GameObject("SunAltar");
            PhysicsLayers.Apply(root, PhysicsLayers.Interactable);
            var col = root.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2f, 3f);
            col.offset = new Vector2(0f, 1.5f);
            // The altar origin sits on the floor; Leo (1.9 tall, centre pivot) respawns 1 unit above it.
            var spawn = Child(root, "SpawnPoint");
            spawn.transform.localPosition = new Vector3(0f, 1f, 0f);
            // Greybox look, unlit so the altar stays visible in the dark regions: a stone plinth with a golden sun block on it.
            AddBlock(root, "Plinth", new Vector3(0f, 0.4f, 0f), new Vector3(1.6f, 0.8f, 1f), new Color(0.55f, 0.45f, 0.3f));
            AddBlock(root, "Sun", new Vector3(0f, 1.5f, 0f), new Vector3(0.7f, 1.2f, 1f), new Color(1f, 0.82f, 0.3f));

            var so = new SerializedObject(root.AddComponent<SunAltar>());
            so.FindProperty("spawnPoint").objectReferenceValue = spawn.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
            Save(root, SunAltarPath);
        }

        public static void GenerateShortcut()
        {
            var root = new GameObject("Shortcut");
            PhysicsLayers.Apply(root, PhysicsLayers.Interactable);
            var trigger = root.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(3f, 4f);
            trigger.offset = new Vector2(-2.5f, 2f);
            root.AddComponent<PersistentId>();
            var blocker = Child(root, "DoorBlocker");
            PhysicsLayers.Apply(blocker, PhysicsLayers.Ground);
            var block = blocker.AddComponent<BoxCollider2D>();
            block.size = new Vector2(1f, 4f);
            block.offset = new Vector2(0f, 2f);
            AddBlock(blocker, "Slab", new Vector3(0f, 2f, 0f), new Vector3(1f, 4f, 1f), new Color(0.5f, 0.38f, 0.26f));

            var so = new SerializedObject(root.AddComponent<Shortcut>());
            so.FindProperty("doorBlocker").objectReferenceValue = blocker;
            so.ApplyModifiedPropertiesWithoutUndo();
            Save(root, ShortcutPath);
        }

        /// <summary>A coloured square (the built-in white sprite, default unlit material) for the greybox look of world props.</summary>
        static void AddBlock(GameObject parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            var go = Child(parent, name);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PrefabKit.Square;
            sr.color = color;
            sr.sortingOrder = 2;
        }

        static GameObject AddTilemap(GameObject grid, string name, int sortingOrder)
        {
            var go = Child(grid, name);
            go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>().sortingOrder = sortingOrder;
            return go;
        }

        internal static GameObject Child(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        static void Save(GameObject root, string path)
        {
            EnsureFolder(path);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        static void EnsureFolder(string assetPath) =>
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
    }
}
