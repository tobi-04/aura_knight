using System.Collections;
using AuraKnight.Combat;
using AuraKnight.Core;
using AuraKnight.Enemies;
using AuraKnight.World;
using AuraKnight.World.Pickups;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AuraKnight.Tests.PlayMode.Enemies
{
    /// <summary>Scene-less arena helpers: flat Ground, a stand-in Leo (Player tag, Health, Hurtbox) and prefab spawning.</summary>
    static class EnemyTestKit
    {
        public sealed class FixedRandom : IRandomSource
        {
            readonly float _value;
            readonly bool _max;
            public FixedRandom(float value, bool maxRange) { _value = value; _max = maxRange; }
            public float Value() => _value;
            public int Range(int minInclusive, int maxExclusive) => _max ? maxExclusive - 1 : minInclusive;
        }

        public static GameObject Spawn(string variant, Vector2 position, Transform parent = null)
        {
#if UNITY_EDITOR
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Enemies/{variant}.prefab");
            Assert_NotNull(prefab, variant);
            var instance = Object.Instantiate(prefab, parent);
            instance.name = variant;
            instance.GetComponent<EnemyBase>().SetSpawnPoint(position);
            return instance;
#else
            return null;
#endif
        }

        static void Assert_NotNull(Object o, string what)
        {
            if (o == null) throw new System.InvalidOperationException("missing prefab " + what);
        }

        public static GameObject Block(Transform parent, string name, float left, float bottom, float right, float top)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3((left + right) * 0.5f, (bottom + top) * 0.5f, 0f);
            PhysicsLayers.Apply(go, PhysicsLayers.Ground);
            go.AddComponent<BoxCollider2D>().size = new Vector2(right - left, top - bottom);
            return go;
        }

        public static Transform Arena()
        {
            var root = new GameObject("TestArena").transform;
            Block(root, "Floor", -30f, -1f, 30f, 0f);
            Block(root, "WallWest", -31f, -1f, -30f, 20f);
            Block(root, "WallEast", 30f, -1f, 31f, 20f);
            return root;
        }

        /// <summary>A stand-in for Leo: tagged Player, Player-layer hurtbox with 5 hearts. Pass withHurtbox false for a plain target point.</summary>
        public static Health FakePlayer(Vector2 position, bool withHurtbox = true)
        {
            var go = new GameObject("Player") { tag = WorldTags.Player };
            go.transform.position = position;
            var health = go.AddComponent<Health>();
            health.Initialize(5);
            if (!withHurtbox) return health;
            go.AddComponent<BoxCollider2D>().isTrigger = true;
            var hurtbox = go.AddComponent<Hurtbox>();
            hurtbox.Team = Team.Player;
            return health;
        }

        /// <summary>A one-shot player-team strike at a point; polls synchronously so tests need no physics wait.</summary>
        public static HitOutcome Strike(Vector2 at, Vector2 direction, int damage, Vector2 size)
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
            hitbox.Activate(damage, direction);
            hitbox.Poll();
            Object.Destroy(go);
            return outcome;
        }

        /// <summary>Drops the Core and Region scenes an earlier world fixture left behind, so their level geometry cannot overlap the arena.</summary>
        public static IEnumerator UnloadWorldScenes()
        {
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (SceneManager.sceneCount <= 1) yield break;
                if (scene.name != "Core" && !scene.name.StartsWith("Region_")) continue;
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }

        public static IEnumerator Seconds(float seconds)
        {
            yield return new WaitForSeconds(seconds);
        }

        public static void Cleanup()
        {
            Time.timeScale = 1f;
            EventBus.Clear();
            foreach (var go in GameObject.FindGameObjectsWithTag(WorldTags.Player)) Object.Destroy(go);
            foreach (var enemy in Object.FindObjectsByType<EnemyBase>()) Object.Destroy(enemy.gameObject);
            foreach (var coin in Object.FindObjectsByType<CoinPickup>(FindObjectsInactive.Include)) Object.Destroy(coin.gameObject);
            foreach (var drop in Object.FindObjectsByType<LightDropPickup>()) Object.Destroy(drop.gameObject);
            var arena = GameObject.Find("TestArena");
            if (arena != null) Object.Destroy(arena);
        }
    }
}
