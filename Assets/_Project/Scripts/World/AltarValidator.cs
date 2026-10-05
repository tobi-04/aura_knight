using System.Collections.Generic;
using System.Linq;

namespace AuraKnight.World
{
    /// <summary>
    /// Pure rules for the altar directory kept in the RegionGraph (altar id to region): respawn and continue rely on it
    /// to know which scene to load, so it must be unique, contain the start altar and match what is actually placed.
    /// </summary>
    public static class AltarValidator
    {
        /// <summary>Graph-level checks: ids unique across regions, and the start altar is listed somewhere.</summary>
        public static List<string> ValidateGraph(IReadOnlyList<RegionNode> regions, string startAltarId)
        {
            var errors = new List<string>();
            var owner = new Dictionary<string, string>();
            foreach (var region in regions)
            {
                foreach (var id in region.altarIds)
                {
                    if (string.IsNullOrEmpty(id)) errors.Add($"Region '{region.regionId}' lists an empty altar id.");
                    else if (owner.TryGetValue(id, out var first))
                        errors.Add($"Altar id '{id}' is listed for both '{first}' and '{region.regionId}'.");
                    else owner[id] = region.regionId;
                }
            }
            if (!owner.ContainsKey(startAltarId))
                errors.Add($"The start altar '{startAltarId}' is not listed in any region of the RegionGraph.");
            return errors;
        }

        /// <summary>
        /// Placement checks for one source (a room prefab or a region scene): every placed altar must be listed for
        /// <paramref name="regionId"/>; with <paramref name="requireAllListed"/> every listed altar must also be placed.
        /// </summary>
        public static List<string> ValidatePlacement(IReadOnlyList<RegionNode> regions, string regionId,
                                                     IReadOnlyCollection<string> placedAltarIds, string source, bool requireAllListed)
        {
            var errors = new List<string>();
            RegionNode node = null;
            foreach (var region in regions)
                if (region.regionId == regionId) node = region;
            if (node == null)
            {
                if (placedAltarIds.Count > 0) errors.Add($"{source} has altars but region '{regionId}' is not in the RegionGraph.");
                return errors;
            }
            foreach (var id in placedAltarIds)
                if (!node.altarIds.Contains(id))
                    errors.Add($"Altar '{id}' in {source} is not listed for region '{regionId}' in the RegionGraph.");
            if (requireAllListed)
                foreach (var id in node.altarIds)
                    if (!placedAltarIds.Contains(id))
                        errors.Add($"Altar '{id}' is listed for region '{regionId}' but missing from {source}.");
            return errors;
        }
    }
}
