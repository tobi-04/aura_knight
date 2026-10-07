using UnityEngine;

namespace AuraKnight.Enemies
{
    /// <summary>Ballistic hop math for hoppers: launch velocity for a given apex height and horizontal distance.</summary>
    public static class HopArc
    {
        /// <summary>
        /// Launch velocity that peaks <paramref name="height"/> tiles up under <paramref name="gravity"/> (positive, tiles/s^2)
        /// and covers <paramref name="dx"/> horizontally by the time it lands at the same height; horizontal speed is capped.
        /// A non-positive height or gravity yields no hop.
        /// </summary>
        public static Vector2 Launch(float dx, float height, float gravity, float maxHorizontalSpeed)
        {
            if (height <= 0f || gravity <= 0f) return Vector2.zero;
            float vy = Mathf.Sqrt(2f * gravity * height);
            float vx = dx / AirTime(vy, gravity);
            float cap = Mathf.Max(0f, maxHorizontalSpeed);
            return new Vector2(Mathf.Clamp(vx, -cap, cap), vy);
        }

        /// <summary>Seconds from launch to landing at the launch height.</summary>
        public static float AirTime(float verticalSpeed, float gravity) => gravity > 0f ? 2f * verticalSpeed / gravity : 0f;
    }
}
