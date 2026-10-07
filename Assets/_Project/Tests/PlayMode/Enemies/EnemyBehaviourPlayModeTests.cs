using System.Collections;
using AuraKnight.Combat;
using AuraKnight.Core;
using AuraKnight.Enemies;
using AuraKnight.Enemies.Modifiers;
using AuraKnight.World.Pickups;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AuraKnight.Tests.PlayMode.Enemies
{
    /// <summary>Real-physics behaviour of the variant prefabs in a scene-less arena (prefabs from Prefabs/Enemies).</summary>
    public sealed class EnemyBehaviourPlayModeTests
    {
        Transform _arena;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return EnemyTestKit.UnloadWorldScenes(); // world fixtures leave Core and Region_* loaded around the origin
            EnemyTestKit.Cleanup();
            yield return null;
            _arena = EnemyTestKit.Arena();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            EnemyTestKit.Cleanup();
            yield return null;
        }

        static void SetPatrol(GameObject enemy, float left, float right)
        {
#if UNITY_EDITOR
            var so = new SerializedObject(enemy.GetComponent<WalkerEnemy>());
            so.FindProperty("patrolLeft").floatValue = left;
            so.FindProperty("patrolRight").floatValue = right;
            so.ApplyModifiedPropertiesWithoutUndo();
            enemy.GetComponent<EnemyBase>().ResetEnemy(); // bounds are computed on reset
#endif
        }

        [UnityTest]
        public IEnumerator WalkerTurnsAtALedgeInsteadOfWalkingOff()
        {
            // 6-tile platform whose floor ends in a pit; patrol bounds are far wider than the platform, so only the ledge probe can turn it.
            Object.Destroy(_arena.Find("Floor").gameObject);
            EnemyTestKit.Block(_arena, "Platform", -3f, -1f, 3f, 0f);
            var bug = EnemyTestKit.Spawn("BugThorn", new Vector2(0f, 0.5f));
            SetPatrol(bug, 20f, 20f);
            var enemy = bug.GetComponent<WalkerEnemy>();
            bool sawLeft = false, sawRight = false;
            float minY = float.MaxValue, maxAbsX = 0f;
            for (float t = 0f; t < 7f; t += Time.deltaTime)
            {
                yield return null;
                if (enemy.Facing > 0) sawRight = true; else sawLeft = true;
                minY = Mathf.Min(minY, bug.transform.position.y);
                maxAbsX = Mathf.Max(maxAbsX, Mathf.Abs(bug.transform.position.x));
            }
            Assert.IsTrue(sawLeft && sawRight, "turned around at least once");
            Assert.Greater(minY, 0f, "never fell off the platform");
            Assert.Less(maxAbsX, 3.2f, "stayed on the platform");
        }

        [UnityTest]
        public IEnumerator WalkerTurnsAtAWall()
        {
            EnemyTestKit.Block(_arena, "Wall", 3f, 0f, 4f, 3f);
            var bug = EnemyTestKit.Spawn("BugThorn", new Vector2(0f, 0.5f));
            SetPatrol(bug, 20f, 20f);
            var enemy = bug.GetComponent<WalkerEnemy>();
            bool turned = false;
            for (float t = 0f; t < 5f && !turned; t += Time.deltaTime)
            {
                yield return null;
                turned = enemy.Facing < 0;
            }
            Assert.IsTrue(turned, "turned away from the wall");
            Assert.Less(bug.transform.position.x, 3.0f, "never entered the wall");
        }

        [UnityTest]
        public IEnumerator WalkerChargesAPlayerInRange()
        {
            var bug = EnemyTestKit.Spawn("BugThorn", new Vector2(0f, 0.5f));
            var enemy = bug.GetComponent<WalkerEnemy>();
            EnemyTestKit.FakePlayer(new Vector2(5f, 0.5f), withHurtbox: false);
            yield return EnemyTestKit.Seconds(1.2f);
            Assert.AreEqual(1, enemy.Facing);
            Assert.Greater(bug.transform.position.x, 1.5f, "moved toward Leo faster than patrol speed allows");
            Assert.AreNotEqual(EnemyState.Patrol, enemy.State);
        }

        [UnityTest]
        public IEnumerator SwordKillsBugThornAndItsCoinsDropAndAreCollected()
        {
            var bug = EnemyTestKit.Spawn("BugThorn", new Vector2(0f, 0.4f));
            var enemy = bug.GetComponent<EnemyBase>();
            enemy.Random = new EnemyTestKit.FixedRandom(0.05f, maxRange: true); // 5 coins and a Light Drop
            yield return null;
            Assert.AreEqual(2, enemy.Health.Current);

            Assert.AreEqual(HitOutcome.Damaged, EnemyTestKit.Strike(bug.transform.position, Vector2.right, 1, new Vector2(2f, 2f)));
            Assert.AreEqual(EnemyState.Hurt, enemy.State);
            Assert.AreEqual(HitOutcome.Killed, EnemyTestKit.Strike(bug.transform.position, Vector2.right, 1, new Vector2(2f, 2f)));
            Assert.AreEqual(EnemyState.Dead, enemy.State);
            Assert.IsFalse(enemy.IsAlive);

            var coins = Object.FindObjectsByType<CoinPickup>();
            Assert.AreEqual(5, coins.Length, "coins dropped");
            Assert.AreEqual(1, Object.FindObjectsByType<LightDropPickup>().Length, "10% light drop rolled");
            Assert.IsFalse(enemy.Hurtbox.gameObject.activeSelf, "dead enemies cannot be hit");
            Assert.IsFalse(enemy.ContactHitbox.gameObject.activeSelf, "dead enemies cannot hurt");
            Assert.AreEqual(HitOutcome.Ignored, enemy.Hurtbox.Receive(new DamageInfo(1, Team.Player)));

            int collected = 0;
            EventBus.Subscribe<CoinsCollected>(e => collected += e.Amount);
            EnemyTestKit.FakePlayer(new Vector2(0f, 0.5f), withHurtbox: false);
            yield return EnemyTestKit.Seconds(2f);
            Assert.AreEqual(5, collected, "every dropped coin is pulled in and credited once");
        }

        [UnityTest]
        public IEnumerator FrontShieldBlocksTheFrontButNotTheBackOrTheTop()
        {
            var knight = EnemyTestKit.Spawn("NightKnight", new Vector2(0f, 0.8f));
            var enemy = knight.GetComponent<EnemyBase>();
            Assert.AreEqual(1, enemy.Facing, "faces right after spawning");
            int blocks = 0;
            knight.GetComponent<FrontShield>().Blocked += () => blocks++;
            var size = new Vector2(2f, 2f);

            Assert.AreEqual(HitOutcome.Absorbed, EnemyTestKit.Strike(knight.transform.position, Vector2.left, 1, size), "attacker in front");
            Assert.AreEqual(6, enemy.Health.Current);
            Assert.AreEqual(1, blocks);
            Assert.AreEqual(HitOutcome.Absorbed, enemy.Hurtbox.Receive(new DamageInfo(1, Team.Player, null, Vector2.left)));
            Assert.AreEqual(6, enemy.Health.Current);

            Assert.AreEqual(HitOutcome.Damaged, EnemyTestKit.Strike(knight.transform.position, Vector2.right, 1, size), "attacker behind");
            Assert.AreEqual(5, enemy.Health.Current);
            Assert.AreEqual(HitOutcome.Damaged, enemy.Hurtbox.Receive(new DamageInfo(1, Team.Player, null, Vector2.down)), "pogo from above");
            Assert.AreEqual(4, enemy.Health.Current);
            yield return null;
        }

        [UnityTest]
        public IEnumerator HopperHopsTowardLeoInAnArcAboutEveryHopInterval()
        {
            var shroom = EnemyTestKit.Spawn("PoisonShroom", new Vector2(0f, 0.5f));
            EnemyTestKit.FakePlayer(new Vector2(12f, 0.5f), withHurtbox: false);
            yield return EnemyTestKit.Seconds(0.3f);
            Assert.AreEqual(0f, shroom.transform.position.x, 0.2f, "waits before the first hop");
            yield return EnemyTestKit.Seconds(0.2f);
            var player = GameObject.FindGameObjectWithTag("Player");
            player.transform.position = new Vector2(5f, 0.5f); // inside the 6-tile range
            float apex = 0f;
            bool landedAfterApex = false;
            for (float t = 0f; t < 3.6f; t += Time.deltaTime)
            {
                yield return null;
                float y = shroom.transform.position.y;
                apex = Mathf.Max(apex, y);
                landedAfterApex |= apex > 1.2f && y < 0.8f;
            }
            Assert.Greater(apex, 1.2f, "an arc, not a slide");
            Assert.IsTrue(landedAfterApex, "landed again");
            Assert.Greater(shroom.transform.position.x, 2f, "moved toward Leo");
        }

        [UnityTest]
        public IEnumerator BatDivesAtLeoThenReturnsToItsHoverPoint()
        {
            var bat = EnemyTestKit.Spawn("Bat", new Vector2(0f, 5f));
            var enemy = bat.GetComponent<EnemyBase>();
            var leo = EnemyTestKit.FakePlayer(new Vector2(3f, 0.5f), withHurtbox: false);
            bool dived = false;
            float lowest = 99f;
            for (float t = 0f; t < 9f && !(dived && enemy.State == EnemyState.Patrol); t += Time.deltaTime)
            {
                yield return null;
                dived |= enemy.State == EnemyState.Attack;
                if (enemy.State == EnemyState.Cooldown) leo.transform.position = new Vector2(100f, 0.5f); // Leo leaves: no second dive
                lowest = Mathf.Min(lowest, bat.transform.position.y);
            }
            Assert.IsTrue(dived);
            Assert.AreEqual(EnemyState.Patrol, enemy.State, "settled back into hovering");
            Assert.Less(lowest, 2.5f, "swooped down toward Leo");
            Assert.AreEqual(5f, bat.transform.position.y, 1f, "back up at the hover height");
        }

        [UnityTest]
        public IEnumerator GhostPassesThroughAWallThatStopsTheBat()
        {
            EnemyTestKit.Block(_arena, "DiveWall", 1.5f, 0f, 2.5f, 12f);
            var ghost = EnemyTestKit.Spawn("Ghost", new Vector2(0f, 5f));
            var bat = EnemyTestKit.Spawn("Bat", new Vector2(0f, 5f));
            EnemyTestKit.FakePlayer(new Vector2(4f, 2f), withHurtbox: false);
            float ghostMax = -99f, batMax = -99f;
            for (float t = 0f; t < 4f; t += Time.deltaTime)
            {
                yield return null;
                ghostMax = Mathf.Max(ghostMax, ghost.transform.position.x);
                batMax = Mathf.Max(batMax, bat.transform.position.x);
            }
            Assert.IsFalse(ghost.GetComponent<Collider2D>().enabled, "the ghost has no solid body");
            Assert.Greater(ghostMax, 3f, "ghost crossed the wall");
            Assert.Less(batMax, 1.6f, "bat was stopped by it");
        }

        [UnityTest]
        public IEnumerator BatHealsOneWhenItsContactHitLands()
        {
            var bat = EnemyTestKit.Spawn("Bat", new Vector2(0f, 3f));
            var enemy = bat.GetComponent<EnemyBase>();
            enemy.Health.TakeDamage(new DamageInfo(1, Team.Player, null, Vector2.zero, 0f)); // no knockback: stay on Leo
            Assert.AreEqual(1, enemy.Health.Current);
            var leo = EnemyTestKit.FakePlayer(new Vector2(0f, 3f)); // overlaps the bat's contact hitbox
            yield return EnemyTestKit.Seconds(0.3f);
            Assert.AreEqual(4, leo.Current, "Leo lost a heart");
            Assert.AreEqual(2, enemy.Health.Current, "the bat healed one point");
        }

        [UnityTest]
        public IEnumerator ScrapZapperTelegraphsThenZapsTheRingEveryThreeSeconds()
        {
            var zapper = EnemyTestKit.Spawn("ScrapZapper", new Vector2(0f, 0.5f));
            var enemy = zapper.GetComponent<EnemyBase>();
            var leo = EnemyTestKit.FakePlayer(new Vector2(1.6f, 0.5f)); // inside the 2-tile ring, outside the body
            bool telegraphed = false;
            float firstHit = -1f, secondHit = -1f;
            int lastHearts = leo.Current;
            float start = Time.time;
            while (Time.time - start < 6.6f)
            {
                yield return null;
                telegraphed |= enemy.State == EnemyState.Detect;
                if (leo.Current == lastHearts) continue;
                lastHearts = leo.Current;
                if (firstHit < 0f) firstHit = Time.time - start; else if (secondHit < 0f) secondHit = Time.time - start;
            }
            Assert.IsTrue(telegraphed, "warning phase seen before the zap");
            Assert.That(firstHit, Is.InRange(2.5f, 3.4f), "first zap about 3 s in");
            Assert.That(secondHit - firstHit, Is.InRange(2.7f, 3.3f), "zaps repeat every 3 s");
            Assert.AreEqual(3, leo.Current);
        }

        [UnityTest]
        public IEnumerator StoneSpiderFollowsItsWaypointsAndIsNotKnockedOffThem()
        {
            var spider = EnemyTestKit.Spawn("StoneSpider", new Vector2(0f, 5f));
            var enemy = spider.GetComponent<EnemyBase>();
            yield return EnemyTestKit.Seconds(1f);
            float x = spider.transform.position.x;
            Assert.Greater(x, 1f, "crawling along the first leg");
            Assert.AreEqual(5f, spider.transform.position.y, 0.05f, "stays on the path");
            enemy.Hurtbox.Receive(new DamageInfo(1, Team.Player, null, Vector2.right, 3f));
            yield return null;
            Assert.AreEqual(2, enemy.Health.Current);
            Assert.AreNotEqual(EnemyState.Hurt, enemy.State, "crawlers only flash");
            Assert.AreEqual(5f, spider.transform.position.y, 0.05f);
            yield return EnemyTestKit.Seconds(5f);
            Assert.That(spider.transform.position.x, Is.InRange(-0.1f, 4.1f), "ping-pongs between the waypoints");
        }

        [UnityTest]
        public IEnumerator WalkerKnockbackPushesItAwayFromTheAttacker()
        {
            var bug = EnemyTestKit.Spawn("BugThorn", new Vector2(0f, 0.4f));
            SetPatrol(bug, 0f, 0f);
            yield return null;
            float before = bug.transform.position.x;
            bug.GetComponent<EnemyBase>().Hurtbox.Receive(new DamageInfo(1, Team.Player, null, Vector2.right, 3f));
            yield return EnemyTestKit.Seconds(0.3f);
            Assert.Greater(bug.transform.position.x - before, 1.5f);
        }

        [UnityTest]
        public IEnumerator AnEnemyThatFallsFarBelowItsSpawnIsSentHome()
        {
            Object.Destroy(_arena.Find("Floor").gameObject);
            var bug = EnemyTestKit.Spawn("BugThorn", new Vector2(0f, 0.4f));
            var enemy = bug.GetComponent<EnemyBase>();
            for (float t = 0f; t < 4f; t += Time.deltaTime)
            {
                yield return null;
                Assert.Greater(bug.transform.position.y, -EnemyBase.FallResetDepth - 8f, "never lost below the map");
            }
            Assert.IsTrue(enemy.IsAlive);
        }

        [UnityTest]
        public IEnumerator ResetRestoresHealthPositionAndStateAfterDeath()
        {
            var bug = EnemyTestKit.Spawn("BugThorn", new Vector2(4f, 0.4f));
            var enemy = bug.GetComponent<EnemyBase>();
            EnemyTestKit.Strike(bug.transform.position, Vector2.right, 5, new Vector2(2f, 2f));
            Assert.IsFalse(enemy.IsAlive);
            bug.transform.position = new Vector3(-9f, 3f, 0f);
            bug.SetActive(false);
            bug.SetActive(true); // what Room.Restart does to the container
            yield return null;
            Assert.IsTrue(enemy.IsAlive);
            Assert.AreEqual(2, enemy.Health.Current);
            Assert.AreEqual(4f, bug.transform.position.x, 0.3f);
            Assert.AreEqual(EnemyState.Patrol, enemy.State);
            Assert.IsTrue(enemy.Hurtbox.gameObject.activeSelf);
            Assert.IsTrue(enemy.ContactHitbox.gameObject.activeSelf);
            Assert.IsTrue(bug.GetComponent<Collider2D>().enabled);
        }
    }
}
