namespace AuraKnight.World
{
    /// <summary>Global Light 2D intensity per region (GDD 10): the hub is bright, the Castle is nearly black outside Leo's glow.</summary>
    public static class RegionLightingTable
    {
        public const float Hub = 1f;
        public const float Forest = 0.6f;
        public const float Cave = 0.25f;
        public const float City = 0.45f;
        public const float Castle = 0.05f;

        public static float IntensityOf(string regionId)
        {
            switch (regionId)
            {
                case "forest": return Forest;
                case "cave": return Cave;
                case "city": return City;
                case "castle": return Castle;
                default: return Hub;
            }
        }
    }
}
