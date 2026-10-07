using System.Linq;
using AuraKnight.Editor;
using NUnit.Framework;

namespace AuraKnight.Tests.Levels
{
    /// <summary>Walks the whole game on paper: from the start altar to Malakor, and what stays shut without each Aura.</summary>
    public sealed class LevelProgressionTests
    {
        static ProgressionResult Walk(LevelAbilities start, bool grant) => LevelProgression.Simulate(LevelTestKit.Catalog, start, grant);

        static string[] Region(string region) =>
            LevelTestKit.Catalog.Where(r => r.Region == region).Select(r => r.Id).ToArray();

        [Test]
        public void TheMainPathReachesEveryRoomAndEveryBossIncludingMalakor()
        {
            var result = Walk(LevelAbilities.None, true);
            CollectionAssert.IsEmpty(result.Problems);
            foreach (var room in LevelTestKit.Catalog) Assert.IsTrue(result.Reached(room.Id), room.Id);
            foreach (var region in LevelRegions.All.Where(r => r.HasBoss)) Assert.IsTrue(result.BossRooms.Contains(region.BossRoomId), region.BossRoomId);
            Assert.IsTrue(result.BossRooms.Contains("castle_boss"));
            Assert.AreEqual(LevelAbilities.Wind | LevelAbilities.Fire | LevelAbilities.Water | LevelAbilities.Seals, result.Abilities & ~LevelAbilities.ShortcutOpen);
        }

        [Test]
        public void WithoutAnyAuraOnlyTheHubTheForestAndTheCaveDoorstepOpen()
        {
            var result = Walk(LevelAbilities.None, false);
            foreach (var id in Region("hub").Concat(Region("forest"))) Assert.IsTrue(result.Reached(id), id);
            Assert.IsTrue(result.BossRooms.Contains("forest_boss"));
            Assert.IsTrue(result.Reached("cave_01"), "the first cave room is open (the wall is inside it)");
            foreach (var id in Region("cave").Where(i => i != "cave_01")) Assert.IsFalse(result.Reached(id), $"{id} needs Wind");
            foreach (var id in Region("city").Concat(Region("castle"))) Assert.IsFalse(result.Reached(id), id);
        }

        [Test]
        public void WindOpensTheCaveButNotTheCity()
        {
            var result = Walk(LevelAbilities.Wind, false);
            foreach (var id in Region("cave")) Assert.IsTrue(result.Reached(id), id);
            Assert.IsTrue(result.BossRooms.Contains("cave_boss"));
            foreach (var id in Region("city").Concat(Region("castle"))) Assert.IsFalse(result.Reached(id), id);
        }

        [Test]
        public void TheBarricadeNeedsFireNotWind()
        {
            var windOnly = Walk(LevelAbilities.Wind | LevelAbilities.Water, false);
            Assert.IsFalse(windOnly.Reached("city_01"), "Wind and Water do not burn the barricade");
            var fire = Walk(LevelAbilities.Fire, false);
            foreach (var id in Region("city")) Assert.IsTrue(fire.Reached(id), $"{id} with Fire");
        }

        [Test]
        public void TheCastleNeedsAllThreeAuras()
        {
            var two = Walk(LevelAbilities.Wind | LevelAbilities.Fire, false);
            Assert.IsFalse(two.Reached("castle_01"), "two seals are not enough");
            var three = Walk(LevelAbilities.Wind | LevelAbilities.Fire | LevelAbilities.Water | LevelAbilities.Seals, false);
            foreach (var id in Region("castle")) Assert.IsTrue(three.Reached(id), id);
            Assert.IsTrue(three.BossRooms.Contains("castle_boss"));
        }

        [Test]
        public void TheSealsFlagExistsOnlyWithAllThreeAuras()
        {
            Assert.AreEqual(LevelAbilities.None, LevelAbilityNames.FromAuras(false, false, false));
            Assert.AreEqual(LevelAbilities.Wind | LevelAbilities.Fire, LevelAbilityNames.FromAuras(true, true, false));
            Assert.AreEqual(LevelAbilities.Wind | LevelAbilities.Fire | LevelAbilities.Water | LevelAbilities.Seals, LevelAbilityNames.FromAuras(true, true, true));
        }

        [Test]
        public void EachBossArenaIsFoundBeforeItsRewardIsNeeded()
        {
            foreach (var region in LevelRegions.All.Where(r => r.HasBoss && r.Id != "castle"))
            {
                var needed = region.Id == "forest" ? LevelAbilities.None : region.Id == "cave" ? LevelAbilities.Wind : LevelAbilities.Wind | LevelAbilities.Fire;
                Assert.IsTrue(Walk(needed, false).BossRooms.Contains(region.BossRoomId), $"{region.Id} boss is reachable with only the Auras before it");
            }
        }

        [Test]
        public void AuraOrderIsForestWindThenCaveFireThenCityWater()
        {
            var forest = LevelRegions.Get("forest");
            var cave = LevelRegions.Get("cave");
            var city = LevelRegions.Get("city");
            Assert.AreEqual("Wind", forest.BossReward);
            Assert.AreEqual("Fire", cave.BossReward);
            Assert.AreEqual("Water", city.BossReward);
            Assert.IsNull(LevelRegions.Get("castle").BossReward, "Malakor is the last boss");
        }

        [Test]
        public void ReachingARoomsPocketDoorOpensItsShortcutForGood()
        {
            var result = Walk(LevelAbilities.None, true);
            foreach (var region in new[] { "forest", "cave", "city", "castle" }) CollectionAssert.Contains(result.OpenedShortcuts, $"{region}_01");
        }

        [Test]
        public void TheMapDoorsFormOneConnectedWorld()
        {
            var graph = LevelTestKit.Catalog.SelectMany(r => r.Doors.Select(d => (a: r.Id, b: d.Target))).ToList();
            foreach (var (a, b) in graph.Where(e => !e.b.EndsWith("_boss")))
                Assert.IsTrue(graph.Contains((b, a)), $"{a} -> {b} has no return door");
        }
    }
}
