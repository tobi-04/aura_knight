using AuraKnight.Aura;
using AuraKnight.Player;
using AuraKnight.Player.States;
using AuraKnight.Tests.Player;
using AuraKnight.World;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Aura
{
    /// <summary>Real PlayerController + Physics2D simulations with the Aura passives applied through the binder.</summary>
    public sealed class AuraSimulationTests
    {
        static readonly AuraPassives None = AuraPassives.None;
        static readonly AuraPassives Wind = new AuraPassives { speedMultiplier = 1f, doubleJump = true, glide = true };
        static readonly AuraPassives Water = new AuraPassives { speedMultiplier = 1f, swim = true, acidImmune = true };

        PlayerSimHarness _h;
        PlayerAuraBinder _binder;

        [SetUp]
        public void SetUp()
        {
            _h = new PlayerSimHarness();
            _binder = _h.Player.gameObject.AddComponent<PlayerAuraBinder>();
            _binder.Bind(_h.Player);
            _h.Settle();
        }

        [TearDown]
        public void TearDown() => _h.Dispose();

        /// <summary>Holds jump, presses it again at the apex, returns the highest centre height gained.</summary>
        float JumpTwiceAndMeasure()
        {
            float startY = _h.Position.y;
            float maxY = startY;
            _h.Input.JumpHeld = true;
            _h.Input.JumpPressed = true;
            _h.Frame();
            bool second = false;
            for (int i = 0; i < 150; i++)
            {
                if (!second && _h.Player.Velocity.y <= 0f)
                {
                    _h.Input.JumpPressed = true;
                    second = true;
                }
                _h.Frame();
                maxY = Mathf.Max(maxY, _h.Position.y);
            }
            _h.Input.JumpHeld = false;
            return maxY - startY;
        }

        [Test]
        public void WindDoubleJumpReachesAboutSevenPointTwoTiles()
        {
            _binder.SetPassives(Wind);
            Assert.AreEqual(4.5f + 2.72f, JumpTwiceAndMeasure(), 0.4f);
        }

        [Test]
        public void WithoutWindTheSecondPressDoesNothing()
        {
            _binder.SetPassives(None);
            Assert.AreEqual(4.5f, JumpTwiceAndMeasure(), 0.3f);
        }

        [Test]
        public void SwitchingAwayFromWindRemovesDoubleJump()
        {
            _binder.SetPassives(Wind);
            Assert.IsTrue(_h.Player.CanDoubleJump);
            Assert.IsTrue(_h.Player.CanGlide);
            _binder.SetPassives(None);
            Assert.IsFalse(_h.Player.CanDoubleJump);
            Assert.IsFalse(_h.Player.CanGlide);
        }

        [Test]
        public void FirePassiveRunsTwentyPercentFaster()
        {
            _binder.SetPassives(new AuraPassives { speedMultiplier = 1.2f, heatImmune = true });
            _h.Input.Move = Vector2.right;
            _h.Run(1f);
            Assert.AreEqual(9.6f, _h.Player.Velocity.x, 0.1f);
            Assert.IsTrue(_binder.HeatImmune);
        }

        [Test]
        public void WadingWithoutWaterAuraRunsAtHalfSpeedAndJumpsLower()
        {
            _binder.SetPassives(None);
            _binder.EnterWater();
            _h.Input.Move = Vector2.right;
            _h.Run(1f);
            Assert.AreEqual(4f, _h.Player.Velocity.x, 0.1f);

            _h.Input.Move = Vector2.zero;
            _h.Run(0.5f);
            float startY = _h.Position.y, maxY = startY;
            _h.Input.JumpHeld = true;
            _h.Input.JumpPressed = true;
            for (int i = 0; i < 60; i++)
            {
                _h.Frame();
                maxY = Mathf.Max(maxY, _h.Position.y);
            }
            // v0 * 0.6 = 15.4 u/s against 73.5 u/s^2 gives about 1.6 tiles
            Assert.AreEqual(1.6f, maxY - startY, 0.25f);
        }

        [Test]
        public void LeavingWaterRestoresSpeedJumpAndOxygenState()
        {
            _binder.SetPassives(None);
            _binder.EnterWater();
            _binder.EnterWater();
            _binder.ExitWater();
            Assert.IsTrue(_binder.InWater, "two overlapping volumes: still wet after one exit");
            Assert.IsTrue(_binder.Modifiers.Drowning);
            _binder.ExitWater();
            _binder.ExitWater(); // extra exit must not go negative
            Assert.IsFalse(_binder.InWater);
            Assert.AreEqual(1f, _h.Player.SpeedMultiplier);
            Assert.AreEqual(1f, _h.Player.JumpMultiplier);
            Assert.IsFalse(_binder.Modifiers.Drowning);
            _binder.EnterWater();
            Assert.IsTrue(_binder.InWater);
        }

        [Test]
        public void WaterAuraSwimsInEightDirectionsAtFullSpeedWithoutGravity()
        {
            _h.Player.StateMachine.Register(PlayerStateId.Swim, new SwimState(_h.Player));
            _h.Teleport(0f, 6f);
            _binder.SetPassives(Water);
            _binder.EnterWater();
            Assert.IsTrue(_h.Player.SwimMode);
            _h.Frame();
            Assert.AreEqual(PlayerStateId.Swim, _h.State);

            _h.Input.Move = new Vector2(0.7f, 0.7f);
            _h.Run(0.5f);
            Assert.AreEqual(5.66f, _h.Player.Velocity.x, 0.1f);
            Assert.AreEqual(5.66f, _h.Player.Velocity.y, 0.1f);

            _h.Input.Move = Vector2.zero;
            _h.Run(0.4f);
            float y = _h.Position.y;
            _h.Run(0.5f);
            Assert.AreEqual(y, _h.Position.y, 0.02f, "neutral buoyancy: no sinking");
        }

        [Test]
        public void SwimmingOutThroughTheSurfaceGivesASmallHop()
        {
            _h.Player.StateMachine.Register(PlayerStateId.Swim, new SwimState(_h.Player));
            _h.Teleport(0f, 6f);
            _binder.SetPassives(Water);
            _binder.EnterWater();
            _h.Frame();
            _h.Input.Move = Vector2.up;
            _h.Run(0.3f);

            _binder.ExitWater();
            _h.Frame();
            Assert.AreNotEqual(PlayerStateId.Swim, _h.State);
            Assert.Greater(_h.Player.Velocity.y, 8f);
        }

        [Test]
        public void WindCurrentLiftsTheRiderUpwardAgainstGravity()
        {
            _h.Teleport(0f, 3f);
            float startY = _h.Position.y;
            for (int i = 0; i < 50; i++)
            {
                WindCurrent.Lift(_h.Player, PlayerSimHarness.Dt, WindCurrent.LiftSpeed, WindCurrent.LiftAcceleration);
                _h.Frame();
            }
            Assert.Greater(_h.Position.y - startY, 5f);
            Assert.LessOrEqual(_h.Player.Velocity.y, WindCurrent.LiftSpeed + 0.01f);
        }
    }
}
