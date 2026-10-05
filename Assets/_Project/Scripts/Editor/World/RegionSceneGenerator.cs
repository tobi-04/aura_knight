using AuraKnight.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Puts a greybox start room and its sun altar into every region scene listed in the RegionGraph, so every altar id in the
    /// graph exists in its scene (continue, respawn and new game all need that). Rebuilt idempotently under one "StartRoom" root.
    /// Menu: Aura/Generate Region Start Rooms. Batch: AuraKnight.Editor.RegionSceneGenerator.GenerateAll
    /// </summary>
    public static class RegionSceneGenerator
    {
        const string ScenesDir = "Assets/_Project/Scenes";
        const string RootName = "StartRoom";
        const float RoomWidth = 40f, RegionSpacing = 100f;

        [MenuItem("Aura/Generate Region Start Rooms")]
        public static void GenerateAll()
        {
            WorldAssetGenerator.GenerateAll();
            var graph = AssetDatabase.LoadAssetAtPath<RegionGraph>(WorldAssetGenerator.RegionGraphPath);
            var template = AssetDatabase.LoadAssetAtPath<GameObject>(WorldAssetGenerator.RoomTemplatePath);
            var altar = AssetDatabase.LoadAssetAtPath<GameObject>(WorldAssetGenerator.SunAltarPath);
            for (int i = 0; i < graph.Regions.Count; i++)
                Populate(graph.Regions[i], i, template, altar);
            Debug.Log($"[RegionSceneGenerator] Populated {graph.Regions.Count} region scene(s).");
        }

        static void Populate(RegionNode region, int index, GameObject template, GameObject altarPrefab)
        {
            string path = $"{ScenesDir}/{region.sceneName}.unity";
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == RootName) Object.DestroyImmediate(root);

            string roomId = $"{region.regionId}_01";
            var room = (GameObject)PrefabUtility.InstantiatePrefab(template, scene);
            room.name = RootName;
            room.transform.position = new Vector3(index * RegionSpacing, 0f, 0f);
            var so = new SerializedObject(room.GetComponent<Room>());
            so.FindProperty("roomId").stringValue = roomId;
            so.FindProperty("regionId").stringValue = region.regionId;
            so.ApplyModifiedPropertiesWithoutUndo();
            Greybox.AddFloor(room.transform, RoomWidth);

            if (region.altarIds.Count > 0)
            {
                var altar = (GameObject)PrefabUtility.InstantiatePrefab(altarPrefab, room.transform);
                altar.transform.localPosition = new Vector3(6f, 1f, 0f);
                var altarSo = new SerializedObject(altar.GetComponent<SunAltar>());
                altarSo.FindProperty("altarId").stringValue = region.altarIds[0];
                altarSo.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
