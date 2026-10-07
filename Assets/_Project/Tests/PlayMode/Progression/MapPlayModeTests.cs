using System.Collections;
using AuraKnight.Core;
using AuraKnight.Progression;
using AuraKnight.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AuraKnight.Tests.PlayMode.Progression
{
    public sealed class MapPlayModeTests : ProgressionPlayModeBase
    {
        static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++) yield return null;
        }

        [UnityTest]
        public IEnumerator MapShowsTheVisitedRoomAndLeoAndNothingUnvisited()
        {
            yield return EnterWorld(true);
            var map = Find<MapScreen>();
            Assert.IsTrue(Manager.State.visitedRooms.Contains("hub_01"), "entering the hub recorded the room");

            EventBus.Publish(new MapRequested());
            yield return null;

            Assert.IsTrue(map.IsVisible);
            Assert.IsTrue(map.Rooms.ContainsKey("hub_01"), "visited room is drawn");
            Assert.AreEqual(RoomVisibility.Visited, map.Rooms["hub_01"].Visibility);
            Assert.IsFalse(map.Rooms.ContainsKey("forest_01"), "unvisited room of an unbought region stays hidden");
            Assert.IsNotNull(map.LeoMarker, "Leo's marker is placed in the current room");
            Assert.That(map.Icons.Count, Is.GreaterThanOrEqualTo(1), "the hub altar icon shows");
            Assert.IsTrue(PauseController.Instance.IsPaused);

            EventBus.Publish(new MapRequested()); // second press closes it
            yield return Frames(30);
            Assert.IsFalse(map.IsVisible);
            Assert.IsFalse(PauseController.Instance.IsPaused);
        }

        [UnityTest]
        public IEnumerator BuyingARegionMapRevealsItsRoomsDimmed()
        {
            yield return EnterWorld(true);
            var map = Find<MapScreen>();
            Manager.State.coins = 100;
            Assert.AreEqual(ShopResult.Bought, ShopService.Buy(ScriptableObject.CreateInstance<ShopItem>()
                .Configure(ShopItem.MapItemId("forest"), "n", "d", ShopEffect.MapRegion, 1, new[] { 50 }, 1, "forest")));

            EventBus.Publish(new MapRequested());
            yield return null;

            Assert.IsTrue(map.Rooms.ContainsKey("forest_01"));
            Assert.AreEqual(RoomVisibility.Dim, map.Rooms["forest_01"].Visibility);
            Assert.IsFalse(map.Rooms.ContainsKey("cave_01"), "only the bought region is revealed");
            UIRouter.Instance.Back();
        }

        [UnityTest]
        public IEnumerator ZoomAndDragButtonsMoveTheViewAndStayClamped()
        {
            yield return EnterWorld(true);
            var map = Find<MapScreen>();
            EventBus.Publish(new MapRequested());
            yield return null;
            float zoom = map.Viewport.Zoom;
            foreach (var button in map.GetComponentsInChildren<UIButton>(true))
                if (button.name == "ZoomIn") button.onClick.Invoke();
            Assert.Greater(map.Viewport.Zoom, zoom);
            for (int i = 0; i < 20; i++)
                foreach (var button in map.GetComponentsInChildren<UIButton>(true))
                    if (button.name == "ZoomIn") button.onClick.Invoke();
            Assert.AreEqual(MapViewport.DefaultMaxZoom, map.Viewport.Zoom, 1e-3f);
            UIRouter.Instance.Back();
        }

        [UnityTest]
        public IEnumerator MapCannotOpenWhileDeadOrPaused()
        {
            yield return EnterWorld(true);
            var map = Find<MapScreen>();
            PauseController.Instance.Pause(false);
            EventBus.Publish(new MapRequested());
            yield return null;
            Assert.IsFalse(map.IsVisible);
            PauseController.Instance.Resume();
        }
    }
}
