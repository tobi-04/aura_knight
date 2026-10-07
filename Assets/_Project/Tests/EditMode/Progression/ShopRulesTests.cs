using AuraKnight.Core;
using AuraKnight.Progression;
using NUnit.Framework;

namespace AuraKnight.Tests.Progression
{
    public sealed class ShopRulesTests
    {
        [TestCase(0, 100)]
        [TestCase(1, 200)]
        [TestCase(2, 300)]
        [TestCase(3, 400)]
        public void HeartPriceFollowsTheTierByPurchaseCount(int purchased, int price) =>
            Assert.AreEqual(price, ShopRules.PriceFor(ProgressionTestUtil.Heart().Prices, purchased));

        [Test]
        public void PriceRepeatsTheLastTierAndHandlesBadInput()
        {
            var prices = ProgressionTestUtil.Sword().Prices;
            Assert.AreEqual(600, ShopRules.PriceFor(prices, 5));
            Assert.AreEqual(300, ShopRules.PriceFor(prices, -2));
            Assert.AreEqual(-1, ShopRules.PriceFor(null, 0));
            Assert.AreEqual(-1, ShopRules.PriceFor(new int[0], 0));
        }

        [Test]
        public void QuoteIsAvailableWhenAffordableAndUnaffordableWhenShort()
        {
            var item = ProgressionTestUtil.Heart();
            var rich = ShopRules.Quote(ProgressionTestUtil.StateWithCoins(100), item);
            Assert.AreEqual(ShopStatus.Available, rich.Status);
            Assert.AreEqual(100, rich.Price);
            Assert.IsTrue(rich.CanBuy);
            var poor = ShopRules.Quote(ProgressionTestUtil.StateWithCoins(99), item);
            Assert.AreEqual(ShopStatus.Unaffordable, poor.Status);
            Assert.IsFalse(poor.CanBuy);
        }

        [Test]
        public void QuoteReportsMaxedOutAfterTheLastPurchase()
        {
            var item = ProgressionTestUtil.Sword();
            var state = ProgressionTestUtil.StateWithCoins(5000);
            state.AddPurchase("sword", 2);
            var quote = ShopRules.Quote(state, item);
            Assert.AreEqual(ShopStatus.MaxedOut, quote.Status);
            Assert.AreEqual(2, quote.Purchased);
            Assert.AreEqual(2, quote.Max);
        }

        [Test]
        public void QuoteReportsAStatAtItsCapEvenWhenPurchasesRemain()
        {
            var state = ProgressionTestUtil.StateWithCoins(5000);
            state.maxHearts = 9;
            Assert.AreEqual(ShopStatus.AtStatLimit, ShopRules.Quote(state, ProgressionTestUtil.Heart()).Status);
        }

        [Test]
        public void QuoteOfBrokenItemsIsInvalid()
        {
            var state = ProgressionTestUtil.StateWithCoins(100);
            Assert.AreEqual(ShopStatus.Invalid, ShopRules.Quote(state, null).Status);
            Assert.AreEqual(ShopStatus.Invalid, ShopRules.Quote(null, ProgressionTestUtil.Heart()).Status);
            var noPrices = ProgressionTestUtil.Make("x", ShopEffect.Heart, 1, new int[0], 1);
            Assert.AreEqual(ShopStatus.Invalid, ShopRules.Quote(state, noPrices).Status);
            var noId = ProgressionTestUtil.Make("", ShopEffect.Heart, 1, new[] { 1 }, 1);
            Assert.AreEqual(ShopStatus.Invalid, ShopRules.Quote(state, noId).Status);
        }

        [Test]
        public void EffectCapsMatchThePlayerLimits()
        {
            var state = GameState.NewGame();
            state.maxHearts = 8;
            Assert.IsTrue(ShopEffects.Apply(state, ShopEffect.Heart, 1));
            Assert.AreEqual(9, state.maxHearts);
            Assert.IsFalse(ShopEffects.Apply(state, ShopEffect.Heart, 1));
            Assert.AreEqual(9, state.maxHearts);
            state.maxEnergy = 190;
            Assert.IsTrue(ShopEffects.Apply(state, ShopEffect.Energy, 25));
            Assert.AreEqual(200, state.maxEnergy, "an overshooting upgrade is clamped");
            state.swordLevel = 3;
            Assert.IsFalse(ShopEffects.CanApply(state, ShopEffect.Sword));
            Assert.IsTrue(ShopEffects.CanApply(state, ShopEffect.MapRegion));
        }
    }
}
