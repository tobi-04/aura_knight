using System.Collections;
using AuraKnight.Aura;
using AuraKnight.Combat;
using AuraKnight.Core;
using AuraKnight.Player;
using AuraKnight.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AuraKnight.Tests.PlayMode
{
    /// <summary>End-to-end world flow in the real Core and region scenes: new game, continue-from-save, cross-region respawn, leaks.</summary>
    public sealed class WorldFlowPlayModeTests : WorldPlayTestBase
    {
        static GameState SavedAt(string altarId, int hearts = 7)
        {
            var state = GameState.NewGame();
            state.lastAltarId = altarId;
            state.maxHearts = hearts;
            state.coins = 12;
            state.unlockedAuras.Add("Wind");
            state.currentAura = "Wind";
            return state;
        }

        [UnityTest]
        public IEnumerator NewGameStartsAtTheHubAltarInPlayingMode()
        {
            bool ok = false;
            yield return Enter(true, r => ok = r);
            Assert.IsTrue(ok);
            Assert.AreEqual(GameMode.Playing, Manager.Mode);
            Assert.IsTrue(SceneLoader.IsLoaded("Region_Hub"));
            AssertNear(AltarSpawn(GameState.StartAltarId), PlayerPosition, "player at the hub altar");
            Assert.AreEqual("hub_01", RoomManager.Instance.Current.RoomId);
        }

        [UnityTest]
        public IEnumerator ContinueFromSavePlacesLeoAtTheLastAltarInItsRegion()
        {
            Assert.IsTrue(new SaveSystem(new FileSaveStorage(SavePath)).Save(SavedAt("forest_altar_01")));
            bool ok = false;
            yield return Enter(false, r => ok = r);

            Assert.IsTrue(ok, "continue succeeded");
            Assert.AreEqual(GameMode.Playing, Manager.Mode);
            Assert.AreEqual("forest_altar_01", Manager.State.lastAltarId);
            Assert.IsTrue(SceneLoader.IsLoaded("Region_Forest"), "the region of lastAltarId is loaded next to Core");
            Assert.IsTrue(SceneLoader.IsLoaded("Core"));
            AssertNear(AltarSpawn("forest_altar_01"), PlayerPosition, "player at the saved altar");
            Assert.AreEqual("forest_01", RoomManager.Instance.Current.RoomId);
            // Systems that read the state at startup took the loaded values, not the defaults.
            Assert.AreEqual(7, Player.GetComponent<PlayerStats>().Health.Max);
            Assert.AreEqual(AuraId.Wind, AuraManager.Instance.Current);
            Assert.IsFalse(SceneLoader.IsLoaded("Region_Cave"), "a region that is not a neighbour of the current room is not loaded");
            Assert.IsFalse(SceneLoader.IsLoaded("Region_Castle"));
        }

        [UnityTest]
        public IEnumerator ContinueWithoutASaveIsRefusedAndNothingLoads()
        {
            Assert.IsFalse(Entry.Continue());
            Assert.AreEqual(GameMode.Menu, Manager.Mode);
            Assert.IsNull(Player);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ContinueWithAnUnknownAltarFallsBackToTheHub()
        {
            Assert.IsTrue(new SaveSystem(new FileSaveStorage(SavePath)).Save(SavedAt("moon_altar_99")));
            bool ok = false;
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true; // unknown altar warnings
            yield return Enter(false, r => ok = r);
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            Assert.IsTrue(ok);
            Assert.AreEqual(GameState.StartAltarId, Manager.State.lastAltarId);
            AssertNear(AltarSpawn(GameState.StartAltarId), PlayerPosition, "fell back to the hub altar");
        }

        [UnityTest]
        public IEnumerator ContinuingAgainRefreshesSystemsThatAlreadyExist()
        {
            bool ok = false;
            yield return Enter(true, r => ok = r);
            Assert.IsTrue(ok);
            var stats = Player.GetComponent<PlayerStats>();
            Assert.AreEqual(5, stats.Health.Max);

            Assert.IsTrue(new SaveSystem(new FileSaveStorage(SavePath)).Save(SavedAt(GameState.StartAltarId, hearts: 9)));
            Assert.IsTrue(Manager.Continue()); // publishes GameStateLoaded
            Assert.AreEqual(9, stats.Health.Max, "PlayerStats re-read the new state");
            Assert.AreEqual(AuraId.Wind, AuraManager.Instance.Current, "AuraManager re-read the new state");
        }

        [UnityTest]
        public IEnumerator DeathInOneRegionRespawnsAtTheAltarOfAnotherRegion()
        {
            bool ok = false;
            yield return Enter(true, r => ok = r);
            Assert.IsTrue(ok);
            yield return SettlePhysics();
            EventBus.Publish(new CheckpointReached("forest_altar_01")); // as if Leo had rested there
            Assert.AreEqual("forest_altar_01", Manager.State.lastAltarId);
            int respawned = 0;
            EventBus.Subscribe<PlayerRespawned>(_ => respawned++);

            var health = Player.GetComponent<Health>();
            health.TakeDamage(new DamageInfo(99, Team.Enemy));
            Assert.IsTrue(health.IsDead);
            yield return WaitUntil(() => respawned > 0, "respawn", 40f);

            Assert.AreEqual(1, respawned);
            Assert.IsTrue(SceneLoader.IsLoaded("Region_Forest"));
            AssertNear(AltarSpawn("forest_altar_01"), PlayerPosition, "Leo ends at the altar of the newly loaded region");
            Assert.AreEqual("forest_01", RoomManager.Instance.Current.RoomId);
            // The warp went through RoomManager, so the camera jumped with Leo instead of gliding 100 units across the map.
            yield return null;
            yield return null;
            Assert.Less(Mathf.Abs(Camera.main.transform.position.x - PlayerPosition.x), 25f, "camera follows the warp");
            Assert.IsFalse(health.IsDead);
            Assert.AreEqual(health.Max, health.Current);
            Assert.AreNotEqual(PlayerStateId.Dead, Player.GetComponent<PlayerController>().StateMachine.CurrentId);
            // forest_01 is a gateway room: it keeps the hub preloaded for the way back, so the hub stays; unrelated regions never load.
            Assert.IsFalse(SceneLoader.IsLoaded("Region_Cave"));
            Assert.IsFalse(SceneLoader.IsLoaded("Region_City"));
        }

        [UnityTest]
        public IEnumerator RespawnWithAnUnresolvableAltarFallsBackToTheHubStartAltar()
        {
            Assert.IsTrue(new SaveSystem(new FileSaveStorage(SavePath)).Save(SavedAt("forest_altar_01")));
            bool ok = false;
            yield return Enter(false, r => ok = r);
            Assert.IsTrue(ok);
            yield return SettlePhysics();
            Manager.State.lastAltarId = "moon_altar_99";
            int respawned = 0;
            EventBus.Subscribe<PlayerRespawned>(_ => respawned++);
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            Player.GetComponent<Health>().TakeDamage(new DamageInfo(99, Team.Enemy));
            yield return WaitUntil(() => respawned > 0, "respawn at the hub", 40f);
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;

            AssertNear(AltarSpawn(GameState.StartAltarId), PlayerPosition, "hub start altar");
            Assert.AreEqual(GameState.StartAltarId, Manager.State.lastAltarId);
        }

        [UnityTest]
        public IEnumerator LoadingAndUnloadingARegionLeavesNoEventBusSubscribers()
        {
            int baseline = EventBus.SubscriberCount;
            Assert.Greater(baseline, 0, "Core's managers are subscribed");

            yield return SceneManager.LoadSceneAsync("Region_Forest", LoadSceneMode.Additive);
            var forest = SceneManager.GetSceneByName("Region_Forest");
            var content = new GameObject("RuntimeRegionContent");
            content.AddComponent<BoxCollider2D>().isTrigger = true;
            content.AddComponent<BurnableGate>();
            content.AddComponent<ExtinguishableGate>();
            content.AddComponent<WindLiftZone>();
            content.AddComponent<HeatVent>();
            content.AddComponent<Shortcut>();
            SceneManager.MoveGameObjectToScene(content, forest);
            yield return null;
            Assert.Greater(EventBus.SubscriberCount, baseline, "the region's components subscribed while loaded");

            yield return SceneManager.UnloadSceneAsync(forest);
            yield return null;
            Assert.AreEqual(baseline, EventBus.SubscriberCount, "unloading the region unsubscribed everything it subscribed");
        }

        [UnityTest]
        public IEnumerator DuplicateManagersNeverSubscribe()
        {
            int baseline = EventBus.SubscriberCount;
            var duplicate = new GameObject("DuplicateManagers");
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            duplicate.AddComponent<GameManager>();
            duplicate.AddComponent<CheckpointService>();
            duplicate.AddComponent<RoomManager>();
            yield return null;
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            Assert.AreEqual(baseline, EventBus.SubscriberCount, "a duplicate must not subscribe in OnEnable");
            Assert.IsNotNull(GameManager.Instance);
            Assert.AreNotSame(duplicate.GetComponent<GameManager>(), GameManager.Instance);
            Object.Destroy(duplicate);
        }
    }
}
