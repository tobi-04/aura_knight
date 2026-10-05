using AuraKnight.Aura;
using AuraKnight.Aura.Skills;
using AuraKnight.Player.States;
using AuraKnight.World;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Aura
{
    public sealed class WorldRulesTests
    {
        // ---- oxygen ----

        [Test]
        public void OxygenLastsEightSecondsThenCostsAHeartEveryTwo()
        {
            var timer = new OxygenTimer();
            Assert.AreEqual(0, timer.Tick(7.99f, true));
            Assert.IsFalse(timer.IsEmpty);

            Assert.AreEqual(1, timer.Tick(0.02f, true), "air runs out: first heart immediately");
            Assert.IsTrue(timer.IsEmpty);

            Assert.AreEqual(0, timer.Tick(1.98f, true));
            Assert.AreEqual(1, timer.Tick(0.04f, true));
        }

        [Test]
        public void OxygenRefillsInstantlyWhenNotDrowning()
        {
            var timer = new OxygenTimer();
            timer.Tick(5f, true);
            Assert.AreEqual(3f, timer.Remaining, 1e-4f);
            Assert.AreEqual(0, timer.Tick(0.016f, false));
            Assert.AreEqual(timer.Max, timer.Remaining);
        }

        [Test]
        public void LongFrameLosesAsManyHeartsAsIntervalsPassed()
        {
            var timer = new OxygenTimer();
            timer.Tick(8f, true);
            Assert.AreEqual(2, timer.Tick(4f, true));
        }

        // ---- shield ----

        [Test]
        public void ShieldAbsorbsExactlyOneHit()
        {
            var shield = new ShieldState();
            Assert.IsFalse(shield.TryAbsorb(), "inactive shield absorbs nothing");
            shield.Activate();
            Assert.IsTrue(shield.TryAbsorb());
            Assert.IsFalse(shield.TryAbsorb());
            Assert.IsFalse(shield.IsActive);
        }

        [Test]
        public void ShieldExpiresAfterSixSeconds()
        {
            var shield = new ShieldState();
            shield.Activate();
            shield.Tick(5.9f);
            Assert.IsTrue(shield.IsActive);
            shield.Tick(0.2f);
            Assert.IsFalse(shield.IsActive);
            Assert.IsFalse(shield.TryAbsorb());
        }

        // ---- vent ----

        [Test]
        public void VentPulsesTwoSecondsOnTwoOff()
        {
            var vent = new VentCycle();
            Assert.IsTrue(vent.IsOn);
            vent.Tick(1.99f);
            Assert.IsTrue(vent.IsOn);
            vent.Tick(0.02f);
            Assert.IsFalse(vent.IsOn);
            vent.Tick(2f);
            Assert.IsTrue(vent.IsOn);
        }

        // ---- swim ----

        [Test]
        public void SwimQuantizesToEightDirections()
        {
            Assert.AreEqual(Vector2.zero, SwimMath.Quantize8(new Vector2(0.1f, 0.1f)));
            AssertDir(new Vector2(1f, 0.1f), 1f, 0f);
            AssertDir(new Vector2(0.9f, 0.6f), 0.7071f, 0.7071f);
            AssertDir(new Vector2(-0.2f, 1f), 0f, 1f);
            AssertDir(new Vector2(-1f, -1f), -0.7071f, -0.7071f);
            AssertDir(new Vector2(0.3f, -1f), 0f, -1f);
        }

        static void AssertDir(Vector2 stick, float x, float y)
        {
            var d = SwimMath.Quantize8(stick);
            Assert.AreEqual(x, d.x, 1e-3f);
            Assert.AreEqual(y, d.y, 1e-3f);
        }

        // ---- interaction rules ----

        [Test]
        public void EachInteractionNeedsItsOwnAuraOnly()
        {
            foreach (AuraInteraction kind in System.Enum.GetValues(typeof(AuraInteraction)))
            {
                var required = AuraInteractionRules.RequiredAura(kind);
                foreach (var source in AuraIds.All)
                    Assert.AreEqual(source == required, AuraInteractionRules.Accepts(required, kind, kind, source), $"{kind} by {source}");
            }
        }

        [Test]
        public void WrongInteractionKindIsRejectedEvenFromTheRightAura()
        {
            Assert.IsFalse(AuraInteractionRules.Accepts(AuraId.Fire, AuraInteraction.Burn, AuraInteraction.Extinguish, AuraId.Fire));
            Assert.IsFalse(AuraInteractionRules.Accepts(AuraId.None, AuraInteraction.Burn, AuraInteraction.Burn, AuraId.None));
        }
    }
}
