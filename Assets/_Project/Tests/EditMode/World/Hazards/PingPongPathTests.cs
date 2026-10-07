using AuraKnight.World.Hazards;
using NUnit.Framework;

namespace AuraKnight.Tests.World.Hazards
{
    public sealed class PingPongPathTests
    {
        [Test]
        public void StartsAtTheLeftEndAndReachesTheRightEnd()
        {
            Assert.AreEqual(-3f, PingPongPath.Offset(6f, 2f, 0f), 1e-5f);
            Assert.AreEqual(3f, PingPongPath.Offset(6f, 2f, 3f), 1e-5f);
            Assert.AreEqual(-3f, PingPongPath.Offset(6f, 2f, 6f), 1e-5f, "back at the start after two legs");
        }

        [Test]
        public void MovesAtTheGivenSpeed()
        {
            Assert.AreEqual(-2f, PingPongPath.Offset(6f, 2f, 0.5f), 1e-5f);
            Assert.AreEqual(2f, PingPongPath.Offset(6f, 2f, 3.5f), 1e-5f);
        }

        [Test]
        public void NeverLeavesTheTravelRange()
        {
            for (float t = -20f; t < 40f; t += 0.07f) Assert.That(PingPongPath.Offset(6f, 2.5f, t), Is.InRange(-3f, 3f));
        }

        [TestCase(0f, 2f)]
        [TestCase(6f, 0f)]
        [TestCase(-1f, 2f)]
        public void DegenerateInputsStandStill(float travel, float speed) => Assert.AreEqual(0f, PingPongPath.Offset(travel, speed, 1.234f));
    }
}
