using System.Collections.Generic;
using AuraKnight.Editor;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Levels
{
    /// <summary>The grid solver on tiny rooms: what each ability opens, what the comfortable (Safe) model allows and what the physical (Max) model still refuses.</summary>
    public sealed class LevelReachabilityTests
    {
        static readonly Vector2Int Start = new Vector2Int(1, 1);
        const LevelAbilities Auras = LevelAbilities.Wind | LevelAbilities.Fire | LevelAbilities.Water | LevelAbilities.Seals;

        static HashSet<Vector2Int> Reach(RoomFile room, LevelAbilities ab = LevelAbilities.None, ReachMode mode = ReachMode.Safe) =>
            LevelReachability.Reach(room, Start, ab, mode);

        /// <summary>An empty hall (floor and ceiling rows plus <paramref name="height"/> free rows) whose free rows read "#" + middle + "#".</summary>
        static RoomFile Hall(string middle, int height = 7, string header = "")
        {
            var rows = new List<string> { LevelTestKit.Repeat('#', middle.Length + 2) };
            for (int i = 0; i < height; i++) rows.Add("#" + middle + "#");
            rows.Add(LevelTestKit.Repeat('#', middle.Length + 2));
            return LevelTestKit.MiniWith(header, rows.ToArray());
        }

        /// <summary>A hall whose right part is a block <paramref name="ledge"/> cells tall; its top is the standing cell (5, ledge + 1).</summary>
        static RoomFile LedgeHall(int ledge)
        {
            int total = ledge + 5;
            var rows = new List<string>();
            for (int y = total - 1; y >= 0; y--)
                rows.Add(y == 0 || y == total - 1 ? "#########" : y <= ledge ? "#....####" : "#.......#");
            return LevelTestKit.Mini(rows.ToArray());
        }

        /// <summary>A hall with a kill-zone pit <paramref name="width"/> wide starting at x = 3; the far side standing cell is (width + 4, 1).</summary>
        static RoomFile PitHall(int width)
        {
            int w = 24;
            string pit = "#.." + LevelTestKit.Repeat('K', width) + LevelTestKit.Repeat('.', w - 4 - width) + "#";
            string open = "#" + LevelTestKit.Repeat('.', w - 2) + "#";
            return LevelTestKit.Mini(LevelTestKit.Repeat('#', w), open, open, pit, LevelTestKit.Repeat('#', w));
        }

        [Test]
        public void WalkingAlongTheFloorReachesBothEnds()
        {
            var room = Hall("........", 2);
            Assert.IsTrue(Reach(room).Contains(new Vector2Int(8, 1)));
            Assert.IsFalse(Reach(room).Contains(new Vector2Int(0, 1)), "the wall is not a standing cell");
        }

        [Test]
        public void AStandingCellNeedsTwoFreeCellsAndASupport()
        {
            var room = LevelTestKit.Mini("#####", "#.#.#", "#...#", "#####");
            Assert.IsTrue(LevelReachability.IsStandable(room, 1, 1, LevelAbilities.None));
            Assert.IsFalse(LevelReachability.IsStandable(room, 2, 1, LevelAbilities.None), "wall");
            Assert.IsFalse(LevelReachability.IsStandable(room, 3, 2, LevelAbilities.None), "head would be in the ceiling");
            Assert.IsFalse(LevelReachability.IsStandable(room, 2, 2, LevelAbilities.None), "nothing underneath");
        }

        [Test]
        public void ALedgeThreeHighIsAComfortableJumpButFourIsNot()
        {
            Assert.IsTrue(Reach(LedgeHall(3)).Contains(new Vector2Int(5, 4)));
            Assert.IsFalse(Reach(LedgeHall(4)).Contains(new Vector2Int(5, 5)));
        }

        [Test]
        public void ThePhysicalModelStillReachesFourUpButNeverFiveOrSix()
        {
            Assert.IsTrue(Reach(LedgeHall(4), LevelAbilities.None, ReachMode.Max).Contains(new Vector2Int(5, 5)));
            Assert.IsFalse(Reach(LedgeHall(5), LevelAbilities.None, ReachMode.Max).Contains(new Vector2Int(5, 6)));
            Assert.IsFalse(Reach(LedgeHall(6), LevelAbilities.None, ReachMode.Max).Contains(new Vector2Int(5, 7)));
        }

        [Test]
        public void ADoubleJumpClearsASixTallLedge()
        {
            Assert.IsTrue(Reach(LedgeHall(6), LevelAbilities.Wind).Contains(new Vector2Int(5, 7)));
            Assert.IsFalse(Reach(LedgeHall(6), Auras & ~LevelAbilities.Wind, ReachMode.Max).Contains(new Vector2Int(5, 7)));
        }

        [TestCase(2, true, true)]
        [TestCase(3, true, true)]
        [TestCase(4, false, true)]
        [TestCase(9, false, true)]
        [TestCase(10, false, false)]
        public void GapsCrossAccordingToTheMovementModel(int gap, bool comfortable, bool physical)
        {
            var far = new Vector2Int(gap + 4, 1);
            Assert.AreEqual(comfortable, Reach(PitHall(gap)).Contains(far), "Safe model");
            Assert.AreEqual(physical, Reach(PitHall(gap), LevelAbilities.None, ReachMode.Max).Contains(far), "Max model");
        }

        [Test]
        public void SpikesForbidWalkingButNotJumpingOverThem()
        {
            var room = LevelTestKit.Mini("############", "#..........#", "#..........#", "#...^^^....#", "############");
            var reach = Reach(room);
            Assert.IsTrue(reach.Contains(new Vector2Int(9, 1)), "jumped over 3 spikes");
            Assert.IsFalse(reach.Contains(new Vector2Int(5, 1)), "never stands in spikes");
        }

        [Test]
        public void AcidAndKillZonesAreNeverStoodIn()
        {
            var acid = LevelTestKit.Mini("############", "#..........#", "#..........#", "#...aaa....#", "############");
            Assert.IsFalse(LevelReachability.IsStandable(acid, 5, 1, LevelAbilities.All));
        }

        [Test]
        public void OneWayPlatformsAreStoodOnFromAboveAndPassedFromBelow()
        {
            var room = LevelTestKit.Mini("##########", "#........#", "#........#", "#........#", "#........#", "#..===...#", "#........#", "##########");
            var reach = Reach(room);
            Assert.IsTrue(reach.Contains(new Vector2Int(4, 3)), "stand on it");
            Assert.IsTrue(reach.Contains(new Vector2Int(4, 1)), "walk beneath it");
        }

        [TestCase("...ZZ...")]
        [TestCase("...TT...")]
        public void ThornsAndBarricadesNeedFire(string middle)
        {
            var room = Hall(middle, 7, $"ids: {middle[3]}=gate_a");
            var goal = new Vector2Int(8, 1);
            Assert.IsFalse(Reach(room, Auras & ~LevelAbilities.Fire, ReachMode.Max).Contains(goal));
            Assert.IsTrue(Reach(room, LevelAbilities.Fire).Contains(goal));
        }

        [Test]
        public void TheSealGateNeedsTheSeals()
        {
            var room = Hall("...GG...", 7, "ids: G=gate_a");
            var goal = new Vector2Int(8, 1);
            Assert.IsFalse(Reach(room, Auras & ~LevelAbilities.Seals, ReachMode.Max).Contains(goal));
            Assert.IsTrue(Reach(room, LevelAbilities.Seals).Contains(goal));
        }

        [Test]
        public void FireTrapsNeedWater()
        {
            var room = Hall("...ff...", 7, "ids: f=trap_a");
            var goal = new Vector2Int(8, 1);
            Assert.IsFalse(Reach(room, Auras & ~LevelAbilities.Water, ReachMode.Max).Contains(goal));
            Assert.IsTrue(Reach(room, LevelAbilities.Water).Contains(goal));
        }

        [Test]
        public void ShortcutDoorsAreSolidUntilOpened()
        {
            var room = Hall("...kk...", 7, "ids: k=door_a");
            var goal = new Vector2Int(8, 1);
            Assert.IsFalse(Reach(room, LevelAbilities.All & ~LevelAbilities.ShortcutOpen, ReachMode.Max).Contains(goal));
            Assert.IsTrue(Reach(room, LevelAbilities.ShortcutOpen).Contains(goal));
        }

        /// <summary>A 3-wide column of <paramref name="kind"/> eight cells tall with a one-way ledge 9 up beside it (standing cell (8, 10)).</summary>
        static RoomFile Chimney(char kind)
        {
            var rows = new List<string> { LevelTestKit.Repeat('#', 14) };
            for (int y = 13; y >= 1; y--)
            {
                var row = new char[12];
                for (int x = 0; x < 12; x++) row[x] = '.';
                if (y <= 8) for (int x = 3; x <= 5; x++) row[x - 1] = kind;
                if (y == 9) for (int x = 7; x <= 9; x++) row[x - 1] = '=';
                rows.Add("#" + new string(row) + "#");
            }
            rows.Add(LevelTestKit.Repeat('#', 14));
            return LevelTestKit.Mini(rows.ToArray());
        }

        [Test]
        public void AnUpdraftLiftsOnlyWithWind()
        {
            var room = Chimney('Y');
            var ledge = new Vector2Int(8, 10);
            Assert.IsTrue(Reach(room, LevelAbilities.Wind).Contains(ledge));
            Assert.IsFalse(Reach(room, Auras & ~LevelAbilities.Wind, ReachMode.Max).Contains(ledge));
            Assert.IsFalse(Reach(room, LevelAbilities.None, ReachMode.Max).Contains(ledge));
        }

        [Test]
        public void WaterIsSwamOnlyWithWater()
        {
            var room = Chimney('~');
            var ledge = new Vector2Int(8, 10);
            Assert.IsTrue(Reach(room, LevelAbilities.Water).Contains(ledge));
            Assert.IsFalse(Reach(room, Auras & ~LevelAbilities.Water, ReachMode.Max).Contains(ledge));
        }

        [Test]
        public void ReachesCellAcceptsTheCellOrTheOneBelow()
        {
            var states = new HashSet<Vector2Int> { new Vector2Int(2, 2) };
            Assert.IsTrue(LevelReachability.ReachesCell(states, new Vector2Int(2, 2)));
            Assert.IsTrue(LevelReachability.ReachesCell(states, new Vector2Int(2, 3)));
            Assert.IsFalse(LevelReachability.ReachesCell(states, new Vector2Int(3, 2)));
        }
    }
}
