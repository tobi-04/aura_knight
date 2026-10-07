using System.Collections;
using AuraKnight.Combat;
using AuraKnight.Core;
using AuraKnight.Enemies;
using AuraKnight.World;
using AuraKnight.World.Pickups;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AuraKnight.Tests.PlayMode.Enemies
{
    /// <summary>Enemies inside the real Core flow: room containers, Room.Restart and the coin credit path.</summary>
    public sealed class EnemyWorldPlayModeTests : WorldPlayTestBase
    {
        [UnityTearDown]
        public IEnumerator RemoveEnemies()
        {
            foreach (var enemy in Object.FindObjectsByType<EnemyBase>()) Object.Destroy(enemy.gameObject);
            foreach (var coin in Object.FindObjectsByType<CoinPickup>(FindObjectsInactive.Include)) Object.Destroy(coin.gameObject);
            yield return null;
        }

        static Transform EnemiesContainer() => RoomManager.Instance.Current.transform.Find("Enemies");

        [UnityTest]
        public IEnumerator RoomRestartRevivesAndResetsRoomEnemies()
        {
            bool ok = false;
            yield return Enter(true, r => ok = r);
            Assert.IsTrue(ok);
            yield return SettlePhysics();
            var container = EnemiesContainer();
            Assert.IsNotNull(container, "start room has an Enemies container");
            Assert.IsTrue(container.gameObject.activeInHierarchy, "room is live");

            var spawn = PlayerPosition + new Vector2(14f, 1f); // beyond detect range, so Leo is never attacked mid-test
            var walker = EnemyTestKit.Spawn("BugThorn", spawn, container);
            var knight = EnemyTestKit.Spawn("NightKnight", spawn + new Vector2(2f, 0f), container);
            var walkerEnemy = walker.GetComponent<EnemyBase>();
            var knightEnemy = knight.GetComponent<EnemyBase>();

            EnemyTestKit.Strike(walker.transform.position, Vector2.right, 5, new Vector2(2f, 2f)); // dies
            knightEnemy.Health.TakeDamage(new DamageInfo(2, Team.Player, null, Vector2.right, 0f));
            knight.transform.position += new Vector3(3f, 2f, 0f);
            Assert.IsFalse(walkerEnemy.IsAlive);
            Assert.AreEqual(4, knightEnemy.Health.Current);

            RoomManager.Instance.Current.Restart();
            yield return null;

            Assert.IsTrue(walkerEnemy.IsAlive, "dead enemy is back");
            Assert.AreEqual(walkerEnemy.Stats.maxHp, walkerEnemy.Health.Current);
            Assert.AreEqual(6, knightEnemy.Health.Current, "damaged enemy is healed");
            Assert.AreEqual(EnemyState.Patrol, knightEnemy.State);
            AssertNear(spawn, walker.transform.position, "walker back at its spawn point", 0.6f);
            AssertNear(spawn + new Vector2(2f, 0f), knight.transform.position, "knight back at its spawn point", 0.6f);
            Assert.IsTrue(walkerEnemy.Hurtbox.gameObject.activeSelf);
            Assert.IsTrue(walkerEnemy.ContactHitbox.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator EnemiesStopWhenTheirRoomIsDeactivatedAndReturnFresh()
        {
            bool ok = false;
            yield return Enter(true, r => ok = r);
            Assert.IsTrue(ok);
            yield return SettlePhysics();
            var container = EnemiesContainer();
            var bug = EnemyTestKit.Spawn("BugThorn", PlayerPosition + new Vector2(14f, 1f), container);
            var enemy = bug.GetComponent<EnemyBase>();
            enemy.Health.TakeDamage(new DamageInfo(1, Team.Player, null, Vector2.right, 0f));

            RoomManager.Instance.Current.Deactivate();
            Assert.IsFalse(bug.activeInHierarchy, "no AI while the room is not current");
            RoomManager.Instance.Current.Activate();
            yield return null;
            Assert.AreEqual(2, enemy.Health.Current, "fresh on re-entry");
        }

        [UnityTest]
        public IEnumerator CoinCollectorCreditsTheGameStateAndAnnouncesIt()
        {
            bool ok = false;
            yield return Enter(true, r => ok = r);
            Assert.IsTrue(ok);
            int before = Manager.State.coins;
            int changedTo = -1, collected = 0;
            EventBus.Subscribe<CoinsChanged>(e => changedTo = e.Coins);
            EventBus.Subscribe<CoinsCollected>(e => collected += e.Amount);

            Assert.AreEqual(before + 7, CoinCollector.Collect(7));
            Assert.AreEqual(before + 7, Manager.State.coins);
            Assert.AreEqual(before + 7, changedTo);
            Assert.AreEqual(7, collected);
        }
    }
}
