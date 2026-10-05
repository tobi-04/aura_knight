using AuraKnight.Player;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Player
{
    public sealed class HorizontalMotionTests
    {
        PlayerMovementConfig _config;

        [SetUp] public void SetUp() => _config = ScriptableObject.CreateInstance<PlayerMovementConfig>();
        [TearDown] public void TearDown() => Object.DestroyImmediate(_config);

        [Test]
        public void ReachesRunSpeedInAccelerationTime()
        {
            float v = 0f;
            for (int i = 0; i < 6; i++) v = HorizontalMotion.Step(_config, v, 8f, 0.01f, 1f);
            Assert.AreEqual(8f, v, 1e-3f);
        }

        [Test]
        public void StopsInDecelerationTime()
        {
            float v = 8f;
            for (int i = 0; i < 4; i++) v = HorizontalMotion.Step(_config, v, 0f, 0.01f, 1f);
            Assert.AreEqual(0f, v, 1e-3f);
        }

        [Test]
        public void SpeedMultiplierScalesRates()
        {
            float v = HorizontalMotion.Step(_config, 0f, 9.6f, 0.06f, 1.2f);
            Assert.AreEqual(9.6f, v, 1e-3f);
        }

        [Test]
        public void ReversingPassesThroughZeroWithoutOvershoot()
        {
            float v = HorizontalMotion.Step(_config, 8f, -8f, 0.02f, 1f);
            Assert.That(v, Is.InRange(-8f, 8f));
            Assert.Less(v, 8f);
        }
    }
}
