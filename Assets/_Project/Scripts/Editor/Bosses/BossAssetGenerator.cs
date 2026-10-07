using AuraKnight.Bosses;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Generates the phase 8 assets: BossStats under Data/Bosses, the four boss prefabs, the four arena room prefabs and the four Test_Boss_* scenes.
    /// The generator is the source of truth; rerun it instead of editing outputs. Needs Room_Template and the enemy prefabs (spiderling) to exist.
    /// Menu: Aura/Bosses/Generate Everything. Batch: AuraKnight.Editor.BossAssetGenerator.GenerateAll
    /// </summary>
    public static class BossAssetGenerator
    {
        [MenuItem("Aura/Bosses/Generate Everything (assets, rooms, test scenes)")]
        public static void GenerateAll()
        {
            Generate();
            foreach (var spec in BossSpecs.All) BossTestSceneGenerator.Generate(spec);
            Debug.Log("[BossAssetGenerator] Test scenes generated");
        }

        [MenuItem("Aura/Bosses/Generate Boss Assets and Rooms")]
        public static void Generate()
        {
            PlayerAssetGenerator.Generate(); // placeholder square sprite and unlit material
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/StoneSpider.prefab") == null) EnemyAssetGenerator.Generate();
            PlayerGeneratorUtil.EnsureFolder(BossSpec.DataFolder);
            AssetDatabase.Refresh();
            var square = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerAssetGenerator.SquareSpritePath);
            foreach (var spec in BossSpecs.All)
            {
                var stats = WriteStats(spec);
                BossPrefabBuilder.Build(spec, stats, square);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            foreach (var spec in BossSpecs.All) BossRoomBuilder.Build(spec, square);
            AssetDatabase.SaveAssets();
            Debug.Log($"[BossAssetGenerator] {BossSpecs.All.Length} bosses and arena rooms generated");
        }

        static BossStats WriteStats(BossSpec spec)
        {
            var stats = AssetDatabase.LoadAssetAtPath<BossStats>(spec.StatsPath);
            if (stats == null)
            {
                stats = ScriptableObject.CreateInstance<BossStats>();
                AssetDatabase.CreateAsset(stats, spec.StatsPath);
            }
            stats.bossId = spec.Name;
            stats.displayName = spec.DisplayName;
            stats.regionId = spec.RegionId;
            stats.rewardAura = spec.Reward;
            stats.finalBoss = spec.FinalBoss;
            stats.maxHp = spec.Hp;
            stats.contactDamage = 1;
            stats.footOffset = spec.FootOffset;
            stats.bodySize = spec.BodySize;
            stats.introSeconds = 1.8f;
            stats.transitionSeconds = 0.9f;
            stats.minTelegraph = BossTiming.MinTelegraph;
            EditorUtility.SetDirty(stats);
            return stats;
        }
    }
}
