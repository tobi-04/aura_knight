using AuraKnight.Player;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Player
{
    /// <summary>Integrates jump math at a fixed step exactly as the controller does.</summary>
    public sealed class AirPhysicsSimulationTests
    {
        const float Dt = 0.02f;
        PlayerMovementConfig _config;

        [SetUp] public void SetUp() => _config = ScriptableObject.CreateInstance<PlayerMovementConfig>();
        [TearDown] public void TearDown() => Object.DestroyImmediate(_config);

        /// <summary>Returns (apex height, time to apex) of a jump released after <paramref name="holdSeconds"/>.</summary>
        (float height, float apexTime) Simulate(float holdSeconds, float dt)
        {
            var cut = new JumpCut(_config);
            cut.Begin();
            float vy = _config.JumpVelocity, y = 0f, apexY = 0f, apexT = 0f, t = 0f;
            for (int i = 0; i < 600; i++)
            {
                float prev = vy;
                vy = AirPhysics.Step(_config, vy, dt, _config.MaxFallSpeed);
                vy = cut.Update(vy, dt, t < holdSeconds);
                y += AirPhysics.Displacement(prev, vy, dt);
                t += dt;
                if (y > apexY) { apexY = y; apexT = t; }
                if (vy < 0f) break;
            }
            return (apexY, apexT);
        }

        [TestCase(Dt)]
        [TestCase(1f / 60f)]
        public void HeldJumpReachesFourAndAHalfTiles(float dt)
        {
            var (h, t) = Simulate(10f, dt);
            Assert.AreEqual(4.5f, h, 0.1f);
            Assert.AreEqual(0.35f, t, 0.05f);
        }

        [Test]
        public void TappedJumpIsMuchLowerThanHeldJump()
        {
            var (tap, _) = Simulate(0f, Dt);
            var (held, _) = Simulate(10f, Dt);
            // GDD: a tap is about 2 tiles (cut multiplier 0.25 after the 0.08 s minimum hold).
            Assert.AreEqual(2.0f, tap, 0.25f);
            Assert.Less(tap, held * 0.65f);
        }

        [Test]
        public void ReleasingBeforeMinHoldDelaysCutUntilMinHold()
        {
            var cut = new JumpCut(_config);
            cut.Begin();
            float vy = 20f;
            Assert.AreEqual(20f, cut.Update(vy, 0.04f, false), 1e-4f);
            Assert.AreEqual(20f * _config.JumpCutMultiplier, cut.Update(vy, 0.05f, false), 1e-4f);
        }

        [Test]
        public void CutAppliesOnlyOnce()
        {
            var cut = new JumpCut(_config);
            cut.Begin();
            float vy = cut.Update(20f, 0.1f, false);
            Assert.AreEqual(20f * _config.JumpCutMultiplier, vy, 1e-4f);
            Assert.AreEqual(vy, cut.Update(vy, 0.1f, false), 1e-4f);
        }

        [Test]
        public void CutIgnoredWhenAlreadyFalling()
        {
            var cut = new JumpCut(_config);
            cut.Begin();
            Assert.AreEqual(-5f, cut.Update(-5f, 0.1f, false), 1e-4f);
        }

        [Test]
        public void FallingUsesHeavierGravityAndClampsToMaxFallSpeed()
        {
            float up = AirPhysics.Step(_config, 0f + 1f, 0.1f, 20f);
            float down = AirPhysics.Step(_config, 0f - 1f, 0.1f, 20f);
            Assert.AreEqual(1f - _config.JumpGravity * 0.1f, up, 1e-3f);
            Assert.AreEqual(-1f - _config.FallGravity * 0.1f, down, 1e-3f);
            Assert.AreEqual(-20f, AirPhysics.Step(_config, -19f, 1f, 20f));
        }

        [Test]
        public void GlideClampsFallSpeed()
        {
            Assert.AreEqual(-3.5f, AirPhysics.Step(_config, -10f, Dt, _config.GlideMaxFallSpeed));
        }
    }
}
