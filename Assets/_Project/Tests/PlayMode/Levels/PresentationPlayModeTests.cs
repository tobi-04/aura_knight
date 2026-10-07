using System.Collections;
using System.Linq;
using AuraKnight.Core;
using AuraKnight.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace AuraKnight.Tests.PlayMode.Levels
{
    /// <summary>Region lighting and parallax in the real scenes: one global light at a time, only the current room's backdrop draws.</summary>
    public sealed class PresentationPlayModeTests : LevelsPlayModeBase
    {
        static Light2D LightOf(string region) =>
            All<RegionLighting>().First(l => l.RegionId == region).GetComponent<Light2D>();

        [UnityTest, Timeout(120000)]
        public IEnumerator OnlyTheCurrentRegionsGlobalLightIsOn()
        {
            yield return NewGame();
            yield return WaitUntil(() => SceneLoader.IsLoaded("Region_Forest"), "the forest preloaded from hub_01");
            yield return null;
            Assert.IsTrue(LightOf("hub").enabled);
            Assert.AreEqual(1f, LightOf("hub").intensity, 1e-4f);
            Assert.IsFalse(LightOf("forest").enabled, "the preloaded neighbour does not light the hub");

            yield return Through("forest_01");
            Assert.IsTrue(LightOf("forest").enabled);
            Assert.AreEqual(0.6f, LightOf("forest").intensity, 1e-4f);
            Assert.IsFalse(LightOf("hub").enabled);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator EachRegionLightHasItsConfiguredIntensity()
        {
            foreach (var (altar, region, intensity) in new[] { ("cave_altar_01", "cave", 0.25f), ("city_altar_01", "city", 0.45f), ("castle_altar_01", "castle", 0.15f) })
            {
                yield return StartAt(altar, "Wind", "Fire", "Water");
                yield return null;
                Assert.IsTrue(LightOf(region).enabled, region);
                Assert.AreEqual(intensity, LightOf(region).intensity, 1e-4f, region);
            }
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator OnlyTheBackdropOfTheRoomLeoIsInDraws()
        {
            yield return NewGame();
            yield return null;
            yield return null;
            Assert.IsTrue(RoomRegistry.TryGet("hub_01", out var here));
            Assert.IsTrue(RoomRegistry.TryGet("hub_02", out var next));
            var mine = here.GetComponentsInChildren<ParallaxLayer>(true);
            var theirs = next.GetComponentsInChildren<ParallaxLayer>(true);
            Assert.AreEqual(4, mine.Length);
            Assert.IsTrue(mine.All(l => l.GetComponent<SpriteRenderer>().enabled), "this room's layers draw");
            Assert.IsTrue(theirs.All(l => !l.GetComponent<SpriteRenderer>().enabled), "the next room's layers do not");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator ALayerFollowsTheCameraAtItsFactor()
        {
            yield return NewGame();
            yield return null;
            RoomRegistry.TryGet("hub_01", out var room);
            var layers = room.GetComponentsInChildren<ParallaxLayer>(true).OrderBy(l => l.Factor).ToArray();
            var cam = Camera.main.transform;
            Assert.AreEqual(cam.position.y, layers[0].transform.position.y, 0.05f, "locked to the camera vertically");
            float anchor = room.transform.position.x + 20f;
            for (int i = 0; i < layers.Length; i++)
            {
                float expected = ParallaxMath.PositionX(anchor, cam.position.x, layers[i].Factor);
                Assert.AreEqual(expected, layers[i].transform.position.x, 0.1f, $"layer x{layers[i].Factor}");
            }
        }
    }
}
