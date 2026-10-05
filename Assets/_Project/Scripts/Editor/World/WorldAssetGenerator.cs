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
            graph.SetRegions(new List<RegionNode>
            {
                new RegionNode("hub", "Region_Hub", "forest", "cave", "city", "castle").WithAltars(GameState.StartAltarId),
                new RegionNode("forest", "Region_Forest", "hub").WithAltars("forest_altar_01"),
                new RegionNode("cave", "Region_Cave", "hub").WithAltars("cave_altar_01"),
                new RegionNode("city", "Region_City", "hub").WithAltars("city_altar_01"),
                new RegionNode("castle", "Region_Castle", "hub").WithAltars("castle_altar_01"),
            });
            EditorUtility.SetDirty(graph);
            return graph;
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

            var so = new SerializedObject(root.AddComponent<Shortcut>());
            so.FindProperty("doorBlocker").objectReferenceValue = blocker;
            so.ApplyModifiedPropertiesWithoutUndo();
            Save(root, ShortcutPath);
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
