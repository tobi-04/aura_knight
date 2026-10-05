using UnityEngine;

namespace AuraKnight.Aura
{
    /// <summary>The 0.2 s flare when switching Aura: 1 at the switch, falling linearly to 0.</summary>
    public static class AuraFlash
    {
        public const float Duration = 0.2f;
        /// <summary>Extra light intensity added at full flash strength.</summary>
        public const float PeakIntensityBoost = 1.5f;

        public static float Strength(float elapsed) =>
            elapsed <= 0f ? 1f : Mathf.Clamp01(1f - elapsed / Duration);
    }
}
