using AuraKnight.Core;
using AuraKnight.Progression;
using NUnit.Framework;

namespace AuraKnight.Tests.Progression
{
    public sealed class ChestLogicTests
    {
        [SetUp]
        public void SetUp() => EventBus.Clear();

        [Test]
        public void OpeningPaysCoinsRecordsTheChestAndOnlyHappensOnce()
        {
            var state = ProgressionTestUtil.StateWithCoins(10);
            var rng = new ProgressionTestUtil.FakeRandom(120, 120);
            var first = ChestLogic.Open(state, "chest_forest_01", ChestReward.Coins(), rng);
            Assert.IsTrue(first.Opened);
            Assert.AreEqual(120, first.Coins);
            Assert.AreEqual(130, state.coins);
            Assert.IsTrue(ChestLogic.IsOpen(state, "chest_forest_01"));
            var second = ChestLogic.Open(state, "chest_forest_01", ChestReward.Coins(), rng);
            Assert.IsFalse(second.Opened);
            Assert.AreEqual(130, state.coins, "no second payout");
            Assert.AreEqual(1, state.openedChests.Count);
        }

        [TestCase(0, 100)]
        [TestCase(1000, 150)]
        public void CoinRollStaysWithinOneHundredToOneHundredFiftyInclusive(int rolled, int expected)
        {
            var state = ProgressionTestUtil.StateWithCoins(0);
            var outcome = ChestLogic.Open(state, "c", ChestReward.Coins(), new ProgressionTestUtil.FakeRandom(rolled));
            Assert.AreEqual(expected, outcome.Coins);
            Assert.AreEqual(expected, state.coins);
        }

        [Test]
        public void AFreeUpgradeAppliesTheEffectAndPaysNoCoins()
        {
            var state = ProgressionTestUtil.StateWithCoins(0);
            var outcome = ChestLogic.Open(state, "chest_city_02", ChestReward.FreeUpgrade(ShopEffect.Energy), new ProgressionTestUtil.FakeRandom());
            Assert.IsTrue(outcome.UpgradeGranted);
            Assert.AreEqual(ShopEffect.Energy, outcome.Upgrade);
            Assert.AreEqual(125, state.maxEnergy);
            Assert.AreEqual(0, state.coins);
        }

        [Test]
        public void AnUpgradeForACappedStatFallsBackToCoins()
        {
            var state = ProgressionTestUtil.StateWithCoins(0);
            state.maxHearts = 9;
            var outcome = ChestLogic.Open(state, "c", ChestReward.FreeUpgrade(ShopEffect.Heart), new ProgressionTestUtil.FakeRandom(100));
            Assert.IsFalse(outcome.UpgradeGranted);
            Assert.AreEqual(100, outcome.Coins);
            Assert.AreEqual(9, state.maxHearts);
            Assert.AreEqual(100, state.coins);
        }

        [Test]
        public void ChestCoinsGoThroughTheWalletEvent()
        {
            var state = ProgressionTestUtil.StateWithCoins(0);
            int last = -1;
            EventBus.Subscribe<CoinsChanged>(e => last = e.Coins);
            ChestLogic.Open(state, "c", ChestReward.Coins(), new ProgressionTestUtil.FakeRandom(130));
            Assert.AreEqual(130, last);
            EventBus.Clear();
        }

        [TestCase(null)]
        [TestCase("")]
        public void ChestsWithoutAnIdNeverOpen(string id)
        {
            var state = ProgressionTestUtil.StateWithCoins(0);
            Assert.IsFalse(ChestLogic.Open(state, id, ChestReward.Coins(), new ProgressionTestUtil.FakeRandom()).Opened);
            Assert.AreEqual(0, state.coins);
            Assert.IsFalse(ChestLogic.Open(null, "c", ChestReward.Coins(), null).Opened);
        }

        [Test]
        public void InvertedOrNegativeRangesAreRepaired()
        {
            var state = ProgressionTestUtil.StateWithCoins(0);
            var reward = new ChestReward { kind = ChestRewardKind.Coins, coinsMin = 50, coinsMax = 10 };
            Assert.AreEqual(50, ChestLogic.Open(state, "a", reward, new ProgressionTestUtil.FakeRandom(10)).Coins);
            var negative = new ChestReward { kind = ChestRewardKind.Coins, coinsMin = -5, coinsMax = -1 };
            var outcome = ChestLogic.Open(state, "b", negative, new ProgressionTestUtil.FakeRandom());
            Assert.IsTrue(outcome.Opened);
            Assert.AreEqual(0, outcome.Coins);
            Assert.AreEqual(50, state.coins);
        }

        [Test]
        public void ChestOpenedStateSurvivesJsonRoundTrip()
        {
            var state = ProgressionTestUtil.StateWithCoins(0);
            ChestLogic.Open(state, "chest_cave_01", ChestReward.Coins(), new ProgressionTestUtil.FakeRandom(100));
            var loaded = UnityEngine.JsonUtility.FromJson<GameState>(UnityEngine.JsonUtility.ToJson(state));
            Assert.IsTrue(ChestLogic.IsOpen(loaded, "chest_cave_01"));
        }
    }
}
