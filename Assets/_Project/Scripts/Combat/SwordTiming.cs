using UnityEngine;

namespace AuraKnight.Combat
{
    /// <summary>Sword numbers from GDD 5.1. Hitbox timing lives in code so it works without an Animator.</summary>
    public static class SwordTiming
    {
        /// <summary>Length of one swing (0.25 s per hit).</summary>
        public const float SwingDuration = 0.25f;
        /// <summary>Hitbox is armed from here...</summary>
        public const float ActiveStart = 0.05f;
        /// <summary>...until here (seconds into the swing).</summary>
        public const float ActiveEnd = 0.18f;
        /// <summary>Reach beyond the body edge, in tiles.</summary>
        public const float Reach = 1.5f;
        /// <summary>Energy gained per landed sword hit.</summary>
        public const float EnergyPerHit = 8f;
        /// <summary>Pogo bounce height in tiles.</summary>
        public const float PogoTiles = 3f;

        public static bool IsHitboxActive(float elapsed) => elapsed >= ActiveStart && elapsed < ActiveEnd;

        /// <summary>Launch speed that rises <see cref="PogoTiles"/> under the given upward gravity: v = sqrt(2 g h).</summary>
        public static float PogoSpeed(float upwardGravity) => Mathf.Sqrt(2f * upwardGravity * PogoTiles);
    }
}
