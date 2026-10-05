using System.Collections.Generic;
using System.Linq;
using AuraKnight.Combat;
using AuraKnight.Core;
using AuraKnight.World;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Scans room prefabs under Assets/_Project/Prefabs/Rooms (skipping _Template) and reports duplicate ids,
    /// exits to missing rooms and duplicate PersistentIds. Rules live in AuraKnight.World.RoomValidator.
    /// Menu: Aura/Validate Rooms. Batch: -executeMethod AuraKnight.Editor.RoomIdValidator.RunBatch
    /// </summary>
    public static class RoomIdValidator
    {
        const string RoomsRoot = "Assets/_Project/Prefabs/Rooms";
        const string ScenesRoot = "Assets/_Project/Scenes";
        const string TemplateFolder = "/_Template/";

        [MenuItem("Aura/Validate Rooms")]
        public static void ValidateMenu()
        {
            var errors = Validate(out int count);
            Report(errors, count);
        }

        /// <summary>Exit code 1 when any error is found, so CI can gate on it.</summary>
        public static void RunBatch()
        {
            var errors = Validate(out int count);
            Report(errors, count);
            EditorApplication.Exit(errors.Count == 0 ? 0 : 1);
        }

        public static List<string> Validate(out int roomCount)
        {
            var graph = AssetDatabase.LoadAssetAtPath<RegionGraph>(WorldAssetGenerator.RegionGraphPath);
            var regions = graph != null ? graph.Regions : null;
            var infos = new List<RoomInfo>();
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { RoomsRoot });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains(TemplateFolder)) continue;
                var info = Read(path);
                if (info != null) infos.Add(info);
            }
            roomCount = infos.Count;
            var errors = RoomValidator.Validate(infos, regions);
            if (regions != null) errors.AddRange(ValidateAltarDirectory(regions));
            else errors.Add($"RegionGraph not found at {WorldAssetGenerator.RegionGraphPath}.");
            return errors;
        }

        /// <summary>Graph rules plus: every altar listed for a region exists in that region's scene (and nothing else is placed there).</summary>
        static List<string> ValidateAltarDirectory(IReadOnlyList<RegionNode> regions)
        {
            var errors = AltarValidator.ValidateGraph(regions, GameState.StartAltarId);
            foreach (var region in regions)
            {
                string path = $"{ScenesRoot}/{region.sceneName}.unity";
                if (!System.IO.File.Exists(path)) { errors.Add($"Scene {path} of region '{region.regionId}' does not exist."); continue; }
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    var placed = new List<string>();
                    foreach (var root in scene.GetRootGameObjects())
                        foreach (var altar in root.GetComponentsInChildren<SunAltar>(true)) placed.Add(altar.AltarId);
                    errors.AddRange(AltarValidator.ValidatePlacement(regions, region.regionId, placed, path, requireAllListed: true));
                }
                finally { EditorSceneManager.CloseScene(scene, true); }
            }
            return errors;
        }

        static RoomInfo Read(string path)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var room = root != null ? root.GetComponent<Room>() : null;
            if (room == null)
            {
                Debug.LogWarning($"[RoomIdValidator] {path} has no Room component; skipped.");
                return null;
            }
            var exits = root.GetComponentsInChildren<RoomExit>(true).Select(e => e.TargetRoomId).ToList();
            var pids = root.GetComponentsInChildren<PersistentId>(true).Select(p => p.Id).ToList();
            var altars = root.GetComponentsInChildren<SunAltar>(true).Select(a => a.AltarId).ToList();
            var gates = root.GetComponentsInChildren<OneTimeAuraGate>(true)
                .Where(g => !g.TryGetComponent<PersistentId>(out _)).Select(g => g.name).ToList();
            var mixed = root.GetComponentsInChildren<Hitbox>(true)
                .Where(h => h.TryGetComponent<Hurtbox>(out var hurt) && hurt.Team != h.Team).Select(h => h.name).ToList();
            return new RoomInfo(room.RoomId, room.RegionId, path, exits, pids, altars, gates, mixed);
        }

        static void Report(List<string> errors, int count)
        {
            foreach (var e in errors) Debug.LogError($"[RoomIdValidator] {e}");
            Debug.Log($"[RoomIdValidator] Scanned {count} room prefab(s): {errors.Count} error(s).");
        }
    }
}
