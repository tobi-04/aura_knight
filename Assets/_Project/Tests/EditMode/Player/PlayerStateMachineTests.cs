using System.Collections.Generic;
using AuraKnight.Player;
using NUnit.Framework;

namespace AuraKnight.Tests.Player
{
    public sealed class PlayerStateMachineTests
    {
        sealed class Spy : IPlayerState
        {
            readonly string _name; readonly List<string> _log;
            public Spy(string name, List<string> log) { _name = name; _log = log; }
            public void Enter() => _log.Add(_name + ":enter");
            public void Tick() => _log.Add(_name + ":tick");
            public void FixedTick() => _log.Add(_name + ":fixed");
            public void Exit() => _log.Add(_name + ":exit");
        }

        List<string> _log;
        PlayerStateMachine _machine;

        [SetUp]
        public void SetUp()
        {
            _log = new List<string>();
            _machine = new PlayerStateMachine();
            _machine.Register(PlayerStateId.Idle, new Spy("idle", _log));
            _machine.Register(PlayerStateId.Run, new Spy("run", _log));
        }

        [Test]
        public void ChangeCallsExitThenEnter()
        {
            Assert.IsTrue(_machine.TryChange(PlayerStateId.Idle));
            Assert.IsTrue(_machine.TryChange(PlayerStateId.Run));
            CollectionAssert.AreEqual(new[] { "idle:enter", "idle:exit", "run:enter" }, _log);
            Assert.AreEqual(PlayerStateId.Run, _machine.CurrentId);
            Assert.AreEqual(PlayerStateId.Idle, _machine.PreviousId);
        }

        [Test]
        public void ChangingToCurrentStateIsNoOp()
        {
            _machine.TryChange(PlayerStateId.Idle);
            _log.Clear();
            Assert.IsTrue(_machine.TryChange(PlayerStateId.Idle));
            Assert.IsEmpty(_log);
        }

        [Test]
        public void UnregisteredStateIsRejectedAndKeepsCurrent()
        {
            _machine.TryChange(PlayerStateId.Idle);
            Assert.IsFalse(_machine.TryChange(PlayerStateId.Swim));
            Assert.AreEqual(PlayerStateId.Idle, _machine.CurrentId);
        }

        [Test]
        public void ExtraStatesCanBeRegisteredLater()
        {
            _machine.TryChange(PlayerStateId.Idle);
            Assert.IsFalse(_machine.IsRegistered(PlayerStateId.Attack));
            _machine.Register(PlayerStateId.Attack, new Spy("attack", _log));
            Assert.IsTrue(_machine.TryChange(PlayerStateId.Attack));
            Assert.AreEqual(PlayerStateId.Attack, _machine.CurrentId);
        }

        [Test]
        public void RegisteringNullThrows() =>
            Assert.Throws<System.ArgumentNullException>(() => _machine.Register(PlayerStateId.Dead, null));

        [Test]
        public void StateChangedEventReportsTransition()
        {
            PlayerStateId from = PlayerStateId.Dead, to = PlayerStateId.Dead;
            _machine.TryChange(PlayerStateId.Idle);
            _machine.StateChanged += (a, b) => { from = a; to = b; };
            _machine.TryChange(PlayerStateId.Run);
            Assert.AreEqual(PlayerStateId.Idle, from);
            Assert.AreEqual(PlayerStateId.Run, to);
        }
    }
}
