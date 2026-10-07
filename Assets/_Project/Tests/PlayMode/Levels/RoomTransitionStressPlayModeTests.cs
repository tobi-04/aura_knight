using System.Collections;
using AuraKnight.World;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.TestTools;

namespace AuraKnight.Tests.PlayMode.Levels
{
    /// <summary>GDD 15.1: switching rooms back and forth 50 times loses neither the camera nor the player.</summary>
    public sealed class RoomTransitionStressPlayModeTests : LevelsPlayModeBase
    {
        [UnityTest, Timeout(300000)]
        public IEnumerator FiftyDoorCrossingsBetweenTwoHubRoomsKeepTheCameraAndLeo()
        {
            yield return NewGame();
            MakeInvulnerable();
            var vcam = Object.FindAnyObjectByType<CinemachineCamera>();
            var confiner = Object.FindAnyObjectByType<CinemachineConfiner2D>();
            Assert.IsNotNull(vcam);
            Assert.IsNotNull(confiner);
            for (int i = 0; i < 25; i++)
            {
                yield return Through("hub_02");
                yield return Through("hub_01");
            }
            yield return Seconds(1.5f); // the last camera blend ends
            Assert.AreEqual("hub_01", CurrentRoomId);
            Assert.IsNotNull(Player, "Leo is still in the scene");
            Assert.IsTrue(RoomManager.Instance.Current.Contains(PlayerPosition), "Leo is inside the current room");
            Assert.AreSame(Player.transform, vcam.Follow, "the camera still follows Leo");
            Assert.AreSame(RoomManager.Instance.Current.Bounds, confiner.BoundingShape2D, "the camera is confined to the current room");
            var camera = Camera.main.transform.position;
            Assert.IsTrue(RoomManager.Instance.Current.Bounds.bounds.Contains(new Vector3(camera.x, camera.y, RoomManager.Instance.Current.Bounds.bounds.center.z)),
                "the camera is inside the current room");
            Assert.IsFalse(Player.GetComponent<AuraKnight.Combat.Health>().IsDead);
        }
    }
}
