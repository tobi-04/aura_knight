using AuraKnight.Core;
using AuraKnight.World;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Idempotent scene generators: Test_Rooms grey-box and the Core scene managers/camera.
    /// Menu: Aura/Generate World Scenes. Batch: -executeMethod AuraKnight.Editor.WorldSceneGenerator.GenerateAll
    /// </summary>
    public static class WorldSceneGenerator
    {
        const string TestScenePath = "Assets/_Project/Scenes/Test/Test_Rooms.unity";
        const string CoreScenePath = "Assets/_Project/Scenes/Core.unity";
        // Same asset PlayerAssetGenerator writes (that generator lives in another editor assembly).
        const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";
        const float RoomWidth = 40f;
        const int TestRoomCount = 3;

        [MenuItem("Aura/Generate World Scenes")]
        public static void GenerateAll()
        {
            WorldAssetGenerator.GenerateAll();
            RegionSceneGenerator.GenerateAll();
            GenerateTestRooms();
            PopulateCore();
        }

        public static void GenerateTestRooms()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var template = AssetDatabase.LoadAssetAtPath<GameObject>(WorldAssetGenerator.RoomTemplatePath);
            var rooms = new Room[TestRoomCount];
            for (int i = 0; i < TestRoomCount; i++)
                rooms[i] = BuildTestRoom(template, i);

            var player = BuildTestPlayer();
            var cam = BuildCamera(player.transform);
            BuildManagers(cam.vcam, cam.confiner, null, true);
            cam.confiner.BoundingShape2D = rooms[0].Bounds;
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(TestScenePath));
            EditorSceneManager.SaveScene(scene, TestScenePath);
            Debug.Log("[WorldSceneGenerator] Test_Rooms saved.");
        }

        public static void PopulateCore()
        {
            var scene = EditorSceneManager.OpenScene(CoreScenePath, OpenSceneMode.Single);
            var cam = FindOrBuildCamera();
            var graph = AssetDatabase.LoadAssetAtPath<RegionGraph>(WorldAssetGenerator.RegionGraphPath);
            BuildManagers(cam.vcam, cam.confiner, graph, false);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[WorldSceneGenerator] Core scene populated.");
        }

        static Room BuildTestRoom(GameObject template, int index)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(template);
            go.name = $"Room_test_{index + 1:00}";
            go.transform.position = new Vector3(index * RoomWidth, 0f, 0f);
            var room = go.GetComponent<Room>();
            var so = new SerializedObject(room);
            so.FindProperty("roomId").stringValue = $"test_{index + 1:00}";
            so.FindProperty("regionId").stringValue = "test";
            var exits = so.FindProperty("exits");
            exits.arraySize = 0;

            var exitsRoot = go.transform.Find("Exits").gameObject;
            // Exit zones sit just across the border so the trigger for room B lives inside B (no ping-pong).
            if (index < TestRoomCount - 1)
                AddExit(so, exitsRoot, "ExitRight", $"test_{index + 2:00}", RoomWidth + 0.5f);
            if (index > 0)
                AddExit(so, exitsRoot, "ExitLeft", $"test_{index:00}", -0.5f);
            so.ApplyModifiedPropertiesWithoutUndo();

            Greybox.AddFloor(go.transform, RoomWidth);
            return room;
        }

        static void AddExit(SerializedObject roomSo, GameObject container, string name, string target, float x)
        {
            var go = WorldAssetGenerator.Child(container, name);
            go.transform.localPosition = new Vector3(x, 4f, 0f);
            PhysicsLayers.Apply(go, PhysicsLayers.Interactable);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 8f);
            var exit = go.AddComponent<RoomExit>();
            var exitSo = new SerializedObject(exit);
            exitSo.FindProperty("targetRoomId").stringValue = target;
            exitSo.ApplyModifiedPropertiesWithoutUndo();
            var list = roomSo.FindProperty("exits");
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = exit;
        }

        static GameObject BuildTestPlayer()
        {
            var go = new GameObject("TestPlayer") { tag = "Player" };
            PhysicsLayers.Apply(go, PhysicsLayers.Player);
            go.transform.position = new Vector3(3f, 3f, 0f);
            go.AddComponent<BoxCollider2D>().size = new Vector2(0.8f, 1.6f);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            return go;
        }

        static (CinemachineCamera vcam, CinemachineConfiner2D confiner) BuildCamera(Transform follow)
        {
            var cam = FindOrBuildCamera();
            cam.vcam.Follow = follow;
            return cam;
        }

        static (CinemachineCamera vcam, CinemachineConfiner2D confiner) FindOrBuildCamera()
        {
            var main = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = Ensure<Camera>(main);
            camera.orthographic = true;
            camera.orthographicSize = 5.5f;
            var brain = Ensure<CinemachineBrain>(main);
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 0.3f);

            var vcam = Object.FindAnyObjectByType<CinemachineCamera>();
            if (vcam == null) vcam = new GameObject("CM Camera").AddComponent<CinemachineCamera>();
            vcam.Lens.OrthographicSize = 5.5f;
            Ensure<CinemachinePositionComposer>(vcam.gameObject);
            var confiner = Ensure<CinemachineConfiner2D>(vcam.gameObject);
            return (vcam, confiner);
        }

        static GameObject BuildManagers(CinemachineCamera vcam, CinemachineConfiner2D confiner, RegionGraph graph,
                                          bool enterPlayerRoomOnStart)
        {
            var root = GameObject.Find("Managers");
            if (root == null) root = new GameObject("Managers");
            Ensure<GameManager>(root);
            Ensure<CheckpointService>(root);
            var rm = Ensure<RoomManager>(root);
            var rmSo = new SerializedObject(rm);
            rmSo.FindProperty("virtualCamera").objectReferenceValue = vcam;
            rmSo.FindProperty("confiner").objectReferenceValue = confiner;
            rmSo.FindProperty("enterPlayerRoomOnStart").boolValue = enterPlayerRoomOnStart;
            rmSo.ApplyModifiedPropertiesWithoutUndo();

            if (graph != null)
            {
                var loader = Ensure<RegionLoader>(root);
                var so = new SerializedObject(loader);
                so.FindProperty("graph").objectReferenceValue = graph;
                so.ApplyModifiedPropertiesWithoutUndo();

                var entry = Ensure<WorldEntry>(root);
                var entrySo = new SerializedObject(entry);
                entrySo.FindProperty("graph").objectReferenceValue = graph;
                entrySo.FindProperty("loader").objectReferenceValue = loader;
                entrySo.FindProperty("playerPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
                entrySo.ApplyModifiedPropertiesWithoutUndo();
                if (entrySo.FindProperty("playerPrefab").objectReferenceValue == null)
                    Debug.LogWarning("[WorldSceneGenerator] Player prefab missing; run the player asset generator first.");
            }
            return root;
        }

        static T Ensure<T>(GameObject go) where T : Component =>
            go.GetComponent<T>() != null ? go.GetComponent<T>() : go.AddComponent<T>();
    }
}
