using System.Collections;
using System.Collections.Generic;
using AuraKnight.Bosses;
using AuraKnight.Combat;
using AuraKnight.Core;
using AuraKnight.Enemies;
using AuraKnight.UI;
using AuraKnight.World;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AuraKnight.Tests.PlayMode.Bosses
{
    /// <summary>Arena helpers: the generated boss room far from the origin, a stand-in Leo, strikes, and an event recorder.</summary>
    static class BossTestKit
    {
        public const float RoomOffsetX = 2000f;
        public static readonly string[] Names = { "RootTree", "GiantStoneSpider", "RogueMachine", "Malakor" };

        public sealed class FixedRandom : IRandomSource
        {
            readonly int _range;
            public FixedRandom(int range = 0) { _range = range; }
            public float Value() => 0f;
            public int Range(int minInclusive, int maxExclusive) => Mathf.Clamp(_range, minInclusive, maxExclusive - 1);
        }

        /// <summary>Everything the encounter publishes, in order.</summary>
        public sealed class Events
        {
            public readonly List<string> Order = new List<string>();
            public readonly List<BossHealthChanged> Health = new List<BossHealthChanged>();
            public BossEncounterStarted? Started;
            public BossEncounterEnded? Ended;
            public bool GameCompleted;

            public Events()
            {
                EventBus.Subscribe<BossEncounterStarted>(e => { Started = e; Order.Add("Started"); });
                EventBus.Subscribe<BossEncounterEnded>(e => { Ended = e; Order.Add("Ended"); });
                EventBus.Subscribe<BossHealthChanged>(e => Health.Add(e));
                EventBus.Subscribe<BossDefeated>(e => Order.Add("BossDefeated"));
                EventBus.Subscribe<AuraUnlocked>(e => Order.Add("AuraUnlocked"));
                EventBus.Subscribe<GameCompleted>(e => { GameCompleted = true; Order.Add("GameCompleted"); });
            }
        }

        public static GameObject SpawnRoom(string bossName, float offsetX = RoomOffsetX)
        {
#if UNITY_EDITOR
            string folder = bossName == "RootTree" ? "Forest" : bossName == "GiantStoneSpider" ? "Cave" : bossName == "RogueMachine" ? "City" : "Castle";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Rooms/{folder}/Room_Boss_{folder}.prefab");
            if (prefab == null) throw new System.InvalidOperationException("missing boss room for " + bossName);
            // Built inactive and renamed first: with the real region scenes loaded the world already has a room with this id.
            var parked = new GameObject("Parked");
            parked.SetActive(false);
            var room = Object.Instantiate(prefab, parked.transform);
            var component = room.GetComponent<AuraKnight.World.Room>();
            typeof(AuraKnight.World.Room).GetField("roomId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(component, component.RoomId + "_test");
            room.transform.SetParent(null);
            Object.Destroy(parked);
            room.transform.position = new Vector3(offsetX, 0f, 0f);
            return room;
#else
            return null;
#endif
        }

        public static Vector3 RoomPoint(GameObject room, float x, float y) => room.transform.TransformPoint(new Vector3(x, y, 0f));

        /// <summary>Tagged Player on the Player layer: 0.8 x 1.9 body (resizable for the slide), 5 hearts, kinematic so triggers fire.</summary>
        public static GameObject FakeLeo(Vector3 position, out Health health, float height = 1.9f)
        {
            var go = new GameObject("Leo") { tag = WorldTags.Player };
            go.transform.position = position;
            health = go.AddComponent<Health>();
            health.Initialize(5);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.8f, height);
            var hurtbox = go.AddComponent<Hurtbox>();
            hurtbox.Team = Team.Player;
            return go;
        }

        /// <summary>One-shot player-team strike at a world point; polls synchronously.</summary>
        public static HitOutcome Strike(Vector2 at, int damage, Vector2 size)
        {
            var go = new GameObject("Strike");
            go.transform.position = at;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = size;
            var hitbox = go.AddComponent<Hitbox>();
            hitbox.Team = Team.Player;
            hitbox.AutoPoll = false;
            var outcome = HitOutcome.Ignored;
            hitbox.Hit += report => outcome = report.Outcome;
            hitbox.Activate(damage, Vector2.right);
            hitbox.Poll();
            Object.Destroy(go);
            return outcome;
        }

        public static void Damage(BossBase boss, int amount) =>
            boss.Health.TakeDamage(new DamageInfo(amount, Team.Player, null, Vector2.right));

        /// <summary>Starts an attack and ticks it with the physics step until it ends or <paramref name="seconds"/> pass.</summary>
        public static IEnumerator RunAttack(BossBase boss, BossAttack attack, float seconds = 8f)
        {
            attack.Begin(boss);
            float end = Time.realtimeSinceStartup + seconds;
            while (attack.IsRunning && Time.realtimeSinceStartup < end)
            {
                yield return new WaitForFixedUpdate();
                attack.Tick(Time.fixedDeltaTime);
            }
        }

        public static IEnumerator FixedSteps(int count)
        {
            for (int i = 0; i < count; i++) yield return new WaitForFixedUpdate();
        }

        public static IEnumerator WaitUntil(System.Func<bool> condition, float timeout = 10f)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (!condition() && Time.realtimeSinceStartup < end) yield return null;
        }

        /// <summary>Drops the Core and Region scenes (and their GameManager) an earlier fixture left behind, so saved boss progress cannot leak in.</summary>
        public static IEnumerator EnsureCleanWorld()
        {
            bool dirty = GameManager.Instance != null;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                string name = SceneManager.GetSceneAt(i).name;
                dirty |= name == "Core" || name.StartsWith("Region_");
            }
            if (!dirty) yield break;
            var holder = SceneManager.CreateScene("BossTests_" + System.Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(holder);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name == "Core" || scene.name.StartsWith("Region_")) yield return SceneManager.UnloadSceneAsync(scene);
            }
            yield return null;
        }

        public static void Cleanup()
        {
            Time.timeScale = 1f;
            EventBus.Clear();
            foreach (var go in GameObject.FindGameObjectsWithTag(WorldTags.Player)) Object.Destroy(go);
            foreach (var enemy in Object.FindObjectsByType<EnemyBase>()) Object.Destroy(enemy.gameObject);
            foreach (var arena in Object.FindObjectsByType<BossArena>()) Object.Destroy(arena.transform.root.gameObject);
        }
    }
}
