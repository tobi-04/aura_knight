using AuraKnight.UI;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Player
{
    public sealed class VirtualControlsMathTests
    {
        // ---- JoystickMath ----
        [Test]
        public void JoystickInsideDeadzoneIsZero() =>
            Assert.AreEqual(Vector2.zero, JoystickMath.Normalize(new Vector2(5f, 0f), 100f, 0.2f));

        [Test]
        public void JoystickScalesLinearlyInsideRadius() =>
            Assert.AreEqual(0.5f, JoystickMath.Normalize(new Vector2(50f, 0f), 100f, 0.1f).x, 1e-4f);

        [Test]
        public void JoystickClampsToUnitLength()
        {
            var v = JoystickMath.Normalize(new Vector2(300f, -400f), 100f, 0.1f);
            Assert.AreEqual(1f, v.magnitude, 1e-4f);
            Assert.AreEqual(-0.8f, v.y, 1e-4f);
        }

        [Test]
        public void JoystickWithZeroRadiusIsZero() =>
            Assert.AreEqual(Vector2.zero, JoystickMath.Normalize(new Vector2(10f, 10f), 0f, 0.1f));

        [Test]
        public void ClampToRadiusKeepsDirection()
        {
            var v = JoystickMath.ClampToRadius(new Vector2(0f, -250f), 100f);
            Assert.AreEqual(new Vector2(0f, -100f), v);
        }

        // ---- SwipeGesture ----
        [Test]
        public void SwipeDownFarAndFastTriggersOnce()
        {
            var g = new SwipeGesture(60f, 0.25f);
            g.Begin(new Vector2(500f, 400f), 0f);
            Assert.IsFalse(g.Update(new Vector2(502f, 380f), 0.05f));
            Assert.IsTrue(g.Update(new Vector2(505f, 335f), 0.1f));
            Assert.IsFalse(g.Update(new Vector2(505f, 200f), 0.15f), "already fired");
        }

        [Test]
        public void SwipeTooSlowDoesNotTrigger()
        {
            var g = new SwipeGesture(60f, 0.25f);
            g.Begin(new Vector2(0f, 400f), 0f);
            Assert.IsFalse(g.Update(new Vector2(0f, 300f), 0.4f));
        }

        [Test]
        public void SwipeUpOrSidewaysDoesNotTrigger()
        {
            var g = new SwipeGesture(60f, 0.25f);
            g.Begin(new Vector2(0f, 400f), 0f);
            Assert.IsFalse(g.Update(new Vector2(0f, 500f), 0.1f));
            Assert.IsFalse(g.Update(new Vector2(200f, 330f), 0.12f), "mostly horizontal");
        }

        [Test]
        public void SwipeNeedsBeginBeforeUpdate()
        {
            var g = new SwipeGesture(60f, 0.25f);
            Assert.IsFalse(g.Update(new Vector2(0f, -500f), 0.1f));
        }

        [Test]
        public void SwipeEndResetsTracking()
        {
            var g = new SwipeGesture(60f, 0.25f);
            g.Begin(Vector2.zero, 0f);
            g.End();
            Assert.IsFalse(g.Update(new Vector2(0f, -100f), 0.1f));
        }

        // ---- UiMetrics / SafeArea ----
        [Test]
        public void DpConvertsUsingDpi()
        {
            Assert.AreEqual(60f, UiMetrics.DpToPixels(60f, 160f), 1e-4f);
            Assert.AreEqual(157.5f, UiMetrics.DpToPixels(60f, 420f), 1e-3f);
        }

        [Test]
        public void UnknownDpiFallsBackToBaseline() =>
            Assert.AreEqual(60f, UiMetrics.DpToPixels(60f, 0f), 1e-4f);

        [Test]
        public void SafeAreaMapsToNormalisedAnchors()
        {
            SafeAreaMath.ToAnchors(new Rect(100f, 0f, 1800f, 1080f), 2000, 1080, out var min, out var max);
            Assert.AreEqual(new Vector2(0.05f, 0f), min);
            Assert.AreEqual(new Vector2(0.95f, 1f), max);
        }

        [Test]
        public void ZeroScreenFallsBackToFullRect()
        {
            SafeAreaMath.ToAnchors(new Rect(0, 0, 0, 0), 0, 0, out var min, out var max);
            Assert.AreEqual(Vector2.zero, min);
            Assert.AreEqual(Vector2.one, max);
        }

        [Test]
        public void OneSwipeGestureInstanceServesConsecutiveTouchesWithTheirOwnThreshold()
        {
            var g = new SwipeGesture(0f, 0.25f);
            g.Begin(new Vector2(100f, 100f), 0f, 60f);
            Assert.IsFalse(g.Update(new Vector2(100f, 60f), 0.1f), "40 px is below the 60 px threshold");
            Assert.IsTrue(g.Update(new Vector2(100f, 30f), 0.15f));
            g.End();
            g.Begin(new Vector2(100f, 100f), 1f, 30f);
            Assert.IsTrue(g.Update(new Vector2(100f, 60f), 1.1f), "the next touch uses its own 30 px threshold");
        }
    }
}
