using AuraKnight.World.Hazards;
using NUnit.Framework;

namespace AuraKnight.Tests.World.Hazards
{
    public sealed class CollapseCycleTests
    {
        [Test]
        public void StartsSolidAndIgnoresTime()
        {
            var cycle = new CollapseCycle();
            Assert.AreEqual(CollapsePhase.Solid, cycle.Phase);
            Assert.IsFalse(cycle.Tick(10f));
            Assert.AreEqual(CollapsePhase.Solid, cycle.Phase);
        }

        [Test]
        public void CollapsesSixTenthsOfASecondAfterTheStepAndReturnsAfterThree()
        {
            var cycle = new CollapseCycle();
            Assert.IsTrue(cycle.Trigger());
            Assert.AreEqual(CollapsePhase.Shaking, cycle.Phase);
            Assert.IsFalse(cycle.Tick(0.59f));
            Assert.AreEqual(CollapsePhase.Shaking, cycle.Phase);
            Assert.IsTrue(cycle.Tick(0.02f));
            Assert.AreEqual(CollapsePhase.Collapsed, cycle.Phase);
            Assert.IsFalse(cycle.Tick(2.9f));
            Assert.AreEqual(CollapsePhase.Collapsed, cycle.Phase);
            Assert.IsTrue(cycle.Tick(0.2f));
            Assert.AreEqual(CollapsePhase.Solid, cycle.Phase);
        }

        [Test]
        public void ADefaultCycleUsesTheGddNumbers()
        {
            var cycle = new CollapseCycle();
            Assert.AreEqual(0.6f, cycle.Delay, 1e-6f);
            Assert.AreEqual(3f, cycle.RespawnSeconds, 1e-6f);
        }

        [Test]
        public void AShakingOrFallenPlatformIgnoresFurtherSteps()
        {
            var cycle = new CollapseCycle();
            cycle.Trigger();
            Assert.IsFalse(cycle.Trigger(), "already shaking");
            cycle.Tick(0.7f);
            Assert.IsFalse(cycle.Trigger(), "already fallen");
            Assert.AreEqual(CollapsePhase.Collapsed, cycle.Phase);
        }

        [Test]
        public void NegativeOrZeroTimeDoesNothingAndResetRestoresSolid()
        {
            var cycle = new CollapseCycle();
            cycle.Trigger();
            Assert.IsFalse(cycle.Tick(0f));
            Assert.IsFalse(cycle.Tick(-1f));
            Assert.AreEqual(0f, cycle.Elapsed, 1e-6f);
            cycle.Tick(0.7f);
            cycle.Reset();
            Assert.AreEqual(CollapsePhase.Solid, cycle.Phase);
            Assert.IsTrue(cycle.Trigger(), "a reset platform reacts again");
        }

        [Test]
        public void ABigTickOnlyAdvancesOnePhase()
        {
            var cycle = new CollapseCycle();
            cycle.Trigger();
            cycle.Tick(100f);
            Assert.AreEqual(CollapsePhase.Collapsed, cycle.Phase, "the respawn wait starts after the collapse, not inside the same tick");
        }

        [Test]
        public void TooSmallTimesAreClamped()
        {
            var cycle = new CollapseCycle(0f, -1f);
            Assert.Greater(cycle.Delay, 0f);
            Assert.Greater(cycle.RespawnSeconds, 0f);
        }
    }
}
