using UnityEngine;

namespace AuraKnight.Enemies
{
    /// <summary>
    /// Patrols between two points, turns at ledges and walls (Ground-mask probes) and charges Leo when he is within
    /// <c>detectRange</c> on the same floor. Bug Thorn, Patrol Bot and (with FrontShield) Night Knight.
    /// Charge = Detect, a short lunge when close = Attack, a pause afterwards = Cooldown.
    /// </summary>
    public sealed class WalkerEnemy : EnemyBase
    {
        const float LungeMultiplier = 1.5f;

        [Tooltip("Tiles the patrol segment extends left and right of the spawn point.")]
        [SerializeField, Min(0f)] float patrolLeft = 3f;
        [SerializeField, Min(0f)] float patrolRight = 3f;

        float _minX, _maxX;
        float _turnTimer;

        protected override void OnReset()
        {
            var x = SpawnPosition.x;
            _minX = x - patrolLeft;
            _maxX = x + patrolRight;
            _turnTimer = 0f;
        }

        protected override void OnPatrol(float deltaTime)
        {
            if (TargetInRange(Stats.detectRange) && Enter(EnemyState.Detect)) return;
            Face(PatrolRules.NextFacing(Facing, Position.x, _minX, _maxX, GroundAhead(Facing), WallAhead(Facing)));
            MoveX(Facing * Stats.moveSpeed);
        }

        protected override void OnDetect(float deltaTime)
        {
            if (!TargetInRange(Stats.detectRange * LoseRangeFactor)) { MoveX(0f); Enter(EnemyState.Patrol); return; }
            var delta = TargetDelta;
            TurnToward(delta.x, deltaTime);
            if (Mathf.Abs(delta.x) <= Stats.attackRange && Enter(EnemyState.Attack)) return;
            MoveX(PatrolRules.ChargeBlocked(GroundAhead(Facing), WallAhead(Facing)) ? 0f : Facing * Stats.chargeSpeed);
        }

        protected override void OnAttack(float deltaTime)
        {
            bool blocked = PatrolRules.ChargeBlocked(GroundAhead(Facing), WallAhead(Facing));
            MoveX(blocked ? 0f : Facing * Stats.chargeSpeed * LungeMultiplier);
            if (Machine.TimeInState >= Stats.attackSeconds) Enter(EnemyState.Cooldown);
        }

        protected override void OnCooldown(float deltaTime)
        {
            MoveX(0f);
            if (Machine.TimeInState < Stats.cooldownSeconds) return;
            Enter(TargetInRange(Stats.detectRange) ? EnemyState.Detect : EnemyState.Patrol);
        }

        protected override void OnHurtEnded() => _turnTimer = 0f;

        // Slow turners (Night Knight) keep facing the old way for turnDelaySeconds, which is the window to hit them from behind.
        void TurnToward(float dx, float deltaTime)
        {
            int wanted = EnemyDetection.FacingToward(dx, Facing);
            if (wanted == Facing) { _turnTimer = 0f; return; }
            _turnTimer += deltaTime;
            if (_turnTimer >= Stats.turnDelaySeconds) { Face(wanted); _turnTimer = 0f; }
        }
    }
}
