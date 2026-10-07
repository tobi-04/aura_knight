using AuraKnight.Core;
using AuraKnight.Enemies;
using AuraKnight.Player;
using AuraKnight.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Builds Scenes/Test/Test_Enemies.unity: a long floor with one of every variant in its own lane (a raised ledge for the
    /// patrol bot, a wall and a ceiling for the spider, a wall segment beside the ghost) plus the Player and a checkpoint.
    /// Menu: Aura/Enemies/Generate Test Enemies Scene. Batch: AuraKnight.Editor.EnemyTestSceneGenerator.Generate
    /// </summary>
    public static class EnemyTestSceneGenerator
    {
        public const string ScenePath = "Assets/_Project/Scenes/Test/Test_Enemies.unity";
        static readonly Color Ground = new Color(0.35f, 0.38f, 0.45f);
        static readonly Color Wall = new Color(0.50f, 0.54f, 0.62f);

        [MenuItem("Aura/Enemies/Generate Test Enemies Scene")]
        public static void Generate()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(EnemyVariants.All[0].PrefabPath) == null) EnemyAssetGenerator.Generate();
            PlayerAssetGenerator.Generate();
            var square = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerAssetGenerator.SquareSpritePath);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var level = new GameObject("Level").transform;
            BuildTerrain(level, square);
            var enemies = new GameObject("Enemies").transform;
            PlaceEnemies(enemies);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerAssetGenerator.PlayerPrefabPath));
            player.transform.position = new Vector3(0f, 0.97f, 0f);
            TestCheckpoint.Build(new Vector2(0f, 0.97f));
            CreateCamera(player.transform);
            var hud = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerAssetGenerator.VirtualControlsPrefabPath));
            hud.name = "VirtualControls";
            VirtualControlsBuilder.CreateEventSystem();

            PlayerGeneratorUtil.EnsureFolder("Assets/_Project/Scenes/Test");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[EnemyTestSceneGenerator] Saved {ScenePath}");
        }

        static void BuildTerrain(Transform level, Sprite square)
        {
            Block(level, square, "Floor", -12f, -2f, 84f, 0f, Ground);
            Block(level, square, "Wall_West", -13f, -2f, -12f, 20f, Wall);
            Block(level, square, "Wall_East", 84f, -2f, 85f, 20f, Wall);
            Block(level, square, "Ledge_PatrolBot", 17f, 0f, 24f, 2f, Ground);   // top at y=2: the bot must turn at its edges
            Block(level, square, "Wall_Ghost", 54f, 0f, 55f, 7f, Wall);          // the ghost drifts through this one
            Block(level, square, "Wall_Spider", 64f, 0f, 65f, 9f, Wall);
            Block(level, square, "Ceiling", 60f, 9f, 80f, 10f, Wall);
        }

        static void PlaceEnemies(Transform parent)
        {
            Place("BugThorn", parent, 8f, 0.4f);
            Place("PatrolBot", parent, 20.5f, 2.5f);
            Place("NightKnight", parent, 30f, 0.8f);
            Place("PoisonShroom", parent, 40f, 0.45f);
            Place("Bat", parent, 48f, 5f);
            Place("Ghost", parent, 52f, 3.5f);
            Place("ScrapZapper", parent, 70f, 0.5f);
            var spider = Place("StoneSpider", parent, 63.5f, 1f);
            var path = spider.transform.Find("Path"); // vertical crawl up the wall instead of the default horizontal line
            path.GetChild(1).localPosition = new Vector2(0f, 7f);
        }

        static GameObject Place(string variant, Transform parent, float x, float y)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{EnemyAssetGenerator.PrefabFolder}/{variant}.prefab");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.position = new Vector3(x, y, 0f);
            return instance;
        }

        static void Block(Transform parent, Sprite square, string name, float left, float bottom, float right, float top, Color color)
        {
            var go = AuraPrefabParts.AddSprite(parent, name, square, Vector2.zero, new Vector2(right - left, top - bottom), color);
            go.transform.position = new Vector3((left + right) * 0.5f, (bottom + top) * 0.5f, 0f);
            PlayerGeneratorUtil.SetLayer(go, PhysicsLayers.Ground);
            go.AddComponent<BoxCollider2D>();
        }

        static void CreateCamera(Transform target)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x07, 0x0D, 0x1F, 0xFF);
            go.transform.position = new Vector3(0f, 2f, -10f);
            PlayerGeneratorUtil.SetReference(go.AddComponent<SimpleCameraFollow>(), "target", target);
        }
    }
}
