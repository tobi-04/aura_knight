using System.Collections;
using AuraKnight.Bosses;
using AuraKnight.Combat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AuraKnight.Tests.PlayMode.Bosses
{
    /// <summary>Boss projectiles and spikes are reused through <see cref="BossHazardPool"/>; a reused hazard is indistinguishable from a new one.</summary>
    public sealed class BossHazardPoolPlayModeTests
    {
        GameObject _room;
        BossBase _boss;

        IEnumerator Setup()
        {
            BossTestKit.Cleanup();
            yield return BossTestKit.EnsureCleanWorld();
            Time.timeScale = 1f;
            BossHazardPool.Clear(); // earlier fixtures may have parked hazards that died with their rooms
            _room = BossTestKit.SpawnRoom("RootTree");
            _boss = _room.GetComponentInChildren<BossArena>().Boss;
            yield return new WaitForFixedUpdate();
            yield return null;
        }

        [TearDown]
        public void TearDown() => BossTestKit.Cleanup();

        HazardSpec SpecAt(Vector2 position, float lifetime = 0.2f, bool harmless = false) => new HazardSpec
        {
            Position = position, Size = Vector2.one, Damage = 1, Telegraph = 0f, Lifetime = lifetime, Color = Color.red, Harmless = harmless,
        };

        [UnityTest]
        public IEnumerator AnExpiredHazardIsReusedAndComesBackClean()
        {
            yield return Setup();
            var origin = (Vector2)BossTestKit.RoomPoint(_room, 20f, 8f);
            var first = BossHazard.Spawn(_boss, SpecAt(origin));
            int hits = 0;
            first.Hit += _ => hits++;
            first.Suppressed = () => true;
            yield return BossTestKit.WaitUntil(() => !first.gameObject.activeSelf, 3f);
            Assert.IsFalse(first.gameObject.activeSelf, "the expired hazard is parked");
            Assert.AreEqual(1, BossHazardPool.FreeCount);

            var second = BossHazard.Spawn(_boss, SpecAt(origin + Vector2.right, lifetime: 5f));
            Assert.AreSame(first, second, "the parked object is rented");
            Assert.AreEqual(0, BossHazardPool.FreeCount);
            Assert.IsTrue(second.gameObject.activeSelf);
            Assert.IsNull(second.Suppressed, "the old suppression is gone");
            Assert.AreEqual(origin.x + 1f, second.transform.position.x, 1e-3f);
            Assert.AreSame(_boss, second.Owner);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(second.IsArmed, "telegraph 0: armed again from scratch");
            Assert.AreEqual(0, hits, "the old Hit subscriber is gone");
        }

        [UnityTest]
        public IEnumerator AReusedHazardStillHurtsLeo()
        {
            yield return Setup();
            var origin = (Vector2)BossTestKit.RoomPoint(_room, 20f, 8f);
            BossHazard.Spawn(_boss, SpecAt(origin, 0.1f));
            yield return BossTestKit.WaitUntil(() => BossHazardPool.FreeCount == 1, 3f);
            Assert.AreEqual(1, BossHazardPool.FreeCount);

            var leo = BossTestKit.FakeLeo(origin, out var health);
            var reused = BossHazard.Spawn(_boss, SpecAt(origin, 2f));
            int landed = 0;
            reused.Hit += report => { if (report.Outcome.DealtDamage()) landed++; };
            yield return BossTestKit.WaitUntil(() => health.Current < health.Max, 3f);
            Assert.Less(health.Current, health.Max, "a rented hazard damages the player");
            Assert.AreEqual(1, landed);
            Object.Destroy(leo);
        }

        [UnityTest]
        public IEnumerator ClearSpawnedParksDamagingHazardsAndDestroysMarkers()
        {
            yield return Setup();
            var origin = (Vector2)BossTestKit.RoomPoint(_room, 20f, 8f);
            var damaging = BossHazard.Spawn(_boss, SpecAt(origin, 30f));
            var marker = BossHazard.Spawn(_boss, SpecAt(origin, 30f, harmless: true));
            Assert.AreEqual(2, _boss.SpawnedCount);
            _boss.ClearSpawned();
            Assert.AreEqual(0, _boss.SpawnedCount);
            Assert.IsFalse(damaging.gameObject.activeSelf);
            Assert.AreEqual(1, BossHazardPool.FreeCount, "only the damaging one is pooled");
            yield return null;
            Assert.IsTrue(marker == null, "a marker is destroyed, never recycled behind an attack's handle");
        }

        [UnityTest]
        public IEnumerator AHazardRecycledByAnotherBossIsNotClearedByTheFirst()
        {
            yield return Setup();
            var origin = (Vector2)BossTestKit.RoomPoint(_room, 20f, 8f);
            var hazard = BossHazard.Spawn(_boss, SpecAt(origin, 0.1f));
            yield return BossTestKit.WaitUntil(() => !hazard.gameObject.activeSelf, 3f);
            var otherRoom = BossTestKit.SpawnRoom("GiantStoneSpider", 4000f);
            var other = otherRoom.GetComponentInChildren<BossArena>().Boss;
            var reused = BossHazard.Spawn(other, SpecAt(origin, 30f));
            Assert.AreSame(hazard, reused);
            _boss.ClearSpawned(); // the first boss still lists the object
            Assert.IsTrue(reused.gameObject.activeSelf, "owned by the second boss now");
            Assert.AreEqual(1, other.SpawnedCount);
            Object.Destroy(otherRoom);
        }
    }
}
