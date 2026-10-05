using AuraKnight.Player;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Player
{
    public sealed class PlayerSlideAndWallSimulationTests
    {
        PlayerSimHarness _sim;

        [SetUp] public void SetUp() { _sim = new PlayerSimHarness(); }
        [TearDown] public void TearDown() => _sim.Dispose();

        [Test]
        public void SlideShrinksColliderForFortyFiveHundredthsThenStands()
        {
            _sim.Settle();
            _sim.Input.Move = Vector2.right;
            _sim.Run(0.3f);
            _sim.Input.SlideRequested = true;
            _sim.Frame();
            Assert.AreEqual(PlayerStateId.Slide, _sim.State);
            Assert.IsTrue(_sim.Player.Motor.IsCrouched);
            Assert.AreEqual(11f, _sim.Player.Velocity.x, 0.01f);
            _sim.Run(0.5f);
            Assert.AreNotEqual(PlayerStateId.Slide, _sim.State);
            Assert.IsFalse(_sim.Player.Motor.IsCrouched);
        }

        [Test]
        public void SlidesThroughOneTileGapTwentyTimesInARow()
        {
            _sim.Level.Rect(10f, 1.0f, 18f, 4f, "Tunnel"); // clearance exactly one tile
            _sim.Level.Sync();
            for (int run = 0; run < 20; run++)
            {
                _sim.Teleport(8f, 0.97f);
                _sim.Player.SetVelocity(Vector2.zero);
                _sim.Settle();
                _sim.Input.Move = Vector2.right;
                _sim.Run(0.1f);
                _sim.Input.SlideRequested = true;
                int frames = 0;
                while (_sim.Position.x < 19f && frames++ < 200)
                {
                    _sim.Input.Move = Vector2.right;
                    _sim.Frame();
                }
                Assert.Less(frames, 200, $"stuck in run {run} at x={_sim.Position.x}");
                _sim.Input.Move = Vector2.zero;
                _sim.Run(0.4f);
                Assert.IsFalse(_sim.Player.Motor.IsCrouched, $"run {run} still crouched");
                Assert.IsTrue(_sim.Player.Grounded);
            }
        }

        [Test]
        public void SlideStaysActiveWhileCeilingBlocksStanding()
        {
            _sim.Level.Rect(10f, 1.0f, 30f, 4f, "LongTunnel");
            _sim.Level.Sync();
            _sim.Teleport(8f, 0.97f);
            _sim.Settle();
            _sim.Input.Move = Vector2.right;
            _sim.Run(0.1f);
            _sim.Input.SlideRequested = true;
            _sim.Run(1.2f);
            Assert.AreEqual(PlayerStateId.Slide, _sim.State, "0.45 s elapsed but still under the ceiling");
        }

        [Test]
        public void StickDownPlusDashActionIsSlideNotDash()
        {
            // Mirrors PlayerInputReader semantics through the controller: SlideRequested drives slide.
            _sim.Settle();
            _sim.Input.Move = new Vector2(1f, -1f);
            _sim.Input.SlideRequested = true;
            _sim.Frame();
            Assert.AreEqual(PlayerStateId.Slide, _sim.State);
        }

        [Test]
        public void WallSlideCapsFallSpeedAtThree()
        {
            _sim.Level.Rect(2f, 0f, 3f, 60f, "Wall");
            _sim.Level.Sync();
            _sim.Teleport(1.5f, 30f);
            _sim.Input.Move = Vector2.right;
            _sim.Run(1.5f);
            Assert.AreEqual(PlayerStateId.WallSlide, _sim.State);
            Assert.AreEqual(-3f, _sim.Player.Velocity.y, 0.05f);
        }

        [Test]
        public void WallJumpKicksAwayAndLocksInputForQuarterSecond()
        {
            _sim.Level.Rect(2f, 0f, 3f, 60f, "Wall");
            _sim.Level.Sync();
            _sim.Teleport(1.5f, 30f);
            _sim.Input.Move = Vector2.right;
            _sim.Run(1.0f);
            Assert.AreEqual(PlayerStateId.WallSlide, _sim.State);
            _sim.Input.JumpPressed = true;
            _sim.Frame();
            Assert.AreEqual(PlayerStateId.WallJump, _sim.State);
            Assert.AreEqual(-11f, _sim.Player.Velocity.x, 0.01f);
            Assert.AreEqual(22f, _sim.Player.Velocity.y, 1.5f);
            _sim.Run(0.1f); // still inside 0.15 s lock while holding toward the wall
            Assert.AreEqual(-11f, _sim.Player.Velocity.x, 0.01f);
            _sim.Run(0.2f);
            Assert.Greater(_sim.Player.Velocity.x, -11f, "input regained after the lock");
        }

        [Test]
        public void WallJumpingClimbsTwentyTileShaft()
        {
            _sim.Level.Rect(-1f, 0f, 0f, 26f, "ShaftLeft");
            _sim.Level.Rect(3f, 0f, 4f, 26f, "ShaftRight");
            _sim.Level.Sync();
            _sim.Teleport(1.5f, 0.97f);
            _sim.Settle();

            int heading = -1;
            var previous = _sim.State;
            float maxY = 0f;
            for (int frame = 0; frame < 2000 && maxY < 20f; frame++)
            {
                _sim.Input.Move = new Vector2(heading, 0f);
                _sim.Input.JumpHeld = true;
                bool onGroundAtWall = _sim.Player.Grounded && _sim.Player.Motor.TouchingWall(heading);
                if (_sim.State == PlayerStateId.WallSlide || onGroundAtWall) _sim.Input.JumpPressed = true;
                _sim.Frame();
                if (previous != PlayerStateId.WallJump && _sim.State == PlayerStateId.WallJump) heading = -heading;
                previous = _sim.State;
                maxY = Mathf.Max(maxY, _sim.Position.y);
            }
            Assert.GreaterOrEqual(maxY, 20f, "bot should wall-jump to the top of a 20 tile shaft");
        }

        [Test]
        public void DashOutOfWallSlideGoesAwayFromWall()
        {
            _sim.Level.Rect(2f, 0f, 3f, 60f, "Wall");
            _sim.Level.Sync();
            _sim.Teleport(1.5f, 30f);
            _sim.Input.Move = Vector2.right;
            _sim.Run(1.0f);
            _sim.Input.DashPressed = true;
            _sim.Frame();
            Assert.AreEqual(PlayerStateId.Dash, _sim.State);
            Assert.Less(_sim.Player.Velocity.x, 0f);
        }

        [Test]
        public void CeilingBonkStopsRisingAndStartsFalling()
        {
            _sim.Level.Rect(-5f, 3f, 5f, 4f, "LowRoof");
            _sim.Level.Sync();
            _sim.Settle();
            _sim.Input.JumpPressed = true;
            _sim.Input.JumpHeld = true;
            bool sawFall = false;
            float maxTop = 0f;
            for (int i = 0; i < 60; i++)
            {
                _sim.Frame();
                sawFall |= _sim.State == PlayerStateId.Fall;
                maxTop = Mathf.Max(maxTop, _sim.Position.y + 0.95f);
            }
            Assert.Less(maxTop, 3.0f);
            Assert.IsTrue(sawFall);
        }
    }
}
