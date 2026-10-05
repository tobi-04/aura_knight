using AuraKnight.Combat;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Combat
{
    public sealed class SwordLogicTests
    {
        [Test]
        public void ComboAdvancesWithinWindowAndWrapsAfterTwoSwings()
        {
            var combo = new ComboTracker();
            Assert.AreEqual(1, combo.Begin());
            combo.EndSwing();
            combo.Tick(0.2f);
            Assert.IsTrue(combo.CanContinue);
            Assert.AreEqual(2, combo.Begin());
            combo.EndSwing();
            Assert.IsFalse(combo.CanContinue);
            Assert.AreEqual(1, combo.Begin());
        }

        [Test]
        public void ComboResetsWhenWindowExpires()
        {
            var combo = new ComboTracker();
            combo.Begin();
            combo.EndSwing();
            combo.Tick(ComboTracker.Window - 0.01f);
            Assert.IsTrue(combo.CanContinue);
            combo.Tick(0.02f);
            Assert.IsFalse(combo.CanContinue);
            Assert.AreEqual(1, combo.Begin());
        }

        [Test]
        public void ComboWindowOnlyOpensAfterSwingEnds()
        {
            var combo = new ComboTracker();
            combo.Begin();
            combo.Tick(1f);
            Assert.AreEqual(1, combo.Step);
            Assert.IsFalse(combo.CanContinue);
        }

        [Test]
        public void ComboResetClearsProgress()
        {
            var combo = new ComboTracker();
            combo.Begin();
            combo.EndSwing();
            combo.Reset();
            Assert.AreEqual(1, combo.Begin());
        }

        [Test]
        public void SwordTimingMatchesGdd()
        {
            Assert.AreEqual(0.25f, SwordTiming.SwingDuration);
            Assert.AreEqual(0.3f, ComboTracker.Window);
            Assert.AreEqual(1.5f, SwordTiming.Reach);
            Assert.AreEqual(8f, SwordTiming.EnergyPerHit);
            Assert.AreEqual(2, ComboTracker.MaxSteps);
        }

        [Test]
        public void HitboxIsActiveOnlyInsideTheSwingWindow()
        {
            Assert.IsFalse(SwordTiming.IsHitboxActive(0f));
            Assert.IsTrue(SwordTiming.IsHitboxActive((SwordTiming.ActiveStart + SwordTiming.ActiveEnd) * 0.5f));
            Assert.IsFalse(SwordTiming.IsHitboxActive(SwordTiming.ActiveEnd + 0.01f));
            Assert.Less(SwordTiming.ActiveEnd, SwordTiming.SwingDuration);
            Assert.IsFalse(SwordTiming.IsHitboxActive(-1f));
        }

        [Test]
        public void PogoSpeedReachesThreeTilesUnderGravity()
        {
            float g = 73.47f;
            float v = SwordTiming.PogoSpeed(g);
            Assert.AreEqual(3f, v * v / (2f * g), 1e-3f);
        }

        [TestCase(0f, 1f, true, AttackDirection.Up)]
        [TestCase(0.2f, 0.9f, false, AttackDirection.Up)]
        [TestCase(0f, -1f, false, AttackDirection.Down)]
        [TestCase(0f, -1f, true, AttackDirection.Forward)]
        [TestCase(1f, 0f, false, AttackDirection.Forward)]
        [TestCase(0.9f, 0.7f, false, AttackDirection.Forward)]
        [TestCase(0f, 0.4f, true, AttackDirection.Forward)]
        [TestCase(0.9f, -0.7f, false, AttackDirection.Forward)]
        public void AimFollowsStickDirection(float x, float y, bool grounded, AttackDirection expected) =>
            Assert.AreEqual(expected, AttackAim.From(new Vector2(x, y), grounded));

        [Test]
        public void ForwardShapeReachesOneAndAHalfTilesBeyondTheBody()
        {
            var right = SwordShape.Compute(AttackDirection.Forward, 1);
            Assert.AreEqual(SwordShape.BodyHalfWidth + SwordTiming.Reach, right.Center.x + right.Size.x * 0.5f, 1e-4f);
            Assert.AreEqual(SwordShape.BodyHalfWidth, right.Center.x - right.Size.x * 0.5f, 1e-4f);
            var left = SwordShape.Compute(AttackDirection.Forward, -1);
            Assert.AreEqual(-right.Center.x, left.Center.x, 1e-4f);
        }

        [Test]
        public void UpAndDownShapesStartAtTheBodyEdge()
        {
            var up = SwordShape.Compute(AttackDirection.Up, 1);
            var down = SwordShape.Compute(AttackDirection.Down, -1);
            Assert.AreEqual(SwordShape.BodyHalfHeight, up.Center.y - up.Size.y * 0.5f, 1e-4f);
            Assert.AreEqual(SwordTiming.Reach, up.Size.y, 1e-4f);
            Assert.AreEqual(-SwordShape.BodyHalfHeight, down.Center.y + down.Size.y * 0.5f, 1e-4f);
            Assert.AreEqual(0f, up.Center.x);
        }
    }
}
