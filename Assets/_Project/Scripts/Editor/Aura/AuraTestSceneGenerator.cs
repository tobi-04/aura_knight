using AuraKnight.Aura;
using AuraKnight.Core;
using AuraKnight.Combat;
using AuraKnight.Player;
using AuraKnight.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Builds Scenes/Test/Test_Aura.unity: floor with one of every Aura obstacle in a row (burn barricade, fire trap,
    /// lava pit, wind shaft, heat vent, water pool) plus a target dummy; all three Auras are unlocked for testing.
    /// Menu: Aura/Aura System/Generate Test Aura Scene. Batch: AuraKnight.Editor.AuraTestSceneGenerator.Generate
    /// </summary>
    public static class AuraTestSceneGenerator
    {
        public const string ScenePath = "Assets/_Project/Scenes/Test/Test_Aura.unity";
        static readonly Color Ground = new Color(0.35f, 0.38f, 0.45f);
        static readonly Color Wall = new Color(0.50f, 0.54f, 0.62f);

        [MenuItem("Aura/Aura System/Generate Test Aura Scene")]
        public static void Generate()
        {
            PlayerAssetGenerator.Generate();
            var square = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerAssetGenerator.SquareSpritePath);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var level = new GameObject("Level").transform;
            BuildTerrain(level, square);
            BuildObstacles(level);
            CombatTestObjects.Build(level, square);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(PlayerAssetGenerator.PlayerPrefabPath));
            player.transform.position = new Vector3(0f, 0.97f, 0f);
            UnlockAllForTesting(player);
            TestCheckpoint.Build(new Vector2(0f, 0.97f));
            CreateCamera(player.transform);

            var hud = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(PlayerAssetGenerator.VirtualControlsPrefabPath));
            hud.name = "VirtualControls";
            VirtualControlsBuilder.CreateEventSystem();

            PlayerGeneratorUtil.EnsureFolder("Assets/_Project/Scenes/Test");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[AuraTestSceneGenerator] Saved {ScenePath}");
        }

        static void BuildTerrain(Transform level, Sprite square)
        {
            Block(level, square, "Floor_Left", -12f, -2f, 40f, 0f, Ground);
            Block(level, square, "Floor_Mid", 46f, -2f, 66f, 0f, Ground);
            Block(level, square, "Floor_PoolBank", 66f, -2f, 72f, 0f, Ground);
            Block(level, square, "Pool_Bottom", 72f, -7f, 82f, -5f, Ground);
            Block(level, square, "Floor_Right", 82f, -8f, 96f, 0f, Ground);
            Block(level, square, "Wall_West", -13f, -8f, -12f, 30f, Wall);
            Block(level, square, "Wall_East", 96f, -8f, 97f, 30f, Wall);
            Block(level, square, "LavaPit_Floor", 40f, -6f, 46f, -5f, Ground);
            Block(level, square, "WindShaft_Ledge", 52f, 9f, 60f, 10f, Wall);
        }

        static void BuildObstacles(Transform level)
        {
            Place(AuraInteractablePrefabBuilder.BurnGatePath, level, 14f, 1.5f);
            Place(AuraInteractablePrefabBuilder.ExtinguishGatePath, level, 22f, 1.25f);
            Place(AuraInteractablePrefabBuilder.LavaPath, level, 43f, -1.25f);
            Place(AuraInteractablePrefabBuilder.WindCurrentPath, level, 56f, 5f);
            Place(AuraInteractablePrefabBuilder.HeatVentPath, level, 62f, 2f);
            Place(AuraInteractablePrefabBuilder.WaterPath, level, 77f, -2f);
        }

        static void Place(string prefabPath, Transform parent, float x, float y)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), parent);
            instance.transform.position = new Vector3(x, y, 0f);
        }

        static void UnlockAllForTesting(GameObject player)
        {
            var manager = player.GetComponent<AuraManager>();
            AuraSerialized.SetEnumArray(manager, "debugUnlocked", new[] { (int)AuraId.Wind, (int)AuraId.Fire, (int)AuraId.Water });
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
