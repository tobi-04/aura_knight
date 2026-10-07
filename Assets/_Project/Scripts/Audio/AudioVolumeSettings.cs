using UnityEngine;
using UnityEngine.Audio;

namespace AuraKnight.Audio
{
    /// <summary>
    /// Music / SFX volume from the settings screen. The sliders store floats 0..1 in PlayerPrefs
    /// ("settings.musicVolume", "settings.sfxVolume", default 0.8); this class pushes them to the mixer's exposed params.
    /// UI sounds follow the SFX slider. The settings screen calls <see cref="Set"/> (or <see cref="Apply"/> after writing the prefs itself).
    /// </summary>
    public static class AudioVolumeSettings
    {
        public const string MusicKey = "settings.musicVolume";
        public const string SfxKey = "settings.sfxVolume";
        public const float DefaultVolume = 0.8f;
        public const string MusicParam = "MusicVolume";
        public const string SfxParam = "SfxVolume";
        public const string UiParam = "UiVolume";

        public static float MusicVolume => Read(MusicKey);
        public static float SfxVolume => Read(SfxKey);

        /// <summary>Saves both volumes (clamped to 0..1) and applies them. Returns false when no mixer is loaded yet (they apply on the next <see cref="Apply"/>).</summary>
        public static bool Set(float music, float sfx)
        {
            PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(music));
            PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(sfx));
            PlayerPrefs.Save();
            return Apply();
        }

        /// <summary>Reads the stored volumes and writes them to the AudioManager's mixer. False when there is no manager or mixer.</summary>
        public static bool Apply()
        {
            var manager = AudioManager.Instance;
            return manager != null && ApplyTo(manager.Mixer, MusicVolume, SfxVolume);
        }

        /// <summary>Writes linear volumes to the exposed params of <paramref name="mixer"/> as decibels.</summary>
        public static bool ApplyTo(AudioMixer mixer, float music, float sfx)
        {
            if (mixer == null) return false;
            bool ok = mixer.SetFloat(MusicParam, AudioMath.LinearToDb(music));
            float sfxDb = AudioMath.LinearToDb(sfx);
            ok &= mixer.SetFloat(SfxParam, sfxDb);
            ok &= mixer.SetFloat(UiParam, sfxDb);
            return ok;
        }

        static float Read(string key) => Mathf.Clamp01(PlayerPrefs.GetFloat(key, DefaultVolume));
    }
}
