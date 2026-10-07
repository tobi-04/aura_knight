using System.Collections.Generic;
using AuraKnight.Enemies;
using AuraKnight.World;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Level-design rule check: no room prefab under Prefabs/Rooms may hold more than <see cref="EnemyLimits.MaxActivePerRoom"/>
    /// enemies in its enemies container. Menu: Aura/Enemies/Validate Room Limits. Batch: AuraKnight.Editor.EnemyRoomLimitValidator.RunBatch
    /// </summary>
    public static class EnemyRoomLimitValidator
    {
        const string RoomsRoot = "Assets/_Project/Prefabs/Rooms";

        [MenuItem("Aura/Enemies/Validate Room Limits")]
        public static void ValidateMenu() => Report(Validate());

        public static void RunBatch()
        {
            var errors = Validate();
            Report(errors);
            EditorApplication.Exit(errors.Count == 0 ? 0 : 1);
        }

        public static List<string> Validate()
        {
            var errors = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { RoomsRoot }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null || !prefab.TryGetComponent<Room>(out _)) continue;
                int count = prefab.GetComponentsInChildren<EnemyBase>(true).Length;
                if (!EnemyLimits.IsWithinLimit(count))
                    errors.Add($"{path}: {count} enemies, the limit is {EnemyLimits.MaxActivePerRoom} per room");
            }
            return errors;
        }

        static void Report(List<string> errors)
        {
            foreach (var e in errors) Debug.LogError("[EnemyRoomLimitValidator] " + e);
            Debug.Log($"[EnemyRoomLimitValidator] {errors.Count} errors");
        }
    }
}
