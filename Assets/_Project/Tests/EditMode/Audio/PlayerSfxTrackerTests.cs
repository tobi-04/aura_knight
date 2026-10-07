using AuraKnight.Audio;
using NUnit.Framework;

namespace AuraKnight.Tests.Audio
{
    public sealed class PlayerSfxTrackerTests
    {
        [Test]
        public void Footsteps_FireOncePerStride()
        {
            var steps = new FootstepTracker(1f);
            int count = 0;
            for (int i = 0; i < 100; i++) if (steps.Advance(0.1f, true)) count++; // 10 units of running
            Assert.That(count, Is.InRange(9, 11));
        }

        [Test]
        public void Footsteps_FirstStepComesSoonAfterStartingToRun()
        {
            var steps = new FootstepTracker(1f);
            int stepsUntilFirst = 0;
            while (!steps.Advance(0.1f, true)) stepsUntilFirst++;
            Assert.LessOrEqual(stepsUntilFirst, 4);
        }

        [Test]
        public void Footsteps_SilentInTheAirAndWhenStandingStill()
        {
            var steps = new FootstepTracker(1f);
            for (int i = 0; i < 50; i++) Assert.IsFalse(steps.Advance(0.5f, false));
            for (int i = 0; i < 50; i++) Assert.IsFalse(steps.Advance(0f, true));
        }

        [Test]
        public void Footsteps_CountDistanceRunningLeft()
        {
            var steps = new FootstepTracker(1f);
            int count = 0;
            for (int i = 0; i < 100; i++) if (steps.Advance(-0.1f, true)) count++;
            Assert.That(count, Is.InRange(9, 11));
        }

        [Test]
        public void Landing_NeedsRealAirTime()
        {
            var landing = new LandingDetector(0.12f);
            Assert.IsFalse(landing.Update(false, 0.05f));
            Assert.IsFalse(landing.Update(true, 0.02f), "a 50 ms hop is not a landing");
            Assert.IsFalse(landing.Update(false, 0.1f));
            Assert.IsFalse(landing.Update(false, 0.1f));
            Assert.IsTrue(landing.Update(true, 0.02f));
            Assert.IsFalse(landing.Update(true, 0.02f), "standing on the ground does not land again");
        }
    }
}
