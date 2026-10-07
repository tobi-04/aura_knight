using System.Collections;
using AuraKnight.Core;
using AuraKnight.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AuraKnight.Tests.PlayMode.Levels
{
    /// <summary>
    /// Walks every doorway of every region in the real scenes: step on the exit, arrive at the named spawn in the right room, no
    /// bounce, the next region preloaded in time. (Gates are not tested here, the physics tests do that: Leo is placed in the exit.)
    /// </summary>
    public sealed class TraversalPlayModeTests : LevelsPlayModeBase
    {
        /// <summary>Forward through the chain, then back out through the boss arena and the hub door.</summary>
        IEnumerator Chain(params string[] rooms)
        {
            MakeInvulnerable();
            for (int i = 0; i < rooms.Length; i++) yield return Through(rooms[i]);
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator TheHubRoomsConnectAndSolSitsInTheSecondRoom()
        {
            yield return NewGame();
            Assert.AreEqual("hub_01", CurrentRoomId);
            yield return Chain("hub_02", "hub_03", "hub_02", "hub_01");
            yield return Through("hub_02");
            Assert.IsTrue(RoomRegistry.TryGet("hub_02", out var shop));
            Assert.IsNotNull(shop.GetComponentInChildren<NpcSol>(true), "Sol and the shop are in hub_02");
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator ForestFromTheStartAltarToTheRottenStumpAndBack()
        {
            yield return NewGame();
            yield return Chain("forest_01", "forest_02", "forest_03", "forest_04", "forest_05", "forest_06", "forest_07", "forest_boss");
            yield return Chain("forest_07", "forest_01", "hub_01");
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator CaveFromTheHubThirdRoomToTheStoneSpiderAndBack()
        {
            yield return NewGame();
            yield return Chain("hub_02", "hub_03", "cave_01", "cave_02", "cave_03", "cave_04", "cave_05", "cave_06", "cave_07", "cave_boss");
            yield return Chain("cave_07", "cave_01", "hub_03");
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator CityIsReachedThroughTheBarricadeZoneAndBack()
        {
            yield return NewGame();
            yield return Chain("hub_02", "hub_03");
            yield return StepIntoPreloadZone("city");
            yield return Chain("city_01", "city_02", "city_03", "city_04", "city_05", "city_06", "city_07", "city_boss");
            yield return Chain("city_07", "city_01", "hub_03");
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator CastleIsReachedThroughTheSealGateZoneAndBack()
        {
            yield return NewGame();
            yield return Chain("hub_02", "hub_03");
            yield return StepIntoPreloadZone("castle");
            yield return Chain("castle_01", "castle_02", "castle_03", "castle_04", "castle_05", "castle_06", "castle_boss");
            yield return Chain("castle_06", "castle_01", "hub_03");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator AZoneLoadsItsRegionWhileLeoStandsInItAndTheDefaultComesBackWhenHeLeaves()
        {
            yield return NewGame();
            yield return Chain("hub_02", "hub_03");
            yield return WaitUntil(() => SceneLoader.IsLoaded("Region_Cave"), "the room's own preload: the Cave");
            Assert.IsFalse(SceneLoader.IsLoaded("Region_City"));
            yield return StepIntoPreloadZone("city");
            yield return WaitUntil(() => SceneLoader.IsLoaded("Region_City"), "the City while Leo stands by the barricade");
            var hub = RoomManager.Instance.Current.transform.position;
            Teleport(new Vector2(hub.x + 18f, hub.y + 2f)); // outside both zones: the City one is x 24-32 and the Castle one x 0-13
            yield return WaitUntil(() => SceneLoader.IsLoaded("Region_Cave") && !SceneLoader.IsLoaded("Region_City"), "back to the Cave when he leaves the zone", 20f);
        }

        IEnumerator StepIntoPreloadZone(string region)
        {
            var room = RoomManager.Instance.Current;
            RegionPreloadZone zone = null;
            foreach (var z in room.GetComponentsInChildren<RegionPreloadZone>(true)) if (z.RegionId == region) zone = z;
            Assert.IsNotNull(zone, $"{room.RoomId} has a preload zone for {region}");
            Teleport(zone.GetComponent<Collider2D>().bounds.center);
            yield return WaitUntil(() => SceneLoader.IsLoaded("Region_" + char.ToUpper(region[0]) + region.Substring(1)), $"{region} preloaded", 20f);
        }
    }
}
