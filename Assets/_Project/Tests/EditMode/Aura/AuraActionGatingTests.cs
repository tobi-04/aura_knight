using AuraKnight.Aura;
using AuraKnight.Player;
using AuraKnight.Tests.Player;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Aura
{
    /// <summary>Aura switches and skills are refused while Leo is dead, hurt (skills) or the controls are off.</summary>
    public sealed class AuraActionGatingTests : AuraTestBase
    {
        sealed class StubState : IPlayerState
        {
            public void Enter() { }
            public void Tick() { }
            public void FixedTick() { }
            public void Exit() { }
        }

        PlayerSimHarness _sim;
        AuraManager _manager;

        [SetUp]
        public void SetUp()
        {
            _sim = new PlayerSimHarness();
            _manager = Make("Manager", Vector2.zero).AddComponent<AuraManager>();
            SetRef(_manager, "controller", _sim.Player);
            _manager.Unlock(AuraId.Wind);
            _manager.Unlock(AuraId.Fire);
            _manager.State.Tick(1f);
        }

        [TearDown] public void TearDown() => _sim.Dispose();

        void Enter(PlayerStateId id)
        {
            _sim.Player.StateMachine.Register(id, new StubState());
            _sim.Player.StateMachine.TryChange(id);
        }

        [Test]
        public void SwitchWorksInNormalStates() => Assert.IsTrue(_manager.TrySwitch(AuraId.Fire));

        [Test]
        public void SwitchAndCycleAreRefusedWhileDead()
        {
            Enter(PlayerStateId.Dead);
            Assert.IsFalse(_manager.TrySwitch(AuraId.Fire));
            Assert.IsFalse(_manager.TryCycle(1));
            Assert.AreEqual(AuraId.Wind, _manager.Current);
        }

        [Test]
        public void SwitchIsAllowedWhileHurtButSkillsAreNot()
        {
            Enter(PlayerStateId.Hurt);
            Assert.AreEqual(CastResult.Blocked, _manager.TryCastSkill());
            Assert.IsTrue(_manager.TrySwitch(AuraId.Fire));
        }

        [Test]
        public void EverythingIsRefusedWhileControlsAreOff()
        {
            _sim.Player.ControlsEnabled = false;
            Assert.IsFalse(_manager.TrySwitch(AuraId.Fire));
            Assert.AreEqual(CastResult.Blocked, _manager.TryCastSkill());
            _sim.Player.ControlsEnabled = true;
            Assert.IsTrue(_manager.TrySwitch(AuraId.Fire));
        }
    }
}
