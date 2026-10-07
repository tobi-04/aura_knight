using System.Collections;
using System.Linq;
using AuraKnight.Aura;
using AuraKnight.Combat;
using AuraKnight.Core;
using AuraKnight.World;
using AuraKnight.World.Hazards;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AuraKnight.Tests.PlayMode.Levels
{
    /// <summary>The hazards inside the real region scenes, run with the real clock, Hitboxes and Leo.</summary>
    public sealed class HazardPlayModeTests : LevelsPlayModeBase
    {
        [UnityTest, Timeout(120000)]
        public IEnumerator ACollapsingPlatformFallsAfterSixTenthsAndReturnsAfterThreeSeconds()
        {
            yield return StartAt("forest_altar_01");
            var platform = All<CollapsingPlatform>().First();
            Assert.IsTrue(platform.Body.activeSelf);
            float start = Time.realtimeSinceStartup;
            platform.Step();
            yield return WaitUntil(() => !platform.Body.activeSelf, "the platform collapses", 3f);
            Assert.That(Time.realtimeSinceStartup - start, Is.InRange(0.5f, 1.2f));
            float fallen = Time.realtimeSinceStartup;
            yield return WaitUntil(() => platform.Body.activeSelf, "the platform returns", 8f);
            Assert.That(Time.realtimeSinceStartup - fallen, Is.InRange(2.8f, 3.8f));
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator SpikesHurtLeoForOneHeart()
        {
            yield return StartAt("forest_altar_01");
            var health = Player.GetComponent<Health>();
            int before = health.Current;
            var spikes = All<Spikes>().First(s => !s.Lethal);
            Teleport(spikes.GetComponent<Collider2D>().bounds.center);
            yield return WaitUntil(() => health.Current < before, "the spikes hit", 3f);
            Assert.AreEqual(before - 1, health.Current);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator ABottomlessPitSendsLeoBackToTheAltar()
        {
            yield return StartAt("forest_altar_01");
            int respawned = 0;
            EventBus.Subscribe<PlayerRespawned>(_ => respawned++);
            var pit = All<Spikes>().First(s => s.Lethal);
            Teleport(pit.GetComponent<Collider2D>().bounds.center);
            yield return WaitUntil(() => respawned > 0, "death and respawn", 40f);
            AssertNear(AltarSpawn("forest_altar_01"), PlayerPosition, "back at the altar, not stuck in the pit");
            Assert.IsFalse(Player.GetComponent<Health>().IsDead);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator AStalactiteShakesFallsLandsAndHangsAgain()
        {
            yield return StartAt("cave_altar_01", "Wind");
            var stalactite = All<FallingStalactite>().First();
            Assert.AreEqual(StalactitePhase.Hanging, stalactite.Phase);
            float start = Time.realtimeSinceStartup;
            stalactite.Arm();
            Assert.AreEqual(StalactitePhase.Shaking, stalactite.Phase);
            yield return WaitUntil(() => stalactite.Phase == StalactitePhase.Falling, "it falls", 3f);
            Assert.GreaterOrEqual(Time.realtimeSinceStartup - start, 0.4f, "it shakes for half a second first");
            Assert.IsTrue(stalactite.GetComponentInChildren<Hitbox>().IsActive, "it hurts while it drops");
            yield return WaitUntil(() => stalactite.Phase == StalactitePhase.Resting, "it lands on the floor", 5f);
            Assert.IsFalse(stalactite.GetComponentInChildren<Hitbox>().IsActive);
            yield return WaitUntil(() => stalactite.Phase == StalactitePhase.Hanging, "it hangs again", 8f);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator AStalactiteFallsWhenLeoWalksUnderIt()
        {
            yield return StartAt("cave_altar_01", "Wind");
            var stalactite = All<FallingStalactite>().First(s => s.GetComponentInParent<Room>().RoomId == "cave_02");
            Teleport(stalactite.GetComponent<Collider2D>().bounds.center + Vector3.down * 3f);
            yield return WaitUntil(() => stalactite.Phase != StalactitePhase.Hanging, "the sensor under it reacts", 3f);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator APistonSlamsOnItsCycleAndHurtsOnlyWhileExtended()
        {
            yield return StartAt("city_altar_01", "Wind", "Fire");
            var piston = All<Piston>().First();
            var hitbox = piston.GetComponentInChildren<Hitbox>(true);
            var head = hitbox.transform;
            float home = head.localPosition.y;
            Assert.IsFalse(hitbox.IsActive, "retracted at first");
            yield return WaitUntil(() => hitbox.IsActive, "the slam", 6f);
            Assert.Less(head.localPosition.y, home - 1.5f, "the head is down while it hurts");
            yield return WaitUntil(() => !hitbox.IsActive, "the retract", 3f);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator AMovingSpikeFloorSlidesBackAndForth()
        {
            yield return StartAt("castle_altar_01", "Wind", "Fire", "Water");
            var floor = All<MovingSpikeFloor>().First();
            float x0 = floor.transform.localPosition.x;
            yield return Seconds(1.5f);
            Assert.Greater(Mathf.Abs(floor.transform.localPosition.x - x0), 1.5f, "it moves 2.5 units per second");
            Assert.LessOrEqual(Mathf.Abs(floor.transform.localPosition.x - x0), 6.5f, "within its travel");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator AcidBurnsUnlessTheWaterAuraIsWorn()
        {
            yield return StartAt("city_altar_01", "Fire", "Wind", "Water");
            var acid = All<AcidPool>().First();
            var hitbox = acid.GetComponentInChildren<Hitbox>(true);
            yield return WaitUntil(() => hitbox.IsActive, "acid is armed while Fire is worn", 3f);
            Assert.IsFalse(acid.IsHarmless);
            yield return Seconds(0.4f);
            Assert.IsTrue(AuraManager.Instance.TrySwitch(AuraId.Water));
            yield return WaitUntil(() => !hitbox.IsActive, "Water makes it harmless", 3f);
            Assert.IsTrue(acid.IsHarmless);
        }
    }
}
