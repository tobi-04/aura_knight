using System.Collections.Generic;
using System.Linq;
using AuraKnight.Editor;
using NUnit.Framework;

namespace AuraKnight.Tests.Levels
{
    /// <summary>The shape of the game as the room files define it: 34 rooms, four regions with two altars, two secret chests and one shortcut each.</summary>
    public sealed class LevelCatalogTests
    {
        static IReadOnlyList<RoomFile> Rooms => LevelTestKit.Catalog;

        static List<RoomFile> Of(string region) => LevelCatalog.InRegion(Rooms, region);

        [Test]
        public void EveryRegionHasTheRoomCountOfTheDesign()
        {
            Assert.AreEqual(3, Of("hub").Count, "hub");
            Assert.AreEqual(7, Of("forest").Count, "forest (+ boss arena)");
            Assert.AreEqual(7, Of("cave").Count, "cave (+ boss arena)");
            Assert.AreEqual(7, Of("city").Count, "city (+ boss arena)");
            Assert.AreEqual(6, Of("castle").Count, "castle (+ boss arena)");
            int bosses = LevelRegions.All.Count(r => r.HasBoss);
            Assert.AreEqual(4, bosses);
            Assert.AreEqual(34, Rooms.Count + bosses, "GDD 7.2: 3 + 8 + 8 + 8 + 7 rooms, boss arenas included");
        }

        [Test]
        public void RoomsAreNumberedWithoutGapsInsideTheirRegion()
        {
            foreach (var region in new[] { "forest", "cave", "city", "castle" })
            {
                var ids = Of(region).Select(r => r.Id).OrderBy(i => i).ToArray();
                for (int i = 0; i < ids.Length; i++) Assert.AreEqual($"{region}_{i + 1:00}", ids[i]);
            }
            CollectionAssert.AreEqual(new[] { "hub_01", "hub_02", "hub_03" }, Of("hub").Select(r => r.Id).OrderBy(i => i));
        }

        [Test]
        public void RoomsAreStandardOrVerticalSized()
        {
            foreach (var room in Rooms)
            {
                bool vertical = room.Id == "forest_04" || room.Id == "city_04" || room.Id == "castle_04";
                Assert.AreEqual(vertical ? 22 : 40, room.Width, room.Id);
                Assert.AreEqual(vertical ? 44 : 22, room.Height, room.Id);
                Assert.AreEqual(vertical, room.IsVertical, room.Id);
            }
        }

        [Test]
        public void EveryRoomSitsInItsRegionFolder()
        {
            foreach (var room in Rooms)
                StringAssert.Contains($"/Levels/{LevelRegions.Get(room.Region).Folder}/", room.Source, room.Id);
        }

        [Test]
        public void NoRoomHoldsMoreThanSixEnemies()
        {
            foreach (var room in Rooms) Assert.LessOrEqual(RoomEnemies.Count(room), 6, room.Id);
            Assert.GreaterOrEqual(Rooms.Sum(RoomEnemies.Count), 30, "the game is not empty");
        }

        [Test]
        public void EachRegionHasAnAltarAtTheEntryAndOneBeforeTheBoss()
        {
            foreach (var region in new[] { "forest", "cave", "city", "castle" })
            {
                var info = LevelRegions.Get(region);
                Assert.AreEqual($"{region}_altar_01", Of(region).First(r => r.Id == $"{region}_01").AltarId, region);
                Assert.AreEqual($"{region}_altar_02", Of(region).First(r => r.Id == info.BossPreviousRoom).AltarId, region);
                CollectionAssert.AreEqual(new[] { $"{region}_altar_01", $"{region}_altar_02" }, LevelCatalog.AltarIds(Rooms, region), region);
                Assert.IsNotNull(Of(region).First(r => r.Id == info.BossPreviousRoom).DoorTo(info.BossRoomId), $"{region}: the antechamber leads to the arena");
            }
            Assert.AreEqual("hub_altar_01", LevelTestKit.Room("hub_01").AltarId);
        }

        [Test]
        public void EachRegionHasTwoSecretChestsWithTheAgreedIds()
        {
            foreach (var region in new[] { "forest", "cave", "city", "castle" })
            {
                var ids = Of(region).SelectMany(r => r.IdsOf('C')).OrderBy(i => i).ToArray();
                CollectionAssert.AreEqual(new[] { $"chest_{region}_01", $"chest_{region}_02" }, ids, region);
            }
            Assert.IsEmpty(Of("hub").SelectMany(r => r.IdsOf('C')));
        }

        [Test]
        public void EverySecretChestIsGatedByAnAuraOfTheRegionsEndOrLater()
        {
            foreach (var room in Rooms)
                foreach (var id in room.IdsOf('C'))
                {
                    var need = room.Requires.FirstOrDefault(q => q.Target == id);
                    Assert.IsNotNull(need, $"{id} is a secret: it needs an Aura");
                    Assert.AreNotEqual(LevelAbilities.None, need.Needs, id);
                }
        }

        [Test]
        public void EachRegionHasOneShortcutBackToItsEntry()
        {
            foreach (var region in new[] { "forest", "cave", "city", "castle" })
            {
                var info = LevelRegions.Get(region);
                var entry = LevelTestKit.Room($"{region}_01");
                Assert.AreEqual(1, entry.Groups('k').Count + entry.Groups('j').Count, $"{region}: one shortcut door in the entry room");
                Assert.AreEqual(info.BossPreviousRoom, entry.DoorByDigit('7').Target, $"{region}: it comes from the altar room before the boss");
                Assert.AreEqual(entry.Id, LevelTestKit.Room(info.BossPreviousRoom).DoorByDigit('7').Target);
            }
        }

        [Test]
        public void TheHubHasTheStartAltarTheShopNpcAndTheThreeGates()
        {
            Assert.AreEqual(1, LevelTestKit.Room("hub_01").Cells('A').Count);
            Assert.AreEqual(1, LevelTestKit.Room("hub_02").Cells('N').Count, "Sol and her shop");
            var gates = LevelTestKit.Room("hub_03");
            CollectionAssert.AreEqual(new[] { "gate_hub_barricade" }, gates.IdsOf('Z'));
            CollectionAssert.AreEqual(new[] { "gate_hub_castle" }, gates.IdsOf('G'));
            CollectionAssert.AreEqual(new[] { "seal_wind", "seal_fire", "seal_water" }, gates.IdsOf('Q'));
            Assert.AreEqual("city_01", gates.DoorByDigit('4').Target, "the barricade closes the way to the City");
            Assert.AreEqual("castle_01", gates.DoorByDigit('3').Target);
            Assert.AreEqual("cave_01", gates.DoorByDigit('2').Target);
        }

        [Test]
        public void EveryRegionEntersFromTheHubOnlyThroughItsFirstRoom()
        {
            foreach (var region in new[] { "forest", "cave", "city", "castle" })
            {
                var outside = Of("hub").SelectMany(r => r.Doors.Select(d => (room: r, door: d))).Where(x => x.door.Target.StartsWith(region + "_")).ToList();
                Assert.AreEqual(1, outside.Count, region);
                Assert.AreEqual($"{region}_01", outside[0].door.Target, region);
            }
        }

        [Test]
        public void TheCaveEntranceIsASixTallSmoothWall()
        {
            var cave = LevelTestKit.Room("cave_01");
            var walls = cave.Groups('W');
            Assert.AreEqual(1, walls.Count);
            var r = RoomGeometry.Bounds(walls[0]);
            Assert.AreEqual(6, r.height, "GDD 7.2: 6 cells, above the 4.5 jump");
            Assert.AreEqual(1, r.y, "it stands on the floor");
            Assert.GreaterOrEqual(r.width, 2, "no thin edge to grab");
        }

        [Test]
        public void SlotsPlaceEveryRoomOnItsOwnCellOfTheMap()
        {
            var seen = new HashSet<string>();
            foreach (var room in Rooms)
            {
                var region = LevelRegions.Get(room.Region);
                Assert.IsTrue(seen.Add($"{region.Id}:{room.SlotCell.x},{room.SlotCell.y}"), $"{room.Id}: slot used twice");
            }
            foreach (var region in LevelRegions.All.Where(r => r.HasBoss))
                Assert.IsTrue(seen.Add($"{region.Id}:{region.BossSlot.x},{region.BossSlot.y}"), $"{region.Id} boss slot overlaps a room");
        }

        [Test]
        public void ForestRunsWestAndTheOthersEastOfTheirEntry()
        {
            foreach (var room in Of("forest")) Assert.LessOrEqual(room.SlotCell.x, 0, room.Id);
            foreach (var region in new[] { "cave", "city", "castle" })
                foreach (var room in Of(region)) Assert.GreaterOrEqual(room.SlotCell.x, 0, room.Id);
        }
    }
}
