using AuraKnight.Combat;
using AuraKnight.Core;
using AuraKnight.Player;
using NUnit.Framework;

namespace AuraKnight.Tests.Combat
{
    public sealed class PlayerStatsLogicTests
    {
        [Test]
        public void EnergyStartsFullAndAddClampsToMax()
        {
            var energy = new EnergyPool(100f);
            Assert.AreEqual(100f, energy.Current);
            energy.TrySpend(30f);
            Assert.AreEqual(8f, energy.Add(8f));
            Assert.AreEqual(78f, energy.Current, 1e-4f);
            Assert.AreEqual(22f, energy.Add(500f), 1e-4f);
            Assert.AreEqual(100f, energy.Current);
        }

        [Test]
        public void SpendSucceedsOnlyWithEnoughEnergy()
        {
            var energy = new EnergyPool(100f);
            Assert.IsTrue(energy.TrySpend(40f));
            Assert.IsFalse(energy.TrySpend(61f));
            Assert.AreEqual(60f, energy.Current, 1e-4f);
            Assert.IsTrue(energy.TrySpend(60f));
            Assert.AreEqual(0f, energy.Current);
        }

        [Test]
        public void SpendRejectsInvalidAmounts()
        {
            var energy = new EnergyPool(100f);
            Assert.IsFalse(energy.TrySpend(-5f));
            Assert.IsFalse(energy.TrySpend(float.NaN));
            Assert.AreEqual(100f, energy.Current);
            Assert.IsTrue(energy.TrySpend(0f));
        }

        [Test]
        public void EnergyChangedFiresOnlyOnRealChange()
        {
            var energy = new EnergyPool(100f);
            int fired = 0;
            energy.Changed += (_, _) => fired++;
            energy.Add(5f);
            Assert.AreEqual(0, fired);
            energy.TrySpend(10f);
            energy.Add(4f);
            energy.Refill();
            Assert.AreEqual(3, fired);
        }

        [Test]
        public void EnergyMaxCanGrowWithRefill()
        {
            var energy = new EnergyPool(100f);
            energy.SetMax(200f, true);
            Assert.AreEqual(200f, energy.Max);
            Assert.AreEqual(200f, energy.Current);
        }

        [Test]
        public void SeedUsesDefaultsWithoutGameState()
        {
            var seed = PlayerStatsSeed.From(null);
            Assert.AreEqual(5, seed.MaxHearts);
            Assert.AreEqual(100, seed.MaxEnergy);
            Assert.AreEqual(1, seed.SwordLevel);
        }

        [Test]
        public void SeedReadsGameState()
        {
            var state = GameState.NewGame();
            state.maxHearts = 7;
            state.maxEnergy = 150;
            state.swordLevel = 3;
            var seed = PlayerStatsSeed.From(state);
            Assert.AreEqual(7, seed.MaxHearts);
            Assert.AreEqual(150, seed.MaxEnergy);
            Assert.AreEqual(3, seed.SwordLevel);
        }

        [Test]
        public void SeedClampsCorruptValues()
        {
            var state = GameState.NewGame();
            state.maxHearts = 99;
            state.maxEnergy = -4;
            state.swordLevel = 0;
            var seed = PlayerStatsSeed.From(state);
            Assert.AreEqual(PlayerStatsSeed.MaxHeartsLimit, seed.MaxHearts);
            Assert.AreEqual(1, seed.MaxEnergy);
            Assert.AreEqual(1, seed.SwordLevel);
            state.swordLevel = 9;
            Assert.AreEqual(3, PlayerStatsSeed.From(state).SwordLevel);
        }
    }
}
