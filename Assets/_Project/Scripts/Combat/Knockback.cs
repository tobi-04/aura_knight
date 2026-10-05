using UnityEngine;

namespace AuraKnight.Combat
{
    /// <summary>Knockback numbers (GDD 5.1: pushed back 3 tiles) shared by every victim.</summary>
    public static class Knockback
    {
        public const float DefaultTiles = 3f;
        /// <summary>Seconds the victim is carried (and stunned) while being pushed.</summary>
        public const float Duration = 0.25f;

        /// <summary>Constant speed that covers <paramref name="tiles"/> in <paramref name="duration"/>.</summary>
        public static float Speed(float tiles, float duration = Duration) =>
            duration > 0f ? Mathf.Max(0f, tiles) / duration : 0f;

        /// <summary>-1 / +1 push direction from the hit; <paramref name="fallback"/> when the hit has no horizontal part.</summary>
        public static int Sign(in DamageInfo info, int fallback)
        {
            if (info.Direction.x > 0.01f) return 1;
            if (info.Direction.x < -0.01f) return -1;
            return fallback >= 0 ? 1 : -1;
        }
    }
}
