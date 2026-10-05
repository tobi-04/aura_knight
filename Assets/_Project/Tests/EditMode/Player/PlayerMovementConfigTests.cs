using AuraKnight.Player;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Player
{
    public sealed class PlayerMovementConfigTests
    {
        PlayerMovementConfig _config;

        [SetUp] public void SetUp() => _config = ScriptableObject.CreateInstance<PlayerMovementConfig>();
        [TearDown] public void TearDown() => Object.DestroyImmediate(_config);

        [Test]
        public void DerivedJumpValuesMatchGdd()
        {
            Assert.AreEqual(73.47f, _config.JumpGravity, 0.01f);
            Assert.AreEqual(25.71f, _config.JumpVelocity, 0.01f);
        }

        [Test]
        public void DerivedValuesFollowFormula()
        {
            Assert.AreEqual(2f * _config.MaxJumpHeight / (_config.TimeToApex * _config.TimeToApex), _config.JumpGravity, 1e-3f);
            Assert.AreEqual(2f * _config.MaxJumpHeight / _config.TimeToApex, _config.JumpVelocity, 1e-3f);
            Assert.AreEqual(_config.JumpGravity * 1.6f, _config.FallGravity, 1e-3f);
        }

        [Test]
        public void DefaultsMatchGddMovementTable()
        {
            Assert.AreEqual(8f, _config.RunSpeed);
            Assert.AreEqual(0.06f, _config.AccelerationTime);
            Assert.AreEqual(0.04f, _config.DecelerationTime);
            Assert.AreEqual(0.25f, _config.JumpCutMultiplier); // tap jump = 2 tiles (GDD section 4)
            Assert.AreEqual(0.08f, _config.JumpMinHoldTime);
            Assert.AreEqual(20f, _config.MaxFallSpeed);
            Assert.AreEqual(25f * 0.2f, _config.DashSpeed * _config.DashDuration, 1e-4f);
            Assert.AreEqual(0.1f, _config.DashInvulnerableTime);
            Assert.AreEqual(0.8f, _config.DashCooldown);
            Assert.AreEqual(11f, _config.SlideSpeed);
            Assert.AreEqual(0.45f, _config.SlideDuration);
            Assert.AreEqual(3f, _config.WallSlideMaxSpeed);
            Assert.AreEqual(new Vector2(11f, 22f), _config.WallJumpVelocity);
            Assert.AreEqual(0.15f, _config.WallJumpInputLock);
            Assert.AreEqual(0.1f, _config.CoyoteTime);
            Assert.AreEqual(0.12f, _config.InputBufferTime);
            Assert.AreEqual(0.2f, _config.CornerCorrection);
            Assert.AreEqual(20f, _config.DoubleJumpVelocity);
            Assert.AreEqual(3.5f, _config.GlideMaxFallSpeed);
        }
    }
}
