using System.Collections;
using AuraKnight.Aura;
using AuraKnight.Bosses;
using AuraKnight.Combat;
using AuraKnight.Core;
using AuraKnight.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AuraKnight.Tests.PlayMode.Bosses
{
    /// <summary>Boss effects on the real Player prefab: web slow versus the Aura binder, steam versus heat immunity, reward unlock order.</summary>
    public sealed class BossPlayerPlayModeTests
    {
        GameObject _room, _player;
        BossArena _arena;
        BossBase _boss;
        PlayerController _controller;
        PlayerAuraBinder _binder;
        Health _health;

        IEnumerator Setup(string bossName)
        {
            BossTestKit.Cleanup();
            yield return BossTestKit.EnsureCleanWorld();
            Time.timeScale = 1f;
            _room = BossTestKit.SpawnRoom(bossName);
            _arena = _room.GetComponentInChildren<BossArena>();
            _boss = _arena.Boss;
#if UNITY_EDITOR
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/Player.prefab");
            _player = Object.Instantiate(prefab, BossTestKit.RoomPoint(_room, 3.5f, 2.4f), Quaternion.identity);
#endif
            _controller = _player.GetComponent<PlayerController>();
            _binder = _player.GetComponent<PlayerAuraBinder>();
            _health = _player.GetComponent<Health>();
            yield return new WaitForFixedUpdate();
            yield return null;
            yield return null;
        }

        [TearDown]
        public void TearDown() => BossTestKit.Cleanup();

        [UnityTest]
        public IEnumerator WebSlowsLeoByHalfOnTopOfAuraAndWaterAndRestoresIt()
        {
            yield return Setup("GiantStoneSpider");
            Assert.AreEqual(1f, _controller.SpeedMultiplier, 1e-4f);
            PlayerSlowStatus.ApplyTo(_player.transform, 0.5f, 0.6f);
            Assert.AreEqual(0.5f, _controller.SpeedMultiplier, 1e-4f, "web: 50%");
            _binder.EnterWater();
            Assert.AreEqual(0.25f, _controller.SpeedMultiplier, 1e-4f, "wading 0.5 x web 0.5");
            _binder.ExitWater();
            Assert.AreEqual(0.5f, _controller.SpeedMultiplier, 1e-4f, "the binder's rewrite did not erase the slow");
            AuraManager.Instance.Unlock(AuraId.Fire);
            Assert.AreEqual(0.6f, _controller.SpeedMultiplier, 1e-4f, "Fire 1.2 x web 0.5");
            yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(1.2f, _controller.SpeedMultiplier, 1e-4f, "the slow ends and Fire's own speed returns");
        }

        [UnityTest]
        public IEnumerator SlowEndsWhenLeoDiesOrTheArenaResets()
        {
            yield return Setup("GiantStoneSpider");
            PlayerSlowStatus.ApplyTo(_player.transform, 0.5f, 30f);
            Assert.AreEqual(0.5f, _controller.SpeedMultiplier, 1e-4f);
            EventBus.Publish(new PlayerDied());
            Assert.AreEqual(1f, _controller.SpeedMultiplier, 1e-4f);
        }

        [UnityTest]
        public IEnumerator SteamHurtsLeoUnlessTheFireAuraMakesHimHeatImmune()
        {
            yield return Setup("RogueMachine");
            var steam = _boss.GetComponentInChildren<SteamFloodAttack>(true);
            int lowest = _health.Max;
            _health.Changed += (current, max) => lowest = Mathf.Min(lowest, current);
            yield return BossTestKit.RunAttack(_boss, steam);
            Assert.Less(lowest, _health.Max, "no Aura: the steam burns");

            _boss.ClearSpawned();
            _health.Refill();
            _health.Invulnerability.Clear();
            lowest = _health.Max;
            AuraManager.Instance.Unlock(AuraId.Fire);
            yield return null;
            Assert.IsTrue(_binder.HeatImmune);
            yield return BossTestKit.RunAttack(_boss, steam);
            Assert.AreEqual(_health.Max, lowest, "Fire is heat-immune: the steam never hurt him");
        }

        [UnityTest]
        public IEnumerator VictoryUnlocksTheRewardAuraBeforeBossDefeatedIsPublished()
        {
            yield return Setup("RootTree");
            var events = new BossTestKit.Events();
            _player.transform.position = BossTestKit.RoomPoint(_room, 15f, 2.4f);
            yield return BossTestKit.WaitUntil(() => _arena.State == BossArena.ArenaState.Fighting, 3f);
            Assert.AreEqual(BossArena.ArenaState.Fighting, _arena.State);
            Assert.IsFalse(AuraManager.Instance.IsUnlocked(AuraId.Wind));
            BossTestKit.Damage(_boss, 999);
            Assert.IsTrue(AuraManager.Instance.IsUnlocked(AuraId.Wind), "Wind is the Forest reward");
            CollectionAssert.AreEqual(new[] { "Started", "AuraUnlocked", "BossDefeated", "Ended" }, events.Order);
            Assert.AreEqual(BossArena.ArenaState.Defeated, _arena.State);
        }
    }
}
