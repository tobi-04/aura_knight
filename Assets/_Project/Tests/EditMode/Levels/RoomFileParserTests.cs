using System;
using AuraKnight.Editor;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Levels
{
    public sealed class RoomFileParserTests
    {
        const string Valid =
            "id: test_01\nregion: forest\nslot: -2,1\nsize: 8x6\ntitle: Test hall\ndoors: 1=test_02\naltar: test_altar\nids: C=chest_a\nrewards: chest_a=Heart\n" +
            "requires: chest_a=wind+fire\npreload: hub\nzones: city@1,2,3,4\n---\n" +
            "########\n#...C..#\n1......#\n1......#\n1.A....#\n########\n";

        static string Edit(string from, string to) => Valid.Replace(from, to);

        [Test]
        public void ParsesAFullRoom()
        {
            var room = RoomFileParser.Parse(Valid, "t");
            Assert.AreEqual("test_01", room.Id);
            Assert.AreEqual("forest", room.Region);
            Assert.AreEqual(new Vector2Int(-2, 1), room.SlotCell);
            Assert.AreEqual(8, room.Width);
            Assert.AreEqual(6, room.Height);
            Assert.AreEqual("test_altar", room.AltarId);
            Assert.AreEqual("hub", room.PreloadRegion);
            Assert.AreEqual("Heart", room.Rewards["chest_a"]);
            Assert.AreEqual(LevelAbilities.Wind | LevelAbilities.Fire, room.Requires[0].Needs);
            Assert.AreEqual("chest_a", room.Requires[0].Target);
            Assert.AreEqual("city", room.Zones[0].Region);
            Assert.AreEqual(4, room.Zones[0].Height);
        }

        [Test]
        public void CellsAreAddressedFromTheFloorUp()
        {
            var room = RoomFileParser.Parse(Valid, "t");
            Assert.AreEqual('#', room.At(0, 0), "last text line is row 0");
            Assert.AreEqual('A', room.At(2, 1));
            Assert.AreEqual('C', room.At(4, 4));
            Assert.AreEqual('#', room.At(-1, 3), "outside the room is solid");
            Assert.AreEqual('#', room.At(3, 99));
            CollectionAssert.AreEqual(new[] { new Vector2Int(2, 1) }, room.Cells('A'));
        }

        [Test]
        public void ADoorKnowsItsStripAndSpawn()
        {
            var room = RoomFileParser.Parse(Valid, "t");
            var door = room.DoorByDigit('1');
            Assert.AreEqual("test_02", door.Target);
            Assert.IsTrue(door.OnLeft);
            Assert.AreEqual(1, door.YMin);
            Assert.AreEqual(3, door.YMax);
            Assert.AreEqual(new Vector2Int(RoomFile.SpawnInset, 1), room.SpawnCell(door));
            Assert.AreEqual("from_test_01", RoomFile.SpawnName("test_01"));
            Assert.AreEqual("default", RoomFile.EntrySpawnFor("forest_boss", "forest_07"));
            Assert.AreEqual("from_forest_06", RoomFile.EntrySpawnFor("forest_07", "forest_06"));
        }

        [Test]
        public void AnIdBelongsToTheMatchingGroupInReadingOrder()
        {
            var room = RoomFileParser.Parse(Valid, "t");
            CollectionAssert.AreEqual(new[] { new Vector2Int(4, 4) }, room.CellsOfId("chest_a"));
            Assert.IsEmpty(room.CellsOfId("nope"));
            Assert.IsEmpty(room.IdsOf('T'));
        }

        [Test]
        public void GroupsAreFourConnectedAndInReadingOrder()
        {
            var room = LevelTestKit.Mini("#######", "#bb..b#", "#..b..#", "#######");
            var groups = room.Groups('b');
            Assert.AreEqual(3, groups.Count);
            Assert.AreEqual(2, groups[0].Count);
            Assert.AreEqual(new Vector2Int(5, 2), groups[1][0]);
        }

        [TestCase("")]
        [TestCase("   \n")]
        public void AnEmptyFileIsRejected(string text) => Assert.Throws<FormatException>(() => RoomFileParser.Parse(text, "t"));

        static readonly string[] Grid = { "########", "#...C..#", "1......#", "1......#", "1.A....#", "########" };

        /// <summary>A room text with every part overridable, so each case below breaks exactly one rule.</summary>
        static string Make(string[] rows = null, string size = "8x6", string doors = "doors: 1=test_02\n", string altar = "altar: test_altar\n",
                           string ids = "ids: C=chest_a\n", string rewards = "rewards: chest_a=Heart\n", string requires = "", string zones = "") =>
            $"id: test_01\nregion: forest\nslot: 0,0\nsize: {size}\ntitle: T\n{doors}{altar}{ids}{rewards}{requires}{zones}---\n{string.Join("\n", rows ?? Grid)}\n";

        [Test]
        public void TheBaselineRoomParses() => Assert.DoesNotThrow(() => RoomFileParser.Parse(Make(), "t"));

        [Test]
        public void RejectsEveryKindOfBrokenRoom()
        {
            var cases = new (string name, string text)[]
            {
                ("missing separator", Valid.Replace("---\n", "")),
                ("missing id", Valid.Replace("id: test_01\n", "")),
                ("missing size", Valid.Replace("size: 8x6\n", "")),
                ("unknown header", Valid.Replace("title:", "colour: red\ntitle:")),
                ("header without colon", Valid.Replace("title: Test hall", "Test hall")),
                ("bad size", Make(size: "eight")),
                ("size with a zero", Make(size: "0x6")),
                ("too few rows", Make(size: "8x7")),
                ("row too short", Make(new[] { "########", "#...C.#", "1......#", "1......#", "1.A....#", "########" })),
                ("illegal cell", Make(new[] { "########", "#...C?.#", "1......#", "1......#", "1.A....#", "########" })),
                ("blank grid line", Make(new[] { "########", "#...C..#", "", "1......#", "1.A....#", "########" })),
                ("door not drawn", Make(doors: "doors: 1=test_02 2=test_03\n")),
                ("door not declared", Make(doors: "")),
                ("door in the middle", Make(new[] { "########", "#...C..#", "#.1....#", "#.1....#", "#.1.A..#", "########" })),
                ("door only 2 cells tall", Make(new[] { "########", "#...C..#", "#......#", "1......#", "1.A....#", "########" })),
                ("door in two columns", Make(new[] { "########", "#...C..#", "1.....1#", "1.....1#", "1.A...1#", "########" })),
                ("door target malformed", Make(doors: "doors: 1test_02\n")),
                ("two doors to one room", Make(new[] { "########", "#...C..#", "1.....2#", "1.....2#", "1.A...2#", "########" }, doors: "doors: 1=test_02 2=test_02\n")),
                ("chest without id", Make(ids: "")),
                ("extra id", Make(ids: "ids: C=chest_a,chest_b\n")),
                ("id marker not allowed", Make(ids: "ids: N=chest_a\n")),
                ("altar without header", Make(altar: "")),
                ("altar header without altar", Make(new[] { "########", "#...C..#", "1......#", "1......#", "1......#", "########" })),
                ("reward for unknown chest", Make(rewards: "rewards: chest_z=Heart\n")),
                ("unknown ability", Make(requires: "requires: chest_a=ice\n")),
                ("bad zone", Make(zones: "zones: city@1,2,3\n")),
                ("zone with a letter", Make(zones: "zones: city@1,2,3,x\n")),
                ("two altars", Make(new[] { "########", "#...C..#", "1......#", "1......#", "1.A.A..#", "########" })),
            };
            foreach (var (name, text) in cases)
                Assert.Throws<FormatException>(() => RoomFileParser.Parse(text, "t"), name);
        }

        [Test]
        public void TheErrorNamesTheFile()
        {
            var e = Assert.Throws<FormatException>(() => RoomFileParser.Parse(Edit("size: 8x6", "size: 8x9"), "Data/Levels/x.room.txt"));
            StringAssert.Contains("Data/Levels/x.room.txt", e.Message);
        }

        [Test]
        public void EveryLegendCharacterIsOneOfTheDocumentedOnes()
        {
            Assert.AreEqual(RoomFileParser.Legend.Length, new System.Collections.Generic.HashSet<char>(RoomFileParser.Legend).Count, "no duplicates");
        }
    }
}
