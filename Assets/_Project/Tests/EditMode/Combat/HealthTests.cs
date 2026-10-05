using AuraKnight.Combat;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Combat
{
    public sealed class HealthTests
    {
        GameObject _go;
        Health _health;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Health");
            _health = _go.AddComponent<Health>();
            _health.Initialize(5);
        }

        [TearDown] public void TearDown() => Object.DestroyImmediate(_go);

        static DamageInfo Hit(int amount = 1) => new DamageInfo(amount, Team.Enemy);

        [Test]
        public void StartsFull()
        {
            Assert.AreEqual(5, _health.Current);
            Assert.AreEqual(5, _health.Max);
            Assert.IsFalse(_health.IsDead);
        }

        [Test]
        public void DamageReducesCurrentAndReportsOutcome()
        {
            Assert.AreEqual(HitOutcome.Damaged, _health.TakeDamage(Hit(2)));
            Assert.AreEqual(3, _health.Current);
        }

        [Test]
        public void LethalDamageKillsExactlyOnce()
        {
            int died = 0;
            _health.Died += _ => died++;
            Assert.AreEqual(HitOutcome.Killed, _health.TakeDamage(Hit(9)));
            Assert.AreEqual(0, _health.Current);
            Assert.IsTrue(_health.IsDead);
            Assert.AreEqual(HitOutcome.Ignored, _health.TakeDamage(Hit()));
            Assert.AreEqual(1, died);
        }

        [Test]
        public void ZeroOrNegativeDamageIsIgnored()
        {
            Assert.AreEqual(HitOutcome.Ignored, _health.TakeDamage(Hit(0)));
            Assert.AreEqual(HitOutcome.Ignored, _health.TakeDamage(Hit(-3)));
            Assert.AreEqual(5, _health.Current);
        }

        [Test]
        public void EventsReportAppliedAmount()
        {
            int applied = -1, changedCurrent = -1;
            _health.Damaged += (_, a) => applied = a;
            _health.Changed += (c, _) => changedCurrent = c;
            _health.Initialize(2);
            _health.TakeDamage(Hit(5));
            Assert.AreEqual(2, applied);
            Assert.AreEqual(0, changedCurrent);
        }

        [Test]
        public void HitStartsInvulnerabilityWindow()
        {
            _health.InvulnerableAfterHit = 1f;
            Assert.AreEqual(HitOutcome.Damaged, _health.TakeDamage(Hit()));
            Assert.AreEqual(HitOutcome.Absorbed, _health.TakeDamage(Hit()));
            Assert.AreEqual(4, _health.Current);
            _health.Invulnerability.Tick(0.5f);
            Assert.AreEqual(HitOutcome.Absorbed, _health.TakeDamage(Hit()));
            _health.Invulnerability.Tick(0.6f);
            Assert.AreEqual(HitOutcome.Damaged, _health.TakeDamage(Hit()));
            Assert.AreEqual(3, _health.Current);
        }

        [Test]
        public void ExternalGateAbsorbsDamage()
        {
            bool gate = true;
            _health.InvulnerabilityGate = () => gate;
            Assert.AreEqual(HitOutcome.Absorbed, _health.TakeDamage(Hit()));
            gate = false;
            Assert.AreEqual(HitOutcome.Damaged, _health.TakeDamage(Hit()));
        }

        [Test]
        public void HealClampsToMaxAndReportsApplied()
        {
            _health.TakeDamage(Hit(2));
            Assert.AreEqual(1, _health.Heal(1));
            Assert.AreEqual(1, _health.Heal(5));
            Assert.AreEqual(0, _health.Heal(1));
            Assert.AreEqual(0, _health.Heal(-2));
            Assert.AreEqual(5, _health.Current);
        }

        [Test]
        public void DeadHealthCannotBeHealedButRefillRevives()
        {
            _health.TakeDamage(Hit(9));
            Assert.AreEqual(0, _health.Heal(1));
            _health.Refill();
            Assert.IsFalse(_health.IsDead);
            Assert.AreEqual(5, _health.Current);
            Assert.IsFalse(_health.Invulnerability.IsActive);
        }

        [Test]
        public void InitializeClampsValues()
        {
            _health.Initialize(0);
            Assert.AreEqual(1, _health.Max);
            _health.Initialize(4, 9);
            Assert.AreEqual(4, _health.Current);
        }
    }
}
