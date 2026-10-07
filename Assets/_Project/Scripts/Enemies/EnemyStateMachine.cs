using System;

namespace AuraKnight.Enemies
{
    /// <summary>
    /// Pure state holder for the enemy loop. Legal moves: Patrol -> Detect -> Attack -> Cooldown -> (Patrol | Detect);
    /// Hurt interrupts any live state and returns to Patrol/Detect; Dead is terminal until <see cref="Reset"/>.
    /// Re-entering the current state is a no-op (timer kept) except Hurt, which restarts so a second hit extends the stun.
    /// </summary>
    public sealed class EnemyStateMachine
    {
        public EnemyState Current { get; private set; } = EnemyState.Patrol;
        public EnemyState Previous { get; private set; } = EnemyState.Patrol;
        public float TimeInState { get; private set; }
        public bool IsDead => Current == EnemyState.Dead;

        /// <summary>(from, to), raised after a successful transition or a reset to a different state.</summary>
        public event Action<EnemyState, EnemyState> Changed;

        public static bool CanTransition(EnemyState from, EnemyState to)
        {
            if (from == EnemyState.Dead) return false;
            if (to == EnemyState.Hurt || to == EnemyState.Dead) return true;
            switch (from)
            {
                case EnemyState.Patrol: return to == EnemyState.Detect;
                case EnemyState.Detect: return to == EnemyState.Patrol || to == EnemyState.Attack;
                case EnemyState.Attack: return to == EnemyState.Cooldown;
                case EnemyState.Cooldown: return to == EnemyState.Patrol || to == EnemyState.Detect;
                case EnemyState.Hurt: return to == EnemyState.Patrol || to == EnemyState.Detect || to == EnemyState.Cooldown;
                default: return false;
            }
        }

        public bool TryEnter(EnemyState to)
        {
            if (to == Current && to != EnemyState.Hurt) return Current != EnemyState.Dead;
            if (!CanTransition(Current, to)) return false;
            Move(to);
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime > 0f) TimeInState += deltaTime;
        }

        /// <summary>Forces a state without checking the rules (respawn / test setup).</summary>
        public void Reset(EnemyState to = EnemyState.Patrol)
        {
            if (to == Current) { TimeInState = 0f; return; }
            Move(to);
        }

        void Move(EnemyState to)
        {
            var from = Current;
            Previous = from;
            Current = to;
            TimeInState = 0f;
            Changed?.Invoke(from, to);
        }
    }
}
