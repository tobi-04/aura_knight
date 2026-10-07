using AuraKnight.Core;
using UnityEngine;

namespace AuraKnight.UI
{
    /// <summary>
    /// Typed access to the PlayerPrefs settings (<see cref="SettingsKeys"/>): clamps every value, falls back to defaults
    /// for missing or garbage data, and publishes <see cref="SettingsChanged"/> on each write.
    /// </summary>
    public static class GameSettings
    {
        public const float DefaultVolume = 0.8f;
        public const float MinButtonScale = 0.8f, MaxButtonScale = 1.3f;
        public const float MinButtonOpacity = 0.3f, MaxButtonOpacity = 1f, DefaultButtonOpacity = 0.6f;
        public const int NormalFps = 60, PowerSavingFps = 30;

        public static float MusicVolume
        {
            get => Read(SettingsKeys.MusicVolume, DefaultVolume, 0f, 1f);
            set => Write(SettingsKeys.MusicVolume, value, 0f, 1f, DefaultVolume);
        }

        public static float SfxVolume
        {
            get => Read(SettingsKeys.SfxVolume, DefaultVolume, 0f, 1f);
            set => Write(SettingsKeys.SfxVolume, value, 0f, 1f, DefaultVolume);
        }

        public static float ButtonScale
        {
            get => Read(SettingsKeys.ButtonScale, 1f, MinButtonScale, MaxButtonScale);
            set => Write(SettingsKeys.ButtonScale, value, MinButtonScale, MaxButtonScale, 1f);
        }

        public static float ButtonOpacity
        {
            get => Read(SettingsKeys.ButtonOpacity, DefaultButtonOpacity, MinButtonOpacity, MaxButtonOpacity);
            set => Write(SettingsKeys.ButtonOpacity, value, MinButtonOpacity, MaxButtonOpacity, DefaultButtonOpacity);
        }

        public static bool HapticsEnabled
        {
            get => Haptics.Enabled;
            set
            {
                Haptics.SetEnabled(value);
                EventBus.Publish(new SettingsChanged(SettingsKeys.Haptics));
            }
        }

        public static bool PowerSaving
        {
            get => PlayerPrefs.GetInt(SettingsKeys.PowerSaving, 0) != 0;
            set
            {
                PlayerPrefs.SetInt(SettingsKeys.PowerSaving, value ? 1 : 0);
                PlayerPrefs.Save();
                ApplyFrameRate();
                EventBus.Publish(new SettingsChanged(SettingsKeys.PowerSaving));
            }
        }

        public static string Language
        {
            get
            {
                string code = PlayerPrefs.GetString(SettingsKeys.Language, Localization.DefaultLanguage);
                return Localization.IsSupported(code) ? code : Localization.DefaultLanguage;
            }
            set
            {
                string code = Localization.IsSupported(value) ? value : Localization.DefaultLanguage;
                PlayerPrefs.SetString(SettingsKeys.Language, code);
                PlayerPrefs.Save();
                Localization.SetLanguage(code);
                EventBus.Publish(new SettingsChanged(SettingsKeys.Language));
            }
        }

        /// <summary>Pushes the stored settings into the engine (frame rate, haptics cache, language). Call once at scene start.</summary>
        public static void ApplyAll()
        {
            Haptics.Refresh();
            ApplyFrameRate();
            Localization.SetLanguage(Language);
        }

        /// <summary>Deletes every setting key (back to defaults).</summary>
        public static void ResetToDefaults()
        {
            foreach (string key in SettingsKeys.All) PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            Haptics.Refresh();
            ApplyFrameRate();
            Localization.SetLanguage(Localization.DefaultLanguage);
        }

        /// <summary>Clamps to a range; NaN/infinity (corrupt prefs) become the fallback.</summary>
        public static float Sanitize(float value, float min, float max, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return fallback;
            return Mathf.Clamp(value, min, max);
        }

        static void ApplyFrameRate() => Application.targetFrameRate = PowerSaving ? PowerSavingFps : NormalFps;

        static float Read(string key, float fallback, float min, float max) =>
            Sanitize(PlayerPrefs.GetFloat(key, fallback), min, max, fallback);

        static void Write(string key, float value, float min, float max, float fallback)
        {
            PlayerPrefs.SetFloat(key, Sanitize(value, min, max, fallback));
            PlayerPrefs.Save();
            EventBus.Publish(new SettingsChanged(key));
        }
    }
}
