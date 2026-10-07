using UnityEngine;

namespace AuraKnight.Audio
{
    /// <summary>Which mixer group a sound plays through.</summary>
    public enum SfxBus { Sfx, Ui }

    /// <summary>Pure audio rules: volume to decibels, pitch variance, bus routing. No scene state.</summary>
    public static class AudioMath
    {
        public const float SilenceDb = -80f;
        public const float DefaultPitchVariance = 0.05f;
        public const float MaxPitchVariance = 0.2f;

        /// <summary>Linear volume 0..1 to mixer decibels. Zero (and anything below -80 dB) is silence; above 1 clamps to 0 dB.</summary>
        public static float LinearToDb(float volume)
        {
            if (volume <= 0.0001f) return SilenceDb;
            return Mathf.Clamp(20f * Mathf.Log10(volume), SilenceDb, 0f);
        }

        /// <summary>Pitch multiplier for a roll in [0,1]: 0 gives 1 - variance, 1 gives 1 + variance (variance clamped to 0..MaxPitchVariance).</summary>
        public static float RandomPitch(float variance, float roll01)
        {
            float v = Mathf.Clamp(variance, 0f, MaxPitchVariance);
            return 1f + (Mathf.Clamp01(roll01) * 2f - 1f) * v;
        }

        public static SfxBus BusOf(SfxId id) => id == SfxId.UiTap || id == SfxId.UiBack ? SfxBus.Ui : SfxBus.Sfx;

        /// <summary>Equal-power layer gains for a combat mix in [0,1]: explore fades out as combat fades in, loudness stays level.</summary>
        public static (float explore, float combat) LayerGains(float combatMix)
        {
            float angle = Mathf.Clamp01(combatMix) * Mathf.PI * 0.5f;
            return (Mathf.Cos(angle), Mathf.Sin(angle));
        }
    }
}
