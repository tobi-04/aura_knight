using AuraKnight.Aura;
using NUnit.Framework;

namespace AuraKnight.Tests.Aura
{
    public sealed class AuraPassiveTests
    {
        static AuraPassives Wind => new AuraPassives { speedMultiplier = 1f, doubleJump = true, glide = true };
        static AuraPassives Fire => new AuraPassives { speedMultiplier = 1.2f, heatImmune = true };
        static AuraPassives Water => new AuraPassives { speedMultiplier = 1f, swim = true, acidImmune = true };

        [Test]
        public void NoAuraGivesNoAbilitiesAndNormalSpeed()
        {
            var m = AuraPassiveResolver.Resolve(AuraPassives.None, false);
            Assert.IsFalse(m.CanDoubleJump || m.CanGlide || m.SwimMode || m.HeatImmune || m.AcidImmune || m.Drowning);
            Assert.AreEqual(1f, m.SpeedMultiplier);
            Assert.AreEqual(1f, m.JumpMultiplier);
        }

        [Test]
        public void WindGrantsDoubleJumpAndGlideOnly()
        {
            var m = AuraPassiveResolver.Resolve(Wind, false);
            Assert.IsTrue(m.CanDoubleJump);
            Assert.IsTrue(m.CanGlide);
            Assert.IsFalse(m.HeatImmune || m.SwimMode);
            Assert.AreEqual(1f, m.SpeedMultiplier);
        }

        [Test]
        public void FireGivesPlusTwentyPercentSpeedAndHeatImmunity()
        {
            var m = AuraPassiveResolver.Resolve(Fire, false);
            Assert.AreEqual(1.2f, m.SpeedMultiplier, 1e-5f);
            Assert.IsTrue(m.HeatImmune);
            Assert.IsFalse(m.CanDoubleJump || m.CanGlide || m.AcidImmune);
        }

        [Test]
        public void WaterGivesSwimmingOnlyInsideWaterAndAcidImmunity()
        {
            var dry = AuraPassiveResolver.Resolve(Water, false);
            Assert.IsFalse(dry.SwimMode);
            Assert.IsTrue(dry.AcidImmune);

            var wet = AuraPassiveResolver.Resolve(Water, true);
            Assert.IsTrue(wet.SwimMode);
            Assert.IsFalse(wet.Drowning);
            Assert.AreEqual(1f, wet.SpeedMultiplier);
            Assert.AreEqual(1f, wet.JumpMultiplier);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void WadingWithoutWaterAuraHalvesSpeedCutsJumpAndDrowns(int which)
        {
            var passives = which == 0 ? AuraPassives.None : which == 1 ? Wind : Fire;
            var dry = AuraPassiveResolver.Resolve(passives, false);
            var wet = AuraPassiveResolver.Resolve(passives, true);
            Assert.IsFalse(wet.SwimMode);
            Assert.IsTrue(wet.Drowning);
            Assert.AreEqual(dry.SpeedMultiplier * 0.5f, wet.SpeedMultiplier, 1e-5f);
            Assert.AreEqual(0.6f, wet.JumpMultiplier, 1e-5f);
        }

        [Test]
        public void ZeroSpeedMultiplierFromHandMadeAssetCountsAsOne()
        {
            var m = AuraPassiveResolver.Resolve(default(AuraPassives), false);
            Assert.AreEqual(1f, m.SpeedMultiplier);
        }
    }
}
