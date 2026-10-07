using UnityEngine;

namespace AuraKnight.Bosses
{
    /// <summary>
    /// Root spikes erupt around Leo's position at the moment the attack starts. Each spike shows a flat dust marker for the whole telegraph
    /// (0.6 s), then rises. The phase 2 variant is a second instance with more, closer spikes.
    /// </summary>
    public sealed class RootSpikeAttack : BossAttack
    {
        static readonly Color Dust = new Color(0.55f, 0.4f, 0.25f);

        [SerializeField, Min(1)] int spikeCount = 3;
        [SerializeField, Min(0.5f)] float spacing = 3.2f;
        [SerializeField] Vector2 spikeSize = new Vector2(1.1f, 3f);
        [SerializeField, Min(0.1f)] float spikeLifetime = 0.5f;

        protected override void OnTelegraph()
        {
            var field = Boss.Playfield;
            float centre = Boss.Target != null ? Boss.Target.position.x : Boss.HomePosition.x;
            float wait = TelegraphLength;
            for (int i = 0; i < spikeCount; i++)
            {
                float x = BossMath.SpreadX(centre, i, spikeCount, spacing, field.min.x + 1f, field.max.x - 1f);
                BossHazard.Spawn(Boss, new HazardSpec
                {
                    Position = new Vector2(x, Boss.FloorY + spikeSize.y * 0.5f),
                    Size = spikeSize,
                    Damage = Damage,
                    Telegraph = wait,
                    Lifetime = spikeLifetime,
                    Color = Dust,
                    MarkerHeightFactor = 0.12f,
                });
            }
        }

        protected override void OnExecute() { }
    }
}
