using UnityEngine;

namespace AuraKnight.Enemies
{
    /// <summary>Flyer dive maths: aim and the end condition of the swoop.</summary>
    public static class DiveRules
    {
        const float MinHorizontal = 0.3f;

        /// <summary>
        /// Unit vector from the flyer toward the target, kept diagonal: a near-vertical aim gets a horizontal component
        /// (toward <paramref name="preferredSign"/> when the target is directly below) so it reads as a diagonal swoop.
        /// </summary>
        public static Vector2 Direction(Vector2 from, Vector2 to, int preferredSign = 1)
        {
            var delta = to - from;
            if (delta.sqrMagnitude < 1e-6f) delta = new Vector2(preferredSign >= 0 ? 1f : -1f, -1f);
            var dir = delta.normalized;
            if (Mathf.Abs(dir.x) < MinHorizontal)
            {
                float sign = Mathf.Abs(dir.x) > 1e-4f ? Mathf.Sign(dir.x) : (preferredSign >= 0 ? 1f : -1f);
                dir = new Vector2(sign * MinHorizontal, dir.y).normalized;
            }
            return dir;
        }

        /// <summary>The swoop stops at its time limit, its distance limit, or when it hits something.</summary>
        public static bool Ended(float timeInDive, float distanceTravelled, float maxSeconds, float maxDistance, bool blocked) =>
            blocked || timeInDive >= maxSeconds || distanceTravelled >= maxDistance;
    }
}
