using AuraKnight.Combat;
using AuraKnight.Core;
using AuraKnight.Player;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Combat
{
    public sealed class PlayerCombatSimulationTests
    {
        CombatSimHarness _sim;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _sim = new CombatSimHarness();
        }

        [TearDown] public void TearDown() => _sim.Dispose();

        // ---- sword ----

        [Test]
        public void SwingDamagesTargetInReachBySwordLevel()
        {
            _sim.Dispose();
            _sim = new CombatSimHarness(swordLevel: 3);
            var dummy = _sim.AddDummy(1.5f, 0.97f, 20);
            _sim.PressAttack();
            Assert.AreEqual(PlayerStateId.Attack, _sim.State);
            _sim.Run(0.3f);
            Assert.AreEqual(17, dummy.Current);
        }

        [Test]
        public void OneSwingHitsATargetOnlyOnce()
        {
            var dummy = _sim.AddDummy(1.5f, 0.97f, 20);
            _sim.PressAttack();
            _sim.Run(0.4f);
            Assert.AreEqual(19, dummy.Current);
        }

        [Test]
        public void SwingDoesNotReachBeyondOneAndAHalfTiles()
        {
            var dummy = _sim.AddDummy(2.6f, 0.97f, 20, size: 0.4f);
            _sim.PressAttack();
            _sim.Run(0.4f);
            Assert.AreEqual(20, dummy.Current);
        }

        [Test]
        public void SwingNeverHurtsOwnTeam()
        {
            var ally = _sim.AddDummy(1.5f, 0.97f, 20, team: Team.Player);
            _sim.Stats.TrySpendEnergy(50f);
            _sim.PressAttack();
            _sim.Run(0.4f);
            Assert.AreEqual(20, ally.Current);
            Assert.AreEqual(50f, _sim.Stats.Energy.Current, 1e-4f, "no energy gained from allies");
        }

        [Test]
        public void SwingFacesTheStickDirection()
        {
            var behind = _sim.AddDummy(-1.5f, 0.97f, 20);
            _sim.PressAttack(new Vector2(-1f, 0.2f));
            _sim.Run(0.4f);
            Assert.AreEqual(19, behind.Current);
        }

        [Test]
        public void SecondPressInsideComboWindowThrowsSecondSwing()
        {
            var dummy = _sim.AddDummy(1.5f, 0.97f, 20);
            _sim.PressAttack();
            Assert.AreEqual(1, _sim.Combat.Combo.Step);
            _sim.Run(0.3f);
            Assert.AreEqual(19, dummy.Current);
            Assert.AreNotEqual(PlayerStateId.Attack, _sim.State);
            _sim.PressAttack();
            Assert.AreEqual(PlayerStateId.Attack, _sim.State);
            Assert.AreEqual(2, _sim.Combat.Combo.Step);
            _sim.Run(0.3f);
            Assert.AreEqual(18, dummy.Current);
        }

        [Test]
        public void ThirdPressStartsANewCombo()
        {
            _sim.PressAttack();
            _sim.Run(0.3f);
            _sim.PressAttack();
            _sim.Run(0.3f);
            _sim.PressAttack();
            Assert.AreEqual(1, _sim.Combat.Combo.Step);
        }

        [Test]
        public void ComboExpiresWhenWindowIsMissed()
        {
            _sim.PressAttack();
            _sim.Run(0.3f + ComboTracker.Window + 0.1f);
            _sim.PressAttack();
            Assert.AreEqual(1, _sim.Combat.Combo.Step);
        }

        [Test]
        public void PressDuringSwingChainsIntoNextSwing()
        {
            var dummy = _sim.AddDummy(1.5f, 0.97f, 20);
            _sim.PressAttack();
            _sim.Run(0.16f); // 0.18 s into a 0.25 s swing; buffer (0.12 s) still alive when it ends
            _sim.PressAttack();
            _sim.Run(0.5f);
            Assert.AreEqual(18, dummy.Current);
        }

        [Test]
        public void LandedHitGivesEightEnergyAndMissGivesNone()
        {
            _sim.Stats.TrySpendEnergy(50f);
            _sim.PressAttack();
            _sim.Run(0.4f);
            Assert.AreEqual(50f, _sim.Stats.Energy.Current, 1e-4f);
            _sim.AddDummy(1.5f, 0.97f, 20);
            _sim.PressAttack();
            _sim.Run(0.3f);
            Assert.AreEqual(58f, _sim.Stats.Energy.Current, 1e-4f);
        }

        [Test]
        public void EnergyIsCappedAtMax()
        {
            _sim.AddDummy(1.5f, 0.97f, 20);
            _sim.PressAttack();
            _sim.Run(0.3f);
            Assert.AreEqual(100f, _sim.Stats.Energy.Current, 1e-4f);
        }

        [Test]
        public void UpSlashHitsTargetsAbove()
        {
            var above = _sim.AddDummy(0f, 2.8f, 20, size: 0.6f);
            _sim.PressAttack(Vector2.up);
            Assert.AreEqual(AttackDirection.Up, _sim.Combat.Aim);
            _sim.Run(0.3f);
            Assert.AreEqual(19, above.Current);
        }

        [Test]
        public void StateReturnsToGroundedLocomotionAfterSwing()
        {
            _sim.PressAttack();
            _sim.Run(0.3f);
            Assert.AreEqual(PlayerStateId.Idle, _sim.State);
            Assert.IsFalse(_sim.Sword.IsActive);
        }

        // ---- air attack and pogo ----

        /// <summary>Runs 2 s and returns the apex above the height where the bounce started (NaN-free: 0 if none).</summary>
        float PogoApexAboveContact(ref bool bounced)
        {
            float contactY = 0f, apex = float.MinValue, previousY = _sim.Position.y;
            for (int i = 0; i < 100; i++)
            {
                _sim.Frame();
                if (!bounced && _sim.Player.Velocity.y > 5f) { bounced = true; contactY = previousY; }
                if (bounced) apex = Mathf.Max(apex, _sim.Position.y);
                previousY = _sim.Position.y;
            }
            return bounced ? apex - contactY : 0f;
        }

        [Test]
        public void DownSlashOnEnemyPogosThreeTiles()
        {
            var dummy = _sim.AddDummy(0f, 4f, 20, size: 1f);
            _sim.Teleport(0f, 6f);
            _sim.Input.Move = Vector2.down;
            _sim.Input.AttackPressed = true;
            bool bounced = false;
            float height = PogoApexAboveContact(ref bounced);
            Assert.IsTrue(bounced, "no bounce");
            Assert.AreEqual(19, dummy.Current);
            Assert.AreEqual(3f, height, 0.3f);
        }

        [Test]
        public void DownSlashOnHazardPogosToo()
        {
            _sim.AddHazard(0f, 4f);
            _sim.Teleport(0f, 6f);
            _sim.Input.Move = Vector2.down;
            _sim.Input.AttackPressed = true;
            bool bounced = false;
            float height = PogoApexAboveContact(ref bounced);
            Assert.IsTrue(bounced, "no bounce");
            Assert.AreEqual(3f, height, 0.3f);
        }

        [Test]
        public void DownSlashOnSolidNonPogoTargetDoesNotBounce()
        {
            _sim.AddDummy(0f, 4f, 20, pogo: false, size: 1f);
            _sim.Teleport(0f, 6f);
            _sim.Input.Move = Vector2.down;
            _sim.Input.AttackPressed = true;
            bool bounced = false;
            PogoApexAboveContact(ref bounced);
            Assert.IsFalse(bounced);
        }

        [Test]
        public void DownSlashDoesNotBounceOffAllies()
        {
            _sim.AddDummy(0f, 4f, 20, team: Team.Player, size: 1f);
            _sim.Teleport(0f, 6f);
            _sim.Input.Move = Vector2.down;
            _sim.Input.AttackPressed = true;
            bool bounced = false;
            PogoApexAboveContact(ref bounced);
            Assert.IsFalse(bounced);
        }

        [Test]
        public void AirSwingUsesAirAttackStateAndReturnsToFall()
        {
            _sim.Teleport(0f, 12f);
            _sim.PressAttack();
            Assert.AreEqual(PlayerStateId.AirAttack, _sim.State);
            _sim.Run(0.3f);
            Assert.AreEqual(PlayerStateId.Fall, _sim.State);
        }

        [Test]
        public void AttackWhileHurtOrDeadIsIgnored()
        {
            _sim.EnemyHit();
            _sim.PressAttack();
            Assert.AreEqual(PlayerStateId.Hurt, _sim.State);
        }

        // ---- being hit ----

        [Test]
        public void HitCostsOneHeartAndEntersHurt()
        {
            Assert.AreEqual(HitOutcome.Damaged, _sim.EnemyHit());
            Assert.AreEqual(4, _sim.Stats.Health.Current);
            Assert.AreEqual(PlayerStateId.Hurt, _sim.State);
        }

        [Test]
        public void BossHitCanCostTwoHearts()
        {
            _sim.EnemyHit(2);
            Assert.AreEqual(3, _sim.Stats.Health.Current);
        }

        [Test]
        public void KnockbackPushesAboutThreeTilesAwayFromAttacker()
        {
            float startX = _sim.Position.x;
            _sim.EnemyHit(1, Vector2.right);
            _sim.Run(0.5f);
            Assert.AreEqual(3f, _sim.Position.x - startX, 0.3f);
            _sim.EnemyHit(1, Vector2.left); // still invulnerable, must not push
            Assert.AreEqual(4, _sim.Stats.Health.Current);
        }

        [Test]
        public void KnockbackGoesLeftWhenHitFromTheRight()
        {
            float startX = _sim.Position.x;
            _sim.EnemyHit(1, Vector2.left);
            _sim.Run(0.5f);
            Assert.AreEqual(-3f, _sim.Position.x - startX, 0.3f);
        }

        [Test]
        public void AttackedTwiceWithinOneSecondLosesOnlyOneHeart()
        {
            _sim.EnemyHit();
            _sim.Run(0.5f);
            Assert.AreEqual(HitOutcome.Absorbed, _sim.EnemyHit());
            Assert.AreEqual(4, _sim.Stats.Health.Current);
            _sim.Run(0.45f);
            Assert.AreEqual(HitOutcome.Absorbed, _sim.EnemyHit()); // 0.95 s
            Assert.AreEqual(4, _sim.Stats.Health.Current);
            _sim.Run(0.2f);
            Assert.AreEqual(HitOutcome.Damaged, _sim.EnemyHit()); // 1.15 s
            Assert.AreEqual(3, _sim.Stats.Health.Current);
        }

        [Test]
        public void HurtStateEndsAndControlReturnsWhileStillInvulnerable()
        {
            _sim.EnemyHit();
            _sim.Run(0.4f);
            Assert.AreEqual(PlayerStateId.Idle, _sim.State);
            Assert.IsTrue(_sim.Player.ExternalInvulnerable);
            Assert.IsTrue(_sim.Player.IsInvulnerable);
            _sim.Run(0.7f);
            Assert.IsFalse(_sim.Player.ExternalInvulnerable);
        }

        [Test]
        public void DashIFramesAbsorbHits()
        {
            _sim.Input.DashPressed = true;
            _sim.Frame();
            Assert.AreEqual(PlayerStateId.Dash, _sim.State);
            Assert.AreEqual(HitOutcome.Absorbed, _sim.EnemyHit());
            Assert.AreEqual(5, _sim.Stats.Health.Current);
        }

        [Test]
        public void HurtInterruptsSwingAndDisarmsTheSword()
        {
            _sim.AddDummy(1.5f, 0.97f, 20);
            _sim.PressAttack();
            _sim.Run(0.08f);
            Assert.IsTrue(_sim.Sword.IsActive);
            _sim.EnemyHit();
            Assert.AreEqual(PlayerStateId.Hurt, _sim.State);
            Assert.IsFalse(_sim.Sword.IsActive);
            Assert.AreEqual(0, _sim.Combat.Combo.Step, "combo reset on hurt");
        }

        [Test]
        public void StatsPublishEventsToTheBus()
        {
            int damagedAmount = -1, damagedHearts = -1, heartsCurrent = -1, heartsMax = -1;
            EventBus.Subscribe<PlayerDamaged>(e => { damagedAmount = e.Amount; damagedHearts = e.CurrentHearts; });
            EventBus.Subscribe<HeartsChanged>(e => { heartsCurrent = e.Current; heartsMax = e.Max; });
            _sim.EnemyHit();
            Assert.AreEqual(1, damagedAmount);
            Assert.AreEqual(4, damagedHearts);
            Assert.AreEqual(4, heartsCurrent);
            Assert.AreEqual(5, heartsMax);
        }

        [Test]
        public void EnergyEventsReachTheBus()
        {
            float current = -1f, max = -1f;
            EventBus.Subscribe<EnergyChanged>(e => { current = e.Current; max = e.Max; });
            Assert.IsTrue(_sim.Stats.TrySpendEnergy(30f));
            Assert.AreEqual(70f, current, 1e-4f);
            Assert.AreEqual(100f, max, 1e-4f);
            Assert.IsFalse(_sim.Stats.TrySpendEnergy(71f));
        }

        // ---- death, respawn, checkpoint ----

        void Kill() => _sim.EnemyHit(99);

        [Test]
        public void LethalHitEntersDeadAndPublishesPlayerDied()
        {
            int died = 0;
            EventBus.Subscribe<PlayerDied>(_ => died++);
            Kill();
            Assert.AreEqual(PlayerStateId.Dead, _sim.State);
            Assert.AreEqual(1, died);
            Assert.AreEqual(0, _sim.Stats.Health.Current);
        }

        [Test]
        public void RespawnIsRequestedAfterOnePointTwoSeconds()
        {
            Kill();
            _sim.Run(1.1f);
            Assert.AreEqual(0, _sim.RespawnCalls);
            Assert.AreEqual(PlayerStateId.Dead, _sim.State);
            _sim.Run(0.2f);
            Assert.AreEqual(1, _sim.RespawnCalls);
            _sim.Run(0.5f);
            Assert.AreEqual(1, _sim.RespawnCalls, "respawn requested once");
        }

        [Test]
        public void RespawnRestoresVitalsAndReturnsToIdleAtTheAltar()
        {
            _sim.Stats.TrySpendEnergy(80f);
            Kill();
            _sim.Run(1.5f);
            Assert.AreEqual(5, _sim.Stats.Health.Current);
            Assert.AreEqual(100f, _sim.Stats.Energy.Current, 1e-4f);
            Assert.AreEqual(_sim.RespawnPoint.x, _sim.Position.x, 0.5f);
            Assert.IsFalse(_sim.Player.IsInvulnerable);
            _sim.Run(0.5f);
            Assert.AreEqual(PlayerStateId.Idle, _sim.State);
        }

        [Test]
        public void NoDestinationKeepsLeoDownAndRetriesUntilOneExists()
        {
            _sim.RespawnSucceeds = false;
            int respawned = 0;
            EventBus.Subscribe<PlayerRespawned>(_ => respawned++);
            Kill();
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true; // each failed request logs a warning
            try
            {
                _sim.Run(3.0f);
                Assert.AreEqual(PlayerStateId.Dead, _sim.State, "never revived in place");
                Assert.AreEqual(0, respawned);
                Assert.AreEqual(0, _sim.Stats.Health.Current);
                Assert.GreaterOrEqual(_sim.RespawnCalls, 2, "asks again after each respawn delay");

                _sim.RespawnSucceeds = true;
                _sim.Run(1.5f);
            }
            finally { UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false; }
            Assert.AreEqual(1, respawned);
            Assert.AreEqual(5, _sim.Stats.Health.Current);
            Assert.AreEqual(_sim.RespawnPoint.x, _sim.Position.x, 0.5f);
        }

        [Test]
        public void DeadPlayerCannotBeHitAgainOrAttack()
        {
            Kill();
            Assert.AreEqual(HitOutcome.Ignored, _sim.EnemyHit());
            _sim.PressAttack();
            Assert.AreEqual(PlayerStateId.Dead, _sim.State);
        }

        [Test]
        public void CheckpointRefillsHeartsAndEnergy()
        {
            _sim.EnemyHit(2);
            _sim.Stats.TrySpendEnergy(60f);
            EventBus.Publish(new CheckpointReached("hub_altar_01"));
            Assert.AreEqual(5, _sim.Stats.Health.Current);
            Assert.AreEqual(100f, _sim.Stats.Energy.Current, 1e-4f);
        }

        [Test]
        public void CheckpointDoesNotReviveADeadPlayer()
        {
            Kill();
            EventBus.Publish(new CheckpointReached("hub_altar_01"));
            Assert.AreEqual(0, _sim.Stats.Health.Current);
        }

        [Test]
        public void StatsStartFromGameStateValues()
        {
            var state = GameState.NewGame();
            state.maxHearts = 7;
            state.maxEnergy = 150;
            state.swordLevel = 2;
            _sim.Stats.Initialize(PlayerStatsSeed.From(state));
            Assert.AreEqual(7, _sim.Stats.Health.Max);
            Assert.AreEqual(7, _sim.Stats.Health.Current);
            Assert.AreEqual(150f, _sim.Stats.Energy.Max);
            Assert.AreEqual(2, _sim.Stats.SwordLevel);
        }
    }
}
