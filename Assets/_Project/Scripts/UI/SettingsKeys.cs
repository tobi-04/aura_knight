namespace AuraKnight.UI
{
    /// <summary>
    /// PlayerPrefs keys for personal settings (kept apart from the save game). Other systems read these directly:
    /// the Audio layer reads <see cref="MusicVolume"/> and <see cref="SfxVolume"/> (floats 0..1, default
    /// <see cref="GameSettings.DefaultVolume"/>) and listens for <see cref="SettingsChanged"/> on the EventBus.
    /// </summary>
    public static class SettingsKeys
    {
        public const string MusicVolume = "settings.musicVolume";
        public const string SfxVolume = "settings.sfxVolume";
        /// <summary>Same key <see cref="AuraKnight.Core.Haptics"/> reads (int 0/1, default 1).</summary>
        public const string Haptics = AuraKnight.Core.Haptics.PrefsKey;
        /// <summary>Virtual button size, float 0.8..1.3 (GDD 3.2).</summary>
        public const string ButtonScale = "settings.buttonScale";
        /// <summary>Virtual button opacity, float 0.3..1.</summary>
        public const string ButtonOpacity = "settings.buttonOpacity";
        /// <summary>Power-saving mode (30 fps), int 0/1.</summary>
        public const string PowerSaving = "settings.powerSaving";
        /// <summary>Language code, string ("vi" default; "en" is P2).</summary>
        public const string Language = "settings.language";

        public static readonly string[] All =
        {
            MusicVolume, SfxVolume, Haptics, ButtonScale, ButtonOpacity, PowerSaving, Language
        };
    }
}
