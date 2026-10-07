using UnityEngine;

namespace AuraKnight.Bosses
{
    /// <summary>Lobs poison seeds in an arc that land around Leo's position (centre, left, right of him). Seeds die on Ground.</summary>
    public sealed class SeedVolleyAttack : BossAttack
    {
        static readonly Color Poison = new Color(0.55f, 0.85f, 0.2f);

        [SerializeField, Min(1)] int seedCount = 3;
        [SerializeField, Min(0.5f)] float spread = 2.6f;
        [SerializeField, Min(0.3f)] float flightSeconds = 1f;
        [SerializeField, Min(1f)] float gravity = 14f;
        [SerializeField] Vector2 muzzle = new Vector2(1.2f, 1.1f);
        [SerializeField, Min(0.2f)] float seedSize = 0.6f;

        protected override void OnExecute()
        {
            var field = Boss.Playfield;
            Vector2 from = (Vector2)Boss.transform.position + new Vector2(muzzle.x * Boss.Facing, muzzle.y);
            float centre = Boss.Target != null ? Boss.Target.position.x : Boss.HomePosition.x;
            for (int i = 0; i < seedCount; i++)
            {
                float x = BossMath.SpreadX(centre, i, seedCount, spread, field.min.x + 0.5f, field.max.x - 0.5f);
                var velocity = BossMath.BallisticVelocity(from, new Vector2(x, Boss.FloorY + seedSize), flightSeconds, gravity);
                BossHazard.Spawn(Boss, new HazardSpec
                {
                    Position = from,
                    Size = Vector2.one * seedSize,
                    Damage = Damage,
                    Lifetime = flightSeconds * 2f,
                    Velocity = velocity,
                    Gravity = gravity,
                    Color = Poison,
                    DestroyOnGround = true,
                });
            }
        }
    }
}
