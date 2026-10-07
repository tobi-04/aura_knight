using AuraKnight.Core;
using AuraKnight.Enemies;
using AuraKnight.World.Pickups;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Enemies
{
    public sealed class CoinCollectorTests
    {
        [SetUp]
        public void SetUp() => EventBus.Clear();

        [TearDown]
        public void TearDown() => EventBus.Clear();

        [Test]
        public void AddSaturatingNeverOverflowsOrGoesNegative()
        {
            Assert.AreEqual(8, CoinCollector.AddSaturating(5, 3));
            Assert.AreEqual(int.MaxValue, CoinCollector.AddSaturating(int.MaxValue - 1, 10));
            Assert.AreEqual(5, CoinCollector.AddSaturating(5, 0));
            Assert.AreEqual(5, CoinCollector.AddSaturating(5, -3));
            Assert.AreEqual(0, CoinCollector.AddSaturating(-7, 0));
            Assert.AreEqual(3, CoinCollector.AddSaturating(-7, 3));
        }

        [Test]
        public void CollectWithoutAGameManagerStillPublishesTheCoinEvent()
        {
            int amount = 0, changed = 0;
            EventBus.Subscribe<CoinsCollected>(e => amount += e.Amount);
            EventBus.Subscribe<CoinsChanged>(_ => changed++);
            Assert.AreEqual(-1, CoinCollector.Collect(4), "no GameState to update");
            Assert.AreEqual(4, amount);
            Assert.AreEqual(0, changed);
        }

        [Test]
        public void NonPositiveAmountsPublishNothing()
        {
            int events = 0;
            EventBus.Subscribe<CoinsCollected>(_ => events++);
            Assert.AreEqual(-1, CoinCollector.Collect(0));
            Assert.AreEqual(-1, CoinCollector.Collect(-2));
            Assert.AreEqual(0, events);
        }
    }

    public sealed class CoinMagnetTests
    {
        [Test]
        public void PullsWithinTwoTilesOnly()
        {
            Assert.IsTrue(CoinMagnet.InRange(new Vector2(1.9f, 0f), Vector2.zero));
            Assert.IsTrue(CoinMagnet.InRange(new Vector2(2f, 0f), Vector2.zero));
            Assert.IsFalse(CoinMagnet.InRange(new Vector2(2.1f, 0f), Vector2.zero));
        }

        [Test]
        public void PullMovesTowardTheTargetAndNeverPastIt()
        {
            var coin = new Vector2(1.5f, 0f);
            var next = CoinMagnet.Pull(coin, Vector2.zero, 9f, 0.05f);
            Assert.Less(next.x, coin.x);
            Assert.GreaterOrEqual(next.x, 0f);
            Assert.AreEqual(Vector2.zero, CoinMagnet.Pull(new Vector2(0.1f, 0f), Vector2.zero, 9f, 1f));
        }

        [Test]
        public void ReachedIsAHalfTileOrCloser()
        {
            Assert.IsTrue(CoinMagnet.Reached(new Vector2(0.4f, 0f), Vector2.zero));
            Assert.IsFalse(CoinMagnet.Reached(new Vector2(0.6f, 0f), Vector2.zero));
        }
    }

    public sealed class CoinPoolAndDropsTests
    {
        GameObject _prefabGo;

        [TearDown]
        public void TearDown()
        {
            if (_prefabGo != null) Object.DestroyImmediate(_prefabGo);
        }

        CoinPickup MakeCoin()
        {
            _prefabGo = new GameObject("Coin");
            return _prefabGo.AddComponent<CoinPickup>();
        }

        [Test]
        public void SpawnReusesAReleasedCoin()
        {
            var prefab = MakeCoin();
            var first = CoinPickupPool.Spawn(prefab, new Vector2(1f, 2f), Vector2.up);
            Assert.AreEqual(new Vector3(1f, 2f, 0f), first.transform.position);
            CoinPickupPool.Release(first);
            Assert.IsFalse(first.gameObject.activeSelf);
            int freeBefore = CoinPickupPool.FreeCount;
            var second = CoinPickupPool.Spawn(prefab, new Vector2(5f, 5f), Vector2.zero);
            Assert.AreSame(first, second);
            Assert.AreEqual(freeBefore - 1, CoinPickupPool.FreeCount);
            Assert.IsTrue(second.gameObject.activeSelf);
            Object.DestroyImmediate(first.gameObject);
            CoinPickupPool.Release(null); // tolerated
        }

        [Test]
        public void SpawnSkipsDestroyedPooledCoinsAndMissingPrefabs()
        {
            var prefab = MakeCoin();
            var doomed = CoinPickupPool.Spawn(prefab, Vector2.zero, Vector2.zero);
            CoinPickupPool.Release(doomed);
            Object.DestroyImmediate(doomed.gameObject);
            var fresh = CoinPickupPool.Spawn(prefab, Vector2.one, Vector2.zero);
            Assert.IsNotNull(fresh);
            Assert.IsNull(CoinPickupPool.Spawn(null, Vector2.zero, Vector2.zero));
            Object.DestroyImmediate(fresh.gameObject);
        }

        [Test]
        public void DropsSpawnOneCoinPerCoinAndNoneWithoutAPrefab()
        {
            var prefab = MakeCoin();
            var coinsBefore = Object.FindObjectsByType<CoinPickup>().Length;
            EnemyDrops.Spawn(new DropResult(4, false), Vector2.zero, prefab, null, new FakeRandom(0.5f));
            var coinsAfter = Object.FindObjectsByType<CoinPickup>();
            Assert.AreEqual(coinsBefore + 4, coinsAfter.Length);
            foreach (var coin in coinsAfter) if (coin != prefab) Object.DestroyImmediate(coin.gameObject);

            EnemyDrops.Spawn(new DropResult(3, true), Vector2.zero, null, null, new FakeRandom(0.5f)); // must not throw
        }
    }
}
