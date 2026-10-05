using AuraKnight.Player;
using NUnit.Framework;

namespace AuraKnight.Tests.Player
{
    public sealed class CountdownTests
    {
        const float BufferTime = 0.12f;
        const float CoyoteTime = 0.1f;

        [Test]
        public void NewCountdownIsInactive() => Assert.IsFalse(new Countdown().IsActive);

        [Test]
        public void BufferStaysActiveInsideWindowAndExpiresAfter()
        {
            var buffer = new Countdown();
            buffer.Start(BufferTime);
            buffer.Tick(0.1f);
            Assert.IsTrue(buffer.IsActive);
            buffer.Tick(0.03f);
            Assert.IsFalse(buffer.IsActive);
            Assert.AreEqual(0f, buffer.Remaining);
        }

        [Test]
        public void CoyoteWindowAllowsJumpWithinTenthOfASecond()
        {
            var coyote = new Countdown();
            coyote.Start(CoyoteTime);
            coyote.Tick(0.09f);
            Assert.IsTrue(coyote.IsActive);
            coyote.Tick(0.02f);
            Assert.IsFalse(coyote.IsActive);
        }

        [Test]
        public void ConsumeSucceedsOnceThenFails()
        {
            var buffer = new Countdown();
            buffer.Start(BufferTime);
            Assert.IsTrue(buffer.Consume());
            Assert.IsFalse(buffer.Consume());
            Assert.IsFalse(buffer.IsActive);
        }

        [Test]
        public void ConsumeFailsAfterExpiry()
        {
            var buffer = new Countdown();
            buffer.Start(BufferTime);
            buffer.Tick(1f);
            Assert.IsFalse(buffer.Consume());
        }

        [Test]
        public void NegativeDurationIsClampedToInactive()
        {
            var c = new Countdown();
            c.Start(-1f);
            Assert.IsFalse(c.IsActive);
        }
    }
}
