namespace AuraKnight.Editor
{
    /// <summary>Asset paths and the region / variant lists shared by the art pipeline.</summary>
    public static class ArtPaths
    {
        public const string Root = "Assets/_Project/Art";
        public const string PresetPath = Root + "/Presets/Sprite_Pixel32.preset";
        public const string MaterialPath = Root + "/Materials/Mat_SpriteLit.mat";
        public const string LeoFolder = Root + "/Characters/Leo";
        public const string LeoController = LeoFolder + "/Leo.controller";
        public const string LeoSheet = LeoFolder + "/Leo.png";
        public const string DataFolder = "Assets/_Project/Data/Art";

        /// <summary>Folders whose textures get the Pixel32 settings automatically on import.</summary>
        public static readonly string[] PixelFolders =
        {
            Root + "/Characters", Root + "/Enemies", Root + "/Bosses", Root + "/Tilesets", Root + "/Backgrounds"
        };

        public static readonly string[] Regions = { "Hub", "Forest", "Cave", "City", "Castle" };

        public static readonly string[] Enemies =
        {
            "ThornBug", "MushroomHopper", "Bat", "StoneSpider", "PatrolRobot", "ScrapZapper", "NightKnight", "Ghost"
        };

        public static readonly string[] Bosses = { "RootTree", "GiantStoneSpider", "RogueMachine", "Malakor" };

        /// <summary>Enemy sheet folder name per variant (StoneSpider's sheet folder is StoneSpider, Ghost is Ghost).</summary>
        public static string EnemySheet(string variant) => $"{Root}/Enemies/{variant}/{variant}.png";

        public static string BossSheet(string boss) => $"{Root}/Bosses/{boss}/{boss}.png";
    }
}
