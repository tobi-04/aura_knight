using AuraKnight.Core;
using AuraKnight.Player;
using AuraKnight.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Reproducible generation of phase 3 assets: movement config, placeholder sprites, Player and
    /// VirtualControls prefabs. Menu: Aura/Player/Generate Player Assets.
    /// Batch: -executeMethod AuraKnight.Editor.PlayerAssetGenerator.Generate
    /// </summary>
    public static class PlayerAssetGenerator
    {
        public const string ConfigPath = "Assets/_Project/Data/Player/PlayerMovementConfig.asset";
        public const string InputActionsPath = "Assets/_Project/Settings/Input/AuraKnight.inputactions";
        public const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";
        public const string VirtualControlsPrefabPath = "Assets/_Project/Prefabs/UI/VirtualControls.prefab";
        public const string LeoSpritePath = "Assets/_Project/Prefabs/Player/Placeholders/LeoPlaceholder.png";
        public const string SquareSpritePath = "Assets/_Project/Prefabs/Player/Placeholders/WhiteSquare.png";

        static readonly Color LeoTint = new Color(0.30f, 0.65f, 1f, 1f);

        [MenuItem("Aura/Player/Generate Player Assets")]
        public static void Generate()
        {
            var config = EnsureConfig();
            var leo = PlayerGeneratorUtil.EnsureSolidSprite(LeoSpritePath, 32, 64); // 1 x 2 units
            PlayerGeneratorUtil.EnsureSolidSprite(SquareSpritePath, 32, 32);
            BuildPlayerPrefab(config, leo);
            BuildVirtualControlsPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PlayerAssetGenerator] Generated config, placeholder sprites, Player and VirtualControls prefabs.");
        }

        /// <summary>Keeps an existing config so values tuned on device are never overwritten.</summary>
        static PlayerMovementConfig EnsureConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PlayerMovementConfig>(ConfigPath);
            if (existing != null) return existing;
            PlayerGeneratorUtil.EnsureFolder("Assets/_Project/Data/Player");
            var config = ScriptableObject.CreateInstance<PlayerMovementConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        static void BuildPlayerPrefab(PlayerMovementConfig config, Sprite leo)
        {
            var root = new GameObject("Player") { tag = "Player" };
            try
            {
                var body = root.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                body.gravityScale = 0f;
                body.freezeRotation = true;
                root.AddComponent<CapsuleCollider2D>().size = new Vector2(0.8f, 1.9f);
                PlayerGeneratorUtil.SetLayer(root, PhysicsLayers.Player);
                var motor = root.AddComponent<KinematicMotor2D>();
                PlayerGeneratorUtil.SetLayerMask(motor, "collisionMask", PhysicsLayers.GroundMask);
                var reader = root.AddComponent<PlayerInputReader>();
                PlayerGeneratorUtil.SetReference(reader, "actions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath));
                var controller = root.AddComponent<PlayerController>();
                PlayerGeneratorUtil.SetReference(controller, "config", config);

                var visual = new GameObject("Visual");
                visual.transform.SetParent(root.transform, false);
                var renderer = visual.AddComponent<SpriteRenderer>();
                renderer.sprite = leo;
                renderer.color = LeoTint;
                renderer.sortingOrder = 10;
                var material = PlayerGeneratorUtil.UnlitSpriteMaterial();
                if (material != null) renderer.sharedMaterial = material;

                var bridge = root.AddComponent<PlayerAnimatorBridge>();
                PlayerGeneratorUtil.SetReference(bridge, "controller", controller);
                PlayerGeneratorUtil.SetReference(bridge, "visual", visual.transform);

                PlayerCombatPrefabBuilder.Attach(root, controller, renderer);
                AuraPlayerPrefabBuilder.Attach(root, controller, renderer, AuraAssetGenerator.EnsureAssets());

                PlayerGeneratorUtil.EnsureFolder("Assets/_Project/Prefabs/Player");
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
        }

        static void BuildVirtualControlsPrefab()
        {
            var circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var root = VirtualControlsBuilder.Build(circle, font);
            try
            {
                root.SetActive(true);
                PlayerGeneratorUtil.EnsureFolder("Assets/_Project/Prefabs/UI");
                PrefabUtility.SaveAsPrefabAsset(root, VirtualControlsPrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
