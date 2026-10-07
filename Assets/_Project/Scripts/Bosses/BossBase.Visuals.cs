using System.Collections.Generic;
using UnityEngine;

namespace AuraKnight.Bosses
{
    public abstract partial class BossBase
    {
        static readonly int MovingHash = Animator.StringToHash("Moving");
        static readonly int ExposedHash = Animator.StringToHash("Exposed");
        static readonly int DeadHash = Animator.StringToHash("Dead");
        static readonly int HurtHash = Animator.StringToHash("Hurt");
        static readonly int[] AttackHashes =
        {
            0, Animator.StringToHash("Attack1"), Animator.StringToHash("Attack2"), Animator.StringToHash("Attack3"),
        };
        static readonly Color TelegraphColor = new Color(1f, 0.85f, 0.35f);
        static readonly Color HitColor = new Color(1f, 0.55f, 0.55f);
        static readonly Color PhaseColor = new Color(1f, 0.3f, 0.3f);
        const float HitFlashSeconds = 0.08f, PhaseFlashSeconds = 0.6f, HurtAnimationGap = 0.35f, ShakeAmplitude = 0.06f;

        readonly HashSet<int> _animatorParameters = new HashSet<int>();
        Color _baseColor = Color.white;
        Color _flashColor;
        Color _tint = Color.clear;
        Vector3 _spriteHome;
        float _flashLeft, _telegraphClock, _alpha = -1f, _nextHurtAnimation;
        bool _telegraphing;

        void CacheVisuals()
        {
            if (sprite != null)
            {
                _baseColor = sprite.color;
                _spriteHome = sprite.transform.localPosition;
            }
            if (animator == null) return;
            foreach (var parameter in animator.parameters) _animatorParameters.Add(parameter.nameHash);
        }

        void ResetVisuals()
        {
            _flashLeft = 0f;
            _tint = Color.clear;
            _alpha = -1f;
            _telegraphing = false;
            if (sprite != null)
            {
                sprite.enabled = _present;
                sprite.color = _baseColor;
                sprite.transform.localPosition = _spriteHome;
            }
            if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
            {
                animator.Rebind();
                animator.Update(0f);
            }
            ApplyAnimatorSpeed();
        }

        void Update()
        {
            if (sprite == null || State == BossState.Dormant) return;
            float dt = Time.deltaTime;
            var color = _baseColor;
            if (_tint.a > 0f) color = Color.Lerp(color, _tint, 0.65f);
            if (_telegraphing)
            {
                _telegraphClock += dt;
                color = Color.Lerp(color, TelegraphColor, Mathf.PingPong(_telegraphClock * 5f * Speed, 1f));
                float shake = Mathf.Sin(_telegraphClock * 70f) * ShakeAmplitude;
                sprite.transform.localPosition = _spriteHome + new Vector3(shake, 0f, 0f);
            }
            if (_flashLeft > 0f)
            {
                _flashLeft -= dt;
                color = Color.Lerp(color, _flashColor, Mathf.Clamp01(_flashLeft / PhaseFlashSeconds + 0.2f));
            }
            if (_alpha >= 0f) color.a = _alpha;
            sprite.color = color;
        }

        /// <summary>Warning pulse and shake on the sprite while an attack winds up.</summary>
        public void SetTelegraphing(bool on)
        {
            _telegraphing = on;
            _telegraphClock = 0f;
            if (!on && sprite != null) sprite.transform.localPosition = _spriteHome;
        }

        /// <summary>Mixes a colour over the sprite (exposed core glow, Malakor's Aura colours); <see cref="ClearTint"/> removes it.</summary>
        public void SetTint(Color color) => _tint = color;

        public void ClearTint() => _tint = Color.clear;

        /// <summary>Overrides the sprite alpha (Malakor fading out before a teleport); negative restores it.</summary>
        public void SetAlpha(float alpha) => _alpha = alpha;

        void FlashHit()
        {
            _flashColor = HitColor;
            _flashLeft = HitFlashSeconds;
        }

        void FlashPhase()
        {
            _flashColor = PhaseColor;
            _flashLeft = PhaseFlashSeconds;
        }

        void ApplyAnimatorSpeed()
        {
            if (animator != null) animator.speed = Speed;
        }

        public void SetMoving(bool moving) => SetBool(MovingHash, moving);

        public void SetExposed(bool exposed) => SetBool(ExposedHash, exposed);

        void SetDead() => SetBool(DeadHash, true);

        public void PlayAttackAnimation(int slot)
        {
            if (slot > 0 && slot < AttackHashes.Length) SetTrigger(AttackHashes[slot]);
        }

        void TriggerHurt()
        {
            if (Time.time < _nextHurtAnimation) return;
            _nextHurtAnimation = Time.time + HurtAnimationGap;
            SetTrigger(HurtHash);
        }

        void SetBool(int hash, bool value)
        {
            if (animator != null && _animatorParameters.Contains(hash)) animator.SetBool(hash, value);
        }

        void SetTrigger(int hash)
        {
            if (animator != null && _animatorParameters.Contains(hash)) animator.SetTrigger(hash);
        }
    }
}
