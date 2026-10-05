using System.Collections.Generic;

namespace AuraKnight.World
{
    /// <summary>
    /// Pure decision of which region scenes to load/unload. At most two regions stay loaded
    /// (current plus one preloaded neighbor); the Core scene is never part of the plan.
    /// Regions mid-load cannot be unloaded and regions mid-unload cannot be loaded: those wait in
    /// <see cref="Deferred"/> and the loader re-plans when the operation finishes.
    /// </summary>
    public sealed class RegionLoadPlan
    {
        public const int MaxLoadedRegions = 2;

        public List<string> Desired { get; } = new();
        public List<string> ToLoad { get; } = new();
        public List<string> ToUnload { get; } = new();
        /// <summary>Regions that need a change but are blocked by an operation still running on them.</summary>
        public List<string> Deferred { get; } = new();

        public bool IsSettled => ToLoad.Count == 0 && ToUnload.Count == 0 && Deferred.Count == 0;

        /// <summary><paramref name="loaded"/> = regions loaded or loading (nothing is unloading).</summary>
        public static RegionLoadPlan Resolve(RegionGraph graph, string currentRegion, string preloadRegion,
                                             ICollection<string> loaded) =>
            Resolve(graph, currentRegion, preloadRegion, loaded, new string[0], new string[0]);

        /// <param name="loaded">Regions whose scene is fully loaded.</param>
        /// <param name="loading">Regions with a load operation in flight.</param>
        /// <param name="unloading">Regions with an unload operation in flight.</param>
        public static RegionLoadPlan Resolve(RegionGraph graph, string currentRegion, string preloadRegion,
                                             ICollection<string> loaded, ICollection<string> loading,
                                             ICollection<string> unloading)
        {
            var plan = new RegionLoadPlan();
            if (graph == null || !graph.Contains(currentRegion)) return plan;

            plan.Desired.Add(currentRegion);
            if (!string.IsNullOrEmpty(preloadRegion) && preloadRegion != currentRegion
                && graph.Contains(preloadRegion) && graph.AreNeighbors(currentRegion, preloadRegion)
                && plan.Desired.Count < MaxLoadedRegions)
                plan.Desired.Add(preloadRegion);

            foreach (var id in plan.Desired)
            {
                if (loaded.Contains(id) || loading.Contains(id)) continue;
                if (unloading.Contains(id)) plan.Deferred.Add(id);
                else plan.ToLoad.Add(id);
            }
            foreach (var id in loaded)
                if (!plan.Desired.Contains(id) && !unloading.Contains(id)) plan.ToUnload.Add(id);
            foreach (var id in loading)
                if (!plan.Desired.Contains(id) && !unloading.Contains(id)) plan.Deferred.Add(id);
            return plan;
        }
    }
}
