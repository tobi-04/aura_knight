using System.Collections;
using AuraKnight.Aura;
using AuraKnight.Bosses;
using AuraKnight.Combat;
using AuraKnight.Core;
using AuraKnight.Player;
using AuraKnight.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AuraKnight.Tests.PlayMode.Bosses
{
    /// <summary>Boss progress inside the real Core scene: the reward and the defeat are saved, and a defeated boss stays down after Continue.</summary>
    public sealed class BossWorldPlayModeTests : WorldPlayTestBase
    {
        GameObject _room;

        [UnityTearDown]
        public IEnumerator DestroyRoom()
        {
            if (_room != null) Object.Destroy(_room);
            yield return null;
        }

        IEnumerator EnterArena(GameObject room)
        {
            var player = Player.GetComponent<PlayerController>();
            player.TeleportTo(BossTestKit.RoomPoint(room, 15f, 2.4f), false);
            var arena = room.GetComponentInChildren<BossArena>();
            yield return WaitUntil(() => arena.State != BossArena.ArenaState.Idle || arena.Boss.State != BossState.Dormant, "arena trigger");
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator VictoryUnlocksTheAuraAndBothArePersistedToDisk()
        {
            bool entered = false;
            yield return Enter(true, ok => entered = ok);
            Assert.IsTrue(entered);
            _room = BossTestKit.SpawnRoom("RootTree");
            var arena = _room.GetComponentInChildren<BossArena>();
            yield return null;
            yield return EnterArena(_room);
            Assert.AreEqual(BossArena.ArenaState.Fighting, arena.State);

            BossTestKit.Damage(arena.Boss, 999);

            Assert.IsTrue(AuraManager.Instance.IsUnlocked(AuraId.Wind));
            Assert.Contains("RootTree", Manager.State.defeatedBosses);
            Assert.Contains("Wind", Manager.State.unlockedAuras);
            Assert.IsTrue(new SaveSystem(new FileSaveStorage(SavePath)).TryLoad(out var onDisk), "the victory autosaved");
            Assert.Contains("RootTree", onDisk.defeatedBosses);
            Assert.Contains("Wind", onDisk.unlockedAuras, "the reward is already in the file BossDefeated triggered");
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator ADefeatedBossIsNotRespawnedAfterContinue()
        {
            bool entered = false;
            yield return Enter(true, ok => entered = ok);
            Assert.IsTrue(entered);
            _room = BossTestKit.SpawnRoom("RootTree");
            var first = _room.GetComponentInChildren<BossArena>();
            yield return null;
            yield return EnterArena(_room);
            BossTestKit.Damage(first.Boss, 999);
            Object.Destroy(_room);
            yield return null;

            Assert.IsTrue(Manager.Continue(), "continue loads the save the victory wrote");
            yield return null;
            _room = BossTestKit.SpawnRoom("RootTree");
            var arena = _room.GetComponentInChildren<BossArena>();
            yield return null;

            Assert.AreEqual(BossArena.ArenaState.Defeated, arena.State);
            Assert.IsFalse(arena.Boss.IsPresent, "the boss is not respawned");
            Assert.IsFalse(arena.DoorsClosed, "doors stay open");
            Assert.IsFalse(arena.Boss.Hurtbox.gameObject.activeSelf);
            var started = false;
            EventBus.Subscribe<AuraKnight.UI.BossEncounterStarted>(e => started = true);
            Player.GetComponent<PlayerController>().TeleportTo(BossTestKit.RoomPoint(_room, 15f, 2.4f), false);
            float until = Time.realtimeSinceStartup + 0.6f;
            while (Time.realtimeSinceStartup < until) yield return null;
            Assert.IsFalse(started);
            Assert.AreEqual(BossArena.ArenaState.Defeated, arena.State);
            Assert.AreEqual(BossState.Dormant, arena.Boss.State);
        }
    }
}
