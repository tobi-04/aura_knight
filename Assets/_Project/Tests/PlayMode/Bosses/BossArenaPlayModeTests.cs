using System.Collections;
using System.Linq;
using AuraKnight.Bosses;
using AuraKnight.Combat;
using AuraKnight.Core;
using AuraKnight.Enemies;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AuraKnight.Tests.PlayMode.Bosses
{
    /// <summary>The generated boss rooms with a stand-in Leo: engage, doors, phases, HP events, reset on death, summons, weak points, laser height.</summary>
    public sealed class BossArenaPlayModeTests
    {
        static readonly string[] Names = BossTestKit.Names;
        GameObject _room, _leo;
        BossArena _arena;
        BossBase _boss;
        Health _leoHealth;
        BossTestKit.Events _events;

        IEnumerator Setup(string bossName)
        {
            BossTestKit.Cleanup();
            yield return BossTestKit.EnsureCleanWorld();
            Time.timeScale = 1f;
            _events = new BossTestKit.Events();
            _room = BossTestKit.SpawnRoom(bossName);
            _arena = _room.GetComponentInChildren<BossArena>();
            _boss = _arena.Boss;
            _leo = BossTestKit.FakeLeo(BossTestKit.RoomPoint(_room, 3.5f, 1.95f), out _leoHealth);
            yield return new WaitForFixedUpdate();
            yield return null;
        }

        IEnumerator Fight()
        {
            _leo.transform.position = BossTestKit.RoomPoint(_room, 15f, 1.95f);
            yield return BossTestKit.WaitUntil(() => _arena.State == BossArena.ArenaState.Fighting, 3f);
            Assert.AreEqual(BossArena.ArenaState.Fighting, _arena.State, "the trigger zone starts the fight");
        }

        [TearDown]
        public void TearDown() => BossTestKit.Cleanup();

        [UnityTest]
        public IEnumerator EngagingClosesTheDoorsAndAnnouncesTheEncounter([ValueSource(nameof(Names))] string bossName)
        {
            yield return Setup(bossName);
            Assert.AreEqual(BossArena.ArenaState.Idle, _arena.State);
            Assert.AreEqual(BossState.Dormant, _boss.State);
            Assert.IsFalse(_arena.DoorsClosed);
            Assert.IsFalse(_boss.Hurtbox.gameObject.activeSelf, "a sleeping boss cannot be hit");
            yield return Fight();
            Assert.IsTrue(_arena.DoorsClosed);
            Assert.IsTrue(_room.transform.Find("DoorEntry").gameObject.activeSelf);
            Assert.IsTrue(_room.transform.Find("DoorExit").gameObject.activeSelf);
            Assert.AreEqual(BossState.Idle, _boss.State);
            Assert.IsTrue(_boss.Hurtbox.gameObject.activeSelf);
            Assert.AreEqual(bossName, _events.Started.Value.BossId);
            Assert.AreEqual(_boss.Stats.displayName, _events.Started.Value.DisplayName);
            Assert.AreEqual(_boss.Stats.maxHp, _events.Health[0].Current);
            Assert.AreEqual(_boss.Stats.maxHp, _events.Health[0].Max);
        }

        [UnityTest]
        public IEnumerator PhaseTwoStartsAtHalfHpAndEveryHpChangeIsPublished([ValueSource(nameof(Names))] string bossName)
        {
            yield return Setup(bossName);
            yield return Fight();
            int max = _boss.Stats.maxHp;
            int threshold = BossPhaseRules.ThresholdHp(max, 0.5f);
            int published = _events.Health.Count;
            while (_boss.Health.Current > threshold + 1)
            {
                BossTestKit.Damage(_boss, 1);
                published++;
                Assert.AreEqual(published, _events.Health.Count, "one BossHealthChanged per hit");
            }
            Assert.AreEqual(0, _boss.PhaseIndex);
            Assert.AreEqual(1f, _boss.Speed, 1e-4f);
            BossTestKit.Damage(_boss, 1);
            Assert.AreEqual(threshold, _events.Health.Last().Current);
            Assert.AreEqual(max, _events.Health.Last().Max);
            Assert.AreEqual(1, _boss.PhaseIndex, "phase 2 at 50%");
            Assert.AreEqual(1.25f, _boss.Speed, 1e-4f, "25% faster");
            Assert.AreEqual(BossState.Transition, _boss.State);
        }

        [UnityTest]
        public IEnumerator SkipToPhaseTwoDebugJumpsStraightThere()
        {
            yield return Setup("RootTree");
            yield return Fight();
            _boss.SkipToPhase(1);
            Assert.AreEqual(1, _boss.PhaseIndex);
            Assert.AreEqual(15, _boss.Health.Current);
        }

        [UnityTest]
        public IEnumerator MalakorEntersTheDarkPhaseAtAQuarterHp()
        {
            yield return Setup("Malakor");
            yield return Fight();
            BossTestKit.Damage(_boss, 70 - 35);
            Assert.AreEqual(1, _boss.PhaseIndex);
            BossTestKit.Damage(_boss, 35 - 17);
            Assert.AreEqual(2, _boss.PhaseIndex);
            Assert.AreEqual(1.25f, _boss.Speed, 1e-4f);
        }

        [UnityTest]
        public IEnumerator EveryAttackTelegraphsAtLeastHalfASecondInEveryPhase([ValueSource(nameof(Names))] string bossName)
        {
            yield return Setup(bossName);
            yield return Fight();
            for (int phase = 0; phase < _boss.Phases.Count; phase++)
            {
                if (phase > 0) _boss.SkipToPhase(phase);
                foreach (var entry in _boss.Phase.attacks)
                {
                    entry.attack.Begin(_boss);
                    float telegraph = 0f;
                    while (entry.attack.Stage == BossAttackStage.Telegraph && telegraph < 5f)
                    {
                        entry.attack.Tick(0.02f);
                        telegraph += 0.02f;
                    }
                    entry.attack.Cancel();
                    _boss.ClearSpawned();
                    Assert.GreaterOrEqual(telegraph, 0.5f - 0.001f, $"{bossName} phase {phase + 1} {entry.attack.name}");
                }
            }
        }

        [UnityTest]
        public IEnumerator DeathResetsTheArenaCompletely([ValueSource(nameof(Names))] string bossName)
        {
            yield return Setup(bossName);
            yield return Fight();
            _boss.SkipToPhase(1);
            foreach (var entry in _boss.Phase.attacks)
            {
                _boss.BeginAttack(entry.attack); // leaves hazards / markers behind
                if (_boss.SpawnedCount > 0) break;
            }
            Assert.Greater(_boss.SpawnedCount, 0, "the attack spawned something to clean up");
            Assert.AreEqual(BossState.Attacking, _boss.State);
            EventBus.Publish(new PlayerDied());
            Assert.AreEqual(BossArena.ArenaState.Idle, _arena.State);
            Assert.IsFalse(_arena.DoorsClosed, "doors reopen");
            Assert.AreEqual(BossState.Dormant, _boss.State);
            Assert.AreEqual(_boss.Stats.maxHp, _boss.Health.Current, "boss HP restored");
            Assert.AreEqual(0, _boss.PhaseIndex);
            Assert.AreEqual(1f, _boss.Speed, 1e-4f);
            Assert.AreEqual(0, _boss.SpawnedCount, "no projectile or marker is left");
            Assert.IsNull(_boss.CurrentAttack);
            Assert.AreEqual(_boss.HomePosition.x, _boss.transform.position.x, 0.01f);
            Assert.AreEqual(_boss.HomePosition.y, _boss.transform.position.y, 0.01f);
            Assert.AreEqual(bossName, _events.Ended.Value.BossId, "the HP bar is told the fight is over");
            Assert.IsFalse(_room.transform.Find("DoorEntry").gameObject.activeSelf);
            _leo.transform.position = BossTestKit.RoomPoint(_room, 3.5f, 1.95f); // Leo respawns before the room, then walks in again
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return Fight(); // and the fight can start again
        }

        [UnityTest]
        public IEnumerator SpiderSummonsTwoScaledSpiderlingsAndAResetRemovesThem()
        {
            yield return Setup("GiantStoneSpider");
            yield return Fight();
            var summon = _boss.GetComponentInChildren<SummonSpiderlingsAttack>(true);
            Assert.IsTrue(summon.CanStart(_boss));
            summon.Begin(_boss);
            for (int i = 0; i < 500 && summon.Stage != BossAttackStage.Recover; i++) summon.Tick(0.02f);
            var minions = Object.FindObjectsByType<EnemyBase>().Where(e => e.IsAlive).ToArray();
            Assert.AreEqual(2, minions.Length);
            Assert.AreEqual(0.6f, minions[0].transform.localScale.x, 1e-3f, "scaled down");
            Assert.AreEqual("StoneSpider", minions[0].Stats.enemyId);
            Assert.IsFalse(summon.CanStart(_boss), "no new summon while two live");
            summon.Cancel();
            EventBus.Publish(new PlayerDied());
            yield return null;
            Assert.AreEqual(0, Object.FindObjectsByType<EnemyBase>().Count(e => e.isActiveAndEnabled), "spiderlings are gone after the reset");
        }

        [UnityTest]
        public IEnumerator RootTreeCoreIsExposedOnlyAfterTheSweepAndTakesDoubleDamage()
        {
            yield return Setup("RootTree");
            yield return Fight();
            var tree = (RootTreeBoss)_boss;
            Assert.IsFalse(tree.CoreExposed);
            Assert.IsFalse(tree.CoreWeakPoint.activeSelf);
            var sweep = tree.GetComponentInChildren<BranchSweepAttack>(true);
            sweep.Begin(tree);
            for (int i = 0; i < 500 && sweep.Stage != BossAttackStage.Recover; i++) sweep.Tick(0.02f);
            Assert.IsTrue(tree.CoreExposed, "the mouth is open during the recover window");
            Assert.IsTrue(tree.CoreWeakPoint.activeSelf);
            var box = tree.CoreWeakPoint.GetComponent<BoxCollider2D>();
            int before = tree.Health.Current;
            BossTestKit.Strike(tree.CoreWeakPoint.transform.TransformPoint(box.offset), 1, Vector2.one * 0.3f);
            Assert.AreEqual(before - 2, tree.Health.Current, "core hit counts double");
            var body = tree.Hurtbox.GetComponent<BoxCollider2D>();
            BossTestKit.Strike(tree.Hurtbox.transform.TransformPoint(body.offset), 1, Vector2.one * 0.3f);
            Assert.AreEqual(before - 3, tree.Health.Current, "body hit counts once");
            sweep.Cancel();
            Assert.IsFalse(tree.CoreExposed, "the core closes when the window ends");
        }

        [UnityTest]
        public IEnumerator BoilerTakesDoubleDamageFromAFireball()
        {
            yield return Setup("RogueMachine");
            yield return Fight();
            var machine = (RogueMachineBoss)_boss;
            var box = machine.Boiler.GetComponent<BoxCollider2D>();
            var boilerPoint = (Vector2)machine.Boiler.transform.TransformPoint(box.offset);
            Assert.AreEqual(HitOutcome.Damaged, BossTestKit.Strike(boilerPoint, 2, Vector2.one * 0.3f, DamageKind.Fire));
            Assert.AreEqual(50 - 4, machine.Health.Current, "fireball 2 x2");
            Assert.AreEqual(HitOutcome.Damaged, BossTestKit.Strike(boilerPoint, 1, Vector2.one * 0.3f));
            Assert.AreEqual(50 - 5, machine.Health.Current, "a sword hit on the boiler is not doubled (GDD 7.4)");
        }

        [UnityTest]
        public IEnumerator LaserHitsAStandingLeoButPassesOverASlidingOne()
        {
            yield return Setup("RogueMachine");
            var laser = _boss.GetComponentInChildren<LaserSweepAttack>(true);
            // Leo stands left of the trigger zone (x < 6) so the boss stays asleep and only this laser is in play.
            Object.DestroyImmediate(_leo);
            _leo = BossTestKit.FakeLeo(BossTestKit.RoomPoint(_room, 4.5f, 1.95f), out var standing, 1.9f);
            yield return BossTestKit.RunAttack(_boss, laser);
            yield return BossTestKit.FixedSteps(150); // the beam is still crossing the room when the attack's own clock ends
            Assert.AreEqual(BossState.Dormant, _boss.State);
            Assert.AreEqual(4, standing.Current, "standing: hit for 1");
            _boss.ClearSpawned();
            Object.DestroyImmediate(_leo);
            _leo = BossTestKit.FakeLeo(BossTestKit.RoomPoint(_room, 4.5f, 1.45f), out var sliding, 0.9f);
            yield return BossTestKit.RunAttack(_boss, laser);
            yield return BossTestKit.FixedSteps(150);
            Assert.AreEqual(5, sliding.Current, "sliding (0.9 tall): untouched");
        }

        [UnityTest]
        public IEnumerator ContactWithTheBossHurtsLeoForOneHeart()
        {
            yield return Setup("RootTree");
            yield return Fight();
            _leo.transform.position = _boss.Hurtbox.transform.position;
            yield return BossTestKit.WaitUntil(() => _leoHealth.Current < 5, 2f);
            Assert.AreEqual(4, _leoHealth.Current);
        }

        [UnityTest]
        public IEnumerator MalakorVictoryCompletesTheGameWithoutAReward()
        {
            yield return Setup("Malakor");
            yield return Fight();
            BossTestKit.Damage(_boss, 999);
            Assert.AreEqual(BossArena.ArenaState.Defeated, _arena.State);
            Assert.IsFalse(_arena.DoorsClosed);
            Assert.IsTrue(_events.GameCompleted);
            CollectionAssert.AreEqual(new[] { "Started", "BossDefeated", "Ended", "GameCompleted" }, _events.Order);
        }
    }
}
