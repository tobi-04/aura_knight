using AuraKnight.Player;
using AuraKnight.Player.States;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Combat
{
    /// <summary>Pause / Dead / Hurt gating of actions, and sword swings while swimming.</summary>
    public sealed class PlayerActionGatingTests
    {
        CombatSimHarness _sim;

        [SetUp] public void SetUp() => _sim = new CombatSimHarness();

        [TearDown] public void TearDown() => _sim.Dispose();

        // ---- pure rules ----

        [TestCase(PlayerStateId.Idle, true, true)]
        [TestCase(PlayerStateId.Hurt, true, true)]
        [TestCase(PlayerStateId.Dead, true, false)]
        [TestCase(PlayerStateId.Idle, false, false)]
        public void AuraSwitchRules(PlayerStateId state, bool controls, bool expected) =>
            Assert.AreEqual(expected, PlayerActionRules.CanSwitchAura(state, controls));

        [TestCase(PlayerStateId.Run, true, true)]
        [TestCase(PlayerStateId.Hurt, true, false)]
        [TestCase(PlayerStateId.Dead, true, false)]
        [TestCase(PlayerStateId.Fall, false, false)]
        public void SkillRules(PlayerStateId state, bool controls, bool expected) =>
            Assert.AreEqual(expected, PlayerActionRules.CanCastSkill(state, controls));

        [TestCase(PlayerStateId.Swim, true, true)]
        [TestCase(PlayerStateId.Dash, true, false)]
        [TestCase(PlayerStateId.Hurt, true, false)]
        [TestCase(PlayerStateId.Idle, false, false)]
        public void AttackRules(PlayerStateId state, bool controls, bool expected) =>
            Assert.AreEqual(expected, PlayerActionRules.CanStartAttack(state, controls));

        [Test]
        public void SwimmingSwingsUseTheAirborneAttackEvenWhenTouchingTheBottom()
        {
            Assert.AreEqual(PlayerStateId.AirAttack, PlayerActionRules.AttackStateFor(PlayerStateId.Swim, true));
            Assert.AreEqual(PlayerStateId.Attack, PlayerActionRules.AttackStateFor(PlayerStateId.Idle, true));
            Assert.AreEqual(PlayerStateId.AirAttack, PlayerActionRules.AttackStateFor(PlayerStateId.Fall, false));
        }

        // ---- controls disabled (pause) ----

        [Test]
        public void AttackPressWhileControlsAreOffIsNotLatchedAndDoesNothing()
        {
            _sim.Player.ControlsEnabled = false;
            _sim.PressAttack();
            Assert.IsFalse(_sim.Player.AttackBuffer.IsActive);
            Assert.AreEqual(PlayerStateId.Idle, _sim.State);
            _sim.Player.ControlsEnabled = true;
            _sim.Run(0.2f);
            Assert.AreEqual(PlayerStateId.Idle, _sim.State, "the press made during the pause must not fire after it");
        }

        [Test]
        public void PressLatchedJustBeforeThePauseIsDroppedWhenControlsTurnOff()
        {
            _sim.Input.AttackPressed = true;
            _sim.Player.ReadInput();
            Assert.IsTrue(_sim.Player.AttackBuffer.IsActive);
            _sim.Player.ControlsEnabled = false;
            _sim.Player.ReadInput();
            Assert.IsFalse(_sim.Player.AttackBuffer.IsActive);
        }

        // ---- swimming ----

        void EnterWater()
        {
            _sim.Player.StateMachine.Register(PlayerStateId.Swim, new SwimState(_sim.Player));
            _sim.Teleport(0f, 6f);
            _sim.Player.SwimMode = true;
            _sim.Run(0.1f);
            Assert.AreEqual(PlayerStateId.Swim, _sim.State);
        }

        [Test]
        public void SwordSwingsWhileSwimmingKeepSwimPhysicsAndReturnToSwim()
        {
            EnterWater();
            float y = _sim.Position.y;
            _sim.PressAttack();
            Assert.AreEqual(PlayerStateId.AirAttack, _sim.State);
            _sim.Run(0.1f);
            Assert.AreEqual(PlayerStateId.AirAttack, _sim.State, "water must not pull Leo out of the swing");
            Assert.AreEqual(y, _sim.Position.y, 0.05f, "no gravity during a swim swing");
            _sim.Run(0.3f);
            Assert.AreEqual(PlayerStateId.Swim, _sim.State);
        }

        [Test]
        public void SwimSwingDamagesTargetsInReach()
        {
            EnterWater();
            var dummy = _sim.AddDummy(1.5f, _sim.Position.y, 20, pogo: false);
            _sim.PressAttack();
            _sim.Run(0.3f);
            Assert.Less(dummy.Current, 20);
        }

        [Test]
        public void LeavingTheWaterAfterTheSwingFallsNormally()
        {
            EnterWater();
            _sim.PressAttack();
            _sim.Player.SwimMode = false;
            _sim.Run(0.4f);
            Assert.AreNotEqual(PlayerStateId.Swim, _sim.State);
        }

        [Test]
        public void StickDoesNotSteerWhileControlsAreOff()
        {
            _sim.Input.Move = Vector2.right;
            _sim.Player.ControlsEnabled = false;
            float x = _sim.Position.x;
            _sim.Run(0.5f);
            Assert.AreEqual(x, _sim.Position.x, 0.05f);
            _sim.Player.ControlsEnabled = true;
            _sim.Run(0.5f);
            Assert.Greater(_sim.Position.x, x + 1f);
        }

        [Test]
        public void SwimSteeringIsIgnoredWhileControlsAreOff()
        {
            EnterWater();
            _sim.Player.ControlsEnabled = false;
            _sim.Input.Move = Vector2.right;
            float x = _sim.Position.x;
            _sim.Run(0.5f);
            Assert.AreEqual(x, _sim.Position.x, 0.05f);
        }

        [Test]
        public void DeadLeoHoldsStillOnceTheRespawnIsUnderWay()
        {
            _sim.Combat.RespawnHandler = () => true; // "started", but the arrival never comes (region still loading)
            _sim.Teleport(0f, 8f);
            _sim.EnemyHit(99);
            _sim.Run(1.2f); // falls under gravity until the request
            _sim.Run(0.1f);
            float held = _sim.Position.y;
            _sim.Run(0.5f);
            Assert.AreEqual(held, _sim.Position.y, 0.01f, "no falling into the void while waiting for the destination");
            Assert.AreEqual(PlayerStateId.Dead, _sim.State);
        }
    }
}
