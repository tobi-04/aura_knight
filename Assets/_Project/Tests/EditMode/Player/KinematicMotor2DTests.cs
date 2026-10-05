using AuraKnight.Player;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Player
{
    public sealed class KinematicMotor2DTests
    {
        PhysicsTestLevel _level;
        KinematicMotor2D _motor;

        [SetUp]
        public void SetUp()
        {
            _level = new PhysicsTestLevel();
            _level.Rect(-50f, -1f, 50f, 0f, "Floor");
            _motor = CreateMotor(new Vector2(0f, 1.2f));
        }

        [TearDown] public void TearDown() => _level.Dispose();

        KinematicMotor2D CreateMotor(Vector2 position)
        {
            var go = new GameObject("Motor");
            go.SetActive(false); // keep Awake from running in edit mode; we initialise explicitly
            go.transform.position = position;
            var motor = go.AddComponent<KinematicMotor2D>();
            _level.Track(go);
            _level.Sync();
            motor.Initialize();
            return motor;
        }

        [Test]
        public void FallingStopsOnFloorWithSkinGap()
        {
            var c = _motor.Move(new Vector2(0f, -5f));
            Assert.IsTrue(c.Below);
            Assert.AreEqual(0.95f + 0.02f, _motor.Position.y, 0.01f);
        }

        [Test]
        public void GroundProbeDetectsFloorAndNotAir()
        {
            _motor.Move(new Vector2(0f, -1f));
            Assert.IsTrue(_motor.CheckGround());
            _motor.Teleport(new Vector2(0f, 5f));
            Assert.IsFalse(_motor.CheckGround());
        }

        [Test]
        public void WallBlocksHorizontalMoveAndIsProbed()
        {
            _level.Rect(5f, 0f, 6f, 10f, "Wall");
            _level.Sync();
            _motor.Move(new Vector2(0f, -1f));
            var c = _motor.Move(new Vector2(10f, 0f));
            Assert.IsTrue(c.Right);
            Assert.AreEqual(5f - 0.4f - 0.02f, _motor.Position.x, 0.015f);
            Assert.IsTrue(_motor.TouchingWall(1));
            Assert.IsFalse(_motor.TouchingWall(-1));
        }

        [Test]
        public void CeilingStopsUpwardMove()
        {
            _level.Rect(-5f, 4f, 5f, 5f, "Ceiling");
            _level.Sync();
            var c = _motor.Move(new Vector2(0f, 10f));
            Assert.IsTrue(c.Above);
            Assert.AreEqual(4f - 0.95f - 0.02f, _motor.Position.y, 0.015f);
        }

        [Test]
        public void CornerCorrectionNudgesPlayerPastCeilingEdge()
        {
            // Ceiling edge at x = 0.3: player (half width 0.4) overlaps it by 0.1 horizontally.
            _level.Rect(0.3f, 3.5f, 10f, 5f, "LedgeCeiling");
            _level.Sync();
            _motor.CornerCorrection = 0.2f;
            var c = _motor.Move(new Vector2(0f, 4f));
            Assert.IsFalse(c.Above, "should slip past the edge instead of bonking");
            Assert.Less(_motor.Position.x, 0f);
            Assert.Greater(_motor.Position.y, 3.5f);
        }

        [Test]
        public void CornerCorrectionRespectsLimit()
        {
            _level.Rect(-0.5f, 3.5f, 10f, 5f, "WideCeiling");
            _level.Sync();
            _motor.CornerCorrection = 0.2f;
            var c = _motor.Move(new Vector2(0f, 4f));
            Assert.IsTrue(c.Above);
        }

        [Test]
        public void OneWayPlatformBlocksOnlyWhenFallingOntoIt()
        {
            var platform = _level.Rect(-3f, 2f, 3f, 2.4f, "OneWay");
            platform.GetComponent<BoxCollider2D>().usedByEffector = true;
            platform.AddComponent<PlatformEffector2D>();
            _level.Sync();
            _motor.Teleport(new Vector2(0f, 1.5f));
            var up = _motor.Move(new Vector2(0f, 3f));
            Assert.IsFalse(up.Above, "passes through from below");
            _motor.Teleport(new Vector2(0f, 6f));
            var down = _motor.Move(new Vector2(0f, -10f));
            Assert.IsTrue(down.Below);
            Assert.AreEqual(2.4f + 0.95f + 0.02f, _motor.Position.y, 0.015f);
        }

        [Test]
        public void CrouchShrinksColliderKeepingFeetAndCanStandReportsCeiling()
        {
            _level.Rect(-5f, 1f, 5f, 3f, "LowCeiling"); // clearance exactly one tile
            _level.Sync();
            _motor.Move(new Vector2(0f, -1f));
            float feetBefore = _motor.Position.y - 0.95f;
            _motor.SetCrouched(true);
            Assert.IsTrue(_motor.IsCrouched);
            Assert.IsFalse(_motor.CanStand());
            Assert.IsFalse(_motor.TrySetStanding());
            Assert.IsTrue(_motor.IsCrouched);
            var c = _motor.Move(new Vector2(0f, 1f));
            Assert.IsTrue(c.Above);
            Assert.AreEqual(feetBefore, _motor.Position.y - 0.95f, 0.1f);
        }

        [Test]
        public void StandingUpSucceedsWhenClear()
        {
            _motor.Move(new Vector2(0f, -1f));
            _motor.SetCrouched(true);
            Assert.IsTrue(_motor.CanStand());
            Assert.IsTrue(_motor.TrySetStanding());
            Assert.IsFalse(_motor.IsCrouched);
        }
    }
}
