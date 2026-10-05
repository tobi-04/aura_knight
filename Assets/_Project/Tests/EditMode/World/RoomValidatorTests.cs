using System.Collections.Generic;
using System.Linq;
using AuraKnight.World;
using NUnit.Framework;

namespace AuraKnight.Tests.World
{
    public sealed class RoomValidatorTests
    {
        static RoomInfo Room(string id, string region, string[] exits = null, string[] pids = null) =>
            new RoomInfo(id, region, "Room_" + id + ".prefab", exits, pids);

        [TestCase("forest_03", true)]
        [TestCase("hub_01", true)]
        [TestCase("forest_boss", true)]
        [TestCase("", false)]
        [TestCase("Forest_03", false)]
        [TestCase("forest", false)]
        [TestCase("forest 03", false)]
        [TestCase("forest__03", false)]
        public void IsValidId_FollowsNamingRule(string id, bool expected)
        {
            Assert.AreEqual(expected, RoomValidator.IsValidId(id));
        }

        [Test]
        public void ValidSet_HasNoErrors()
        {
            var errors = RoomValidator.Validate(new[]
            {
                Room("forest_01", "forest", new[] { "forest_02" }),
                Room("forest_02", "forest", new[] { "forest_01", "hub_01" }),
                Room("hub_01", "hub", new[] { "forest_02" }),
            });
            CollectionAssert.IsEmpty(errors);
        }

        [Test]
        public void DuplicateRoomId_IsReported()
        {
            var errors = RoomValidator.Validate(new[]
            {
                Room("forest_01", "forest"), Room("forest_01", "forest"),
            });
            Assert.IsTrue(errors.Any(e => e.Contains("Duplicate room id") && e.Contains("forest_01")));
        }

        [Test]
        public void ExitToMissingRoom_IsReported()
        {
            var errors = RoomValidator.Validate(new[]
            {
                Room("forest_01", "forest", new[] { "forest_99" }),
            });
            Assert.IsTrue(errors.Any(e => e.Contains("forest_99") && e.Contains("missing")));
        }

        [Test]
        public void EmptyExitTarget_IsReported()
        {
            var errors = RoomValidator.Validate(new[] { Room("forest_01", "forest", new[] { "" }) });
            Assert.IsTrue(errors.Any(e => e.Contains("empty target")));
        }

        [Test]
        public void IdNotMatchingRegionPrefix_IsReported()
        {
            var errors = RoomValidator.Validate(new[] { Room("cave_01", "forest") });
            Assert.IsTrue(errors.Any(e => e.Contains("region")));
        }

        [Test]
        public void InvalidId_IsReported()
        {
            var errors = RoomValidator.Validate(new[] { Room("Bad Id", "forest") });
            Assert.IsTrue(errors.Any(e => e.Contains("Invalid room id")));
        }

        [Test]
        public void DuplicatePersistentId_AcrossRooms_IsReported()
        {
            var errors = RoomValidator.Validate(new[]
            {
                Room("forest_01", "forest", null, new[] { "forest_chest_a" }),
                Room("forest_02", "forest", null, new[] { "forest_chest_a" }),
            });
            Assert.IsTrue(errors.Any(e => e.Contains("Duplicate persistent id")));
        }

        [Test]
        public void EmptyPersistentId_IsReported()
        {
            var errors = RoomValidator.Validate(new[] { Room("forest_01", "forest", null, new[] { "" }) });
            Assert.IsTrue(errors.Any(e => e.Contains("empty persistent id")));
        }

        [Test]
        public void NoRooms_NoErrors()
        {
            CollectionAssert.IsEmpty(RoomValidator.Validate(new List<RoomInfo>()));
        }
    }
}
