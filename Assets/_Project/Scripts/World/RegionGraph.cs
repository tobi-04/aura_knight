using System;
using System.Collections.Generic;
using UnityEngine;

namespace AuraKnight.World
{
    /// <summary>One region: its scene and the regions reachable from it.</summary>
    [Serializable]
    public sealed class RegionNode
    {
        public string regionId;
        public string sceneName;
        public List<string> neighbors = new();
        [Tooltip("SunAltar ids that live in this region's scene. Respawn/continue use this to know which scene to load.")]
        public List<string> altarIds = new();

        public RegionNode() { }

        public RegionNode(string regionId, string sceneName, params string[] neighbors)
        {
            this.regionId = regionId;
            this.sceneName = sceneName;
            this.neighbors = new List<string>(neighbors);
        }

        public RegionNode WithAltars(params string[] ids)
        {
            altarIds = new List<string>(ids);
            return this;
        }
    }

    /// <summary>Region topology (GDD §7.1): region id to scene name and neighbor regions.</summary>
    [CreateAssetMenu(menuName = "Aura/Region Graph", fileName = "RegionGraph")]
    public sealed class RegionGraph : ScriptableObject
    {
        [SerializeField] List<RegionNode> regions = new();

        public IReadOnlyList<RegionNode> Regions => regions;

        public void SetRegions(List<RegionNode> nodes) => regions = nodes ?? new List<RegionNode>();

        public bool Contains(string regionId) => Find(regionId) != null;

        public bool TryGetSceneName(string regionId, out string sceneName)
        {
            sceneName = Find(regionId)?.sceneName;
            return !string.IsNullOrEmpty(sceneName);
        }

        /// <summary>The region whose scene contains the altar (declared in <see cref="RegionNode.altarIds"/>).</summary>
        public bool TryGetRegionOfAltar(string altarId, out string regionId)
        {
            regionId = null;
            if (string.IsNullOrEmpty(altarId)) return false;
            foreach (var r in regions)
            {
                if (r.altarIds == null || !r.altarIds.Contains(altarId)) continue;
                regionId = r.regionId;
                return true;
            }
            return false;
        }

        public IReadOnlyList<string> GetNeighbors(string regionId)
        {
            var node = Find(regionId);
            return node == null ? Array.Empty<string>() : node.neighbors;
        }

        /// <summary>Adjacency is symmetric: declaring it on either side is enough.</summary>
        public bool AreNeighbors(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            return Find(a)?.neighbors.Contains(b) == true || Find(b)?.neighbors.Contains(a) == true;
        }

        RegionNode Find(string regionId)
        {
            if (string.IsNullOrEmpty(regionId)) return null;
            foreach (var r in regions)
                if (r.regionId == regionId) return r;
            return null;
        }
    }
}
