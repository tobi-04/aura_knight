using AuraKnight.Combat;
using UnityEngine;

namespace AuraKnight.Bosses
{
    /// <summary>
    /// Spits sticky web at Leo. A web that lands slows him to 50% for a few seconds through <see cref="PlayerSlowStatus"/> (a hit absorbed by
    /// dash i-frames or the water shield slows nothing). The phase 2 variant is a fan of three.
    /// </summary>
    public sealed class WebSpitAttack : BossAttack
    {
        static readonly Color Web = new Color(0.9f, 0.9f, 0.95f);

        [SerializeField, Min(1)] int webCount = 1;
        [SerializeField, Min(0f)] float fanDegrees = 28f;
        [SerializeField, Min(1f)] float webSpeed = 12f;
        [SerializeField, Range(0.1f, 1f)] float slowFactor = 0.5f;
        [SerializeField, Min(0.5f)] float slowSeconds = 3f;
        [SerializeField] Vector2 muzzle = new Vector2(1.8f, 0.6f);

        protected override void OnExecute()
        {
            Vector2 from = (Vector2)Boss.transform.position + new Vector2(muzzle.x * Boss.Facing, muzzle.y);
            Vector2 aim = Boss.Target != null ? (Vector2)Boss.Target.position - from : new Vector2(Boss.Facing, 0f);
            for (int i = 0; i < webCount; i++)
            {
                var direction = BossMath.FanDirection(aim, i, webCount, fanDegrees);
                var web = BossHazard.Spawn(Boss, new HazardSpec
                {
                    Position = from,
                    Size = Vector2.one * 0.8f,
                    Damage = Damage,
                    Lifetime = 3f,
                    Velocity = direction * webSpeed,
                    Color = Web,
                    DestroyOnGround = true,
                });
                web.Hit += OnWebHit;
            }
        }

        void OnWebHit(HitReport report)
        {
            if (report.Outcome.DealtDamage()) PlayerSlowStatus.ApplyTo(report.Target, slowFactor, slowSeconds);
        }
    }
}
