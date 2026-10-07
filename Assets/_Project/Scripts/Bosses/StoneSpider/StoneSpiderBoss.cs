using UnityEngine;

namespace AuraKnight.Bosses
{
    /// <summary>
    /// Giant Stone Spider (40 HP). Between attacks it crawls toward Leo along the floor (Move animation). Its attacks (ceiling drop, web spit,
    /// summon) live in separate components; spiderlings are tracked by the base class, so a reset or death removes them.
    /// </summary>
    public sealed class StoneSpiderBoss : BossBase
    {
        [SerializeField, Min(0f)] float crawlSpeed = 2.2f;
        [SerializeField, Min(0f)] float keepDistance = 4.5f;

        protected override void OnIdle(float deltaTime)
        {
            if (Target == null) return;
            FaceTarget();
            float dx = Target.position.x - transform.position.x;
            bool move = Mathf.Abs(dx) > keepDistance;
            SetMoving(move);
            if (!move) return;
            var field = Playfield;
            float x = Mathf.Clamp(transform.position.x + Mathf.Sign(dx) * crawlSpeed * Speed * deltaTime, field.min.x + 2f, field.max.x - 2f);
            SetPosition(new Vector2(x, transform.position.y));
        }

        protected override void OnReset() => SetMoving(false);
    }
}
