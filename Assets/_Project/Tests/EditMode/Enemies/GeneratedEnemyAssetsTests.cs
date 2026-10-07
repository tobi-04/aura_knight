using AuraKnight.Combat;
using AuraKnight.Core;
using AuraKnight.Enemies;
using AuraKnight.Enemies.Modifiers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Tests.Enemies
{
    /// <summary>
    /// Guards the generator output (AuraKnight.Editor.EnemyAssetGenerator.Generate): GDD 7.3 numbers on the stats assets and the
    /// prefab wiring rules (layers, hitbox/hurtbox separation, modifiers).
    /// </summary>
    public sealed class GeneratedEnemyAssetsTests
    {
        readonly struct Row
        {
            public readonly string Name; public readonly EnemyArchetype Archetype; public readonly int Hp, CoinsMin, CoinsMax;
            public Row(string name, EnemyArchetype archetype, int hp, int coinsMin, int coinsMax)
            { Name = name; Archetype = archetype; Hp = hp; CoinsMin = coinsMin; CoinsMax = coinsMax; }
        }

        // GDD 7.3 table, one row per variant: archetype, HP, coin range. All variants deal 1 contact damage.
        static readonly Row[] Table =
        {
            new Row("BugThorn", EnemyArchetype.Walker, 2, 3, 5),
            new Row("PatrolBot", EnemyArchetype.Walker, 4, 3, 5),
            new Row("NightKnight", EnemyArchetype.Walker, 6, 3, 5),
            new Row("PoisonShroom", EnemyArchetype.Hopper, 2, 3, 3),
            new Row("Bat", EnemyArchetype.Flyer, 2, 4, 6),
            new Row("Ghost", EnemyArchetype.Flyer, 4, 4, 6),
            new Row("StoneSpider", EnemyArchetype.Crawler, 3, 4, 4),
            new Row("ScrapZapper", EnemyArchetype.Static, 4, 4, 4),
        };

        static string[] Names()
        {
            var names = new string[Table.Length];
            for (int i = 0; i < names.Length; i++) names[i] = Table[i].Name;
            return names;
        }

        static GameObject LoadPrefab(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Enemies/{name}.prefab");
            Assert.IsNotNull(prefab, $"missing prefab {name}");
            return prefab;
        }

        [TestCaseSource(nameof(Names))]
        public void StatsMatchTheDesignTable(string name)
        {
            var row = System.Array.Find(Table, r => r.Name == name);
            var stats = AssetDatabase.LoadAssetAtPath<EnemyStats>($"Assets/_Project/Data/Enemies/{name}.asset");
            Assert.IsNotNull(stats, $"missing stats {name}");
            Assert.AreEqual(row.Archetype, stats.archetype);
            Assert.AreEqual(row.Hp, stats.maxHp);
            Assert.AreEqual(1, stats.contactDamage);
            Assert.AreEqual(row.CoinsMin, stats.coinsMin);
            Assert.AreEqual(row.CoinsMax, stats.coinsMax);
            Assert.AreEqual(1.5f, stats.hopInterval, "hopper cadence");
            Assert.AreEqual(3f, stats.zapInterval, "zapper cadence");
            Assert.AreEqual(2f, stats.zapRadius, "two-tile ring");
        }

        [TestCaseSource(nameof(Names))]
        public void PrefabHasTheEnemyRecipe(string name)
        {
            var prefab = LoadPrefab(name);
            var enemy = prefab.GetComponent<EnemyBase>();
            Assert.IsNotNull(enemy, "archetype class");
            Assert.AreEqual(LayerMask.NameToLayer(PhysicsLayers.Enemy), prefab.layer);
            var health = prefab.GetComponent<Health>();
            Assert.IsNotNull(health);
            var hurtbox = enemy.Hurtbox;
            Assert.IsNotNull(hurtbox);
            Assert.AreEqual(Team.Enemy, hurtbox.Team);
            Assert.AreNotSame(prefab, hurtbox.gameObject, "hurtbox lives on a child");
            Assert.AreEqual(LayerMask.NameToLayer(PhysicsLayers.Enemy), hurtbox.gameObject.layer);
            Assert.IsTrue(hurtbox.GetComponent<Collider2D>().isTrigger);
            Assert.AreSame(health, hurtbox.Health);
            var contact = enemy.ContactHitbox;
            Assert.IsNotNull(contact);
            Assert.AreEqual(Team.Enemy, contact.Team);
            Assert.AreNotSame(hurtbox.gameObject, contact.gameObject, "hitbox and hurtbox of different teams never share an object");
            Assert.AreEqual(LayerMask.NameToLayer(PhysicsLayers.EnemyAttack), contact.gameObject.layer);
            Assert.IsTrue(contact.GetComponent<Collider2D>().isTrigger);
            Assert.IsNull(prefab.GetComponent<Hitbox>());
            Assert.IsNull(prefab.GetComponent<Hurtbox>());
        }

        [Test]
        public void EveryHitboxAndHurtboxInEveryPrefabIsOnItsOwnObjectAndLayer()
        {
            foreach (var name in Names())
            {
                var prefab = LoadPrefab(name);
                foreach (var hitbox in prefab.GetComponentsInChildren<Hitbox>(true))
                {
                    Assert.IsNull(hitbox.GetComponent<Hurtbox>(), name);
                    Assert.AreEqual(LayerMask.NameToLayer(PhysicsLayers.EnemyAttack), hitbox.gameObject.layer, name);
                }
            }
        }

        [Test]
        public void ModifiersAreOnTheRightVariantsOnly()
        {
            Assert.IsNotNull(LoadPrefab("NightKnight").GetComponent<FrontShield>());
            Assert.IsNotNull(LoadPrefab("Ghost").GetComponent<PhaseThroughWalls>());
            Assert.IsNotNull(LoadPrefab("Bat").GetComponent<LifeSteal>());
            foreach (var name in new[] { "BugThorn", "PatrolBot", "PoisonShroom", "StoneSpider", "ScrapZapper" })
            {
                var prefab = LoadPrefab(name);
                Assert.IsNull(prefab.GetComponent<FrontShield>(), name);
                Assert.IsNull(prefab.GetComponent<PhaseThroughWalls>(), name);
                Assert.IsNull(prefab.GetComponent<LifeSteal>(), name);
            }
        }

        [Test]
        public void ArchetypeClassMatchesTheStats()
        {
            Assert.IsInstanceOf<WalkerEnemy>(LoadPrefab("BugThorn").GetComponent<EnemyBase>());
            Assert.IsInstanceOf<HopperEnemy>(LoadPrefab("PoisonShroom").GetComponent<EnemyBase>());
            Assert.IsInstanceOf<FlyerEnemy>(LoadPrefab("Bat").GetComponent<EnemyBase>());
            Assert.IsInstanceOf<FlyerEnemy>(LoadPrefab("Ghost").GetComponent<EnemyBase>());
            Assert.IsInstanceOf<CrawlerEnemy>(LoadPrefab("StoneSpider").GetComponent<EnemyBase>());
            Assert.IsInstanceOf<CrawlerEnemy>(LoadPrefab("ScrapZapper").GetComponent<EnemyBase>());
        }

        [Test]
        public void FlyersAndCrawlersHaveNoGravityAndGroundedOnesDo()
        {
            Assert.Greater(LoadPrefab("BugThorn").GetComponent<Rigidbody2D>().gravityScale, 0f);
            Assert.Greater(LoadPrefab("PoisonShroom").GetComponent<Rigidbody2D>().gravityScale, 0f);
            Assert.AreEqual(0f, LoadPrefab("Bat").GetComponent<Rigidbody2D>().gravityScale);
            Assert.AreEqual(0f, LoadPrefab("Ghost").GetComponent<Rigidbody2D>().gravityScale);
            Assert.AreEqual(RigidbodyType2D.Kinematic, LoadPrefab("StoneSpider").GetComponent<Rigidbody2D>().bodyType);
            Assert.AreEqual(RigidbodyType2D.Kinematic, LoadPrefab("ScrapZapper").GetComponent<Rigidbody2D>().bodyType);
        }

        [Test]
        public void ZapperHasARingHitboxAndSpiderHasAPath()
        {
            var zapper = LoadPrefab("ScrapZapper");
            var ring = zapper.transform.Find("ZapHitbox");
            Assert.IsNotNull(ring);
            Assert.AreEqual(2f, ring.GetComponent<CircleCollider2D>().radius, 1e-4f);
            Assert.IsFalse(ring.GetComponent<Hitbox>().IsActive);
            var path = LoadPrefab("StoneSpider").transform.Find("Path");
            Assert.GreaterOrEqual(path.childCount, 2);
        }

        [Test]
        public void PickupPrefabsExist()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<AuraKnight.World.Pickups.CoinPickup>("Assets/_Project/Prefabs/Pickups/CoinPickup.prefab"));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<AuraKnight.World.Pickups.LightDropPickup>("Assets/_Project/Prefabs/Pickups/LightDrop.prefab"));
        }

        [Test]
        public void SharedAnimatorControllerHasTheStatesAndParameters()
        {
            var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/_Project/Data/Enemies/EnemyAnimator.controller");
            Assert.IsNotNull(controller);
            var names = new System.Collections.Generic.HashSet<string>();
            foreach (var child in controller.layers[0].stateMachine.states) names.Add(child.state.name);
            CollectionAssert.IsSubsetOf(new[] { "Idle", "Move", "Attack", "Hurt", "Death" }, names);
            var parameters = new System.Collections.Generic.HashSet<string>();
            foreach (var p in controller.parameters) parameters.Add(p.name);
            CollectionAssert.IsSubsetOf(new[] { "Moving", "Attack", "Hurt", "Dead" }, parameters);
        }
    }
}
