using System;

namespace AuraKnight.Bosses
{
    public enum BossAttackStage { Idle, Telegraph, Execute, Recover }

    [Flags]
    public enum BossStageEvents { None = 0, ExecuteStarted = 1, RecoverStarted = 2, Finished = 4 }

    /// <summary>Pure clock of one attack: Telegraph, then Execute, then Recover. One tick may cross several boundaries.</summary>
    public sealed class BossAttackTimeline
    {
        float _elapsed, _telegraph, _execute, _recover;

        public BossAttackStage Stage { get; private set; }

        /// <summary>0..1 through the current stage (1 for a zero-length stage).</summary>
        public float Progress
        {
            get
            {
                float length = Stage == BossAttackStage.Telegraph ? _telegraph : Stage == BossAttackStage.Execute ? _execute : _recover;
                return length <= 0f ? 1f : Math.Min(1f, _elapsed / length);
            }
        }

        public float StageElapsed => _elapsed;

        public void Begin(float telegraph, float execute, float recover)
        {
            _telegraph = Math.Max(0f, telegraph);
            _execute = Math.Max(0f, execute);
            _recover = Math.Max(0f, recover);
            _elapsed = 0f;
            Stage = BossAttackStage.Telegraph;
        }

        public void Cancel()
        {
            Stage = BossAttackStage.Idle;
            _elapsed = 0f;
        }

        public BossStageEvents Tick(float deltaTime)
        {
            var events = BossStageEvents.None;
            if (Stage == BossAttackStage.Idle) return events;
            _elapsed += deltaTime;
            while (Stage != BossAttackStage.Idle)
            {
                if (Stage == BossAttackStage.Telegraph && _elapsed >= _telegraph)
                {
                    _elapsed -= _telegraph;
                    Stage = BossAttackStage.Execute;
                    events |= BossStageEvents.ExecuteStarted;
                }
                else if (Stage == BossAttackStage.Execute && _elapsed >= _execute)
                {
                    _elapsed -= _execute;
                    Stage = BossAttackStage.Recover;
                    events |= BossStageEvents.RecoverStarted;
                }
                else if (Stage == BossAttackStage.Recover && _elapsed >= _recover)
                {
                    Stage = BossAttackStage.Idle;
                    _elapsed = 0f;
                    events |= BossStageEvents.Finished;
                }
                else break;
            }
            return events;
        }
    }
}
