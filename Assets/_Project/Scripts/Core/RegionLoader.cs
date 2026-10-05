using System.Collections;
using System.Collections.Generic;
using AuraKnight.World;
using UnityEngine;

namespace AuraKnight.Core
{
    /// <summary>
    /// Keeps the current region scene plus at most one preloaded neighbor loaded additively
    /// (Core stays resident). Reacts to RoomEntered; planning lives in RegionLoadPlan.
    /// Every load and unload operation is tracked per region: a region is never loaded while it is still
    /// unloading, never unloaded while it is still loading, and never unloaded twice. When an operation
    /// finishes the loader re-plans, so deferred work happens as soon as it is safe.
    /// </summary>
    public sealed class RegionLoader : MonoBehaviour
    {
        [SerializeField] RegionGraph graph;

        readonly Dictionary<string, AsyncOperation> loading = new();
        readonly Dictionary<string, AsyncOperation> unloading = new();
        string currentRegion;
        string preloadRegion;

        public string CurrentRegion => currentRegion;
        public bool IsBusy => loading.Count > 0 || unloading.Count > 0;

        void OnEnable() => EventBus.Subscribe<RoomEntered>(OnRoomEntered);
        void OnDisable() => EventBus.Unsubscribe<RoomEntered>(OnRoomEntered);

        /// <summary>True when the region's scene is fully loaded and no operation is running on it.</summary>
        public bool IsRegionLoaded(string regionId) =>
            graph != null && graph.TryGetSceneName(regionId, out var scene) && SceneLoader.IsLoaded(scene)
            && !loading.ContainsKey(regionId) && !unloading.ContainsKey(regionId);

        /// <summary>Makes a region current (e.g. from a save) and loads it if needed.</summary>
        public void SetCurrentRegion(string regionId) => Apply(regionId, null);

        /// <summary>Starts loading a neighbor of the current region ahead of the player.</summary>
        public void Preload(string regionId) => Apply(currentRegion, regionId);

        void OnRoomEntered(RoomEntered evt)
        {
            string preload = RoomRegistry.TryGet(evt.RoomId, out var room) ? room.PreloadRegionId : null;
            Apply(evt.RegionId, preload);
        }

        void Apply(string current, string preload)
        {
            if (graph == null)
            {
                Debug.LogWarning("[RegionLoader] No RegionGraph assigned.", this);
                return;
            }
            currentRegion = current;
            preloadRegion = preload;

            var plan = RegionLoadPlan.Resolve(graph, current, preload, LoadedRegions(), loading.Keys, unloading.Keys);
            foreach (var id in plan.ToLoad) BeginLoad(id);
            foreach (var id in plan.ToUnload) BeginUnload(id);
        }

        HashSet<string> LoadedRegions()
        {
            var set = new HashSet<string>();
            foreach (var node in graph.Regions)
                if (!loading.ContainsKey(node.regionId) && !unloading.ContainsKey(node.regionId)
                    && SceneLoader.IsLoaded(node.sceneName)) set.Add(node.regionId);
            return set;
        }

        void BeginLoad(string regionId)
        {
            if (!graph.TryGetSceneName(regionId, out var scene)) return;
            var op = SceneLoader.LoadAdditive(scene);
            if (op == null) return;
            loading[regionId] = op;
            StartCoroutine(WaitThenReconcile(op, () => loading.Remove(regionId)));
        }

        void BeginUnload(string regionId)
        {
            if (!graph.TryGetSceneName(regionId, out var scene)) return;
            var op = SceneLoader.Unload(scene);
            if (op == null) return;
            unloading[regionId] = op;
            StartCoroutine(WaitThenReconcile(op, () => unloading.Remove(regionId)));
        }

        IEnumerator WaitThenReconcile(AsyncOperation op, System.Action finish)
        {
            yield return op;
            finish();
            Apply(currentRegion, preloadRegion);
        }
    }
}
