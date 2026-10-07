using System.Collections.Generic;
using AuraKnight.Bosses;
using AuraKnight.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Bosses
{
    /// <summary>Pure boss rules: phase thresholds, weighted pick, telegraph floor, attack clock, projectile math, slow layering, victory order.</summary>
    public sealed class BossLogicTests
    {
        sealed class Sequence : IRandomSource
        {
            readonly Queue<float> _values;
            public Sequence(params float[] values) { _values = new Queue<float>(values); }
            public float Value() => _values.Count > 0 ? _values.Dequeue() : 0f;
            public int Range(int minInclusive, int maxExclusive) => minInclusive;
        }

        [TestCase(30, 15)]
        [TestCase(40, 20)]
        [TestCase(50, 25)]
        [TestCase(70, 35)]
        public void PhaseTwoStartsAtHalfHp(int max, int threshold)
        {
            var fractions = new[] { 1f, 0.5f };
            Assert.AreEqual(threshold, BossPhaseRules.ThresholdHp(max, 0.5f));
            Assert.AreEqual(0, BossPhaseRules.IndexFor(max, max, fractions));
            Assert.AreEqual(0, BossPhaseRules.IndexFor(threshold + 1, max, fractions));
            Assert.AreEqual(1, BossPhaseRules.IndexFor(threshold, max, fractions));
            Assert.AreEqual(1, BossPhaseRules.IndexFor(1, max, fractions));
        }

        [Test]
        public void ThreePhasesPickTheHighestReachedThreshold()
        {
            var fractions = new[] { 1f, 0.5f, 0.25f };
            Assert.AreEqual(17, BossPhaseRules.ThresholdHp(70, 0.25f));
            Assert.AreEqual(1, BossPhaseRules.IndexFor(35, 70, fractions));
            Assert.AreEqual(1, BossPhaseRules.IndexFor(18, 70, fractions));
            Assert.AreEqual(2, BossPhaseRules.IndexFor(17, 70, fractions));
            Assert.AreEqual(0, BossPhaseRules.IndexFor(70, 70, new float[0]));
        }

        [Test]
        public void WeightedPickFollowsTheWeightsAndSkipsZeroes()
        {
            var weights = new[] { 3f, 0f, 1f };
            Assert.AreEqual(0, WeightedPicker.Pick(weights, new Sequence(0f)));
            Assert.AreEqual(0, WeightedPicker.Pick(weights, new Sequence(0.74f)));
            Assert.AreEqual(2, WeightedPicker.Pick(weights, new Sequence(0.76f)));
            Assert.AreEqual(2, WeightedPicker.Pick(weights, new Sequence(0.999f)));
        }

        [Test]
        public void WeightedPickAvoidsTheLastChoiceUnlessItIsTheOnlyOne()
        {
            Assert.AreEqual(1, WeightedPicker.Pick(new[] { 5f, 1f }, new Sequence(0f), avoid: 0));
            Assert.AreEqual(0, WeightedPicker.Pick(new[] { 5f, 0f }, new Sequence(0.5f), avoid: 0));
            Assert.AreEqual(-1, WeightedPicker.Pick(new[] { 0f, -1f }, new Sequence(0.5f)));
            Assert.AreEqual(-1, WeightedPicker.Pick(new float[0], new Sequence(0.5f)));
        }

        [Test]
        public void WeightedPickDistributionMatchesWeights()
        {
            var rng = new System.Random(7);
            var source = new SystemRandom(rng);
            var counts = new int[3];
            var weights = new[] { 6f, 3f, 1f };
            for (int i = 0; i < 10000; i++) counts[WeightedPicker.Pick(weights, source)]++;
            Assert.AreEqual(6000, counts[0], 300);
            Assert.AreEqual(3000, counts[1], 300);
            Assert.AreEqual(1000, counts[2], 200);
        }

        sealed class SystemRandom : IRandomSource
        {
            readonly System.Random _random;
            public SystemRandom(System.Random random) { _random = random; }
            public float Value() => (float)_random.NextDouble();
            public int Range(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
        }

        [Test]
        public void TelegraphNeverDropsBelowHalfASecondAtPhaseTwoSpeed()
        {
            Assert.AreEqual(0.6f, BossTiming.Telegraph(0.6f, 1f), 1e-4f);
            Assert.AreEqual(0.5f, BossTiming.Telegraph(0.6f, 1.25f), 1e-4f, "0.48 s would be too short");
            Assert.AreEqual(0.72f, BossTiming.Telegraph(0.9f, 1.25f), 1e-4f);
            Assert.AreEqual(0.8f, BossTiming.Scaled(1f, 1.25f), 1e-4f);
        }

        [Test]
        public void TimelineRunsTelegraphExecuteRecoverInOrder()
        {
            var clock = new BossAttackTimeline();
            clock.Begin(0.6f, 0.4f, 0.8f);
            Assert.AreEqual(BossAttackStage.Telegraph, clock.Stage);
            Assert.AreEqual(BossStageEvents.None, clock.Tick(0.5f));
            Assert.AreEqual(BossStageEvents.ExecuteStarted, clock.Tick(0.2f));
            Assert.AreEqual(BossAttackStage.Execute, clock.Stage);
            Assert.AreEqual(BossStageEvents.RecoverStarted, clock.Tick(0.4f));
            Assert.AreEqual(BossStageEvents.None, clock.Tick(0.5f));
            Assert.AreEqual(BossStageEvents.Finished, clock.Tick(0.4f));
            Assert.AreEqual(BossAttackStage.Idle, clock.Stage);
        }

        [Test]
        public void TimelineOneBigTickCrossesEveryBoundaryAndCancelStops()
        {
            var clock = new BossAttackTimeline();
            clock.Begin(0.5f, 0.2f, 0.3f);
            var all = BossStageEvents.ExecuteStarted | BossStageEvents.RecoverStarted | BossStageEvents.Finished;
            Assert.AreEqual(all, clock.Tick(5f));
            clock.Begin(0.5f, 0.2f, 0.3f);
            clock.Cancel();
            Assert.AreEqual(BossAttackStage.Idle, clock.Stage);
            Assert.AreEqual(BossStageEvents.None, clock.Tick(5f), "a cancelled attack never fires again");
        }

        [Test]
        public void TimelineTelegraphLastsAtLeastItsConfiguredLength()
        {
            var clock = new BossAttackTimeline();
            clock.Begin(BossTiming.Telegraph(0.6f, 1.25f), 0.3f, 0.3f);
            float t = 0f;
            while (clock.Stage == BossAttackStage.Telegraph) { clock.Tick(0.02f); t += 0.02f; }
            Assert.GreaterOrEqual(t, BossTiming.MinTelegraph - 0.001f);
        }

        [Test]
        public void BallisticVelocityLandsOnTheTarget()
        {
            var from = new Vector2(10f, 3f);
            var to = new Vector2(4f, 1f);
            var velocity = BossMath.BallisticVelocity(from, to, 1f, 14f);
            var position = from;
            for (int i = 0; i < 50; i++) BossMath.Step(ref position, ref velocity, 14f, 0.02f);
            Assert.AreEqual(to.x, position.x, 0.001f);
            Assert.AreEqual(to.y, position.y, 0.001f);
        }

        [Test]
        public void LaserPassesOverASlideAndHitsAStandingLeo()
        {
            const float bottom = 1.15f, top = 2.45f;
            Assert.IsTrue(LaserSweepAttack.ClearsSlide(bottom, top, LaserSweepAttack.SlideBodyHeight), "sliding (0.9 tall) goes under");
            Assert.IsFalse(LaserSweepAttack.ClearsSlide(bottom, top, LaserSweepAttack.StandingBodyHeight), "standing (1.9 tall) is hit");
            Assert.IsFalse(LaserSweepAttack.ClearsSlide(0.5f, 1.8f, LaserSweepAttack.SlideBodyHeight), "a beam that low would also hit the slide");
        }

        [Test]
        public void FanAndSpreadHelpers()
        {
            Assert.AreEqual(Vector2.right, BossMath.FanDirection(Vector2.right * 5f, 0, 1, 40f));
            var left = BossMath.FanDirection(Vector2.right, 0, 3, 40f);
            var right = BossMath.FanDirection(Vector2.right, 2, 3, 40f);
            Assert.AreEqual(-right.y, left.y, 1e-4f);
            Assert.AreEqual(0f, BossMath.SpreadX(10f, 1, 3, 2f, 0f, 20f) - 10f, 1e-4f);
            Assert.AreEqual(20f, BossMath.SpreadX(19f, 2, 3, 5f, 0f, 20f), "clamped into the room");
        }

        [TestCase(1, 2f, 2)]
        [TestCase(2, 2f, 4)]
        [TestCase(1, 0.5f, 1)]
        [TestCase(0, 2f, 0)]
        public void WeakPointScalesDamageWithAFloorOfOne(int amount, float multiplier, int expected) =>
            Assert.AreEqual(expected, WeakPointMath.Scale(amount, multiplier));

        [Test]
        public void SlowLayersOnTopOfAnotherWritersSpeedAndRestoresIt()
        {
            var layer = new SlowSpeedLayer();
            float speed = 1f;
            speed = layer.Apply(speed, 0.5f);
            Assert.AreEqual(0.5f, speed, 1e-4f);
            speed = layer.Apply(speed, 0.5f);
            Assert.AreEqual(0.5f, speed, 1e-4f, "re-applying must not compound");
            speed = 1.2f; // the Aura binder switched to Fire and overwrote the value
            speed = layer.Apply(speed, 0.5f);
            Assert.AreEqual(0.6f, speed, 1e-4f, "Fire 1.2 x web 0.5");
            speed = 0.6f; // the binder rewrote 1.2 x wading 0.5, by coincidence the very value the layer wrote
            speed = layer.Apply(speed, 0.5f, knownBase: 0.6f);
            Assert.AreEqual(0.3f, speed, 1e-4f, "with the binder's value known the coincidence does not matter");
            speed = layer.Release(speed, knownBase: 0.6f);
            Assert.AreEqual(0.6f, speed, 1e-4f, "released value is the other writer's base");
        }

        [Test]
        public void SlowReleaseWithoutOtherWritersReturnsToOne()
        {
            var layer = new SlowSpeedLayer();
            float speed = layer.Apply(1f, 0.5f);
            Assert.AreEqual(1f, layer.Release(speed), 1e-4f);
        }

        sealed class Recorder : IBossVictorySteps
        {
            public readonly List<string> Calls = new List<string>();
            public bool RewardGranted = true;
            public bool UnlockReward() { Calls.Add("Unlock"); return RewardGranted; }
            public void MarkDefeated() => Calls.Add("Mark");
            public void PublishDefeated() => Calls.Add("BossDefeated");
            public void EndEncounter() => Calls.Add("Ended");
            public void ReleaseMusic() => Calls.Add("ReleaseMusic");
            public void PlayEnding() => Calls.Add("PlayEnding");
            public void CompleteGame() => Calls.Add("GameCompleted");
        }

        [Test]
        public void VictoryUnlocksTheAuraBeforePublishingBossDefeated()
        {
            var steps = new Recorder();
            BossVictorySequence.Run(steps, false);
            CollectionAssert.AreEqual(new[] { "Unlock", "Mark", "BossDefeated", "Ended", "ReleaseMusic" }, steps.Calls);
        }

        [Test]
        public void FinalBossPlaysTheEndingInsteadOfReleasingTheMusic()
        {
            var steps = new Recorder();
            BossVictorySequence.Run(steps, true);
            CollectionAssert.AreEqual(new[] { "Unlock", "Mark", "BossDefeated", "Ended", "PlayEnding", "GameCompleted" }, steps.Calls);
        }

        [Test]
        public void UngrantedRewardLeavesTheBossUndefeatedSoItCanBeFoughtAgain()
        {
            var steps = new Recorder { RewardGranted = false };
            BossVictorySequence.Run(steps, false);
            CollectionAssert.AreEqual(new[] { "Unlock", "Ended", "ReleaseMusic" }, steps.Calls);
        }
    }
}
