using AuraKnight.Core;
using AuraKnight.Player;
using AuraKnight.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Builds Scenes/Test/Test_Movement.unity: floor, tall block (tap vs hold jump), 1-tile slide tunnel,
    /// 4-tile pit (dash/jump), one-way platform, 20-tile wall-jump shaft, a gizmo measuring grid and
    /// combat targets (ground dummy, floating dummy, spikes for pogo) west of the spawn.
    /// Menu: Aura/Player/Generate Movement Test Scene. Batch: AuraKnight.Editor.MovementTestSceneGenerator.Generate
    /// </summary>
    public static class MovementTestSceneGenerator
    {
        public const string ScenePath = "Assets/_Project/Scenes/Test/Test_Movement.unity";
        static readonly Color Ground = new Color(0.35f, 0.38f, 0.45f);
        static readonly Color Wall = new Color(0.50f, 0.54f, 0.62f);
        static readonly Color Ceiling = new Color(0.55f, 0.40f, 0.30f);
        static readonly Color Platform = new Color(0.40f, 0.75f, 0.45f);

        [MenuItem("Aura/Player/Generate Movement Test Scene")]
        public static void Generate()
        {
            PlayerAssetGenerator.Generate();
            var square = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerAssetGenerator.SquareSpritePath);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var level = new GameObject("Level").transform;
            BuildLevel(level, square);
            CombatTestObjects.Build(level, square);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(PlayerAssetGenerator.PlayerPrefabPath));
            player.transform.position = new Vector3(0f, 0.97f, 0f);
            TestCheckpoint.Build(new Vector2(0f, 0.97f));
            CreateGlobalLight();
            var camera = CreateCamera(player.transform);
            CreateGizmos(player.transform);

            var hud = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(PlayerAssetGenerator.VirtualControlsPrefabPath));
            hud.name = "VirtualControls";
            VirtualControlsBuilder.CreateEventSystem();

            PlayerGeneratorUtil.EnsureFolder("Assets/_Project/Scenes/Test");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[MovementTestSceneGenerator] Saved {ScenePath} (camera '{camera.name}').");
        }

        /// <summary>Leo uses the lit sprite material, so a scene needs a Global Light 2D or only his own glow lights him.</summary>
        static void CreateGlobalLight()
        {
            var go = new GameObject("Global Light 2D");
            var light = go.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Global;
            light.intensity = 0.8f;
        }

        static void BuildLevel(Transform parent, Sprite square)
        {
            Block(parent, square, "Floor_A", -12f, -2f, 22f, 0f, Ground);
            Block(parent, square, "Floor_B", 26f, -2f, 76f, 0f, Ground);
            Block(parent, square, "Pit_Bottom", 22f, -6f, 26f, -5f, Ground);
            Block(parent, square, "Wall_West", -13f, -6f, -12f, 30f, Wall);
            Block(parent, square, "Wall_East", 76f, -6f, 77f, 30f, Wall);

            Block(parent, square, "TallBlock_3", 6f, 0f, 8f, 3f, Wall);                  // tap jump (~2.0) fails, hold (4.5) clears
            Block(parent, square, "SlideTunnel_1Tile", 12f, 1f, 18f, 3f, Ceiling);        // clearance exactly 1 tile
            Block(parent, square, "OneWay", 44f, 3f, 48f, 3.4f, Platform, oneWay: true);

            Block(parent, square, "Shaft_Left", 56f, 0f, 57f, 26f, Wall);                 // 3 tile wide shaft
            Block(parent, square, "Shaft_Right", 60f, 0f, 61f, 20f, Wall);
            Block(parent, square, "Shaft_TopLedge", 61f, 19f, 68f, 20f, Platform);
        }

        static void Block(Transform parent, Sprite square, string name, float left, float bottom, float right, float top,
            Color color, bool oneWay = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3((left + right) * 0.5f, (bottom + top) * 0.5f, 0f);
            go.transform.localScale = new Vector3(right - left, top - bottom, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.color = color;
            var material = PlayerGeneratorUtil.UnlitSpriteMaterial();
            if (material != null) renderer.sharedMaterial = material;
            PlayerGeneratorUtil.SetLayer(go, PhysicsLayers.Ground);
            var collider = go.AddComponent<BoxCollider2D>();
            if (!oneWay) return;
            collider.usedByEffector = true;
            go.AddComponent<PlatformEffector2D>();
        }

        static GameObject CreateCamera(Transform target)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x07, 0x0D, 0x1F, 0xFF);
            go.transform.position = new Vector3(0f, 2f, -10f);
            PlayerGeneratorUtil.SetReference(go.AddComponent<SimpleCameraFollow>(), "target", target);
            return go;
        }

        static void CreateGizmos(Transform player)
        {
            var go = new GameObject("MovementDebugGizmos");
            var gizmos = go.AddComponent<MovementDebugGizmos>();
            PlayerGeneratorUtil.SetReference(gizmos, "config",
                AssetDatabase.LoadAssetAtPath<PlayerMovementConfig>(PlayerAssetGenerator.ConfigPath));
            PlayerGeneratorUtil.SetReference(gizmos, "player", player);
        }
    }
}
