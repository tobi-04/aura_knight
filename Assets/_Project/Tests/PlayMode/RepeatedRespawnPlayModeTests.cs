using System.Collections;
using AuraKnight.Combat;
using AuraKnight.Core;
using AuraKnight.Player;
using AuraKnight.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AuraKnight.Tests.PlayMode
{
    /// <summary>A respawn that finishes without ever yielding must not leave the service "busy": the second death respawns too.</summary>
    public sealed class RepeatedRespawnPlayModeTests : WorldPlayTestBase
    {
        [UnityTest]
        public IEnumerator DyingTwiceInAnAlreadyLoadedRegionRespawnsTwice()
        {
            bool ok = false;
            yield return Enter(true, r => ok = r);
            Assert.IsTrue(ok);
            yield return SettlePhysics();
            int respawned = 0;
            EventBus.Subscribe<PlayerRespawned>(_ => respawned++);
            var health = Player.GetComponent<Health>();
            var controller = Player.GetComponent<PlayerController>();

            for (int round = 1; round <= 2; round++)
            {
                health.TakeDamage(new DamageInfo(99, Team.Enemy));
                Assert.IsTrue(health.IsDead);
                yield return WaitUntil(() => respawned >= round, $"respawn #{round}", 20f);
                yield return SettlePhysics();
                Assert.IsFalse(CheckpointService.Instance.IsRespawning);
                Assert.AreEqual(PlayerStateId.Idle, controller.StateMachine.CurrentId, $"round {round}");
                AssertNear(AltarSpawn(GameState.StartAltarId), PlayerPosition, $"round {round} at the altar");
            }
            Assert.AreEqual(2, respawned);
        }

        [UnityTest]
        public IEnumerator LocalAltarPathRespawnsRepeatedly()
        {
            // Test scenes: no WorldEntry use, the altar is already in the scene.
            var altar = new GameObject("Altar");
            altar.AddComponent<BoxCollider2D>().isTrigger = true;
            var sun = altar.AddComponent<SunAltar>();
            typeof(SunAltar).GetField("altarId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(sun, GameState.StartAltarId);
            altar.SetActive(false);
            altar.SetActive(true); // re-run OnEnable so the id registers
            altar.transform.position = new Vector3(300f, 0f, 0f);
            var mover = new GameObject("Mover") { tag = WorldTags.Player };
            var service = new GameObject("Service").AddComponent<CheckpointService>();
            Destroy(WorldEntry.Instance); // the Core one would take over otherwise
            yield return null;
            Assert.IsNull(WorldEntry.Instance);
            int respawned = 0;
            EventBus.Subscribe<PlayerRespawned>(_ => respawned++);
            Manager.State.lastAltarId = GameState.StartAltarId;
            service.RegisterPlayer(mover.transform);
            // The Core service is the registered singleton; drive the duplicate-free one through the Core instance.
            var core = CheckpointService.Instance;
            core.RegisterPlayer(mover.transform);
            for (int round = 1; round <= 2; round++)
            {
                mover.transform.position = Vector3.zero;
                Assert.IsTrue(core.Respawn());
                yield return WaitUntil(() => respawned >= round, $"local respawn #{round}", 5f);
                Assert.IsFalse(core.IsRespawning);
                Assert.Less(Vector3.Distance(mover.transform.position, altar.transform.position), 0.1f);
            }
        }

        static void Destroy(Object o) => Object.Destroy(o);
    }
}
