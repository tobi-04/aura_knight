#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using AuraKnight.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AuraKnight.Tests.PlayMode
{
    /// <summary>Room hand-off timing: the room being left stays alive while the camera is still blending.</summary>
    public sealed class RoomSwitchPlayModeTests : WorldPlayTestBase
    {
        const string TemplatePath = "Assets/_Project/Prefabs/Rooms/_Template/Room_Template.prefab";

        static void SetField(Room room, string field, string value) =>
            typeof(Room).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(room, value);

        /// <summary>An extra room of the hub region (same region, so the region loader keeps the scene), registered under its own id (hub_99: the real hub has hub_01 to hub_03).</summary>
        static Room AddSecondHubRoom()
        {
            var parked = new GameObject("Parked");
            parked.SetActive(false); // children stay inactive, so Awake/OnEnable wait until the ids are set
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TemplatePath);
            var instance = Object.Instantiate(prefab, parked.transform);
            var room = instance.GetComponent<Room>();
            SetField(room, "roomId", "hub_99");
            SetField(room, "regionId", "hub");
            instance.transform.position = new Vector3(200f, 0f, 0f);
            instance.transform.SetParent(null);
            SceneManager.MoveGameObjectToScene(instance, SceneManager.GetSceneByName("Region_Hub"));
            Object.Destroy(parked);
            return room;
        }

        static GameObject EnemiesOf(string roomId)
        {
            Assert.IsTrue(RoomRegistry.TryGet(roomId, out var room), roomId);
            return room.transform.Find("Enemies").gameObject;
        }

        IEnumerator LoadHubWithTwoRooms()
        {
            yield return SceneManager.LoadSceneAsync("Region_Hub", LoadSceneMode.Additive);
            yield return null;
            AddSecondHubRoom();
            yield return null;
        }

        [UnityTest]
        public IEnumerator LeftRoomKeepsItsEnemiesUntilTheCameraBlendIsOver()
        {
            yield return LoadHubWithTwoRooms();
            var rooms = RoomManager.Instance;
            Assert.IsTrue(rooms.EnterRoom("hub_01"));
            Assert.IsTrue(EnemiesOf("hub_01").activeSelf);

            Assert.IsTrue(rooms.EnterRoom("hub_99"));
            Assert.IsTrue(EnemiesOf("hub_99").activeSelf, "the entered room is live at once");
            Assert.IsTrue(EnemiesOf("hub_01").activeSelf, "the room being left stays live while the camera blends");

            yield return WaitUntil(() => !EnemiesOf("hub_01").activeSelf, "left room switched off after the blend", 5f);
            Assert.IsTrue(EnemiesOf("hub_99").activeSelf);
        }

        [UnityTest]
        public IEnumerator ReenteringARoomDuringTheBlendKeepsItAlive()
        {
            yield return LoadHubWithTwoRooms();
            var rooms = RoomManager.Instance;
            rooms.EnterRoom("hub_01");
            rooms.EnterRoom("hub_99");
            rooms.EnterRoom("hub_01"); // back before the delayed deactivation fires
            yield return new WaitForSeconds(RoomBlendTiming.HoldSeconds(0.3f, 1f) + 0.3f);
            Assert.IsTrue(EnemiesOf("hub_01").activeSelf, "the stale deactivation of hub_01 must not hit the current room");
            Assert.IsFalse(EnemiesOf("hub_99").activeSelf);
        }
    }
}
#endif
