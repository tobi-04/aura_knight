using System;
using System.Collections.Generic;
using System.Linq;
using AuraKnight.Editor;
using NUnit.Framework;

namespace AuraKnight.Tests.Levels
{
    /// <summary>The real room files pass every rule, and each rule fires when its room is broken on purpose.</summary>
    public sealed class LevelValidationTests
    {
        static List<string> Problems(List<RoomFile> rooms) => LevelValidator.ValidateFiles(rooms);

        static void AssertProblem(List<RoomFile> rooms, string part)
        {
            var problems = Problems(rooms);
            Assert.IsTrue(problems.Any(p => p.Contains(part)), $"expected a problem containing '{part}', got:\n{string.Join("\n", problems)}");
        }

        /// <summary>Applies <paramref name="edit"/> to the grid part of a room file (everything after the '---' line).</summary>
        static Func<string, string> Grid(Func<string, string> edit) => text =>
        {
            int cut = text.IndexOf("---\n", StringComparison.Ordinal) + 4;
            return text.Substring(0, cut) + edit(text.Substring(cut));
        };

        static string FreeRow(string content) => "#" + content.PadRight(38, '.') + "#";

        [Test]
        public void TheRealRoomFilesAreValid()
        {
            var problems = Problems(new List<RoomFile>(LevelTestKit.Catalog));
            CollectionAssert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void AFourTallCaveWallCanBeJumpedWithoutWind()
        {
            var rooms = LevelTestKit.WithEdited("cave_01", Grid(g => ReplaceFirst(ReplaceFirst(g, "WWW", "..."), "WWW", "...")));
            AssertProblem(rooms, "cave_01: 2 can be reached without Wind");
        }

        [Test]
        public void ASixTallCaveWallIsFine()
        {
            Assert.AreEqual(6, RoomGeometry.Bounds(LevelTestKit.Room("cave_01").Groups('W')[0]).height);
            CollectionAssert.IsEmpty(Problems(new List<RoomFile>(LevelTestKit.Catalog)));
        }

        [Test]
        public void APlatformBesideTheCaveWallBreaksTheGate()
        {
            var rooms = LevelTestKit.WithEdited("cave_01", Grid(g => g.Replace("=====..........WWW", "=====......====WWW")));
            AssertProblem(rooms, "cave_01: 2 can be reached without Wind");
        }

        [Test]
        public void ARoomWithoutItsBarricadeLetsTheCityIn()
        {
            var rooms = LevelTestKit.WithEdited("hub_03", text =>
                Grid(g => g.Replace("ZZ", ".."))(text.Replace(" Z=gate_hub_barricade", "")));
            AssertProblem(rooms, "hub_03: 4 can be reached without Fire");
        }

        [Test]
        public void ADoorWhoseNeighbourHasNoDoorBackIsReported()
        {
            var rooms = LevelTestKit.WithEdited("forest_02", t => t.Replace("doors: 1=forest_01 2=forest_03", "doors: 1=forest_01 2=forest_05"));
            AssertProblem(rooms, "forest_05 has no door back");
        }

        [Test]
        public void ADoorToAMissingRoomIsReported()
        {
            var rooms = LevelTestKit.WithEdited("cave_04", t => t.Replace("2=cave_05", "2=cave_99"));
            AssertProblem(rooms, "missing room cave_99");
        }

        [Test]
        public void SevenEnemiesInARoomAreReported()
        {
            var rooms = LevelTestKit.WithEdited("cave_06", Grid(g => ReplaceFirst(g, FreeRow(""), FreeRow("B.B.B.B.B.B.B.B."))));
            AssertProblem(rooms, "cave_06: more than 6 enemies");
        }

        [Test]
        public void AnEnemyStandingInMidAirIsReported()
        {
            var rooms = LevelTestKit.WithEdited("cave_02", Grid(g => ReplaceFirst(g, FreeRow(""), FreeRow("........b"))));
            AssertProblem(rooms, "needs free space above a floor");
        }

        [Test]
        public void AnEnemyNextToTheArrivalSpotIsReported()
        {
            var rooms = LevelTestKit.WithEdited("cave_02", Grid(g => SetCell(g, row: 20, x: 6, 'b')));
            AssertProblem(rooms, "cave_02: enemy 'b'");
            AssertProblem(rooms, "too close to the arrival spot");
        }

        [Test]
        public void SpikesNextToTheArrivalSpotAreReported()
        {
            var rooms = LevelTestKit.WithEdited("cave_02", Grid(g => SetCell(g, row: 20, x: 4, '^')));
            AssertProblem(rooms, "hazard '^'");
        }

        /// <summary>Replaces one cell of the grid text (row counted from the top, 0 = ceiling row).</summary>
        static string SetCell(string grid, int row, int x, char value)
        {
            var lines = grid.Split('\n');
            lines[row] = lines[row].Remove(x, 1).Insert(x, value.ToString());
            return string.Join("\n", lines);
        }

        [Test]
        public void AnEntryRoomWithoutItsShortcutDoorIsReported()
        {
            var rooms = LevelTestKit.WithEdited("cave_01", text => Grid(g => g.Replace('k', '.'))(text.Replace("ids: k=shortcut_cave\n", "")));
            AssertProblem(rooms, "cave_01: the entry room needs exactly one shortcut door");
        }

        [Test]
        public void AWallAcrossTheRoomCutsTheRoute()
        {
            var rooms = LevelTestKit.WithEdited("cave_04", Grid(g => string.Join("\n", g.Split('\n').Select(row => row.Length > 20 && row[20] == '.' ? row.Remove(20, 1).Insert(20, "#") : row))));
            AssertProblem(rooms, "cave_04: door 2 cannot be reached from door 1");
        }

        [Test]
        public void ARoomNothingLeadsToIsReported()
        {
            var rooms = new List<RoomFile>(LevelTestKit.Catalog);
            rooms.RemoveAll(r => r.Id == "city_02");
            AssertProblem(rooms, "city_03: not reachable");
            AssertProblem(rooms, "city_01 door 2 leads to missing room city_02");
        }

        static string ReplaceFirst(string text, string find, string replacement)
        {
            int i = text.IndexOf(find, StringComparison.Ordinal);
            Assert.GreaterOrEqual(i, 0, $"'{find}' not found");
            return text.Remove(i, find.Length).Insert(i, replacement);
        }
    }
}
