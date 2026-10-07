using AuraKnight.Aura;
using AuraKnight.Bosses;
using AuraKnight.Player;
using AuraKnight.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Builds Scenes/Test/Test_Boss_&lt;Name&gt;.unity: the boss's arena room prefab at the origin, the Player at the room spawn with the Auras owned before this
    /// boss, a camera, a checkpoint and the editor-only <see cref="BossDebugControls"/> ("Skip to next phase"). Walk right to start the fight.
    /// </summary>
    static class BossTestSceneGenerator
    {
        public static void Generate(BossSpec spec)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(spec.RoomPath) == null)
            {
                Debug.LogError($"[BossTestSceneGenerator] {spec.RoomPath} is missing; run BossAssetGenerator.Generate first.");
                return;
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var room = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(spec.RoomPath));
            var spawn = room.transform.Find("SpawnPoints/default");
            var start = spawn != null ? spawn.position : new Vector3(3.5f, 2f, 0f);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerAssetGenerator.PlayerPrefabPath));
            player.transform.position = start;
            var ids = new int[spec.AurasBefore.Length];
            for (int i = 0; i < ids.Length; i++) ids[i] = (int)spec.AurasBefore[i];
            AuraSerialized.SetEnumArray(player.GetComponent<AuraManager>(), "debugUnlocked", ids);
            TestCheckpoint.Build(new Vector2(start.x, start.y));
            CreateCamera(player.transform);
            var hud = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerAssetGenerator.VirtualControlsPrefabPath));
            hud.name = "VirtualControls";
            VirtualControlsBuilder.CreateEventSystem();

            var debug = new GameObject("BossDebugControls").AddComponent<BossDebugControls>();
            PlayerGeneratorUtil.SetReference(debug, "boss", room.GetComponentInChildren<BossBase>(true));

            PlayerGeneratorUtil.EnsureFolder(BossSpec.SceneFolder);
            EditorSceneManager.SaveScene(scene, spec.ScenePath);
            Debug.Log($"[BossTestSceneGenerator] Saved {spec.ScenePath}");
        }

        static void CreateCamera(Transform target)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x07, 0x0D, 0x1F, 0xFF);
            go.transform.position = new Vector3(target.position.x, 4f, -10f);
            PlayerGeneratorUtil.SetReference(go.AddComponent<SimpleCameraFollow>(), "target", target);
        }
    }
}
