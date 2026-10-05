using System.Collections.Generic;
using AuraKnight.Combat;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Combat
{
    public sealed class HitboxTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects) if (go != null) Object.DestroyImmediate(go);
            _objects.Clear();
        }

        Hitbox MakeHitbox(Team team, Vector2 position)
        {
            var go = new GameObject("Hitbox");
            _objects.Add(go);
            go.transform.position = position;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.5f, 1f);
            var hitbox = go.AddComponent<Hitbox>();
            hitbox.Team = team;
            hitbox.AutoPoll = false;
            return hitbox;
        }

        Hurtbox MakeTarget(Team team, Vector2 position, int hp = 10, bool pogo = false)
        {
            var go = new GameObject("Target");
            _objects.Add(go);
            go.transform.position = position;
            go.AddComponent<BoxCollider2D>().isTrigger = true;
            var hurtbox = go.AddComponent<Hurtbox>();
            hurtbox.Team = team;
            hurtbox.AllowsPogo = pogo;
            if (hp > 0)
            {
                var health = go.AddComponent<Health>();
                health.Initialize(hp);
                hurtbox.Health = health;
            }
            return hurtbox;
        }

        [Test]
        public void InactiveHitboxHitsNothing()
        {
            var hitbox = MakeHitbox(Team.Player, Vector2.zero);
            var target = MakeTarget(Team.Enemy, new Vector2(0.5f, 0f));
            Assert.AreEqual(0, hitbox.Poll());
            Assert.AreEqual(10, target.Health.Current);
        }

        [Test]
        public void OneActivationHitsATargetOnlyOnce()
        {
            var hitbox = MakeHitbox(Team.Player, Vector2.zero);
            var target = MakeTarget(Team.Enemy, new Vector2(0.5f, 0f));
            hitbox.Activate(2);
            int reached = 0;
            for (int i = 0; i < 6; i++) reached += hitbox.Poll();
            Assert.AreEqual(1, reached);
            Assert.AreEqual(8, target.Health.Current);
        }

        [Test]
        public void ReactivatingRearmsTheHitbox()
        {
            var hitbox = MakeHitbox(Team.Player, Vector2.zero);
            var target = MakeTarget(Team.Enemy, new Vector2(0.5f, 0f));
            hitbox.Activate(1);
            hitbox.Poll();
            hitbox.Deactivate();
            hitbox.Activate(1);
            hitbox.Poll();
            Assert.AreEqual(8, target.Health.Current);
        }

        [Test]
        public void SameTeamIsNeverHit()
        {
            var hitbox = MakeHitbox(Team.Player, Vector2.zero);
            var ally = MakeTarget(Team.Player, new Vector2(0.5f, 0f));
            var hits = 0;
            hitbox.Hit += _ => hits++;
            hitbox.Activate(3);
            hitbox.Poll();
            Assert.AreEqual(10, ally.Health.Current);
            Assert.AreEqual(0, hits);
        }

        [Test]
        public void MissesTargetsOutOfReach()
        {
            var hitbox = MakeHitbox(Team.Player, Vector2.zero);
            var far = MakeTarget(Team.Enemy, new Vector2(3f, 0f));
            hitbox.Activate(1);
            Assert.AreEqual(0, hitbox.Poll());
            Assert.AreEqual(10, far.Health.Current);
        }

        [Test]
        public void ReportsOutcomeAndDirection()
        {
            var hitbox = MakeHitbox(Team.Enemy, Vector2.zero);
            var player = MakeTarget(Team.Player, new Vector2(-0.5f, 0f), 5);
            HitReport report = default;
            hitbox.Hit += r => report = r;
            hitbox.Activate(1);
            hitbox.Poll();
            Assert.AreSame(player, report.Target);
            Assert.AreEqual(HitOutcome.Damaged, report.Outcome);
            Assert.Less(report.Info.Direction.x, 0f, "direction points from attacker to victim");
            Assert.AreEqual(Team.Enemy, report.Info.Team);
        }

        [Test]
        public void ExplicitDirectionOverridesGeometry()
        {
            var hitbox = MakeHitbox(Team.Player, Vector2.zero);
            MakeTarget(Team.Enemy, new Vector2(-0.5f, 0f));
            HitReport report = default;
            hitbox.Hit += r => report = r;
            hitbox.Activate(1, Vector2.right);
            hitbox.Poll();
            Assert.AreEqual(Vector2.right, report.Info.Direction);
        }

        [Test]
        public void HurtboxWithoutHealthReportsTouched()
        {
            var hitbox = MakeHitbox(Team.Player, Vector2.zero);
            MakeTarget(Team.Hazard, new Vector2(0.5f, 0f), 0, pogo: true);
            HitReport report = default;
            hitbox.Hit += r => report = r;
            hitbox.Activate(1);
            hitbox.Poll();
            Assert.AreEqual(HitOutcome.Touched, report.Outcome);
            Assert.IsTrue(report.Target.AllowsPogo);
        }

        [Test]
        public void HitReportedEvenWhenTargetAbsorbsIt()
        {
            var hitbox = MakeHitbox(Team.Player, Vector2.zero);
            var target = MakeTarget(Team.Enemy, new Vector2(0.5f, 0f));
            target.Health.InvulnerabilityGate = () => true;
            HitReport report = default;
            hitbox.Hit += r => report = r;
            hitbox.Activate(1);
            hitbox.Poll();
            Assert.AreEqual(HitOutcome.Absorbed, report.Outcome);
            Assert.AreEqual(10, target.Health.Current);
        }

        [Test]
        public void RearmIntervalAllowsRepeatedContactDamage()
        {
            var hitbox = MakeHitbox(Team.Hazard, Vector2.zero);
            var player = MakeTarget(Team.Player, new Vector2(0.5f, 0f), 5);
            hitbox.RearmInterval = 0.5f;
            hitbox.Activate(1);
            hitbox.Poll();
            hitbox.AdvanceTime(0.3f);
            hitbox.Poll();
            Assert.AreEqual(4, player.Health.Current);
            hitbox.AdvanceTime(0.3f);
            hitbox.Poll();
            Assert.AreEqual(3, player.Health.Current);
        }
    }
}
