using System.Collections.Generic;
using AuraKnight.World;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.World
{
    public sealed class LightBudgetTests
    {
        static bool[] Run(Vector2[] positions, Vector2 center, int limit)
        {
            var allowed = new bool[positions.Length];
            LightBudget.Select(positions, center, limit, allowed);
            return allowed;
        }

        [Test]
        public void TheNearestLightsWin()
        {
            var positions = new[] { new Vector2(10, 0), new Vector2(1, 0), new Vector2(5, 0), new Vector2(-2, 0) };
            CollectionAssert.AreEqual(new[] { false, true, false, true }, Run(positions, Vector2.zero, 2));
        }

        [Test]
        public void FewerLightsThanTheLimitKeepsThemAll()
        {
            CollectionAssert.AreEqual(new[] { true, true }, Run(new[] { new Vector2(3, 3), new Vector2(-9, 1) }, Vector2.zero, 8));
        }

        [TestCase(0)]
        [TestCase(-3)]
        public void ANonPositiveLimitSwitchesEveryLightOff(int limit)
        {
            CollectionAssert.AreEqual(new[] { false, false }, Run(new[] { Vector2.zero, Vector2.one }, Vector2.zero, limit));
        }

        [Test]
        public void TiesGoToTheLowerIndexAndTheResultHasExactlyLimitEntries()
        {
            var positions = new[] { new Vector2(1, 0), new Vector2(0, 1), new Vector2(-1, 0), new Vector2(0, -1) };
            var allowed = Run(positions, Vector2.zero, 3);
            CollectionAssert.AreEqual(new[] { true, true, true, false }, allowed);
        }

        [Test]
        public void ARecycledBufferIsClearedFirst()
        {
            var allowed = new List<bool> { true, true, true };
            LightBudget.Select(new[] { Vector2.zero, new Vector2(9, 9), new Vector2(8, 8) }, Vector2.zero, 1, allowed);
            CollectionAssert.AreEqual(new[] { true, false, false }, allowed);
        }

        [Test]
        public void PowerSavingHalvesTheBudget()
        {
            Assert.AreEqual(8, LightBudget.Limit(false));
            Assert.AreEqual(4, LightBudget.Limit(true));
        }
    }
}
