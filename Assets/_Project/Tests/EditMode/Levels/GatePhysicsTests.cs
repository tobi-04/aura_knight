using System.Collections.Generic;
using AuraKnight.Core;
using AuraKnight.Editor;
using AuraKnight.Player;
using AuraKnight.Tests.Player;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Levels
{
    /// <summary>
    /// Plays the real PlayerController (the movement code the game runs) against the colliders of the real cave_01 room: without Wind no
    /// input sequence gets over the 6-tall smooth wall, with Wind a double jump does. A control run on an ordinary (grippable) wall shows
    /// why the SmoothWall marker exists: wall jumps would climb it.
    /// </summary>
    public sealed class GatePhysicsTests
    {
        const float Dt = PlayerSimHarness.Dt;
        PlayerSimHarness _sim;
        RoomFile _room;
        float _wallLeft, _wallRight;

        [SetUp]
        public void SetUp()
        {
            _room = LevelTestKit.Room("cave_01");
            var wall = RoomGeometry.Bounds(_room.Groups('W')[0]);
            _wallLeft = wall.xMin;
            _wallRight = wall.xMax;
        }

        [TearDown]
        public void TearDown() => _sim?.Dispose();

        /// <summary>Builds the room's terrain as colliders; the harness floor (top at y 0) is the room's floor row, so rows shift down by 1.</summary>
        void Build(bool smooth)
        {
            _sim = new PlayerSimHarness();
            var f = _room;
            foreach (var r in RoomGeometry.MergeRects(f.Width, f.Height, (x, y) => f.At(x, y) == '#'))
                _sim.Level.Rect(r.xMin, r.yMin - 1, r.xMax, r.yMax - 1, "Solid");
            foreach (var r in RoomGeometry.MergeRects(f.Width, f.Height, (x, y) => f.At(x, y) == 'W'))
            {
                var go = _sim.Level.Rect(r.xMin, r.yMin - 1, r.xMax, r.yMax - 1, "Wall");
                if (smooth) go.AddComponent<SmoothWall>();
            }
            foreach (var run in RoomTerrain.Runs(f.Cells('=')))
            {
                var go = _sim.Level.Rect(run.xMin, run.yMax - 1.4f, run.xMax, run.yMax - 1f, "OneWay");
                go.GetComponent<BoxCollider2D>().usedByEffector = true;
            }
            _sim.Level.Sync();
        }

        void Reset(float x)
        {
            _sim.Teleport(x, 0.97f);
            _sim.Settle();
            _sim.Player.SetVelocity(Vector2.zero);
        }

        float MaxY;

        /// <summary>Runs random input sequences from random spots left of the wall; returns the furthest x reached.</summary>
        float Fuzz(int episodes, float seconds, int seed)
        {
            var rng = new System.Random(seed);
            float best = 0f;
            for (int e = 0; e < episodes; e++)
            {
                Reset(2f + (float)rng.NextDouble() * (_wallLeft - 4f));
                float next = 0f;
                for (float t = 0f; t < seconds; t += Dt)
                {
                    if (t >= next)
                    {
                        next = t + 0.1f + (float)rng.NextDouble() * 0.4f;
                        double m = rng.NextDouble();
                        _sim.Input.Move = new Vector2(m < 0.15 ? -1f : m < 0.3 ? 0f : 1f, 0f);
                        _sim.Input.JumpHeld = rng.NextDouble() < 0.7;
                        if (rng.NextDouble() < 0.5) _sim.Input.JumpPressed = true;
                        if (rng.NextDouble() < 0.2) _sim.Input.DashPressed = true;
                        if (rng.NextDouble() < 0.1) _sim.Input.SlideRequested = true;
                    }
                    _sim.Frame();
                    best = Mathf.Max(best, _sim.Position.x);
                    if (best > _wallRight + 0.5f) return best;
                }
            }
            return best;
        }

        /// <summary>One scripted attempt: run right from x, jump at jumpAt, dash at dashAt (negative = never), double jump at doubleAt (negative = never).</summary>
        bool Attempt(float startX, float jumpAt, float dashAt, float doubleAt, float seconds = 4f)
        {
            Reset(startX);
            _sim.Input.Move = Vector2.right;
            bool wasHeld = false;
            for (float t = 0f; t < seconds; t += Dt)
            {
                if (Near(t, jumpAt) || Near(t, doubleAt)) _sim.Input.JumpPressed = true;
                if (Near(t, dashAt)) _sim.Input.DashPressed = true;
                bool held = (t >= jumpAt && t < jumpAt + 0.45f) || (doubleAt >= 0f && t >= doubleAt && t < doubleAt + 0.35f);
                _sim.Input.JumpHeld = held;
                if (wasHeld && !held) _sim.Input.JumpReleased = true;
                wasHeld = held;
                _sim.Frame();
                if (_sim.Position.x > _wallRight + 0.5f && _sim.Player.Grounded) return true;
            }
            return false;
        }

        static bool Near(float t, float at) => at >= 0f && t >= at - Dt * 0.5f && t < at + Dt * 0.5f;

        [Test]
        public void TheWallIsTheSixTallSmoothWallOfTheDesign()
        {
            Assert.AreEqual(6, RoomGeometry.Bounds(_room.Groups('W')[0]).height);
            Assert.IsTrue(_wallRight - _wallLeft >= 2f, "thick enough that its top is a platform, not an edge");
        }

        [Test]
        public void NoRandomInputSequenceGetsOverTheWallWithoutWind()
        {
            Build(smooth: true);
            float best = Fuzz(episodes: 70, seconds: 14f, seed: 20261008);
            Assert.Less(best, _wallRight + 0.5f, $"reached x = {best} (wall spans {_wallLeft}..{_wallRight})");
        }

        [Test]
        public void NoRunUpJumpAndDashTimingGetsOverTheWallWithoutWind()
        {
            Build(smooth: true);
            for (float startX = _wallLeft - 12f; startX < _wallLeft - 0.9f; startX += 1.5f)
                for (float jumpAt = 0f; jumpAt < 1.6f; jumpAt += 0.08f)
                    for (float dashAt = -1f; dashAt < 1.2f; dashAt += 0.14f)
                        Assert.IsFalse(Attempt(startX, jumpAt, dashAt < 0f ? -1f : jumpAt + dashAt, -1f, 3f), $"start {startX}, jump {jumpAt}, dash {dashAt}");
        }

        [Test]
        public void LeanOnTheWallAndMashJumpNeverClimbsIt()
        {
            Build(smooth: true);
            Reset(_wallLeft - 1.5f);
            MaxY = 0f;
            _sim.Input.Move = Vector2.right;
            for (float t = 0f; t < 20f; t += Dt)
            {
                if (Mathf.Repeat(t, 0.25f) < Dt) _sim.Input.JumpPressed = true;
                _sim.Input.JumpHeld = true;
                _sim.Frame();
                MaxY = Mathf.Max(MaxY, _sim.Position.y);
            }
            Assert.Less(_sim.Position.x, _wallLeft, "still on this side");
            Assert.Less(MaxY, 5.6f, "never above a normal jump (feet 4.5 + capsule half height)");
        }

        [Test]
        public void WindsDoubleJumpClearsTheWall()
        {
            Build(smooth: true);
            _sim.Player.CanDoubleJump = true;
            _sim.Player.CanGlide = true;
            bool crossed = false;
            for (float startX = _wallLeft - 8f; startX < _wallLeft - 0.9f && !crossed; startX += 0.5f)
                for (float doubleAt = 0.2f; doubleAt < 0.6f && !crossed; doubleAt += 0.04f)
                    crossed = Attempt(startX, 0.1f, -1f, 0.1f + doubleAt, 4f);
            Assert.IsTrue(crossed, "some run-up and double-jump timing lands on top of the wall and crosses it");
        }

        [Test]
        public void AnOrdinaryWallOfTheSameSizeCanBeWallJumpedWhichIsWhyTheWallIsSmooth()
        {
            Build(smooth: false);
            Reset(_wallLeft - 1.5f);
            bool crossed = false;
            for (float t = 0f; t < 25f && !crossed; t += Dt)
            {
                var state = _sim.Player.StateMachine.CurrentId;
                _sim.Input.Move = Vector2.right;
                if (state == PlayerStateId.WallSlide || state == PlayerStateId.Idle || state == PlayerStateId.Run) _sim.Input.JumpPressed = true;
                _sim.Input.JumpHeld = state == PlayerStateId.WallJump || state == PlayerStateId.Jump;
                _sim.Frame();
                crossed = _sim.Position.x > _wallLeft + 0.5f && _sim.Position.y > 7f;
            }
            Assert.IsTrue(crossed, "control: a plain wall is climbable by wall jumps, so the smooth marker matters");
        }
    }
}
