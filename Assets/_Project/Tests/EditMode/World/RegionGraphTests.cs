using System.Collections.Generic;
using AuraKnight.World;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.World
{
    public sealed class RegionGraphTests
    {
        RegionGraph graph;

        [SetUp]
        public void SetUp()
        {
            graph = ScriptableObject.CreateInstance<RegionGraph>();
            graph.SetRegions(new List<RegionNode>
            {
                new RegionNode("hub", "Region_Hub", "forest", "cave", "city", "castle"),
                new RegionNode("forest", "Region_Forest", "hub"),
                new RegionNode("cave", "Region_Cave", "hub"),
                new RegionNode("city", "Region_City", "hub"),
                new RegionNode("castle", "Region_Castle", "hub"),
            });
        }

        [TearDown] public void TearDown() => Object.DestroyImmediate(graph);

        [Test]
        public void GetSceneName_ResolvesKnownRegion()
        {
            Assert.IsTrue(graph.TryGetSceneName("cave", out var scene));
            Assert.AreEqual("Region_Cave", scene);
            Assert.IsFalse(graph.TryGetSceneName("moon", out _));
        }

        [Test]
        public void AreNeighbors_IsSymmetricEvenIfDeclaredOneWay()
        {
            Assert.IsTrue(graph.AreNeighbors("hub", "forest"));
            Assert.IsTrue(graph.AreNeighbors("forest", "hub"));
            Assert.IsFalse(graph.AreNeighbors("forest", "cave"));
            graph.SetRegions(new List<RegionNode>
            {
                new RegionNode("a", "A", "b"), new RegionNode("b", "B"),
            });
            Assert.IsTrue(graph.AreNeighbors("b", "a"));
        }

        [Test]
        public void GetNeighbors_ListsHubFour()
        {
            CollectionAssert.AreEquivalent(new[] { "forest", "cave", "city", "castle" }, graph.GetNeighbors("hub"));
            CollectionAssert.IsEmpty(graph.GetNeighbors("unknown"));
        }

        [Test]
        public void Plan_KeepsOnlyCurrentWhenNoPreload()
        {
            var plan = RegionLoadPlan.Resolve(graph, "forest", null, new HashSet<string> { "hub" });
            CollectionAssert.AreEqual(new[] { "forest" }, plan.ToLoad);
            CollectionAssert.AreEqual(new[] { "hub" }, plan.ToUnload);
        }

        [Test]
        public void Plan_PreloadsNeighborAndKeepsAtMostTwo()
        {
            var plan = RegionLoadPlan.Resolve(graph, "hub", "forest", new HashSet<string> { "hub", "cave" });
            CollectionAssert.AreEqual(new[] { "forest" }, plan.ToLoad);
            CollectionAssert.AreEqual(new[] { "cave" }, plan.ToUnload);
            Assert.LessOrEqual(plan.Desired.Count, 2);
        }

        [Test]
        public void Plan_IgnoresPreloadOfNonNeighbor()
        {
            var plan = RegionLoadPlan.Resolve(graph, "forest", "cave", new HashSet<string> { "forest" });
            CollectionAssert.IsEmpty(plan.ToLoad);
            CollectionAssert.IsEmpty(plan.ToUnload);
        }

        [Test]
        public void Plan_UnknownCurrent_ChangesNothing()
        {
            var plan = RegionLoadPlan.Resolve(graph, "moon", null, new HashSet<string> { "hub" });
            CollectionAssert.IsEmpty(plan.ToLoad);
            CollectionAssert.IsEmpty(plan.ToUnload);
        }
    }
}
