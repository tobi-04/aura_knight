using System.Linq;
using AuraKnight.Bosses;
using AuraKnight.Combat;
using AuraKnight.Core;
using AuraKnight.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Tests.Bosses
{
    /// <summary>Guards the generator output (AuraKnight.Editor.BossAssetGenerator): GDD 7.4 numbers, prefab wiring rules, arena rooms.</summary>
    public sealed class GeneratedBossAssetsTests
    {
        static readonly string[] Names = { "RootTree", "GiantStoneSpider", "RogueMachine", "Malakor" };
        static readonly int[] Hp = { 30, 40, 50, 70 };
        static readonly string[] Rewards = { "Wind", "Fire", "Water", "None" };
        static readonly string[] Regions = { "forest", "cave", "city", "castle" };
        static readonly string[] Folders = { "Forest", "Cave", "City", "Castle" };

        static GameObject Prefab(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Bosses/{name}.prefab");

        static GameObject Room(int index) =>
            AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Rooms/{Folders[index]}/Room_Boss_{Folders[index]}.prefab");

        [Test]
        public void StatsMatchTheGddTable()
        {
            for (int i = 0; i < Names.Length; i++)
            {
                var stats = AssetDatabase.LoadAssetAtPath<BossStats>($"Assets/_Project/Data/Bosses/{Names[i]}.asset");
                Assert.IsNotNull(stats, Names[i]);
                Assert.AreEqual(Names[i], stats.bossId);
                Assert.AreEqual(Hp[i], stats.maxHp, Names[i]);
                Assert.AreEqual(Rewards[i], stats.rewardAura, Names[i]);
                Assert.AreEqual(Regions[i], stats.regionId, Names[i]);
                Assert.AreEqual(i == 3, stats.finalBoss, Names[i]);
                Assert.GreaterOrEqual(stats.minTelegraph, 0.5f);
                Assert.That(stats.contactDamage, Is.InRange(1, 2));
            }
        }

        [Test]
        public void EveryBossHasTwoPhasesAndPhaseTwoIsFasterWithAnExtraVariant()
        {
            for (int i = 0; i < Names.Length; i++)
            {
                var boss = Prefab(Names[i]).GetComponent<BossBase>();
                var phases = boss.Phases;
                Assert.AreEqual(i == 3 ? 3 : 2, phases.Count, Names[i]);
                Assert.AreEqual(1f, phases[0].enterAtHpFraction, 1e-4f);
                Assert.AreEqual(0.5f, phases[1].enterAtHpFraction, 1e-4f);
                Assert.AreEqual(1f, phases[0].speed, 1e-4f);
                Assert.AreEqual(1.25f, phases[1].speed, 1e-4f, "phase 2 is 25% faster");
                Assert.Greater(phases[1].attacks.Count, phases[0].attacks.Count, $"{Names[i]} gains an attack variant in phase 2");
                foreach (var phase in phases) Assert.IsTrue(phase.attacks.All(a => a.attack != null && a.weight > 0f), Names[i]);
            }
        }

        [Test]
        public void MalakorThirdPhaseIsTheDarkPhaseAtAQuarterHp()
        {
            var prefab = Prefab("Malakor");
            Assert.AreEqual(0.25f, prefab.GetComponent<BossBase>().Phases[2].enterAtHpFraction, 1e-4f);
            Assert.IsNotNull(prefab.GetComponentInChildren<DarkPhaseController>(true));
            Assert.IsNotNull(prefab.GetComponentInChildren<AuraColorStrikeAttack>(true));
        }

        [Test]
        public void EveryAttackTelegraphsForAtLeastHalfASecondAtEveryPhaseSpeed()
        {
            foreach (var name in Names)
            {
                var boss = Prefab(name).GetComponent<BossBase>();
                foreach (var attack in Prefab(name).GetComponentsInChildren<BossAttack>(true))
                {
                    Assert.GreaterOrEqual(attack.TelegraphSeconds, 0.5f, $"{name}/{attack.name}");
                    foreach (var phase in boss.Phases)
                        Assert.GreaterOrEqual(attack.EffectiveTelegraph(phase.speed, boss.Stats.minTelegraph), 0.5f - 1e-4f, $"{name}/{attack.name}");
                }
            }
        }

        [Test]
        public void RootSpikesTelegraphForSixTenthsOfASecondInPhaseOne()
        {
            var spikes = Prefab("RootTree").transform.Find("Attacks/RootSpikes").GetComponent<RootSpikeAttack>();
            Assert.AreEqual(0.6f, spikes.EffectiveTelegraph(1f), 1e-4f);
        }

        [Test]
        public void BossLayersAndTheHitboxHurtboxSeparationHold()
        {
            foreach (var name in Names)
            {
                var root = Prefab(name);
                Assert.AreEqual(PhysicsLayers.Id(PhysicsLayers.Enemy), root.transform.Find("Hurtbox").gameObject.layer, name);
                Assert.AreEqual(PhysicsLayers.Id(PhysicsLayers.EnemyAttack), root.transform.Find("ContactHitbox").gameObject.layer, name);
                foreach (var hitbox in root.GetComponentsInChildren<Hitbox>(true))
                    Assert.IsFalse(hitbox.TryGetComponent<Hurtbox>(out _), $"{name}/{hitbox.name} mixes a Hitbox and a Hurtbox");
                Assert.AreEqual(Team.Enemy, root.transform.Find("Hurtbox").GetComponent<Hurtbox>().Team);
            }
        }

        [Test]
        public void WeakPointsDoubleTheDamageAndStayClearOfTheBodyHurtbox()
        {
            foreach (var name in new[] { "RootTree", "RogueMachine" })
            {
                var root = Prefab(name);
                var weak = root.GetComponentInChildren<WeakPointHurtbox>(true);
                Assert.IsNotNull(weak, name);
                Assert.AreEqual(2f, weak.Multiplier, 1e-4f);
                var body = root.transform.Find("Hurtbox").GetComponent<Collider2D>();
                var point = weak.GetComponent<Collider2D>();
                Assert.IsFalse(Overlaps(body, point), $"{name}: one swing would hit body and weak point together");
            }
            Assert.IsFalse(Prefab("RootTree").GetComponent<RootTreeBoss>().CoreWeakPoint.activeSelf, "the core is closed until the sweep ends");
        }

        static bool Overlaps(Collider2D a, Collider2D b)
        {
            var ab = (BoxCollider2D)a;
            var bb = (BoxCollider2D)b;
            Vector2 ac = (Vector2)a.transform.localPosition + ab.offset, bc = (Vector2)b.transform.localPosition + bb.offset;
            return Mathf.Abs(ac.x - bc.x) < (ab.size.x + bb.size.x) * 0.5f && Mathf.Abs(ac.y - bc.y) < (ab.size.y + bb.size.y) * 0.5f;
        }

        [Test]
        public void SummonUsesTheStoneSpiderEnemyPrefab()
        {
            var summon = Prefab("GiantStoneSpider").GetComponentInChildren<SummonSpiderlingsAttack>(true);
            var so = new SerializedObject(summon);
            var prefab = (GameObject)so.FindProperty("spiderlingPrefab").objectReferenceValue;
            Assert.IsNotNull(prefab);
            Assert.AreEqual("StoneSpider", prefab.name);
        }

        [Test]
        public void LaserBeamSitsAboveTheSlideColliderAndBelowTheHead()
        {
            var laser = Prefab("RogueMachine").GetComponentInChildren<LaserSweepAttack>(true);
            Assert.IsTrue(LaserSweepAttack.ClearsSlide(laser.BeamBottom, laser.BeamTop, LaserSweepAttack.SlideBodyHeight));
            Assert.IsFalse(LaserSweepAttack.ClearsSlide(laser.BeamBottom, laser.BeamTop, LaserSweepAttack.StandingBodyHeight));
        }

        [Test]
        public void ArenaRoomsAreValidAndWired()
        {
            for (int i = 0; i < Names.Length; i++)
            {
                var room = Room(i);
                Assert.IsNotNull(room, Names[i]);
                var component = room.GetComponent<Room>();
                Assert.AreEqual($"{Regions[i]}_boss", component.RoomId);
                Assert.AreEqual(Regions[i], component.RegionId);
                Assert.IsTrue(RoomValidator.IsValidId(component.RoomId));
                Assert.IsTrue(component.TryGetSpawn("default", out _));
                var arena = room.GetComponentInChildren<BossArena>();
                Assert.AreEqual(Names[i], arena.Boss.Stats.bossId);
                var doors = new SerializedObject(arena).FindProperty("doors");
                Assert.AreEqual(2, doors.arraySize);
                for (int d = 0; d < 2; d++)
                {
                    var door = (GameObject)doors.GetArrayElementAtIndex(d).objectReferenceValue;
                    Assert.IsFalse(door.activeSelf, "doors are open until the fight starts");
                    Assert.AreEqual(PhysicsLayers.Id(PhysicsLayers.Ground), door.layer);
                }
                Assert.IsTrue(arena.GetComponent<Collider2D>().isTrigger);
                Assert.AreEqual(PhysicsLayers.Id(PhysicsLayers.Interactable), arena.gameObject.layer);
                Assert.IsNotEmpty(arena.DesignerNote);
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>($"Assets/_Project/Scenes/Test/Test_Boss_{Names[i]}.unity"));
            }
        }

        [Test]
        public void OnlyTheMachineRoomHasPlatformsForTheSteam()
        {
            Assert.IsNotNull(Room(2).transform.Find("Greybox/Platform_A"));
            Assert.IsNull(Room(0).transform.Find("Greybox/Platform_A"));
        }
    }
}
