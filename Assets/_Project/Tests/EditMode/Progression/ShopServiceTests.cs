using AuraKnight.Core;
using AuraKnight.Progression;
using NUnit.Framework;

namespace AuraKnight.Tests.Progression
{
    public sealed class ShopServiceTests
    {
        [SetUp]
        public void SetUp() => EventBus.Clear();

        [Test]
        public void BuyingAHeartSpendsTheFirstTierAndRaisesMaxHearts()
        {
            var state = ProgressionTestUtil.StateWithCoins(250);
            var result = ShopService.TryBuy(state, ProgressionTestUtil.Heart());
            Assert.AreEqual(ShopResult.Bought, result);
            Assert.AreEqual(150, state.coins);
            Assert.AreEqual(6, state.maxHearts);
            Assert.AreEqual(1, state.GetPurchaseCount("heart"));
        }

        [Test]
        public void TheSecondPurchaseCostsTheSecondTier()
        {
            var state = ProgressionTestUtil.StateWithCoins(300);
            var item = ProgressionTestUtil.Heart();
            ShopService.TryBuy(state, item);
            ShopService.TryBuy(state, item);
            Assert.AreEqual(0, state.coins, "100 + 200");
            Assert.AreEqual(7, state.maxHearts);
        }

        [Test]
        public void NotEnoughCoinsBuysNothingAndKeepsEverything()
        {
            var state = ProgressionTestUtil.StateWithCoins(99);
            Assert.AreEqual(ShopResult.NotEnoughCoins, ShopService.TryBuy(state, ProgressionTestUtil.Heart()));
            Assert.AreEqual(99, state.coins);
            Assert.AreEqual(5, state.maxHearts);
            Assert.AreEqual(0, state.GetPurchaseCount("heart"));
        }

        [Test]
        public void CannotBuyMoreThanTheLimitAndCoinsNeverGoNegative()
        {
            var state = ProgressionTestUtil.StateWithCoins(100000);
            var item = ProgressionTestUtil.Sword();
            Assert.AreEqual(ShopResult.Bought, ShopService.TryBuy(state, item));
            Assert.AreEqual(ShopResult.Bought, ShopService.TryBuy(state, item));
            Assert.AreEqual(ShopResult.MaxedOut, ShopService.TryBuy(state, item));
            Assert.AreEqual(100000 - 300 - 600, state.coins);
            Assert.AreEqual(3, state.swordLevel);
            Assert.GreaterOrEqual(state.coins, 0);
        }

        [Test]
        public void ExactChangeLeavesZero()
        {
            var state = ProgressionTestUtil.StateWithCoins(120);
            Assert.AreEqual(ShopResult.Bought, ShopService.TryBuy(state, ProgressionTestUtil.Energy()));
            Assert.AreEqual(0, state.coins);
            Assert.AreEqual(125, state.maxEnergy);
        }

        [Test]
        public void AStatAtItsCapIsNotChargedFor()
        {
            var state = ProgressionTestUtil.StateWithCoins(1000);
            state.maxHearts = 9;
            Assert.AreEqual(ShopResult.AtStatLimit, ShopService.TryBuy(state, ProgressionTestUtil.Heart()));
            Assert.AreEqual(1000, state.coins);
            Assert.AreEqual(0, state.GetPurchaseCount("heart"));
        }

        [Test]
        public void BuyingAMapOnlyRecordsThePurchase()
        {
            var state = ProgressionTestUtil.StateWithCoins(60);
            Assert.AreEqual(ShopResult.Bought, ShopService.TryBuy(state, ProgressionTestUtil.Map("cave")));
            Assert.AreEqual(10, state.coins);
            Assert.IsTrue(MapRules.OwnsRegionMap(state, "cave"));
            Assert.IsFalse(MapRules.OwnsRegionMap(state, "forest"));
            Assert.AreEqual(ShopResult.MaxedOut, ShopService.TryBuy(state, ProgressionTestUtil.Map("cave")));
        }

        [Test]
        public void InvalidItemsAreRefused()
        {
            var state = ProgressionTestUtil.StateWithCoins(500);
            Assert.AreEqual(ShopResult.InvalidItem, ShopService.TryBuy(state, null));
            Assert.AreEqual(500, state.coins);
        }

        [Test]
        public void SpendingPublishesCoinsChanged()
        {
            var state = ProgressionTestUtil.StateWithCoins(500);
            int last = -1;
            EventBus.Subscribe<CoinsChanged>(e => last = e.Coins);
            ShopService.TryBuy(state, ProgressionTestUtil.Heart());
            Assert.AreEqual(400, last);
            EventBus.Clear();
        }

        [Test]
        public void BuyWithoutAGameManagerReportsNoGame() =>
            Assert.AreEqual(ShopResult.NoGame, ShopService.Buy(ProgressionTestUtil.Heart()));
    }
}
