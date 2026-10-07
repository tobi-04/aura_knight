namespace AuraKnight.Editor
{
    /// <summary>Asset locations owned by the UI phase (generators write them, tests and scene builders read them).</summary>
    public static class UiAssetPaths
    {
        public const string FontsDir = "Assets/_Project/Art/Fonts";
        public const string UiArtDir = "Assets/_Project/Art/UI";
        public const string KeyArtDir = "Assets/_Project/Art/KeyArt";
        public const string DataDir = "Assets/_Project/Data/UI";
        public const string ResourcesDir = "Assets/_Project/Data/UI/Resources";
        public const string PrefabDir = "Assets/_Project/Prefabs/UI";
        public const string ScenesDir = "Assets/_Project/Scenes";

        public const string Theme = ResourcesDir + "/UITheme.asset";
        public const string StringsVi = ResourcesDir + "/Strings_vi.json";
        public const string CreditsText = ResourcesDir + "/CreditsText.txt";

        public const string HudPrefab = PrefabDir + "/Hud.prefab";
        public const string GameScreensPrefab = PrefabDir + "/GameScreens.prefab";
        public const string MenuScreensPrefab = PrefabDir + "/MenuScreens.prefab";
        public const string VirtualControlsPrefab = PrefabDir + "/VirtualControls.prefab";

        public const string CoreScene = ScenesDir + "/Core.unity";
        public const string MainMenuScene = ScenesDir + "/MainMenu.unity";

        public const string HeartSprite = UiArtDir + "/ui-heart.png";
        public const string SunSprite = UiArtDir + "/ui-sun.png";
        public const string WhiteSprite = UiArtDir + "/ui-white.png";
        public const string DiscSprite = UiArtDir + "/ui-disc.png";
        public const string RingSprite = UiArtDir + "/ui-ring.png";
        public const string GradientSprite = UiArtDir + "/ui-gradient-left.png";

        public const string HeroArt = KeyArtDir + "/key-art-hero-moon.jpg";
        public static string AuraArt(string id) => $"{KeyArtDir}/key-art-aura-{id}.jpg";
    }
}
