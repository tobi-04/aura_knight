using AuraKnight.Core;
using AuraKnight.Progression;
using NUnit.Framework;

namespace AuraKnight.Tests.Progression
{
    /// <summary>GDD §8: the main route earns about 1500 coins, enough for 2 hearts + 2 energy + 1 sword and not much more.</summary>
    public sealed class EconomyTests
    {
        const int MainRouteCoins = 1500;

        [Test]
        public void BaselineShoppingCostsExactlyWhatTheGddTablesSay()
        {
            var heart = ProgressionTestUtil.Heart();
            var energy = ProgressionTestUtil.Energy();
            var sword = ProgressionTestUtil.Sword();
            Assert.AreEqual(100 + 200, ShopRules.TotalCost(heart, 2));
            Assert.AreEqual(120 + 240, ShopRules.TotalCost(energy, 2));
            Assert.AreEqual(300, ShopRules.TotalCost(sword, 1));
            int baseline = ShopRules.TotalCost(heart, 2) + ShopRules.TotalCost(energy, 2) + ShopRules.TotalCost(sword, 1);
            Assert.AreEqual(960, baseline);
            Assert.LessOrEqual(baseline, MainRouteCoins);
        }

        [Test]
        public void WithMainRouteCoinsTheBaselineLeavesTooLittleForTheNextTierOfEverything()
        {
            int left = MainRouteCoins - 960;
            Assert.AreEqual(540, left);
            Assert.Less(left, ShopRules.PriceFor(ProgressionTestUtil.Sword().Prices, 1), "no second sword");
            Assert.Less(left, ShopRules.PriceFor(ProgressionTestUtil.Heart().Prices, 2) + ShopRules.PriceFor(ProgressionTestUtil.Energy().Prices, 2),
                "not both a 3rd heart and a 3rd energy");
        }

        [Test]
        public void BuyingTheBaselineWithRealPurchasesUsesExactlyTheGddMoney()
        {
            var state = ProgressionTestUtil.StateWithCoins(MainRouteCoins);
            var heart = ProgressionTestUtil.Heart();
            var energy = ProgressionTestUtil.Energy();
            var sword = ProgressionTestUtil.Sword();
            for (int i = 0; i < 2; i++)
            {
                Assert.AreEqual(ShopResult.Bought, ShopService.TryBuy(state, heart));
                Assert.AreEqual(ShopResult.Bought, ShopService.TryBuy(state, energy));
            }
            Assert.AreEqual(ShopResult.Bought, ShopService.TryBuy(state, sword));
            Assert.AreEqual(MainRouteCoins - 960, state.coins);
            Assert.AreEqual(7, state.maxHearts);
            Assert.AreEqual(150, state.maxEnergy);
            Assert.AreEqual(2, state.swordLevel);
            Assert.AreEqual(ShopResult.NotEnoughCoins, ShopService.TryBuy(state, sword), "second sword costs 600");
        }

        [Test]
        public void BuyingEverythingReachesExactlyTheStatCaps()
        {
            var state = ProgressionTestUtil.StateWithCoins(100000);
            foreach (var item in new[] { ProgressionTestUtil.Heart(), ProgressionTestUtil.Energy(), ProgressionTestUtil.Sword() })
                for (int i = 0; i < item.MaxPurchases; i++) Assert.AreEqual(ShopResult.Bought, ShopService.TryBuy(state, item));
            Assert.AreEqual(9, state.maxHearts);
            Assert.AreEqual(200, state.maxEnergy);
            Assert.AreEqual(3, state.swordLevel);
        }
    }
}
