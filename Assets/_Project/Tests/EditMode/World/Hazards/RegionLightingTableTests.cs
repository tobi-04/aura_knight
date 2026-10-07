using AuraKnight.World;
using NUnit.Framework;

namespace AuraKnight.Tests.World.Hazards
{
    public sealed class RegionLightingTableTests
    {
        [TestCase("forest", 0.6f)]
        [TestCase("cave", 0.25f)]
        [TestCase("city", 0.45f)]
        [TestCase("castle", 0.05f)]
        public void RegionsUseTheGddGlobalLight(string region, float intensity) =>
            Assert.AreEqual(intensity, RegionLightingTable.IntensityOf(region), 1e-6f);

        [TestCase("hub")]
        [TestCase("")]
        [TestCase(null)]
        [TestCase("unknown")]
        public void TheHubAndUnknownRegionsAreBright(string region) =>
            Assert.AreEqual(1f, RegionLightingTable.IntensityOf(region), 1e-6f);

        [Test]
        public void TheCastleIsTheDarkestAndTheHubTheBrightest()
        {
            foreach (var region in new[] { "forest", "cave", "city" })
            {
                Assert.Greater(RegionLightingTable.IntensityOf(region), RegionLightingTable.Castle);
                Assert.Less(RegionLightingTable.IntensityOf(region), RegionLightingTable.Hub);
            }
        }
    }
}
