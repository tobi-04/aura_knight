using System.Collections.Generic;
using AuraKnight.Bosses;
using AuraKnight.Combat;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Adds each boss's attack components with their numbers and wires the phase pools (GDD 7.4). Phase 2 starts at 50% HP, runs 25% faster
    /// and adds one attack variant to the pool; Malakor's phase 3 (25%) adds the dark phase and the Aura-coloured strike.
    /// </summary>
    static class BossAttackSetup
    {
        const string SpiderlingPrefab = "Assets/_Project/Prefabs/Enemies/StoneSpider.prefab";
        const float Phase2Speed = 1.25f;

        struct Entry
        {
            public BossAttack Attack;
            public float Weight;
        }

        public static void Build(BossSpec spec, BossBase boss, Health health)
        {
            var attacks = new GameObject("Attacks");
            attacks.transform.SetParent(boss.transform, false);
            switch (spec.Kind)
            {
                case BossKind.RootTree: BuildRootTree(spec, boss, attacks, health); break;
                case BossKind.StoneSpider: BuildSpider(boss, attacks); break;
                case BossKind.RogueMachine: BuildMachine(spec, boss, attacks, health); break;
                default: BuildMalakor(boss, attacks); break;
            }
        }

        static void BuildRootTree(BossSpec spec, BossBase boss, GameObject at, Health health)
        {
            var tree = (RootTreeBoss)boss;
            var core = BossPrefabBuilder.AddWeakPoint(boss.transform, "CoreWeakPoint", new Vector2(0f, 1.0f), new Vector2(1.4f, 1.2f), 2f, health);
            core.gameObject.SetActive(false);
            PlayerGeneratorUtil.SetReference(tree, "coreWeakPoint", core.gameObject);
            var spikes = Add<RootSpikeAttack>(at, "RootSpikes", 0.6f, 0.5f, 0.9f, 1, 1);
            var wide = Add<RootSpikeAttack>(at, "RootSpikesWide", 0.6f, 0.5f, 0.8f, 1, 1);
            PlayerGeneratorUtil.SetInt(wide, "spikeCount", 5);
            PlayerGeneratorUtil.SetFloat(wide, "spacing", 2.4f);
            var seeds = Add<SeedVolleyAttack>(at, "SeedVolley", 0.7f, 0.3f, 1.0f, 2, 1);
            var sweep = Add<BranchSweepAttack>(at, "BranchSweep", 0.9f, 0.5f, 3.0f, 3, 1);
            SetPhases(boss, 1.2f,
                P("Phase 1", 1f, 1f, E(spikes, 3), E(seeds, 3), E(sweep, 2)),
                P("Phase 2", 0.5f, Phase2Speed, E(spikes, 2), E(wide, 2), E(seeds, 3), E(sweep, 3)));
        }

        static void BuildSpider(BossBase boss, GameObject at)
        {
            var drop = Add<CeilingDropAttack>(at, "CeilingDrop", 1.0f, 0.6f, 1.3f, 1, 1);
            var web = Add<WebSpitAttack>(at, "WebSpit", 0.6f, 0.2f, 0.9f, 2, 1);
            var fan = Add<WebSpitAttack>(at, "WebFan", 0.7f, 0.2f, 1.1f, 2, 1);
            PlayerGeneratorUtil.SetInt(fan, "webCount", 3);
            var summon = Add<SummonSpiderlingsAttack>(at, "SummonSpiderlings", 0.8f, 0.3f, 1.0f, 3, 0);
            PlayerGeneratorUtil.SetReference(summon, "spiderlingPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(SpiderlingPrefab));
            SetPhases(boss, 1.2f,
                P("Phase 1", 1f, 1f, E(drop, 3), E(web, 3), E(summon, 2)),
                P("Phase 2", 0.5f, Phase2Speed, E(drop, 3), E(web, 2), E(fan, 2), E(summon, 2)));
        }

        static void BuildMachine(BossSpec spec, BossBase boss, GameObject at, Health health)
        {
            var machine = (RogueMachineBoss)boss;
            var boiler = BossPrefabBuilder.AddWeakPoint(boss.transform, "BoilerWeakPoint", new Vector2(1f, 1.9f), new Vector2(1.6f, 1.6f), 2f, health);
            PlayerGeneratorUtil.SetBool(boiler, "fireOnly", true); // GDD 7.4: the Fireball into the boiler deals x2
            PlayerGeneratorUtil.SetReference(machine, "boiler", boiler);
            var pistons = Add<PistonAttack>(at, "Pistons", 0.8f, 0.5f, 1.0f, 1, 2);
            var wide = Add<PistonAttack>(at, "Pistons5", 0.8f, 0.5f, 1.0f, 1, 2);
            SetFloats(wide, "columnOffsets", -4f, -9f, -14f, -19f, -24f);
            var laser = Add<LaserSweepAttack>(at, "LaserSweep", 0.9f, 0.3f, 1.2f, 2, 1);
            var steam = Add<SteamFloodAttack>(at, "SteamFlood", 1.0f, 2.2f, 1.0f, 3, 1);
            SetPhases(boss, 1.2f,
                P("Phase 1", 1f, 1f, E(pistons, 3), E(laser, 3), E(steam, 2)),
                P("Phase 2", 0.5f, Phase2Speed, E(pistons, 2), E(wide, 2), E(laser, 3), E(steam, 3)));
        }

        static void BuildMalakor(BossBase boss, GameObject at)
        {
            var slash = Add<ShadowSlashAttack>(at, "ShadowSlash", 0.6f, 0.2f, 0.9f, 1, 1);
            var twin = Add<ShadowSlashAttack>(at, "ShadowSlashDouble", 0.6f, 0.8f, 1.0f, 1, 1);
            PlayerGeneratorUtil.SetInt(twin, "waves", 2);
            var stab = Add<TeleportStabAttack>(at, "TeleportStab", 0.8f, 0.5f, 1.1f, 2, 2);
            var strike = Add<AuraColorStrikeAttack>(at, "AuraColorStrike", 0.9f, 0.3f, 1.0f, 3, 1);
            var dark = new GameObject("DarkPhase").AddComponent<DarkPhaseController>();
            dark.transform.SetParent(boss.transform, false);
            PlayerGeneratorUtil.SetReference(dark, "boss", boss);
            SetPhases(boss, 1.2f,
                P("Phase 1", 1f, 1f, E(slash, 3), E(stab, 2)),
                P("Phase 2", 0.5f, Phase2Speed, E(slash, 2), E(twin, 2), E(stab, 3)),
                P("Phase 3 (dark)", 0.25f, Phase2Speed, E(slash, 2), E(twin, 1), E(stab, 2), E(strike, 4)));
        }

        static T Add<T>(GameObject parent, string name, float telegraph, float execute, float recover, int slot, int damage) where T : BossAttack
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var attack = go.AddComponent<T>();
            PlayerGeneratorUtil.SetFloat(attack, "telegraphSeconds", telegraph);
            PlayerGeneratorUtil.SetFloat(attack, "executeSeconds", execute);
            PlayerGeneratorUtil.SetFloat(attack, "recoverSeconds", recover);
            PlayerGeneratorUtil.SetInt(attack, "animationSlot", slot);
            PlayerGeneratorUtil.SetInt(attack, "damage", damage);
            return attack;
        }

        static Entry E(BossAttack attack, float weight) => new Entry { Attack = attack, Weight = weight };

        static (string Name, float Fraction, float Speed, Entry[] Pool) P(string name, float fraction, float speed, params Entry[] pool) =>
            (name, fraction, speed, pool);

        static void SetPhases(BossBase boss, float think, params (string Name, float Fraction, float Speed, Entry[] Pool)[] phases)
        {
            var so = new SerializedObject(boss);
            var list = so.FindProperty("phases");
            list.arraySize = phases.Length;
            for (int i = 0; i < phases.Length; i++)
            {
                var item = list.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("name").stringValue = phases[i].Name;
                item.FindPropertyRelative("enterAtHpFraction").floatValue = phases[i].Fraction;
                item.FindPropertyRelative("speed").floatValue = phases[i].Speed;
                item.FindPropertyRelative("thinkSeconds").floatValue = think;
                var pool = item.FindPropertyRelative("attacks");
                pool.arraySize = phases[i].Pool.Length;
                for (int k = 0; k < phases[i].Pool.Length; k++)
                {
                    var entry = pool.GetArrayElementAtIndex(k);
                    entry.FindPropertyRelative("attack").objectReferenceValue = phases[i].Pool[k].Attack;
                    entry.FindPropertyRelative("weight").floatValue = phases[i].Pool[k].Weight;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetFloats(Object target, string field, params float[] values)
        {
            var so = new SerializedObject(target);
            var array = so.FindProperty(field);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).floatValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
