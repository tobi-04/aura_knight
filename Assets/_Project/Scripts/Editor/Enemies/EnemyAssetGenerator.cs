using AuraKnight.Enemies;
using AuraKnight.World.Pickups;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Generates the phase 7 assets: eight EnemyStats under Data/Enemies, the coin and Light Drop pickup prefabs, the shared
    /// animator controller and the eight variant prefabs. The generator is the source of truth; rerun it instead of editing
    /// the output. Menu: Aura/Enemies/Generate Enemy Assets. Batch: AuraKnight.Editor.EnemyAssetGenerator.Generate
    /// </summary>
    public static class EnemyAssetGenerator
    {
        public const string DataFolder = "Assets/_Project/Data/Enemies";
        public const string PrefabFolder = "Assets/_Project/Prefabs/Enemies";
        public const string PickupFolder = "Assets/_Project/Prefabs/Pickups";
        public const string CoinPrefabPath = PickupFolder + "/CoinPickup.prefab";
        public const string LightDropPrefabPath = PickupFolder + "/LightDrop.prefab";
        public const string BodyMaterialPath = DataFolder + "/EnemyBody.physicsMaterial2D";

        [MenuItem("Aura/Enemies/Generate Enemy Assets")]
        public static void Generate()
        {
            PlayerAssetGenerator.Generate(); // placeholder white square sprite
            PlayerGeneratorUtil.EnsureFolder(DataFolder);
            PlayerGeneratorUtil.EnsureFolder(PrefabFolder);
            PlayerGeneratorUtil.EnsureFolder(PickupFolder);
            AssetDatabase.Refresh();

            var square = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerAssetGenerator.SquareSpritePath);
            EnemyPickupPrefabBuilder.Build(square);
            var controller = EnemyAnimatorControllerBuilder.Build();
            var material = EnsureBodyMaterial();
            foreach (var spec in EnemyVariants.All)
            {
                var stats = WriteStats(spec);
                EnemyPrefabBuilder.Build(spec, stats, square, controller, material);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[EnemyAssetGenerator] {EnemyVariants.All.Length} variants generated");
        }

        static EnemyStats WriteStats(EnemyVariantSpec spec)
        {
            var stats = AssetDatabase.LoadAssetAtPath<EnemyStats>(spec.StatsPath);
            if (stats == null)
            {
                stats = ScriptableObject.CreateInstance<EnemyStats>();
                AssetDatabase.CreateAsset(stats, spec.StatsPath);
            }
            stats.enemyId = spec.Name;
            stats.archetype = spec.Archetype;
            stats.maxHp = spec.Hp;
            stats.contactDamage = 1;
            stats.knockbackScale = spec.KnockbackScale;
            stats.moveSpeed = spec.MoveSpeed;
            stats.chargeSpeed = spec.ChargeSpeed;
            stats.detectRange = spec.DetectRange;
            stats.verticalTolerance = spec.VerticalTolerance;
            stats.turnDelaySeconds = spec.TurnDelay;
            stats.diveSpeed = spec.DiveSpeed;
            stats.windupSeconds = spec.WindupSeconds;
            stats.coinsMin = spec.CoinsMin;
            stats.coinsMax = spec.CoinsMax;
            EditorUtility.SetDirty(stats);
            return stats;
        }

        static PhysicsMaterial2D EnsureBodyMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(BodyMaterialPath);
            if (material != null) return material;
            material = new PhysicsMaterial2D("EnemyBody") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(material, BodyMaterialPath);
            return material;
        }

        [MenuItem("Aura/Enemies/Generate Everything (assets + Test_Enemies)")]
        public static void GenerateAll()
        {
            Generate();
            EnemyTestSceneGenerator.Generate();
        }
    }
}
