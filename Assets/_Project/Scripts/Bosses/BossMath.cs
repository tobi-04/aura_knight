using UnityEngine;

namespace AuraKnight.Bosses
{
    /// <summary>Small pure helpers for boss projectiles and the laser height check.</summary>
    public static class BossMath
    {
        /// <summary>Launch velocity that lands on <paramref name="to"/> after <paramref name="flightSeconds"/> under downward gravity <paramref name="gravity"/>.</summary>
        public static Vector2 BallisticVelocity(Vector2 from, Vector2 to, float flightSeconds, float gravity)
        {
            float t = Mathf.Max(0.05f, flightSeconds);
            return new Vector2((to.x - from.x) / t, (to.y - from.y) / t + 0.5f * gravity * t);
        }

        /// <summary>Exact constant-gravity step (position uses the average velocity), so arcs match <see cref="BallisticVelocity"/> at any step size.</summary>
        public static void Step(ref Vector2 position, ref Vector2 velocity, float gravity, float deltaTime)
        {
            position += velocity * deltaTime + new Vector2(0f, -0.5f * gravity * deltaTime * deltaTime);
            velocity.y -= gravity * deltaTime;
        }

        /// <summary>True when the vertical ranges [aBottom, aTop] and [bBottom, bTop] overlap. Used for the laser versus Leo's standing or sliding body.</summary>
        public static bool VerticalOverlap(float aBottom, float aTop, float bBottom, float bTop) => aBottom < bTop && bBottom < aTop;

        /// <summary>Directions of a fan of <paramref name="count"/> shots spread over <paramref name="spreadDegrees"/> around <paramref name="aim"/>.</summary>
        public static Vector2 FanDirection(Vector2 aim, int index, int count, float spreadDegrees)
        {
            if (count <= 1) return aim.normalized;
            float angle = Mathf.Lerp(-spreadDegrees * 0.5f, spreadDegrees * 0.5f, index / (float)(count - 1));
            return (Quaternion.Euler(0f, 0f, angle) * aim.normalized);
        }

        /// <summary>Evenly spaced x positions centred on <paramref name="centre"/>, clamped into [minX, maxX].</summary>
        public static float SpreadX(float centre, int index, int count, float spacing, float minX, float maxX) =>
            Mathf.Clamp(centre + (index - (count - 1) * 0.5f) * spacing, minX, maxX);
    }
}
