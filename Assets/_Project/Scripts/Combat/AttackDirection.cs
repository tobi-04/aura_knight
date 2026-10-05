using UnityEngine;

namespace AuraKnight.Combat
{
    public enum AttackDirection
    {
        Forward,
        Up,
        /// <summary>Airborne only; pogo on hit.</summary>
        Down
    }

    /// <summary>Chooses the swing direction from the stick.</summary>
    public static class AttackAim
    {
        const float VerticalThreshold = 0.6f;

        public static AttackDirection From(Vector2 stick, bool grounded)
        {
            float ax = Mathf.Abs(stick.x);
            if (stick.y >= VerticalThreshold && stick.y >= ax) return AttackDirection.Up;
            if (!grounded && stick.y <= -VerticalThreshold && -stick.y >= ax) return AttackDirection.Down;
            return AttackDirection.Forward;
        }
    }
}
