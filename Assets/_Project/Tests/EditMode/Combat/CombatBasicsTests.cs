using AuraKnight.Combat;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Combat
{
    public sealed class CombatBasicsTests
    {
        [Test]
        public void InvulnerabilityRunsDownAndKeepsLongestWindow()
        {
            var inv = new Invulnerability();
            Assert.IsFalse(inv.IsActive);
            inv.Begin(1f);
            inv.Begin(0.2f);
            Assert.AreEqual(1f, inv.Remaining, 1e-5f);
            inv.Tick(0.99f);
            Assert.IsTrue(inv.IsActive);
            inv.Tick(0.02f);
            Assert.IsFalse(inv.IsActive);
            inv.Begin(0f);
            Assert.IsFalse(inv.IsActive);
            inv.Begin(1f);
            inv.Clear();
            Assert.IsFalse(inv.IsActive);
        }

        [Test]
        public void BlinkPatternAlternatesAndEndsVisible()
        {
            Assert.IsTrue(BlinkPattern.IsVisible(0f));
            bool first = BlinkPattern.IsVisible(0.99f);
            bool second = BlinkPattern.IsVisible(0.99f - BlinkPattern.HalfPeriod);
            Assert.AreNotEqual(first, second);
        }

        [Test]
        public void HitRegistryAcceptsEachTargetOncePerActivation()
        {
            var registry = new HitRegistry<string>();
            Assert.IsTrue(registry.TryRegister("a"));
            Assert.IsFalse(registry.TryRegister("a"));
            Assert.IsTrue(registry.TryRegister("b"));
            Assert.AreEqual(2, registry.Count);
            registry.Clear();
            Assert.IsTrue(registry.TryRegister("a"));
        }

        [Test]
        public void SameTeamNeverDamages()
        {
            Assert.IsFalse(DamageRules.CanDamage(Team.Player, Team.Player));
            Assert.IsFalse(DamageRules.CanDamage(Team.Enemy, Team.Enemy));
            Assert.IsFalse(DamageRules.CanDamage(Team.Hazard, Team.Hazard));
            Assert.IsTrue(DamageRules.CanDamage(Team.Player, Team.Enemy));
            Assert.IsTrue(DamageRules.CanDamage(Team.Enemy, Team.Player));
            Assert.IsTrue(DamageRules.CanDamage(Team.Hazard, Team.Player));
            Assert.IsTrue(DamageRules.CanDamage(Team.Player, Team.Hazard));
        }

        [Test]
        public void KnockbackCoversThreeTilesInItsDuration()
        {
            Assert.AreEqual(3f, Knockback.DefaultTiles);
            Assert.AreEqual(Knockback.DefaultTiles, Knockback.Speed(Knockback.DefaultTiles) * Knockback.Duration, 1e-4f);
        }

        [Test]
        public void KnockbackDirectionFollowsDamageDirectionElseFallback()
        {
            var right = new DamageInfo(1, Team.Enemy, null, new Vector2(0.4f, 0.9f));
            var left = new DamageInfo(1, Team.Enemy, null, Vector2.left);
            var none = new DamageInfo(1, Team.Enemy);
            Assert.AreEqual(1, Knockback.Sign(right, -1));
            Assert.AreEqual(-1, Knockback.Sign(left, 1));
            Assert.AreEqual(-1, Knockback.Sign(none, -1));
        }

        [Test]
        public void HitStopFreezesThenRestoresPreviousScale()
        {
            var timer = new HitStopTimer();
            Assert.AreEqual(0f, timer.Request(0.05f, 1f));
            Assert.IsTrue(timer.Active);
            Assert.IsNull(timer.Tick(0.03f, 0f));
            Assert.AreEqual(1f, timer.Tick(0.03f, 0f));
            Assert.IsFalse(timer.Active);
            Assert.IsNull(timer.Tick(0.1f, 1f));
        }

        [Test]
        public void HitStopOverlapExtendsInsteadOfStacking()
        {
            var timer = new HitStopTimer();
            timer.Request(0.05f, 0.5f);
            Assert.IsNull(timer.Request(0.1f, 0f));
            Assert.IsNull(timer.Tick(0.08f, 0f));
            Assert.AreEqual(0.5f, timer.Tick(0.03f, 0f));
        }

        [Test]
        public void HitStopIgnoredWhilePausedOrZeroDuration()
        {
            var timer = new HitStopTimer();
            Assert.IsNull(timer.Request(0.05f, 0f));
            Assert.IsNull(timer.Request(0f, 1f));
            Assert.IsFalse(timer.Active);
        }

        [Test]
        public void HitStopDoesNotOverwriteScaleChangedByOthers()
        {
            var timer = new HitStopTimer();
            timer.Request(0.05f, 1f);
            Assert.IsNull(timer.Tick(0.06f, 0.3f));
            Assert.IsFalse(timer.Active);
        }

        [Test]
        public void HitStopKeepsAPauseThatStartedDuringTheFreeze()
        {
            var timer = new HitStopTimer();
            timer.Request(0.05f, 1f);
            Assert.IsNull(timer.Tick(0.06f, 0f, paused: true), "pause owns the scale; hit stop must not lift it");
            Assert.IsFalse(timer.Active);
        }

        [Test]
        public void HitStopExposesTheScaleToResumeTo()
        {
            var timer = new HitStopTimer();
            timer.Request(0.05f, 0.8f);
            Assert.AreEqual(0.8f, timer.SavedScale);
        }

        [Test]
        public void HitStopAbortReturnsSavedScale()
        {
            var timer = new HitStopTimer();
            timer.Request(0.05f, 0.8f);
            Assert.AreEqual(0.8f, timer.Abort());
            Assert.IsFalse(timer.Active);
        }
    }
}
