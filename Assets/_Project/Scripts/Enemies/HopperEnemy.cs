using UnityEngine;

namespace AuraKnight.Enemies
{
    /// <summary>
    /// Sits still until Leo is within range, then hops toward him in an arc every <c>hopInterval</c> seconds (Poison Shroom).
    /// Detect = waiting for the next hop, Attack = in the air, Cooldown = the short rest after landing.
    /// </summary>
    public sealed class HopperEnemy : EnemyBase
    {
        const float LandingRest = 0.15f;
        const float AirTimeout = 3f;
        const float FirstHopFraction = 0.4f;

        float _sinceHop;

        protected override void OnReset() => _sinceHop = Stats.hopInterval * FirstHopFraction;

        protected override void OnPatrol(float deltaTime)
        {
            _sinceHop += deltaTime;
            MoveX(0f);
            if (TargetInRange(Stats.detectRange)) Enter(EnemyState.Detect);
        }

        protected override void OnDetect(float deltaTime)
        {
            _sinceHop += deltaTime;
            if (!TargetInRange(Stats.detectRange * LoseRangeFactor)) { MoveX(0f); Enter(EnemyState.Patrol); return; }
            Face(EnemyDetection.FacingToward(TargetDelta.x, Facing));
            if (_sinceHop < Stats.hopInterval || !IsGrounded()) return;
            Hop();
        }

        protected override void OnAttack(float deltaTime)
        {
            _sinceHop += deltaTime;
            bool landed = Machine.TimeInState > 0.1f && IsGrounded();
            if (landed || Machine.TimeInState > AirTimeout) { MoveX(0f); Enter(EnemyState.Cooldown); }
        }

        protected override void OnCooldown(float deltaTime)
        {
            _sinceHop += deltaTime;
            MoveX(0f);
            if (Machine.TimeInState < LandingRest) return;
            Enter(TargetInRange(Stats.detectRange) ? EnemyState.Detect : EnemyState.Patrol);
        }

        void Hop()
        {
            float gravity = Mathf.Abs(Physics2D.gravity.y) * Rb.gravityScale;
            var velocity = HopArc.Launch(TargetDelta.x, Stats.hopHeight, gravity, Stats.hopMaxSpeed);
            if (velocity == Vector2.zero) return;
            Rb.linearVelocity = velocity;
            _sinceHop = 0f;
            Enter(EnemyState.Attack);
        }
    }
}
