using System.Collections.Generic;
using AuraKnight.Combat;
using AuraKnight.Core;
using AuraKnight.Player;
using AuraKnight.Tests.Player;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Combat
{
    /// <summary>Project physics layers, the collision matrix and the team-to-layer mapping used by hits and the motor.</summary>
    public sealed class PhysicsLayerTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects) if (go != null) Object.DestroyImmediate(go);
            _objects.Clear();
        }

        [Test]
        public void EveryProjectLayerIsDefinedAndDistinct()
        {
            var seen = new HashSet<int>();
            foreach (var name in PhysicsLayers.All)
            {
                int id = PhysicsLayers.Id(name);
                Assert.GreaterOrEqual(id, 8, $"{name} must be a user layer (run Aura/Setup Project)");
                Assert.IsTrue(seen.Add(id), $"{name} shares an id");
            }
        }

        [Test]
        public void CollisionMatrixOnlyEnablesTheDeclaredPairs()
        {
            foreach (var a in PhysicsLayers.All)
            foreach (var b in PhysicsLayers.All)
            {
                bool declared = false;
                foreach (var pair in PhysicsLayers.CollidingPairs)
                    declared |= (pair[0] == a && pair[1] == b) || (pair[0] == b && pair[1] == a);
                bool ignored = Physics2D.GetIgnoreLayerCollision(PhysicsLayers.Id(a), PhysicsLayers.Id(b));
                Assert.AreEqual(!declared, ignored, $"{a} x {b}");
            }
            Assert.IsFalse(Physics2D.GetIgnoreLayerCollision(PhysicsLayers.Id(PhysicsLayers.Player), PhysicsLayers.Id(PhysicsLayers.Interactable)));
            Assert.IsTrue(Physics2D.GetIgnoreLayerCollision(PhysicsLayers.Id(PhysicsLayers.Ground), PhysicsLayers.Id(PhysicsLayers.Interactable)));
        }

        [TestCase(Team.Player, PhysicsLayers.Player, PhysicsLayers.PlayerAttack)]
        [TestCase(Team.Enemy, PhysicsLayers.Enemy, PhysicsLayers.EnemyAttack)]
        [TestCase(Team.Hazard, PhysicsLayers.Hazard, PhysicsLayers.Hazard)]
        public void TeamsMapToHurtboxAndHitboxLayers(Team team, string hurtLayer, string hitLayer)
        {
            Assert.AreEqual(hurtLayer, HitMasks.HurtboxLayer(team));
            Assert.AreEqual(hitLayer, HitMasks.HitboxLayer(team));
        }

        [Test]
        public void PlayerHitsEnemiesAndHazardsEveryoneElseHitsThePlayer()
        {
            int toPlayer = PhysicsLayers.Mask(PhysicsLayers.Player);
            Assert.AreEqual(PhysicsLayers.Mask(PhysicsLayers.Enemy, PhysicsLayers.Hazard), HitMasks.TargetMask(Team.Player));
            Assert.AreEqual(toPlayer, HitMasks.TargetMask(Team.Enemy));
            Assert.AreEqual(toPlayer, HitMasks.TargetMask(Team.Hazard));
        }

        GameObject Make(string name, Vector2 position, int layer)
        {
            var go = new GameObject(name) { layer = layer };
            go.transform.position = position;
            _objects.Add(go);
            return go;
        }

        [Test]
        public void SettingTheTeamMovesHurtboxesAndHitboxesToTheirLayers()
        {
            var go = Make("Both", Vector2.zero, 0);
            go.AddComponent<BoxCollider2D>().isTrigger = true;
            go.AddComponent<Hurtbox>().Team = Team.Enemy;
            Assert.AreEqual(PhysicsLayers.Id(PhysicsLayers.Enemy), go.layer);
            var swordGo = Make("Sword", Vector2.zero, 0);
            swordGo.AddComponent<BoxCollider2D>().isTrigger = true;
            swordGo.AddComponent<Hitbox>().Team = Team.Player;
            Assert.AreEqual(PhysicsLayers.Id(PhysicsLayers.PlayerAttack), swordGo.layer);
        }

        [Test]
        public void HitboxIgnoresTargetsOnTheWrongLayer()
        {
            var swordGo = Make("Sword", Vector2.zero, 0);
            swordGo.AddComponent<BoxCollider2D>().isTrigger = true;
            var sword = swordGo.AddComponent<Hitbox>();
            sword.Team = Team.Player;
            sword.AutoPoll = false;

            var stray = Make("Stray", new Vector2(0.2f, 0f), PhysicsLayers.Id(PhysicsLayers.Interactable));
            stray.AddComponent<BoxCollider2D>().isTrigger = true;
            var strayHurtbox = stray.AddComponent<Hurtbox>();
            strayHurtbox.Team = Team.Enemy;
            stray.layer = PhysicsLayers.Id(PhysicsLayers.Interactable); // a hurtbox mis-layered on purpose
            var onLayer = Make("Enemy", new Vector2(-0.2f, 0f), 0);
            onLayer.AddComponent<BoxCollider2D>().isTrigger = true;
            onLayer.AddComponent<Hurtbox>().Team = Team.Enemy;
            Physics2D.SyncTransforms();

            sword.Activate(1);
            Assert.AreEqual(1, sword.Poll(), "only the correctly layered enemy is reachable");
        }

        [Test]
        public void HitboxSourceDefaultsToRootAndCanBeOverridden()
        {
            var owner = Make("Owner", new Vector2(10f, 0f), 0);
            var swordGo = Make("Projectile", Vector2.zero, 0);
            swordGo.AddComponent<BoxCollider2D>().isTrigger = true;
            var hitbox = swordGo.AddComponent<Hitbox>();
            hitbox.Team = Team.Player;
            hitbox.AutoPoll = false;
            var targetGo = Make("Target", new Vector2(0.2f, 0f), 0);
            targetGo.AddComponent<BoxCollider2D>().isTrigger = true;
            var health = targetGo.AddComponent<Health>();
            health.Initialize(5);
            var hurtbox = targetGo.AddComponent<Hurtbox>();
            hurtbox.Team = Team.Enemy;
            hurtbox.Health = health;
            GameObject source = null;
            health.Damaged += (info, _) => source = info.Source;
            Physics2D.SyncTransforms();

            hitbox.Activate(1);
            hitbox.Poll();
            Assert.AreSame(swordGo, source);

            hitbox.Deactivate();
            health.Refill();
            health.Invulnerability.Tick(10f);
            hitbox.Source = owner.transform;
            hitbox.Activate(1);
            hitbox.Poll();
            Assert.AreSame(owner, source, "a fireball reports Leo, not itself");
        }

        [Test]
        public void MotorOnlyCollidesWithTheGroundLayer()
        {
            var sim = new PlayerSimHarness();
            try
            {
                var enemy = Make("EnemyBody", new Vector2(3f, 0.97f), PhysicsLayers.Id(PhysicsLayers.Enemy));
                enemy.AddComponent<BoxCollider2D>().size = new Vector2(1f, 2f);
                var wallTrigger = Make("Altar", new Vector2(3f, 1f), PhysicsLayers.Id(PhysicsLayers.Interactable));
                wallTrigger.AddComponent<BoxCollider2D>().size = new Vector2(1f, 2f);
                sim.Level.Sync();
                sim.Input.Move = Vector2.right;
                sim.Run(1.0f);
                Assert.Greater(sim.Position.x, 5f, "enemy and interactable colliders never block Leo");
            }
            finally { sim.Dispose(); }
        }
    }
}
