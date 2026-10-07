using UnityEngine;

namespace AuraKnight.Bosses
{
    /// <summary>
    /// Pistons slam down in columns at fixed offsets from the boss (the gaps between them are the safe spots). Each column shows a flat marker
    /// on the floor during the telegraph. The phase 2 variant uses more columns.
    /// </summary>
    public sealed class PistonAttack : BossAttack
    {
        static readonly Color Steel = new Color(0.62f, 0.66f, 0.72f);

        [Tooltip("Column centres relative to the boss's home x (negative = toward the room centre).")]
        [SerializeField] float[] columnOffsets = { -8f, -16f, -24f };
        [SerializeField] Vector2 columnSize = new Vector2(2.4f, 9f);
        [SerializeField, Min(0.1f)] float slamSeconds = 0.5f;

        protected override void OnTelegraph()
        {
            var field = Boss.Playfield;
            float wait = TelegraphLength;
            foreach (float offset in columnOffsets)
            {
                float x = Mathf.Clamp(Boss.HomePosition.x + offset, field.min.x + columnSize.x * 0.5f, field.max.x - columnSize.x * 0.5f);
                BossHazard.Spawn(Boss, new HazardSpec
                {
                    Position = new Vector2(x, Boss.FloorY + columnSize.y * 0.5f),
                    Size = columnSize,
                    Damage = Damage,
                    Telegraph = wait,
                    Lifetime = slamSeconds,
                    Color = Steel,
                    MarkerHeightFactor = 0.04f,
                });
            }
        }

        protected override void OnExecute() { }
    }
}
