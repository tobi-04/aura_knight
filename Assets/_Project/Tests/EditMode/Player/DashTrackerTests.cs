using AuraKnight.Player;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Player
{
    public sealed class DashTrackerTests
    {
        PlayerMovementConfig _config;
        DashTracker _dash;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<PlayerMovementConfig>();
            _dash = new DashTracker(_config);
        }

        [TearDown] public void TearDown() => Object.DestroyImmediate(_config);

        [Test]
        public void CanDashOnGroundAndInAirInitially()
        {
            Assert.IsTrue(_dash.CanDash(true));
            Assert.IsTrue(_dash.CanDash(false));
        }

        [Test]
        public void CooldownBlocksDashUntilEightTenthsElapse()
        {
            _dash.Begin(true);
            Assert.IsFalse(_dash.CanDash(true));
            _dash.Tick(0.79f);
            Assert.IsFalse(_dash.CanDash(true));
            _dash.Tick(0.02f);
            Assert.IsTrue(_dash.CanDash(true));
        }

        [Test]
        public void OnlyOneAirDashUntilReset()
        {
            _dash.Begin(false);
            _dash.Tick(1f);
            Assert.IsFalse(_dash.CanDash(false), "second air dash must be refused");
            Assert.IsTrue(_dash.CanDash(true), "ground dash is always allowed after cooldown");
            _dash.ResetAir();
            Assert.IsTrue(_dash.CanDash(false));
        }

        [Test]
        public void GroundDashDoesNotConsumeAirDash()
        {
            _dash.Begin(true);
            _dash.Tick(1f);
            Assert.IsTrue(_dash.CanDash(false));
        }

        [Test]
        public void InvulnerableForFirstTenthOnly()
        {
            Assert.IsFalse(_dash.IsInvulnerable);
            _dash.Begin(true);
            Assert.IsTrue(_dash.IsInvulnerable);
            _dash.Tick(0.09f);
            Assert.IsTrue(_dash.IsInvulnerable);
            _dash.Tick(0.02f);
            Assert.IsFalse(_dash.IsInvulnerable);
        }
    }
}
