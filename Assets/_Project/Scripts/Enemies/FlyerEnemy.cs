using UnityEngine;

namespace AuraKnight.Enemies
{
    /// <summary>
    /// Hovers around its spawn point; when Leo comes within range it winds up (Detect), swoops diagonally at where he was
    /// (Attack), then climbs back to its hover point (Cooldown). Bat and Ghost. Needs a gravity-free Rigidbody2D.
    /// Whether walls stop the swoop depends on the solid root collider, which PhaseThroughWalls removes.
    /// </summary>
    public sealed class FlyerEnemy : EnemyBase
    {
        const float HoverSwayX = 1.2f;
        const float HoverBobY = 0.3f;
        const float ArriveDistance = 0.4f;
        const float ReturnTimeout = 4f;
        const float Steering = 3f;

        float _clock;
        Vector2 _diveDirection;
        Vector2 _diveStart;

        protected override void OnReset() { _clock = 0f; }

        protected override void OnPatrol(float deltaTime)
        {
            if (TargetInRange(Stats.detectRange) && Enter(EnemyState.Detect)) return;
            HoverAround(deltaTime);
        }

        protected override void OnDetect(float deltaTime)
        {
            if (!TargetInRange(Stats.detectRange * LoseRangeFactor)) { Enter(EnemyState.Patrol); return; }
            Face(EnemyDetection.FacingToward(TargetDelta.x, Facing));
            HoverAround(deltaTime); // the wind-up: keep bobbing in place, readable for the player
            if (Machine.TimeInState < Stats.windupSeconds) return;
            _diveStart = Position;
            _diveDirection = DiveRules.Direction(Position, TargetPosition, Facing);
            Enter(EnemyState.Attack);
        }

        protected override void OnAttack(float deltaTime)
        {
            Rb.linearVelocity = _diveDirection * Stats.diveSpeed;
            Face(_diveDirection.x >= 0f ? 1 : -1);
            bool blocked = Machine.TimeInState > 0.05f && TouchingGround();
            float travelled = Vector2.Distance(Position, _diveStart);
            if (DiveRules.Ended(Machine.TimeInState, travelled, Stats.diveMaxSeconds, Stats.diveMaxDistance, blocked))
                Enter(EnemyState.Cooldown);
        }

        protected override void OnCooldown(float deltaTime)
        {
            var home = SpawnPosition;
            SteerToward(home, Stats.moveSpeed * 1.5f);
            bool arrived = Vector2.Distance(Position, home) < ArriveDistance && Machine.TimeInState >= Stats.cooldownSeconds;
            if (arrived || Machine.TimeInState > ReturnTimeout) Enter(EnemyState.Patrol);
        }

        void HoverAround(float deltaTime)
        {
            _clock += deltaTime;
            var home = SpawnPosition;
            var wanted = home + new Vector2(Mathf.Sin(_clock * 0.9f) * HoverSwayX, Mathf.Sin(_clock * 2.2f) * HoverBobY);
            SteerToward(wanted, Stats.moveSpeed);
            if (Mathf.Abs(Rb.linearVelocity.x) > 0.1f && Machine.Current == EnemyState.Patrol) Face(Rb.linearVelocity.x > 0f ? 1 : -1);
        }

        void SteerToward(Vector2 point, float maxSpeed) =>
            Rb.linearVelocity = Vector2.ClampMagnitude((point - Position) * Steering, maxSpeed);

        protected override void OnHurtEnded() => Rb.linearVelocity = Vector2.zero;
    }
}
