using AuraKnight.Combat;
using AuraKnight.Enemies;
using AuraKnight.Enemies.Modifiers;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Enemies
{
    public sealed class EnemyStateMachineTests
    {
        [Test]
        public void StartsInPatrolWithZeroTime()
        {
            var machine = new EnemyStateMachine();
            Assert.AreEqual(EnemyState.Patrol, machine.Current);
            Assert.AreEqual(0f, machine.TimeInState);
        }

        [Test]
        public void FollowsTheDesignedLoopAndTracksTime()
        {
            var machine = new EnemyStateMachine();
            Assert.IsTrue(machine.TryEnter(EnemyState.Detect));
            machine.Tick(0.4f);
            Assert.AreEqual(0.4f, machine.TimeInState, 1e-5f);
            Assert.IsTrue(machine.TryEnter(EnemyState.Attack));
            Assert.AreEqual(0f, machine.TimeInState);
            Assert.IsTrue(machine.TryEnter(EnemyState.Cooldown));
            Assert.IsTrue(machine.TryEnter(EnemyState.Patrol));
            Assert.AreEqual(EnemyState.Cooldown, machine.Previous);
        }

        [TestCase(EnemyState.Patrol, EnemyState.Attack)]
        [TestCase(EnemyState.Patrol, EnemyState.Cooldown)]
        [TestCase(EnemyState.Detect, EnemyState.Cooldown)]
        [TestCase(EnemyState.Attack, EnemyState.Patrol)]
        public void RejectsIllegalTransitions(EnemyState from, EnemyState to)
        {
            var machine = new EnemyStateMachine();
            machine.Reset(from);
            Assert.IsFalse(machine.TryEnter(to));
            Assert.AreEqual(from, machine.Current);
        }

        [TestCase(EnemyState.Patrol)]
        [TestCase(EnemyState.Detect)]
        [TestCase(EnemyState.Attack)]
        [TestCase(EnemyState.Cooldown)]
        [TestCase(EnemyState.Hurt)]
        public void HurtAndDeadInterruptAnyLiveState(EnemyState from)
        {
            var machine = new EnemyStateMachine();
            machine.Reset(from);
            Assert.IsTrue(machine.TryEnter(EnemyState.Hurt));
            Assert.IsTrue(machine.TryEnter(EnemyState.Dead));
        }

        [Test]
        public void DeadIsTerminalUntilReset()
        {
            var machine = new EnemyStateMachine();
            machine.TryEnter(EnemyState.Dead);
            foreach (EnemyState state in System.Enum.GetValues(typeof(EnemyState)))
                if (state != EnemyState.Dead) Assert.IsFalse(machine.TryEnter(state), state.ToString());
            machine.Reset();
            Assert.AreEqual(EnemyState.Patrol, machine.Current);
            Assert.IsFalse(machine.IsDead);
        }

        [Test]
        public void RepeatedHurtRestartsTheTimerButOtherSameStateEntriesDoNot()
        {
            var machine = new EnemyStateMachine();
            machine.TryEnter(EnemyState.Hurt);
            machine.Tick(0.2f);
            Assert.IsTrue(machine.TryEnter(EnemyState.Hurt));
            Assert.AreEqual(0f, machine.TimeInState);
            machine.Reset(EnemyState.Detect);
            machine.Tick(0.2f);
            machine.TryEnter(EnemyState.Detect);
            Assert.AreEqual(0.2f, machine.TimeInState, 1e-5f);
        }

        [Test]
        public void ChangedEventReportsFromAndTo()
        {
            var machine = new EnemyStateMachine();
            EnemyState from = EnemyState.Dead, to = EnemyState.Dead;
            machine.Changed += (a, b) => { from = a; to = b; };
            machine.TryEnter(EnemyState.Detect);
            Assert.AreEqual((EnemyState.Patrol, EnemyState.Detect), (from, to));
        }
    }

    public sealed class PatrolRulesTests
    {
        [Test]
        public void TurnsAtLedgeAndWall()
        {
            Assert.AreEqual(-1, PatrolRules.NextFacing(1, 0f, -5f, 5f, groundAhead: false, wallAhead: false));
            Assert.AreEqual(1, PatrolRules.NextFacing(-1, 0f, -5f, 5f, groundAhead: true, wallAhead: true));
        }

        [Test]
        public void TurnsAtThePatrolPointsOnlyWhenMovingTowardThem()
        {
            Assert.AreEqual(-1, PatrolRules.NextFacing(1, 5.1f, -5f, 5f, true, false));
            Assert.AreEqual(1, PatrolRules.NextFacing(-1, -5.1f, -5f, 5f, true, false));
            Assert.AreEqual(-1, PatrolRules.NextFacing(-1, 5.1f, -5f, 5f, true, false), "already heading back");
        }

        [Test]
        public void KeepsWalkingOnOpenGround()
        {
            Assert.AreEqual(1, PatrolRules.NextFacing(1, 0f, -5f, 5f, true, false));
        }

        [Test]
        public void ChargeStopsInsteadOfWalkingOffALedge()
        {
            Assert.IsTrue(PatrolRules.ChargeBlocked(groundAhead: false, wallAhead: false));
            Assert.IsTrue(PatrolRules.ChargeBlocked(true, true));
            Assert.IsFalse(PatrolRules.ChargeBlocked(true, false));
        }
    }

    public sealed class EnemyDetectionTests
    {
        [Test]
        public void SeesPlayerWithinRangeAndVerticalTolerance()
        {
            Assert.IsTrue(EnemyDetection.InRange(new Vector2(5.9f, 1f), 6f, 2.5f));
            Assert.IsFalse(EnemyDetection.InRange(new Vector2(6.1f, 0f), 6f, 2.5f));
            Assert.IsFalse(EnemyDetection.InRange(new Vector2(2f, 3f), 6f, 2.5f), "different floor");
            Assert.IsTrue(EnemyDetection.InRange(new Vector2(2f, 3f), 6f, 0f), "tolerance 0 = pure distance");
        }

        [Test]
        public void FacingTowardFallsBackWhenAligned()
        {
            Assert.AreEqual(1, EnemyDetection.FacingToward(2f, -1));
            Assert.AreEqual(-1, EnemyDetection.FacingToward(-2f, 1));
            Assert.AreEqual(-1, EnemyDetection.FacingToward(0.01f, -1));
        }
    }

    public sealed class FakeRandom : IRandomSource
    {
        readonly float[] _values;
        int _next;
        public FakeRandom(params float[] values) { _values = values; }
        public float Value() => _values[_next++ % _values.Length];
        public int Range(int minInclusive, int maxExclusive) =>
            minInclusive + Mathf.Min(maxExclusive - minInclusive - 1, Mathf.FloorToInt(Value() * (maxExclusive - minInclusive)));
    }

    public sealed class DropTableTests
    {
        [Test]
        public void CoinsStayInsideTheInclusiveRange()
        {
            var table = new DropTable(3, 6);
            Assert.AreEqual(3, table.Roll(new FakeRandom(0f, 0.5f)).Coins);
            Assert.AreEqual(6, table.Roll(new FakeRandom(0.999f, 0.5f)).Coins);
        }

        [Test]
        public void LightDropIsTenPercent()
        {
            var table = new DropTable(3, 4);
            Assert.IsTrue(table.Roll(new FakeRandom(0f, 0.099f)).LightDrop);
            Assert.IsFalse(table.Roll(new FakeRandom(0f, 0.10f)).LightDrop);
        }

        [Test]
        public void FixedAmountAndInvertedRangeAreSafe()
        {
            Assert.AreEqual(3, new DropTable(3, 3).Roll(new FakeRandom(0.9f, 0.9f)).Coins);
            var inverted = new DropTable(5, 2);
            Assert.AreEqual(5, inverted.CoinsMin);
            Assert.AreEqual(5, inverted.CoinsMax);
            Assert.AreEqual(0, new DropTable(-4, -1).Roll(new FakeRandom(0.5f, 0.5f)).Coins);
        }

        static float[] NextValues(System.Random rng, int count)
        {
            var values = new float[count];
            for (int i = 0; i < count; i++) values[i] = (float)rng.NextDouble();
            return values;
        }

        [Test]
        public void ManyRollsHonourTheBoundsAndAverageOut()
        {
            var table = new DropTable(4, 6);
            var rng = new System.Random(7);
            var source = new FakeRandom(NextValues(rng, 200));
            int sum = 0, light = 0;
            for (int i = 0; i < 100; i++)
            {
                var drop = table.Roll(source);
                Assert.That(drop.Coins, Is.InRange(4, 6));
                sum += drop.Coins;
                if (drop.LightDrop) light++;
            }
            Assert.That(sum / 100f, Is.InRange(4.5f, 5.5f));
            Assert.That(light, Is.InRange(1, 25));
        }
    }

    public sealed class HopArcTests
    {
        [Test]
        public void LandsAtTheRequestedDistanceWhenSpeedAllows()
        {
            var v = HopArc.Launch(dx: 3f, height: 1.5f, gravity: 20f, maxHorizontalSpeed: 10f);
            Assert.AreEqual(Mathf.Sqrt(2f * 20f * 1.5f), v.y, 1e-4f);
            Assert.AreEqual(3f, v.x * HopArc.AirTime(v.y, 20f), 1e-3f);
        }

        [Test]
        public void ClampsHorizontalSpeedAndKeepsDirection()
        {
            var v = HopArc.Launch(-40f, 1f, 20f, 4f);
            Assert.AreEqual(-4f, v.x, 1e-4f);
            Assert.Greater(v.y, 0f);
        }

        [Test]
        public void ZeroHeightOrGravityGivesNoHop()
        {
            Assert.AreEqual(Vector2.zero, HopArc.Launch(2f, 0f, 20f, 4f));
            Assert.AreEqual(Vector2.zero, HopArc.Launch(2f, 1f, 0f, 4f));
        }

        [Test]
        public void ApexMatchesTheRequestedHeight()
        {
            var v = HopArc.Launch(2f, 1.2f, 25f, 6f);
            Assert.AreEqual(1.2f, v.y * v.y / (2f * 25f), 1e-4f);
        }
    }

    public sealed class DiveRulesTests
    {
        [Test]
        public void DirectionPointsAtTheTargetAndIsDiagonalFromAbove()
        {
            var dir = DiveRules.Direction(new Vector2(0f, 4f), new Vector2(3f, 0f));
            Assert.AreEqual(1f, dir.magnitude, 1e-4f);
            Assert.Greater(dir.x, 0f);
            Assert.Less(dir.y, 0f);
        }

        [Test]
        public void DirectionNeverCollapsesToStraightDownOrZero()
        {
            Assert.AreEqual(1f, DiveRules.Direction(Vector2.zero, Vector2.zero).magnitude, 1e-4f);
            var steep = DiveRules.Direction(new Vector2(0f, 5f), new Vector2(0f, 0f));
            Assert.AreEqual(1f, steep.magnitude, 1e-4f);
            Assert.Greater(Mathf.Abs(steep.x), 0.2f, "keeps a diagonal component");
        }

        [Test]
        public void DiveEndsOnTimeDistanceOrObstacle()
        {
            Assert.IsFalse(DiveRules.Ended(0.2f, 1f, 1f, 8f, false));
            Assert.IsTrue(DiveRules.Ended(1.0f, 1f, 1f, 8f, false));
            Assert.IsTrue(DiveRules.Ended(0.2f, 8f, 1f, 8f, false));
            Assert.IsTrue(DiveRules.Ended(0.2f, 1f, 1f, 8f, true));
        }
    }

    public sealed class ZapCycleTests
    {
        [Test]
        public void TelegraphsThenFiresEveryInterval()
        {
            var cycle = new ZapCycle(3f, 0.6f, 0.2f);
            Assert.AreEqual(ZapPhase.Idle, cycle.Phase);
            cycle.Tick(2.1f);
            Assert.AreEqual(ZapPhase.Idle, cycle.Phase);
            cycle.Tick(0.2f);
            Assert.AreEqual(ZapPhase.Telegraph, cycle.Phase);
            cycle.Tick(0.6f);
            Assert.AreEqual(ZapPhase.Fire, cycle.Phase);
            cycle.Tick(0.15f);
            Assert.AreEqual(ZapPhase.Idle, cycle.Phase, "the next cycle starts exactly 3 s after the last");
        }

        [Test]
        public void FullCycleLastsExactlyTheInterval()
        {
            var cycle = new ZapCycle(3f, 0.6f, 0.2f);
            int fires = 0;
            cycle.Fired += () => fires++;
            for (int i = 0; i < 600; i++) cycle.Tick(0.05f); // 30 s
            Assert.AreEqual(10, fires);
        }

        [Test]
        public void ResetStartsAFreshCycle()
        {
            var cycle = new ZapCycle(3f, 0.6f, 0.2f);
            cycle.Tick(2.7f);
            cycle.Reset();
            Assert.AreEqual(ZapPhase.Idle, cycle.Phase);
            cycle.Tick(2.1f);
            Assert.AreEqual(ZapPhase.Idle, cycle.Phase);
        }
    }

    public sealed class WaypointPathTests
    {
        static readonly Vector2[] Square = { new Vector2(0, 0), new Vector2(4, 0), new Vector2(4, 4) };

        [Test]
        public void WalksTheLegsInOrderAtConstantSpeed()
        {
            var path = new WaypointPath(Square, loop: false);
            var pos = path.Advance(new Vector2(0, 0), 2f, 1f);
            Assert.AreEqual(new Vector2(2, 0), pos);
            pos = path.Advance(pos, 2f, 1.5f); // 2 to the corner, 1 up
            Assert.AreEqual(4f, pos.x, 1e-4f);
            Assert.AreEqual(1f, pos.y, 1e-4f);
        }

        [Test]
        public void PingPongsAtTheEndWhenNotLooping()
        {
            var path = new WaypointPath(Square, loop: false);
            var pos = path.Advance(Vector2.zero, 8f, 1f); // exactly to the last point
            Assert.AreEqual(new Vector2(4, 4), pos);
            Assert.AreEqual(1, path.TargetIndex, "heading back toward the corner");
            pos = path.Advance(pos, 1f, 1f);
            Assert.AreEqual(4f, pos.x, 1e-4f);
            Assert.AreEqual(3f, pos.y, 1e-4f);
        }

        [Test]
        public void LoopsBackToTheFirstPoint()
        {
            var path = new WaypointPath(Square, loop: true);
            var pos = path.Advance(Vector2.zero, 8f, 1f);
            Assert.AreEqual(new Vector2(4, 4), pos);
            Assert.AreEqual(0, path.TargetIndex);
            pos = path.Advance(pos, 1f, 1f);
            Assert.Less(pos.x, 4f);
            Assert.Less(pos.y, 4f);
        }

        [Test]
        public void NeverOvershootsWithHugeStepsAndHandlesDegeneratePaths()
        {
            var path = new WaypointPath(Square, loop: true);
            var pos = path.Advance(Vector2.zero, 5f, 100f);
            Assert.That(pos.x, Is.InRange(-0.01f, 4.01f));
            Assert.That(pos.y, Is.InRange(-0.01f, 4.01f));
            var single = new WaypointPath(new[] { new Vector2(1, 1) }, true);
            Assert.AreEqual(new Vector2(1, 1), single.Advance(new Vector2(5, 5), 100f, 1f), "a lone waypoint is walked to, then held");
            var empty = new WaypointPath(new Vector2[0], true);
            Assert.AreEqual(new Vector2(2, 2), empty.Advance(new Vector2(2, 2), 1f, 1f));
            var coincident = new WaypointPath(new[] { Vector2.zero, Vector2.zero }, true);
            Assert.AreEqual(Vector2.zero, coincident.Advance(Vector2.zero, 1f, 100f));
        }
    }

    public sealed class ShieldRuleTests
    {
        [TestCase(1, -1f, true)]   // knight faces right, hit travelling left = from the front
        [TestCase(1, 1f, false)]   // hit travelling right = from behind
        [TestCase(-1, 1f, true)]
        [TestCase(-1, -1f, false)]
        [TestCase(1, 0f, false)]   // straight down (pogo) is not frontal
        public void BlocksOnlyHitsComingFromTheFront(int facing, float directionX, bool blocked)
        {
            var info = new DamageInfo(1, Team.Player, null, new Vector2(directionX, 0f));
            Assert.AreEqual(blocked, ShieldRule.Blocks(facing, info));
        }

        [Test]
        public void NonPlayerTeamHitsAreNotBlockedByTheRuleItself()
        {
            Assert.IsFalse(ShieldRule.Blocks(1, new DamageInfo(1, Team.Enemy, null, new Vector2(-1f, 0f))));
        }
    }

    public sealed class EnemyLimitsTests
    {
        [Test]
        public void SixIsTheRoomCap()
        {
            Assert.AreEqual(6, EnemyLimits.MaxActivePerRoom);
            Assert.IsTrue(EnemyLimits.IsWithinLimit(6));
            Assert.IsFalse(EnemyLimits.IsWithinLimit(7));
        }
    }
}
