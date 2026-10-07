using AuraKnight.World.Hazards;
using NUnit.Framework;

namespace AuraKnight.Tests.World.Hazards
{
    public sealed class PistonCycleTests
    {
        static void Run(PistonCycle cycle, float seconds)
        {
            for (float t = 0f; t < seconds; t += 0.01f) cycle.Tick(0.01f);
        }

        [Test]
        public void RestsThenTelegraphsThenSlamsThenRetracts()
        {
            var cycle = new PistonCycle();
            Assert.AreEqual(PistonPhase.Retracted, cycle.Phase);
            Assert.AreEqual(0f, cycle.Extension, 1e-6f);
            Run(cycle, PistonCycle.DefaultRest + 0.05f);
            Assert.AreEqual(PistonPhase.Telegraph, cycle.Phase);
            Assert.IsFalse(cycle.IsDangerous, "the telegraph is harmless");
            Run(cycle, PistonCycle.DefaultTelegraph + PistonCycle.DefaultStrike + 0.02f);
            Assert.AreEqual(PistonPhase.Striking, cycle.Phase);
            Assert.IsTrue(cycle.IsDangerous);
            Assert.AreEqual(1f, cycle.Extension, 0.01f);
            Run(cycle, PistonCycle.DefaultHold + PistonCycle.DefaultRetract * 0.6f);
            Assert.AreEqual(PistonPhase.Retracting, cycle.Phase);
        }

        [Test]
        public void TheCycleRepeats()
        {
            var cycle = new PistonCycle();
            Run(cycle, cycle.Period * 3f + 0.02f);
            Assert.AreEqual(PistonPhase.Retracted, cycle.Phase);
            Assert.Less(cycle.Extension, 0.01f);
        }

        [Test]
        public void ExtensionStaysBetweenZeroAndOne()
        {
            var cycle = new PistonCycle();
            for (int i = 0; i < 1000; i++)
            {
                cycle.Tick(0.013f);
                Assert.That(cycle.Extension, Is.InRange(0f, 1f));
            }
        }

        [Test]
        public void ATelegraphIsNeverShorterThanHalfASecond()
        {
            var cycle = new PistonCycle(rest: 0.05f, telegraph: 0.1f);
            int telegraphSteps = 0;
            for (int i = 0; i < 200; i++)
            {
                cycle.Tick(0.01f);
                if (cycle.Phase == PistonPhase.Telegraph) telegraphSteps++;
            }
            Assert.GreaterOrEqual(telegraphSteps, 49, "0.5 s at 100 steps per second, whatever telegraph was asked for");
        }

        [Test]
        public void StartOffsetShiftsTheCycleAndWrapsNegativeValues()
        {
            var a = new PistonCycle(startOffset: 0f);
            var b = new PistonCycle(startOffset: PistonCycle.DefaultRest + 0.1f);
            Assert.AreEqual(PistonPhase.Retracted, a.Phase);
            Assert.AreEqual(PistonPhase.Telegraph, b.Phase);
            var negative = new PistonCycle(startOffset: -0.1f);
            Assert.AreEqual(PistonPhase.Retracting, negative.Phase, "-0.1 s is the tail of the previous lap");
        }

        [Test]
        public void ZeroOrNegativeTimeDoesNotMove()
        {
            var cycle = new PistonCycle();
            cycle.Tick(0f);
            cycle.Tick(-5f);
            Assert.AreEqual(PistonPhase.Retracted, cycle.Phase);
        }
    }
}
