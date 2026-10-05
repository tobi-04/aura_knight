using AuraKnight.Aura;
using AuraKnight.Aura.Skills;
using AuraKnight.Combat;
using AuraKnight.World;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Aura
{
    public sealed class AuraSkillSimulationTests : AuraTestBase
    {
        GameObject _floor;

        [SetUp]
        public void SetUp() => _floor = MakeBox("Floor", new Vector2(0f, -0.5f), new Vector2(200f, 1f), false);

        FireballProjectile NewProjectile(out Hitbox hitbox)
        {
            var go = Make("Fireball", new Vector2(0f, 3f));
            go.AddComponent<CircleCollider2D>().isTrigger = true;
            hitbox = go.AddComponent<Hitbox>();
            var projectile = go.AddComponent<FireballProjectile>();
            SetRef(projectile, "hitbox", hitbox);
            return projectile;
        }

        static int Fly(FireballProjectile projectile, int maxSteps = 200)
        {
            int steps = 0;
            while (projectile.IsFlying && steps < maxSteps)
            {
                projectile.Step(0.02f);
                steps++;
            }
            return steps;
        }

        GameObject Dummy(Vector2 position, int hp, out Health health)
        {
            var go = MakeBox("Dummy", position, new Vector2(1f, 2f), true);
            health = go.AddComponent<Health>();
            health.Initialize(hp);
            var hurtbox = go.AddComponent<Hurtbox>();
            hurtbox.Team = Team.Enemy;
            hurtbox.Health = health;
            return go;
        }

        [Test]
        public void FireballTravelsTwelveTilesAndDespawns()
        {
            var projectile = NewProjectile(out _);
            projectile.Launch(new Vector2(0f, 3f), 1);
            int steps = Fly(projectile);
            Assert.IsFalse(projectile.IsFlying);
            Assert.AreEqual(12f, projectile.Travelled, 1e-3f);
            Assert.AreEqual(12f, projectile.transform.position.x, 1e-3f);
            Assert.AreEqual(30, steps, 1, "20 u/s over 12 tiles");
            Assert.IsFalse(projectile.gameObject.activeSelf);
        }

        [Test]
        public void FireballFliesLeftWhenFacingLeft()
        {
            var projectile = NewProjectile(out _);
            projectile.Launch(new Vector2(0f, 3f), -1);
            Fly(projectile);
            Assert.AreEqual(-12f, projectile.transform.position.x, 1e-3f);
        }

        [Test]
        public void FireballDealsTwoDamageThroughHitboxAndStops()
        {
            var projectile = NewProjectile(out _);
            Dummy(new Vector2(6f, 3f), 5, out var health);
            projectile.Launch(new Vector2(0f, 3f), 1);
            Fly(projectile);
            Assert.AreEqual(3, health.Current);
            Assert.Less(projectile.Travelled, 6f);
        }

        [Test]
        public void FireballStopsAtWalls()
        {
            var projectile = NewProjectile(out _);
            MakeBox("Wall", new Vector2(5.5f, 3f), new Vector2(1f, 8f), false);
            projectile.Launch(new Vector2(0f, 3f), 1);
            Fly(projectile);
            Assert.Less(projectile.Travelled, 5.2f);
            Assert.Greater(projectile.Travelled, 4f);
        }

        [Test]
        public void FireballBurnsBarricadeButNotAFireTrap()
        {
            var projectile = NewProjectile(out _);
            var barricadeRoot = MakeBox("Barricade", new Vector2(8f, 3f), new Vector2(2f, 4f), true);
            var blocker = MakeBox("Blocker", new Vector2(8f, 3f), new Vector2(1f, 4f), false);
            blocker.transform.SetParent(barricadeRoot.transform, true);
            var gate = barricadeRoot.AddComponent<BurnableGate>();
            SetObjects(gate, "switches.deactivateOnOpen", blocker);

            var trap = MakeBox("Trap", new Vector2(3f, 3f), new Vector2(1f, 1f), true).AddComponent<ExtinguishableGate>();

            projectile.Launch(new Vector2(0f, 3f), 1);
            Fly(projectile);
            Assert.IsTrue(gate.IsOpen);
            Assert.IsFalse(blocker.activeSelf);
            Assert.IsFalse(trap.IsOpen, "fire cannot put out a fire trap");
        }

        [Test]
        public void FireballSkillReusesItsPoolAndCapsItsGrowth()
        {
            var template = NewProjectile(out _);
            template.gameObject.SetActive(false);
            var skillObject = Make("Skill", new Vector2(0f, 3f));
            var skill = skillObject.AddComponent<FireballSkill>();
            SetRef(skill, "projectilePrefab", template);
            skill.Bind(null, null);
            Assert.AreEqual(3, skill.PoolSize);

            skill.Cast();
            Fly(FindFlying());
            skill.Cast();
            Assert.AreEqual(3, skill.PoolSize, "a finished fireball is reused");

            for (int i = 0; i < 10; i++) skill.Cast();
            Assert.LessOrEqual(skill.PoolSize, 6);
        }

        static FireballProjectile FindFlying()
        {
            foreach (var p in Object.FindObjectsByType<FireballProjectile>(FindObjectsInactive.Exclude))
                if (p.IsFlying) return p;
            Assert.Fail("no projectile in flight");
            return null;
        }

        [Test]
        public void WindGustHitsWithinTwoPointFiveTilesOnly()
        {
            var skillObject = Make("Gust", Vector2.zero);
            var skill = skillObject.AddComponent<WindGustSkill>();
            var child = Make("GustHitbox", Vector2.zero);
            child.transform.SetParent(skillObject.transform, false);
            child.AddComponent<CircleCollider2D>().isTrigger = true;
            var hitbox = child.AddComponent<Hitbox>();
            skill.Setup(hitbox);
            var near = Dummy(new Vector2(1.8f, 0f), 3, out var nearHealth);
            Dummy(new Vector2(-4f, 0f), 3, out var farHealth);
            Assert.IsNotNull(near);

            skill.Cast();
            Assert.IsTrue(skill.IsActive);
            hitbox.Poll();
            Assert.AreEqual(2, nearHealth.Current, "1 damage");
            Assert.AreEqual(3, farHealth.Current);
            hitbox.Poll();
            Assert.AreEqual(2, nearHealth.Current, "one hit per cast");

            skill.Tick(0.2f);
            Assert.IsFalse(skill.IsActive);
        }

        [Test]
        public void WaterShieldNegatesExactlyOneHitThroughHealth()
        {
            var player = Make("Player", Vector2.zero);
            var health = player.AddComponent<Health>();
            health.Initialize(5);
            var skillObject = Make("Shield", Vector2.zero);
            var skill = skillObject.AddComponent<WaterShieldSkill>();
            skill.Bind(null, health);
            var hit = new DamageInfo(1, Team.Enemy);
            int absorbed = 0;
            skill.Absorbed += _ => absorbed++;

            Assert.AreEqual(HitOutcome.Damaged, health.TakeDamage(hit), "no shield yet");
            Assert.AreEqual(4, health.Current);

            skill.Cast();
            Assert.IsFalse(skill.CanCast);
            Assert.AreEqual(HitOutcome.Absorbed, health.TakeDamage(hit));
            Assert.AreEqual(4, health.Current);
            Assert.AreEqual(1, absorbed);
            Assert.IsTrue(skill.CanCast, "shield is spent");
            Assert.AreEqual(HitOutcome.Damaged, health.TakeDamage(hit));
            Assert.AreEqual(3, health.Current);
        }

        [Test]
        public void WaterShieldExpiresUnusedAfterSixSeconds()
        {
            var player = Make("Player", Vector2.zero);
            var health = player.AddComponent<Health>();
            health.Initialize(5);
            var skill = Make("Shield", Vector2.zero).AddComponent<WaterShieldSkill>();
            skill.Bind(null, health);
            skill.Cast();
            skill.Tick(5.9f);
            Assert.IsTrue(skill.IsShielded);
            skill.Tick(0.2f);
            Assert.IsFalse(skill.IsShielded);
            Assert.AreEqual(HitOutcome.Damaged, health.TakeDamage(new DamageInfo(1, Team.Enemy)));
        }

        [Test]
        public void ShieldDoesNotConsumeHitsThatInvulnerabilityAlreadyBlocks()
        {
            var player = Make("Player", Vector2.zero);
            var health = player.AddComponent<Health>();
            health.Initialize(5);
            var skill = Make("Shield", Vector2.zero).AddComponent<WaterShieldSkill>();
            skill.Bind(null, health);
            skill.Cast();
            health.Invulnerability.Begin(1f);
            Assert.AreEqual(HitOutcome.Absorbed, health.TakeDamage(new DamageInfo(1, Team.Enemy)));
            Assert.IsTrue(skill.IsShielded, "i-frames absorbed it, the shield is still up");
        }
    }
}
