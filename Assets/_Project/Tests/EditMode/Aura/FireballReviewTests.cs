using AuraKnight.Aura.Skills;
using AuraKnight.Combat;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Aura
{
    /// <summary>Fireball details from the review: damage source is the owner and the kind is Fire, one-way platforms are passed through, walls still stop it.</summary>
    public sealed class FireballReviewTests : AuraTestBase
    {
        FireballProjectile NewProjectile()
        {
            var go = Make("Fireball", new Vector2(0f, 3f));
            go.AddComponent<CircleCollider2D>().isTrigger = true;
            var hitbox = go.AddComponent<Hitbox>();
            var projectile = go.AddComponent<FireballProjectile>();
            SetRef(projectile, "hitbox", hitbox);
            return projectile;
        }

        static void Fly(FireballProjectile projectile)
        {
            for (int i = 0; i < 200 && projectile.IsFlying; i++) projectile.Step(0.02f);
        }

        [Test]
        public void DamageSourceIsTheOwnerAndTheKindIsFire()
        {
            var owner = Make("Leo", new Vector2(-20f, 3f));
            var projectile = NewProjectile();
            var target = MakeBox("Dummy", new Vector2(4f, 3f), new Vector2(1f, 2f), true);
            var health = target.AddComponent<Health>();
            health.Initialize(5);
            var hurtbox = target.AddComponent<Hurtbox>();
            hurtbox.Team = Team.Enemy;
            hurtbox.Health = health;
            GameObject source = null;
            var kind = DamageKind.General;
            health.Damaged += (info, _) => { source = info.Source; kind = info.Kind; };

            projectile.Launch(new Vector2(0f, 3f), 1, owner.transform);
            Fly(projectile);

            Assert.AreEqual(3, health.Current);
            Assert.AreSame(owner, source);
            Assert.AreEqual(DamageKind.Fire, kind, "the Rogue Machine boiler doubles only Fire hits");
        }

        [Test]
        public void FireballFliesThroughOneWayPlatforms()
        {
            var platform = MakeBox("OneWay", new Vector2(5.5f, 3f), new Vector2(1f, 0.5f), false);
            platform.GetComponent<BoxCollider2D>().usedByEffector = true;
            platform.AddComponent<PlatformEffector2D>();
            var projectile = NewProjectile();
            projectile.Launch(new Vector2(0f, 3f), 1);
            Fly(projectile);
            Assert.AreEqual(12f, projectile.Travelled, 1e-3f, "one-way platforms never stop a fireball");
        }

        [Test]
        public void FireballStillStopsAtASolidWall()
        {
            MakeBox("Wall", new Vector2(5.5f, 3f), new Vector2(1f, 0.5f), false);
            var projectile = NewProjectile();
            projectile.Launch(new Vector2(0f, 3f), 1);
            Fly(projectile);
            Assert.Less(projectile.Travelled, 6f);
        }
    }
}
