using AuraKnight.Audio;
using NUnit.Framework;

namespace AuraKnight.Tests.Audio
{
    /// <summary>Crossfade math, loop sync and combat detection used by MusicLayerController.</summary>
    public sealed class MusicLogicTests
    {
        [Test]
        public void Crossfade_ReachesTargetInExactlyTheDuration()
        {
            var fade = new CrossfadeValue(0f);
            fade.SetTarget(1f);
            for (int i = 0; i < 9; i++) fade.Step(0.1f, 1f);
            Assert.AreEqual(0.9f, fade.Value, 1e-4f);
            Assert.IsFalse(fade.IsSettled);
            fade.Step(0.1f, 1f);
            Assert.AreEqual(1f, fade.Value, 1e-4f);
            Assert.IsTrue(fade.IsSettled);
        }

        [Test]
        public void Crossfade_NeverOvershootsAndGoesBothWays()
        {
            var fade = new CrossfadeValue(0.5f);
            fade.SetTarget(1f);
            fade.Step(10f, 1f);
            Assert.AreEqual(1f, fade.Value);
            fade.SetTarget(0f);
            fade.Step(0.25f, 1f);
            Assert.AreEqual(0.75f, fade.Value, 1e-5f);
        }

        [Test]
        public void Crossfade_ZeroDurationJumpsAndNegativeTimeIsIgnored()
        {
            var fade = new CrossfadeValue(0f);
            fade.SetTarget(1f);
            fade.Step(-1f, 1f);
            Assert.AreEqual(0f, fade.Value);
            fade.Step(0.01f, 0f);
            Assert.AreEqual(1f, fade.Value);
        }

        [Test]
        public void Crossfade_ClampsTargetAndStart()
        {
            var fade = new CrossfadeValue(7f);
            Assert.AreEqual(1f, fade.Value);
            fade.SetTarget(-3f);
            Assert.AreEqual(0f, fade.Target);
        }

        [Test]
        public void Sync_ToleratesSmallDriftAndFlagsLargeDrift()
        {
            Assert.IsFalse(MusicSync.NeedsResync(10000, 10900, 1000000));
            Assert.IsTrue(MusicSync.NeedsResync(10000, 20000, 1000000));
        }

        [Test]
        public void Sync_TreatsTheLoopPointAsContinuous()
        {
            Assert.IsFalse(MusicSync.NeedsResync(10, 999990, 1000000), "20 samples apart across the wrap");
            Assert.IsTrue(MusicSync.NeedsResync(500000, 10, 1000000));
            Assert.IsFalse(MusicSync.NeedsResync(1, 2, 0), "unknown length never resyncs");
        }

        [Test]
        public void Intensity_IsOnWhileEnemiesAreNear()
        {
            var tracker = new CombatIntensityTracker(2f);
            Assert.AreEqual(0f, tracker.Update(0, 0.5f));
            Assert.AreEqual(1f, tracker.Update(3, 0.5f));
        }

        [Test]
        public void Intensity_LingersThenDropsAfterTheLastEnemy()
        {
            var tracker = new CombatIntensityTracker(2f);
            tracker.Update(1, 0.5f);
            Assert.AreEqual(1f, tracker.Update(0, 0.5f));
            Assert.AreEqual(1f, tracker.Update(0, 0.5f));
            Assert.AreEqual(1f, tracker.Update(0, 0.5f));
            Assert.AreEqual(0f, tracker.Update(0, 0.5f));
        }

        [Test]
        public void Intensity_NewEnemyRestartsTheLinger()
        {
            var tracker = new CombatIntensityTracker(1f);
            tracker.Update(1, 0.5f);
            tracker.Update(0, 0.5f);
            tracker.Update(1, 0.5f);
            Assert.AreEqual(1f, tracker.Update(0, 0.5f));
            tracker.Reset();
            Assert.AreEqual(0f, tracker.Update(0, 0.5f));
        }
    }
}
