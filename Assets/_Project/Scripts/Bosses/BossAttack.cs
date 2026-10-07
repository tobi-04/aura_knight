using UnityEngine;

namespace AuraKnight.Bosses
{
    /// <summary>
    /// One boss move: Telegraph (visible warning, at least 0.5 s), Execute, Recover. Driven by <see cref="BossBase"/> one physics step
    /// at a time (no coroutines), so <see cref="Cancel"/> on phase change, death or reset stops it cleanly. Subclasses spawn their
    /// hazards through <see cref="BossHazard"/> (tracked and cleared by the boss) and override only the hooks they need.
    /// </summary>
    public abstract class BossAttack : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] float telegraphSeconds = 0.6f;
        [SerializeField, Min(0f)] float executeSeconds = 0.4f;
        [SerializeField, Min(0f)] float recoverSeconds = 0.8f;
        [Tooltip("Animator trigger Attack1..3 played when the telegraph starts (0 = none).")]
        [SerializeField, Range(0, 3)] int animationSlot = 1;
        [SerializeField, Min(0)] int damage = 1;

        readonly BossAttackTimeline _timeline = new BossAttackTimeline();

        protected BossBase Boss { get; private set; }
        protected int Damage => damage;
        protected float StageProgress => _timeline.Progress;
        protected float StageElapsed => _timeline.StageElapsed;
        protected float Speed => Boss != null ? Boss.Speed : 1f;

        public float TelegraphSeconds => telegraphSeconds;
        public float ExecuteSeconds => executeSeconds;
        public float RecoverSeconds => recoverSeconds;
        public bool IsRunning => _timeline.Stage != BossAttackStage.Idle;
        public BossAttackStage Stage => _timeline.Stage;

        /// <summary>Effective telegraph at a phase speed (never below the minimum).</summary>
        public float EffectiveTelegraph(float speed, float minimum = BossTiming.MinTelegraph) =>
            BossTiming.Telegraph(telegraphSeconds, speed, minimum);

        /// <summary>False keeps the attack out of the pool for now (e.g. summon while the minions still live).</summary>
        public virtual bool CanStart(BossBase boss) => true;

        public void Begin(BossBase boss)
        {
            Boss = boss;
            _timeline.Begin(EffectiveTelegraph(boss.Speed, boss.Stats.minTelegraph),
                BossTiming.Scaled(executeSeconds, boss.Speed), BossTiming.Scaled(recoverSeconds, boss.Speed));
            boss.FaceTarget();
            boss.PlayAttackAnimation(animationSlot);
            boss.SetTelegraphing(true);
            OnTelegraph();
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning) return;
            if (_timeline.Stage == BossAttackStage.Telegraph) OnTelegraphing(deltaTime);
            var events = _timeline.Tick(deltaTime);
            if ((events & BossStageEvents.ExecuteStarted) != 0)
            {
                Boss.SetTelegraphing(false);
                OnExecute();
            }
            if (_timeline.Stage == BossAttackStage.Execute) OnExecuting(deltaTime);
            if ((events & BossStageEvents.RecoverStarted) != 0) OnRecover();
            if ((events & BossStageEvents.Finished) != 0) OnEnd(false);
        }

        /// <summary>Stops immediately (phase change, death, reset) and lets the attack undo what it changed.</summary>
        public void Cancel()
        {
            if (!IsRunning) return;
            _timeline.Cancel();
            Boss.SetTelegraphing(false);
            OnEnd(true);
        }

        protected virtual void OnTelegraph() { }
        protected virtual void OnTelegraphing(float deltaTime) { }
        protected abstract void OnExecute();
        protected virtual void OnExecuting(float deltaTime) { }
        protected virtual void OnRecover() { }
        /// <summary>Called when the attack finished or was cancelled; restore anything it changed on the boss.</summary>
        protected virtual void OnEnd(bool cancelled) { }

        /// <summary>The telegraph duration the hazards of this attack should wait before arming, so they land exactly when Execute starts.</summary>
        protected float TelegraphLength => BossTiming.Telegraph(telegraphSeconds, Boss.Speed, Boss.Stats.minTelegraph);
    }
}
