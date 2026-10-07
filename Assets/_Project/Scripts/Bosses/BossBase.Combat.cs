using AuraKnight.Audio;
using AuraKnight.Combat;
using AuraKnight.Core;
using AuraKnight.UI;
using UnityEngine;

namespace AuraKnight.Bosses
{
    public abstract partial class BossBase
    {
        void OnDamaged(DamageInfo info, int applied)
        {
            if (State == BossState.Dormant || State == BossState.Dead) return;
            FlashHit();
            TriggerHurt();
            Sfx.Play(SfxId.EnemyHit, transform.position);
        }

        void OnHealthChanged(int current, int max)
        {
            if (State == BossState.Dormant) return;
            PublishHealth();
            if (State == BossState.Dead || current <= 0) return;
            int next = BossPhaseRules.IndexFor(current, max, _fractions);
            if (next > PhaseIndex) EnterPhase(next);
        }

        void OnHealthDied(DamageInfo info)
        {
            if (State == BossState.Dormant || State == BossState.Dead) return;
            CancelAttack();
            ClearSpawned();
            State = BossState.Dead;
            hurtbox.gameObject.SetActive(false);
            contactHitbox.gameObject.SetActive(false);
            SetDead();
            Sfx.Play(SfxId.EnemyDie, transform.position);
            OnDeath();
            Defeated?.Invoke(this);
        }

        void EnterPhase(int index)
        {
            CancelAttack();
            PhaseIndex = index;
            _lastPick = -1;
            State = BossState.Transition;
            _transitionLeft = stats.transitionSeconds;
            ApplyAnimatorSpeed();
            FlashPhase();
            Sfx.Play(SfxId.BossRoar, transform.position);
            OnPhaseEntered(index);
            PhaseChanged?.Invoke(index);
        }

        void PublishHealth() => EventBus.Publish(new BossHealthChanged(stats.bossId, _health.Current, _health.Max));

        /// <summary>Debug / test aid: drops HP to the start of phase <paramref name="phaseIndex"/> (1 = phase 2) so the boss changes phase immediately.</summary>
        public void SkipToPhase(int phaseIndex)
        {
            if (State == BossState.Dormant || State == BossState.Dead || phaseIndex <= 0 || phaseIndex >= phases.Count) return;
            int hp = BossPhaseRules.ThresholdHp(_health.Max, phases[phaseIndex].enterAtHpFraction);
            _health.Initialize(_health.Max, Mathf.Min(_health.Current, Mathf.Max(1, hp)));
        }
    }
}
