using System.Collections.Generic;
using System.Linq;
using AuraKnight.Core;
using AuraKnight.World;
using NUnit.Framework;

namespace AuraKnight.Tests.World
{
    public sealed class AltarValidatorTests
    {
        static List<RegionNode> Regions() => new List<RegionNode>
        {
            new RegionNode("hub", "Region_Hub", "forest").WithAltars(GameState.StartAltarId),
            new RegionNode("forest", "Region_Forest", "hub").WithAltars("forest_altar_01", "forest_altar_02"),
        };

        [Test]
        public void ConsistentGraphHasNoErrors() =>
            CollectionAssert.IsEmpty(AltarValidator.ValidateGraph(Regions(), GameState.StartAltarId));

        [Test]
        public void DuplicateAltarIdAcrossRegionsIsReported()
        {
            var regions = Regions();
            regions[1].altarIds.Add(GameState.StartAltarId);
            var errors = AltarValidator.ValidateGraph(regions, GameState.StartAltarId);
            Assert.IsTrue(errors.Any(e => e.Contains(GameState.StartAltarId) && e.Contains("both")));
        }

        [Test]
        public void MissingStartAltarIsReported()
        {
            var regions = Regions();
            regions[0].altarIds.Clear();
            Assert.IsTrue(AltarValidator.ValidateGraph(regions, GameState.StartAltarId).Any(e => e.Contains("start altar")));
        }

        [Test]
        public void EmptyAltarIdIsReported()
        {
            var regions = Regions();
            regions[1].altarIds.Add("");
            Assert.IsTrue(AltarValidator.ValidateGraph(regions, GameState.StartAltarId).Any(e => e.Contains("empty")));
        }

        [Test]
        public void AltarPlacedInTheWrongRegionIsReported()
        {
            var errors = AltarValidator.ValidatePlacement(Regions(), "hub", new[] { "forest_altar_01" }, "Region_Hub.unity", false);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("not listed for region 'hub'", errors[0]);
        }

        [Test]
        public void ListedAltarMissingFromItsSceneIsReportedWhenRequired()
        {
            var errors = AltarValidator.ValidatePlacement(Regions(), "forest", new[] { "forest_altar_01" }, "Region_Forest.unity", true);
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("forest_altar_02", errors[0]);
            CollectionAssert.IsEmpty(AltarValidator.ValidatePlacement(Regions(), "forest", new[] { "forest_altar_01" }, "room", false));
        }

        [Test]
        public void AltarsInAnUnknownRegionAreReported()
        {
            var errors = AltarValidator.ValidatePlacement(Regions(), "moon", new[] { "moon_altar" }, "room", false);
            Assert.AreEqual(1, errors.Count);
        }

        [Test]
        public void RoomValidatorAppliesTheAltarDirectoryToRoomAltars()
        {
            var room = new RoomInfo("forest_01", "forest", "Room_forest_01.prefab", altarIds: new[] { "hub_altar_01" });
            var errors = RoomValidator.Validate(new[] { room }, Regions());
            Assert.IsTrue(errors.Any(e => e.Contains("hub_altar_01") && e.Contains("forest")));
            CollectionAssert.IsEmpty(RoomValidator.Validate(new[] { room }));
        }

        [Test]
        public void HitboxAndHurtboxOfDifferentTeamsOnOneObjectIsAnError()
        {
            var room = new RoomInfo("forest_01", "forest", "Room_forest_01.prefab", mixedTeamCombatObjects: new[] { "Goblin" });
            var errors = RoomValidator.Validate(new[] { room });
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("Goblin", errors[0]);
            StringAssert.Contains("different teams", errors[0]);
        }

        [Test]
        public void OneTimeGateWithoutPersistentIdIsAnError()
        {
            var room = new RoomInfo("forest_01", "forest", "Room_forest_01.prefab", gatesWithoutPersistentId: new[] { "AuraGate_Burn" });
            var errors = RoomValidator.Validate(new[] { room });
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains("AuraGate_Burn", errors[0]);
            StringAssert.Contains("PersistentId", errors[0]);
        }
    }
}
