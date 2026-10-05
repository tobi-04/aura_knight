using AuraKnight.Combat;
using AuraKnight.Player;
using AuraKnight.World.Pickups;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Combat
{
    public sealed class LightDropPickupTests
    {
        GameObject _playerGo, _pickupGo;

        [TearDown]
        public void TearDown()
        {
            if (_playerGo != null) Object.DestroyImmediate(_playerGo);
            if (_pickupGo != null) Object.DestroyImmediate(_pickupGo);
        }

        static GameObject NewPickupObject()
        {
            var go = new GameObject("Drop");
            go.AddComponent<BoxCollider2D>().isTrigger = true; // RequireComponent(Collider2D) is abstract, so tests add a concrete one
            return go;
        }

        PlayerStats MakeStats(int hearts)
        {
            _playerGo = new GameObject("Player");
            _playerGo.AddComponent<Health>();
            var stats = _playerGo.AddComponent<PlayerStats>();
            stats.Initialize(new PlayerStatsSeed(5, 100, 1));
            stats.Health.TakeDamage(new DamageInfo(5 - hearts, Team.Enemy));
            return stats;
        }

        [Test]
        public void DropChanceIsTenPercent()
        {
            Assert.AreEqual(0.10f, LightDropPickup.DropChance);
            Assert.IsTrue(LightDropPickup.ShouldDrop(0f));
            Assert.IsTrue(LightDropPickup.ShouldDrop(0.0999f));
            Assert.IsFalse(LightDropPickup.ShouldDrop(0.10f));
            Assert.IsFalse(LightDropPickup.ShouldDrop(0.99f));
        }

        [Test]
        public void CollectHealsExactlyOneHeart()
        {
            var stats = MakeStats(3);
            _pickupGo = NewPickupObject();
            var pickup = _pickupGo.AddComponent<LightDropPickup>();
            Assert.IsTrue(pickup.TryCollect(stats));
            Assert.AreEqual(4, stats.Health.Current);
        }

        [Test]
        public void CollectIsRefusedAtFullHearts()
        {
            var stats = MakeStats(5);
            _pickupGo = NewPickupObject();
            var pickup = _pickupGo.AddComponent<LightDropPickup>();
            Assert.IsFalse(pickup.TryCollect(stats));
            Assert.AreEqual(5, stats.Health.Current);
        }

        [Test]
        public void CollectIsRefusedForNullAndDeadPlayers()
        {
            _pickupGo = NewPickupObject();
            var pickup = _pickupGo.AddComponent<LightDropPickup>();
            Assert.IsFalse(pickup.TryCollect(null));
            var stats = MakeStats(0);
            Assert.IsFalse(pickup.TryCollect(stats));
        }
    }
}
