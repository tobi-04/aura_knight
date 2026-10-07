using UnityEngine;

namespace AuraKnight.Enemies
{
    /// <summary>Pure "can I see Leo" and "which way do I face" rules.</summary>
    public static class EnemyDetection
    {
        /// <summary>
        /// True when <paramref name="delta"/> (target minus self) is within <paramref name="range"/> tiles. A positive
        /// <paramref name="verticalTolerance"/> also requires the target to be on roughly the same floor (|dy| within it).
        /// </summary>
        public static bool InRange(Vector2 delta, float range, float verticalTolerance)
        {
            if (verticalTolerance > 0f && Mathf.Abs(delta.y) > verticalTolerance) return false;
            return delta.sqrMagnitude <= range * range;
        }

        /// <summary>+1 / -1 toward a target at horizontal offset <paramref name="dx"/>; keeps <paramref name="fallback"/> when aligned.</summary>
        public static int FacingToward(float dx, int fallback)
        {
            if (dx > 0.05f) return 1;
            if (dx < -0.05f) return -1;
            return fallback >= 0 ? 1 : -1;
        }
    }
}
