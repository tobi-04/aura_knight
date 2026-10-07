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

        // Leo art produced by the art pipeline (Aura/Art/Generate All). Loaded by path so this assembly needs no art references.
        public const string LeoArtSpritePath = "Assets/_Project/Art/Characters/Leo/Leo.png";
        public const string LeoArtControllerPath = "Assets/_Project/Art/Characters/Leo/Leo.controller";
        public const string LitSpriteMaterialPath = "Assets/_Project/Art/Materials/Mat_SpriteLit.mat";
        const string LeoIdleFrame = "Leo_Idle_0";

        static readonly Color LeoTint = new Color(0.30f, 0.65f, 1f, 1f);

        [MenuItem("Aura/Player/Generate Player Assets")]
        public static void Generate()
        {
            var config = EnsureConfig();
            var leo = PlayerGeneratorUtil.EnsureSolidSprite(LeoSpritePath, 32, 64); // 1 x 2 units
            PlayerGeneratorUtil.EnsureSolidSprite(SquareSpritePath, 32, 32);
            BuildPlayerPrefab(config, leo);
            // The prefab above is rebuilt from scratch, so anything another module bolts on must be re-applied here.
            PlayerSfxProbeInstaller.Apply();
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
                renderer.sortingOrder = 10;
                ApplyLeoVisual(visual, renderer, leo);

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

        /// <summary>
        /// Real Leo sprite + Animator Controller + lit material when the art exists (Light2D and the Aura tint then show);
        /// otherwise the blue placeholder on the unlit default material, so the generator still works before art is imported.
        /// </summary>
        static void ApplyLeoVisual(GameObject visual, SpriteRenderer renderer, Sprite placeholder)
        {
            var art = LoadLeoIdleSprite();
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(LeoArtControllerPath);
            var lit = AssetDatabase.LoadAssetAtPath<Material>(LitSpriteMaterialPath);
            if (art == null || lit == null)
            {
                Debug.LogWarning("[PlayerAssetGenerator] Leo art or lit material missing; run Aura/Art/Generate All. Using the placeholder.");
                renderer.sprite = placeholder;
                renderer.color = LeoTint;
                var unlit = PlayerGeneratorUtil.UnlitSpriteMaterial();
                if (unlit != null) renderer.sharedMaterial = unlit;
                return;
            }
            renderer.sprite = art;
            renderer.color = Color.white;
            renderer.sharedMaterial = lit;
            if (controller != null) visual.AddComponent<Animator>().runtimeAnimatorController = controller;
        }

        static Sprite LoadLeoIdleSprite()
        {
            foreach (var asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(LeoArtSpritePath))
                if (asset is Sprite sprite && sprite.name == LeoIdleFrame) return sprite;
            return null;
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
