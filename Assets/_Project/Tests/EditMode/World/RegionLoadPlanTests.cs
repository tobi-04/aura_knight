using System.Collections.Generic;
using AuraKnight.World;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.World
{
    /// <summary>Pure planning with load/unload operations in flight: never load an unloading region, never unload a loading one.</summary>
    public sealed class RegionLoadPlanTests
    {
        RegionGraph _graph;

        [SetUp]
        public void SetUp()
        {
            _graph = ScriptableObject.CreateInstance<RegionGraph>();
            _graph.SetRegions(new List<RegionNode>
            {
                new RegionNode("hub", "Region_Hub", "forest", "cave").WithAltars("hub_altar_01"),
                new RegionNode("forest", "Region_Forest", "hub").WithAltars("forest_altar_01"),
                new RegionNode("cave", "Region_Cave", "hub"),
            });
        }

        [TearDown] public void TearDown() => Object.DestroyImmediate(_graph);

        static HashSet<string> Set(params string[] ids) => new HashSet<string>(ids);

        [Test]
        public void RegionStillUnloadingIsDeferredNotLoaded()
        {
            var plan = RegionLoadPlan.Resolve(_graph, "forest", null, Set("hub"), Set(), Set("forest"));
            CollectionAssert.IsEmpty(plan.ToLoad, "cannot load a scene that is still unloading");
            CollectionAssert.AreEqual(new[] { "forest" }, plan.Deferred);
            CollectionAssert.AreEqual(new[] { "hub" }, plan.ToUnload);
            Assert.IsFalse(plan.IsSettled);
        }

        [Test]
        public void RegionStillLoadingIsNeverUnloaded()
        {
            var plan = RegionLoadPlan.Resolve(_graph, "forest", null, Set(), Set("hub"), Set());
            CollectionAssert.IsEmpty(plan.ToUnload);
            CollectionAssert.AreEqual(new[] { "hub" }, plan.Deferred, "unload waits until the load finished");
            CollectionAssert.AreEqual(new[] { "forest" }, plan.ToLoad);
        }

        [Test]
        public void RegionAlreadyUnloadingIsNotUnloadedTwice()
        {
            var plan = RegionLoadPlan.Resolve(_graph, "forest", null, Set("forest"), Set(), Set("hub"));
            CollectionAssert.IsEmpty(plan.ToUnload);
            CollectionAssert.IsEmpty(plan.Deferred);
            Assert.IsTrue(plan.IsSettled);
        }

        [Test]
        public void RegionAlreadyLoadingIsNotLoadedTwice()
        {
            var plan = RegionLoadPlan.Resolve(_graph, "forest", null, Set(), Set("forest"), Set());
            CollectionAssert.IsEmpty(plan.ToLoad);
            Assert.IsTrue(plan.IsSettled);
        }

        [Test]
        public void SettledOnceDesiredRegionsAreLoadedAndNothingElse()
        {
            var plan = RegionLoadPlan.Resolve(_graph, "hub", "forest", Set("hub", "forest"), Set(), Set());
            Assert.IsTrue(plan.IsSettled);
        }

        [Test]
        public void AltarDirectoryResolvesTheRegionOfAnAltar()
        {
            Assert.IsTrue(_graph.TryGetRegionOfAltar("forest_altar_01", out var region));
            Assert.AreEqual("forest", region);
            Assert.IsFalse(_graph.TryGetRegionOfAltar("moon_altar", out _));
            Assert.IsFalse(_graph.TryGetRegionOfAltar(null, out _));
        }

        [Test]
        public void RoomBlendHoldsAtLeastTheDampingTime()
        {
            Assert.AreEqual(1f, RoomBlendTiming.HoldSeconds(0.3f, 1f), "damping 1 s must not be cut off after 0.3 s");
            Assert.AreEqual(0.5f, RoomBlendTiming.HoldSeconds(0.5f, 0.2f));
            Assert.AreEqual(0f, RoomBlendTiming.HoldSeconds(-1f, -1f));
        }
    }
}
