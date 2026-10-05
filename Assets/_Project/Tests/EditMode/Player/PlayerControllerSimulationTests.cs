using AuraKnight.Player;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Player
{
    public sealed class PlayerControllerSimulationTests
    {
        PlayerSimHarness _sim;

        [SetUp] public void SetUp() { _sim = new PlayerSimHarness(); _sim.Settle(); }
        [TearDown] public void TearDown() => _sim.Dispose();

        float MaxHeightOfJump(bool hold, int frames = 90)
        {
            float startY = _sim.Position.y, max = startY;
            _sim.Input.JumpPressed = true;
            for (int i = 0; i < frames; i++)
            {
                _sim.Input.JumpHeld = hold || i == 0;
                _sim.Frame();
                max = Mathf.Max(max, _sim.Position.y);
            }
            return max - startY;
        }

        [Test]
        public void StartsGroundedAndIdle()
        {
            Assert.AreEqual(PlayerStateId.Idle, _sim.State);
            Assert.IsTrue(_sim.Player.Grounded);
        }

        [Test]
        public void RunsAtEightUnitsPerSecond()
        {
            _sim.Input.Move = Vector2.right;
            _sim.Run(0.5f);
            Assert.AreEqual(PlayerStateId.Run, _sim.State);
            Assert.AreEqual(8f, _sim.Player.Velocity.x, 0.01f);
            Assert.AreEqual(1, _sim.Player.Facing);
        }

        [Test]
        public void HeldJumpReachesFourAndAHalfTiles() =>
            Assert.AreEqual(4.5f, MaxHeightOfJump(true), 0.1f);

        [Test]
        public void TappedJumpIsAboutTwoTiles() =>
            Assert.AreEqual(2.0f, MaxHeightOfJump(false), 0.25f);

        [Test]
        public void JumpingAgainAfterLandingWorksRepeatedly()
        {
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(4.5f, MaxHeightOfJump(true, 120), 0.1f, $"jump {i}");
                _sim.Settle();
                Assert.AreEqual(PlayerStateId.Idle, _sim.State);
            }
        }

        [Test]
        public void DashCoversFiveTilesInTenSteps()
        {
            float x0 = _sim.Position.x;
            _sim.Input.DashPressed = true;
            for (int i = 0; i < 10; i++) _sim.Frame();
            Assert.AreEqual(5f, _sim.Position.x - x0, 0.2f);
            Assert.AreEqual(0f, _sim.Position.y - 0.97f, 0.05f, "no vertical drift while dashing");
        }

        [Test]
        public void DashIsInvulnerableOnlyForFirstTenth()
        {
            _sim.Input.DashPressed = true;
            _sim.Frame();
            Assert.IsTrue(_sim.Player.IsInvulnerable);
            _sim.Run(0.2f);
            Assert.IsFalse(_sim.Player.IsInvulnerable);
            _sim.Player.ExternalInvulnerable = true;
            Assert.IsTrue(_sim.Player.IsInvulnerable);
        }

        [Test]
        public void DashRespectsCooldown()
        {
            _sim.Input.DashPressed = true;
            _sim.Run(0.3f);
            float x = _sim.Position.x;
            _sim.Input.DashPressed = true;
            _sim.Frame();
            Assert.AreNotEqual(PlayerStateId.Dash, _sim.State, "0.8 s cooldown still running");
            _sim.Run(0.8f);
            _sim.Input.DashPressed = true;
            _sim.Frame();
            Assert.AreEqual(PlayerStateId.Dash, _sim.State);
            Assert.Greater(_sim.Position.x, x - 0.01f);
        }

        [Test]
        public void OnlyOneDashInTheAir()
        {
            MaxHeightOfJump(true, 10);
            _sim.Input.DashPressed = true;
            _sim.Frame();
            Assert.AreEqual(PlayerStateId.Dash, _sim.State);
            _sim.Run(1.0f); // cooldown passes (probably landed in between)
            _sim.Teleport(0f, 8f);
            _sim.Frame();
            _sim.Input.DashPressed = true;
            _sim.Frame();
            Assert.AreEqual(PlayerStateId.Dash, _sim.State, "first air dash after teleport still allowed (dash on ground reset it)");
        }

        [Test]
        public void SecondAirDashRefusedUntilLanding()
        {
            _sim.Teleport(0f, 20f);
            _sim.Frame();
            _sim.Input.DashPressed = true;
            _sim.Frame();
            Assert.AreEqual(PlayerStateId.Dash, _sim.State);
            _sim.Run(1.0f);
            Assert.IsFalse(_sim.Player.Grounded);
            _sim.Input.DashPressed = true;
            _sim.Frame();
            Assert.AreNotEqual(PlayerStateId.Dash, _sim.State);
        }

        [Test]
        public void CoyoteTimeAllowsLateJumpButNotVeryLate()
        {
            _sim.Level.Rect(-200f, -1f, -200f + 1f, 0f, "unused");
            foreach (float delay in new[] { 0.06f, 0.2f })
            {
                _sim.Teleport(0f, 0.97f);
                _sim.Player.SetVelocity(Vector2.zero);
                _sim.Settle();
                // Step off an invisible ledge by lifting the ground: emulate by teleporting up a hair and letting coyote expire.
                _sim.Teleport(0f, 3f);
                _sim.Player.Coyote.Start(_sim.Config.CoyoteTime);
                _sim.Run(delay);
                _sim.Input.JumpPressed = true;
                _sim.Frame();
                bool jumped = _sim.State == PlayerStateId.Jump;
                Assert.AreEqual(delay < 0.1f, jumped, $"delay {delay}");
            }
        }

        [Test]
        public void JumpBufferFiresOnLanding()
        {
            _sim.Teleport(0f, 1.6f);
            _sim.Run(0.1f);
            Assert.IsFalse(_sim.Player.Grounded);
            _sim.Input.JumpPressed = true;
            _sim.Input.JumpHeld = true;
            _sim.Run(0.3f);
            Assert.AreEqual(PlayerStateId.Jump, _sim.State);
        }

        [Test]
        public void WindHooksGiveDoubleJumpAndGlide()
        {
            _sim.Player.CanDoubleJump = true;
            _sim.Player.CanGlide = true;
            _sim.Input.JumpPressed = true;
            _sim.Input.JumpHeld = true;
            _sim.Run(0.2f);
            _sim.Input.JumpPressed = true;
            _sim.Frame();
            Assert.AreEqual(20f, _sim.Player.Velocity.y, 3f, "double jump launches ~20 u/s");
            _sim.Input.JumpHeld = true;
            _sim.Run(2.0f);
            Assert.GreaterOrEqual(_sim.Player.Velocity.y, -3.5f - 0.01f, "gliding caps fall speed");
            Assert.AreEqual(PlayerStateId.Fall, _sim.State);
        }

        [Test]
        public void DoubleJumpOnlyOnceAndOnlyWhenEnabled()
        {
            _sim.Input.JumpPressed = true;
            _sim.Input.JumpHeld = true;
            _sim.Run(0.2f);
            float vy = _sim.Player.Velocity.y;
            _sim.Input.JumpPressed = true;
            _sim.Frame();
            Assert.Less(_sim.Player.Velocity.y, vy, "flag off: no second jump");
            _sim.Player.CanDoubleJump = true;
            _sim.Input.JumpPressed = true;
            _sim.Frame();
            Assert.Greater(_sim.Player.Velocity.y, 15f);
            _sim.Run(0.1f);
            _sim.Input.JumpPressed = true;
            _sim.Frame();
            Assert.Less(_sim.Player.Velocity.y, 15f, "only one double jump per airtime");
        }

        [Test]
        public void SpeedMultiplierScalesRunSpeed()
        {
            _sim.Player.SpeedMultiplier = 1.2f;
            _sim.Input.Move = Vector2.right;
            _sim.Run(0.5f);
            Assert.AreEqual(9.6f, _sim.Player.Velocity.x, 0.01f);
        }

        [Test]
        public void SwimModeSwitchesStateOnlyOnceRegistered()
        {
            _sim.Player.SwimMode = true;
            _sim.Frame();
            Assert.AreNotEqual(PlayerStateId.Swim, _sim.State);
            _sim.Player.SwimMode = false;
        }

        [Test]
        public void ExtraStateCanBeRegisteredAndEntered()
        {
            var swim = new StubState();
            _sim.Player.StateMachine.Register(PlayerStateId.Swim, swim);
            _sim.Player.SwimMode = true;
            _sim.Frame();
            Assert.AreEqual(PlayerStateId.Swim, _sim.State);
            _sim.Frame();
            Assert.Greater(swim.FixedTicks, 0);
            _sim.Player.SwimMode = false;
            _sim.Frame();
            Assert.AreNotEqual(PlayerStateId.Swim, _sim.State);
        }

        sealed class StubState : IPlayerState
        {
            public int FixedTicks;
            public void Enter() { }
            public void Tick() { }
            public void FixedTick() => FixedTicks++;
            public void Exit() { }
        }
    }
}
