using AuraKnight.World.Hazards;
using NUnit.Framework;

namespace AuraKnight.Tests.World.Hazards
{
    public sealed class StalactiteCycleTests
    {
        [Test]
        public void ShakesHalfASecondThenFalls()
        {
            var cycle = new StalactiteCycle();
            Assert.AreEqual(StalactitePhase.Hanging, cycle.Phase);
            Assert.IsFalse(cycle.Tick(5f), "a hanging stalactite waits for Leo");
            Assert.IsTrue(cycle.Arm());
            Assert.AreEqual(StalactitePhase.Shaking, cycle.Phase);
            Assert.IsFalse(cycle.Tick(0.49f));
            Assert.IsTrue(cycle.Tick(0.02f));
            Assert.AreEqual(StalactitePhase.Falling, cycle.Phase);
        }

        [Test]
        public void FallingNeverEndsOnItsOwnAndLandingStartsTheRest()
        {
            var cycle = new StalactiteCycle();
            cycle.Arm();
            cycle.Tick(1f);
            Assert.IsFalse(cycle.Tick(10f), "only the floor ends a fall");
            Assert.IsTrue(cycle.Land());
            Assert.AreEqual(StalactitePhase.Resting, cycle.Phase);
            Assert.IsFalse(cycle.Land(), "it can land only once per fall");
        }

        [Test]
        public void RespawnsAfterTheRestTime()
        {
            var cycle = new StalactiteCycle();
            cycle.Arm();
            cycle.Tick(1f);
            cycle.Land();
            Assert.IsFalse(cycle.Tick(3.9f));
            Assert.IsTrue(cycle.Tick(0.2f));
            Assert.AreEqual(StalactitePhase.Hanging, cycle.Phase);
            Assert.IsTrue(cycle.Arm(), "and it is armed again");
        }

        [Test]
        public void ArmOnlyWorksWhileHanging()
        {
            var cycle = new StalactiteCycle();
            Assert.IsTrue(cycle.Arm());
            Assert.IsFalse(cycle.Arm());
            cycle.Tick(1f);
            Assert.IsFalse(cycle.Arm());
        }

        [Test]
        public void ResetHangsItAgain()
        {
            var cycle = new StalactiteCycle();
            cycle.Arm();
            cycle.Tick(1f);
            cycle.Reset();
            Assert.AreEqual(StalactitePhase.Hanging, cycle.Phase);
            Assert.AreEqual(0f, cycle.Elapsed, 1e-6f);
        }
    }
}
