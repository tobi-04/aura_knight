using UnityEngine;

namespace AuraKnight.Combat
{
    /// <summary>Sword hitbox rectangle relative to Leo's pivot: starts at the body edge and reaches 1.5 tiles.</summary>
    public readonly struct SwordShape
    {
        public const float BodyHalfWidth = 0.4f;
        public const float BodyHalfHeight = 0.95f;
        const float Thickness = 1.2f;

        public readonly Vector2 Center;
        public readonly Vector2 Size;

        SwordShape(Vector2 center, Vector2 size)
        {
            Center = center;
            Size = size;
        }

        public static SwordShape Compute(AttackDirection direction, int facing)
        {
            float reach = SwordTiming.Reach;
            switch (direction)
            {
                case AttackDirection.Up:
                    return new SwordShape(new Vector2(0f, BodyHalfHeight + reach * 0.5f), new Vector2(Thickness, reach));
                case AttackDirection.Down:
                    return new SwordShape(new Vector2(0f, -(BodyHalfHeight + reach * 0.5f)), new Vector2(Thickness, reach));
                default:
                    float side = facing >= 0 ? 1f : -1f;
                    return new SwordShape(new Vector2(side * (BodyHalfWidth + reach * 0.5f), 0f), new Vector2(reach, Thickness));
            }
        }
    }
}
