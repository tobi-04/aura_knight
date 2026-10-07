using System.Collections.Generic;
using AuraKnight.Core;
using AuraKnight.Progression;
using NUnit.Framework;

namespace AuraKnight.Tests.Progression
{
    public sealed class WalletTests
    {
        [SetUp]
        public void SetUp() => EventBus.Clear();

        [TearDown]
        public void TearDown() => EventBus.Clear();

        [Test]
        public void AddCreditsAndReturnsTheNewTotal()
        {
            var state = ProgressionTestUtil.StateWithCoins(10);
            Assert.AreEqual(15, new Wallet(state).Add(5));
            Assert.AreEqual(15, state.coins);
        }

        [TestCase(0)]
        [TestCase(-3)]
        public void NonPositiveAddChangesNothingAndPublishesNothing(int amount)
        {
            var state = ProgressionTestUtil.StateWithCoins(10);
            int published = 0;
            EventBus.Subscribe<CoinsChanged>(_ => published++);
            Assert.AreEqual(-1, new Wallet(state).Add(amount));
            Assert.AreEqual(10, state.coins);
            Assert.AreEqual(0, published);
        }

        [Test]
        public void AddSaturatesAtIntMax()
        {
            var state = ProgressionTestUtil.StateWithCoins(int.MaxValue - 2);
            Assert.AreEqual(int.MaxValue, new Wallet(state).Add(100));
        }

        [Test]
        public void NegativeSavedCoinsCountAsZero()
        {
            var state = ProgressionTestUtil.StateWithCoins(-50);
            var wallet = new Wallet(state);
            Assert.AreEqual(0, wallet.Coins);
            Assert.IsFalse(wallet.TrySpend(1));
            Assert.AreEqual(7, wallet.Add(7));
        }

        [Test]
        public void SpendTakesExactlyThePriceOrNothing()
        {
            var state = ProgressionTestUtil.StateWithCoins(120);
            var wallet = new Wallet(state);
            Assert.IsTrue(wallet.TrySpend(100));
            Assert.AreEqual(20, state.coins);
            Assert.IsFalse(wallet.TrySpend(21), "short by one coin");
            Assert.AreEqual(20, state.coins);
            Assert.IsTrue(wallet.TrySpend(20), "spending everything is allowed");
            Assert.AreEqual(0, state.coins);
        }

        [Test]
        public void NegativePriceIsRejected()
        {
            var state = ProgressionTestUtil.StateWithCoins(50);
            var wallet = new Wallet(state);
            Assert.IsFalse(wallet.CanAfford(-1));
            Assert.IsFalse(wallet.TrySpend(-10));
            Assert.AreEqual(50, state.coins);
        }

        [Test]
        public void ChangesRaiseTheEventAndTheBus()
        {
            var state = ProgressionTestUtil.StateWithCoins(0);
            var wallet = new Wallet(state);
            var seen = new List<int>();
            var bus = new List<int>();
            wallet.OnCoinsChanged += seen.Add;
            EventBus.Subscribe<CoinsChanged>(e => bus.Add(e.Coins));
            wallet.Add(30);
            wallet.TrySpend(10);
            CollectionAssert.AreEqual(new[] { 30, 20 }, seen);
            CollectionAssert.AreEqual(new[] { 30, 20 }, bus);
        }

        [Test]
        public void NullStateIsRejected() => Assert.Throws<System.ArgumentNullException>(() => new Wallet(null));
    }
}
