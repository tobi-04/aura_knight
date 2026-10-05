using AuraKnight.Core;
using AuraKnight.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AuraKnight.Tests.World
{
    /// <summary>The generated RegionGraph, region scenes and Core scene agree with each other (run the world generators first).</summary>
    public sealed class GeneratedWorldAssetsTests
    {
        const string GraphPath = "Assets/_Project/Data/World/RegionGraph.asset";

        [Test]
        public void RegionGraphListsTheStartAltarInTheHub()
        {
            var graph = AssetDatabase.LoadAssetAtPath<RegionGraph>(GraphPath);
            Assert.IsNotNull(graph);
            Assert.IsTrue(graph.TryGetRegionOfAltar(GameState.StartAltarId, out var region));
            Assert.AreEqual("hub", region);
            CollectionAssert.IsEmpty(AltarValidator.ValidateGraph(graph.Regions, GameState.StartAltarId));
        }

        [Test]
        public void EveryListedAltarExistsInItsRegionScene()
        {
            var graph = AssetDatabase.LoadAssetAtPath<RegionGraph>(GraphPath);
            foreach (var region in graph.Regions)
            {
                var scene = EditorSceneManager.OpenScene($"Assets/_Project/Scenes/{region.sceneName}.unity", OpenSceneMode.Additive);
                try
                {
                    var placed = new System.Collections.Generic.List<string>();
                    foreach (var root in scene.GetRootGameObjects())
                        foreach (var altar in root.GetComponentsInChildren<SunAltar>(true)) placed.Add(altar.AltarId);
                    var errors = AltarValidator.ValidatePlacement(graph.Regions, region.regionId, placed, scene.path, requireAllListed: true);
                    CollectionAssert.IsEmpty(errors, string.Join("; ", errors));
                    foreach (var root in scene.GetRootGameObjects())
                        foreach (var room in root.GetComponentsInChildren<Room>(true))
                            Assert.AreEqual(region.regionId, room.RegionId, room.RoomId);
                }
                finally { EditorSceneManager.CloseScene(scene, true); }
            }
        }

        [Test]
        public void CoreSceneHasTheWorldEntryWired()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Core.unity", OpenSceneMode.Additive);
            try
            {
                var entry = Object.FindAnyObjectByType<WorldEntry>();
                Assert.IsNotNull(entry);
                var so = new SerializedObject(entry);
                Assert.IsNotNull(so.FindProperty("graph").objectReferenceValue);
                Assert.IsNotNull(so.FindProperty("loader").objectReferenceValue);
                Assert.IsNotNull(so.FindProperty("playerPrefab").objectReferenceValue);
                Assert.IsNotNull(Object.FindAnyObjectByType<CheckpointService>());
                Assert.IsNotNull(Object.FindAnyObjectByType<GameManager>());
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
