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
    /// <summary>Round-2 review items: save overwrite on a new game, loading mode, clean re-entry, restart of the current room.</summary>
    public sealed class EntryAndRoomRestartPlayModeTests : WorldPlayTestBase
    {
        [UnityTest]
        public IEnumerator NewGameFirstAltarOverwritesAnOldSave()
        {
            var old = GameState.NewGame();
            old.coins = 777;
            old.lastAltarId = "forest_altar_01";
            Assert.IsTrue(new SaveSystem(new FileSaveStorage(SavePath)).Save(old));

            bool ok = false;
            yield return Enter(true, r => ok = r); // new game: the old save stays on disk until the first altar
            Assert.IsTrue(ok);
            yield return SettlePhysics(); // the hub altar trigger fires
            Assert.IsTrue(new SaveSystem(new FileSaveStorage(SavePath)).TryLoad(out var onDisk));
            Assert.AreEqual(0, onDisk.coins, "the new game replaced the old save");
            Assert.AreEqual(GameState.StartAltarId, onDisk.lastAltarId);
        }

        [UnityTest]
        public IEnumerator ModeIsLoadingWhileTheWorldIsBeingEntered()
        {
            Assert.IsTrue(Entry.StartNewGame());
            Assert.AreEqual(GameMode.Loading, Manager.Mode);
            Assert.IsFalse(GameManager.ModeAllowsControl);
            yield return WaitUntil(() => !Entry.IsBusy, "entry finished");
            Assert.AreEqual(GameMode.Playing, Manager.Mode);
        }

        [UnityTest]
        public IEnumerator ReEnteringWithADeadPlayerStartsClean()
        {
            bool ok = false;
            yield return Enter(true, r => ok = r);
            Assert.IsTrue(ok);
            yield return SettlePhysics();
            var health = Player.GetComponent<Health>();
            health.TakeDamage(new DamageInfo(99, Team.Enemy));
            var controller = Player.GetComponent<PlayerController>();
            Assert.AreEqual(PlayerStateId.Dead, controller.StateMachine.CurrentId);

            Assert.IsTrue(Manager.Save());
            ok = false;
            yield return Enter(false, r => ok = r);
            Assert.IsTrue(ok);
            Assert.IsFalse(health.IsDead);
            Assert.AreEqual(PlayerStateId.Idle, controller.StateMachine.CurrentId);
            Assert.IsFalse(controller.ExternalInvulnerable);
        }

        [UnityTest]
        public IEnumerator RespawnIntoTheCurrentRoomRestartsItAndAnnouncesIt()
        {
            bool ok = false;
            yield return Enter(true, r => ok = r);
            Assert.IsTrue(ok);
            yield return SettlePhysics();
            int entered = 0;
            EventBus.Subscribe<RoomEntered>(_ => entered++);
            var enemies = RoomManager.Instance.Current.transform.Find("Enemies").gameObject;
            int enables = 0;
            var probe = enemies.AddComponent<EnableCounter>();
            probe.OnEnabled = () => enables++;
            probe.enabled = false;
            probe.enabled = true;
            enables = 0;

            Player.GetComponent<Health>().TakeDamage(new DamageInfo(99, Team.Enemy));
            yield return WaitUntil(() => !Player.GetComponent<Health>().IsDead, "respawn", 20f);

            Assert.GreaterOrEqual(entered, 1, "RoomEntered is published even though Leo respawned in the same room");
            Assert.GreaterOrEqual(enables, 1, "the room's enemies were switched off and on, so their OnEnable resets ran");
            Assert.IsTrue(enemies.activeSelf);
        }

        sealed class EnableCounter : MonoBehaviour
        {
            public System.Action OnEnabled;
            void OnEnable() => OnEnabled?.Invoke();
        }
    }
}
