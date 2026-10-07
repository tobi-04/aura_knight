using AuraKnight.Aura;
using AuraKnight.World;
using NUnit.Framework;

namespace AuraKnight.Tests.World.Hazards
{
    public sealed class SealRulesTests
    {
        [Test]
        public void TheThreeSealsAreWindFireWater() =>
            CollectionAssert.AreEqual(new[] { AuraId.Wind, AuraId.Fire, AuraId.Water }, SealRules.SealAuras);

        [TestCase(AuraId.Wind, AuraId.Wind, true)]
        [TestCase(AuraId.Fire, AuraId.Fire, true)]
        [TestCase(AuraId.Water, AuraId.Water, true)]
        [TestCase(AuraId.Wind, AuraId.Fire, false)]
        [TestCase(AuraId.Water, AuraId.None, false)]
        [TestCase(AuraId.None, AuraId.None, false)]
        public void ASealLightsOnlyForItsOwnAura(AuraId seal, AuraId worn, bool expected) =>
            Assert.AreEqual(expected, SealRules.Lights(seal, worn));

        [Test]
        public void TheGateNeedsEverySeal()
        {
            Assert.IsTrue(SealRules.AllLit(new[] { true, true, true }));
            Assert.IsFalse(SealRules.AllLit(new[] { true, true, false }));
            Assert.IsFalse(SealRules.AllLit(new[] { false, false, false }));
        }

        [Test]
        public void NoSealsNeverOpensTheGate()
        {
            Assert.IsFalse(SealRules.AllLit(new bool[0]));
            Assert.IsFalse(SealRules.AllLit(null));
        }
    }
}
