using UnityEngine;

namespace AuraKnight.World.Pickups
{
    /// <summary>Pure magnet rule for coins (GDD 8: pulled toward Leo when within 2 tiles).</summary>
    public static class CoinMagnet
    {
        public const float Radius = 2f;
        public const float CollectDistance = 0.45f;

        public static bool InRange(Vector2 coin, Vector2 target, float radius = Radius) =>
            (target - coin).sqrMagnitude <= radius * radius;

        public static bool Reached(Vector2 coin, Vector2 target) => InRange(coin, target, CollectDistance);

        /// <summary>One magnet step; speed rises as the coin gets closer so it snaps in rather than crawling.</summary>
        public static Vector2 Pull(Vector2 coin, Vector2 target, float baseSpeed, float deltaTime)
        {
            float closeness = 1f - Mathf.Clamp01(Vector2.Distance(coin, target) / Radius);
            return Vector2.MoveTowards(coin, target, baseSpeed * (1f + 2f * closeness) * deltaTime);
        }
    }
}
