using UnityEngine;

namespace AuraKnight.Bosses
{
    public abstract partial class BossBase
    {
        void FixedUpdate()
        {
            if (State == BossState.Dormant || State == BossState.Dead) return;
            float dt = Time.fixedDeltaTime;
            switch (State)
            {
                case BossState.Idle:
                    OnIdle(dt);
                    _think -= dt;
                    if (_think <= 0f) StartNextAttack();
                    break;
                case BossState.Attacking:
                    _current.Tick(dt);
                    if (!_current.IsRunning)
                    {
                        _current = null;
                        EnterIdle();
                    }
                    break;
                case BossState.Transition:
                    _transitionLeft -= dt;
                    if (_transitionLeft <= 0f) EnterIdle();
                    break;
            }
        }

        void EnterIdle()
        {
            State = BossState.Idle;
            _think = BossTiming.Scaled(Phase.thinkSeconds, Speed);
        }

        void StartNextAttack()
        {
            var pool = Phase.attacks;
            if (_weights.Length != pool.Count) _weights = new float[pool.Count];
            for (int i = 0; i < pool.Count; i++)
                _weights[i] = pool[i].attack != null && pool[i].attack.CanStart(this) ? pool[i].weight : 0f;
            int pick = WeightedPicker.Pick(_weights, Random, _lastPick);
            if (pick < 0)
            {
                _think = BossTiming.Scaled(Phase.thinkSeconds, Speed);
                return;
            }
            _lastPick = pick;
            BeginAttack(pool[pick].attack);
        }

        /// <summary>Starts a specific attack now (debug tool, tests); the normal loop picks from the weighted pool instead.</summary>
        public bool BeginAttack(BossAttack attack)
        {
            if (attack == null || State == BossState.Dormant || State == BossState.Dead) return false;
            CancelAttack();
            _current = attack;
            State = BossState.Attacking;
            attack.Begin(this);
            return true;
        }

        void CancelAttack()
        {
            if (_current == null) return;
            _current.Cancel();
            _current = null;
        }
    }
}
