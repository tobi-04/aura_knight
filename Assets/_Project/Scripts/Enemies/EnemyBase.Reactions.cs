using AuraKnight.Combat;
using UnityEngine;

namespace AuraKnight.Enemies
{
    public abstract partial class EnemyBase
    {
        const float FlashSeconds = 0.1f;
        const float DeathFadeSeconds = 0.3f;

        Color _baseColor = Color.white;
        Color _tint = Color.clear;
        float _flashLeft;
        float _deadSince;

        /// <summary>Overrides the sprite colour (telegraph glow); <see cref="ClearTint"/> returns to the base colour.</summary>
        protected void SetTint(Color color) => _tint = color;

        protected void ClearTint() => _tint = Color.clear;

        void OnDamaged(DamageInfo info, int applied)
        {
            if (_machine.IsDead || _health.IsDead) return;
            _flashLeft = FlashSeconds;
            if (!Interruptible || !_machine.TryEnter(EnemyState.Hurt)) return;
            ApplyKnockback(info);
        }

        void ApplyKnockback(in DamageInfo info)
        {
            float tiles = info.KnockbackTiles * stats.knockbackScale;
            if (tiles <= 0f || _rb.bodyType != RigidbodyType2D.Dynamic) return;
            float speed = Knockback.Speed(tiles, stats.HurtSeconds);
            _rb.linearVelocity = new Vector2(Knockback.Sign(info, -Facing) * speed, Mathf.Max(0f, _rb.linearVelocity.y));
        }

        void TickHurt()
        {
            if (_machine.TimeInState < stats.HurtSeconds) return;
            if (_rb.bodyType == RigidbodyType2D.Dynamic) _rb.linearVelocity = new Vector2(0f, _rb.gravityScale > 0f ? _rb.linearVelocity.y : 0f);
            OnHurtEnded();
            _machine.TryEnter(TargetInRange(stats.detectRange) ? EnemyState.Detect : EnemyState.Patrol);
        }

        void OnHealthDied(DamageInfo info)
        {
            if (_machine.IsDead) return;
            _machine.TryEnter(EnemyState.Dead);
            _deadSince = Time.time;
            EnemyDrops.Spawn(stats.DropTable.Roll(Random), SpawnDropPoint(), coinPrefab, lightDropPrefab, Random);
            hurtbox.gameObject.SetActive(false);
            contactHitbox.gameObject.SetActive(false);
            RefreshBody();
            _rb.linearVelocity = Vector2.zero;
            _rb.simulated = false;
            OnDeath();
            Died?.Invoke(this);
        }

        Vector2 SpawnDropPoint() => body != null ? (Vector2)body.bounds.center : _rb.position;

        void ResetVisuals()
        {
            _flashLeft = 0f;
            _tint = Color.clear;
            if (sprite == null) return;
            sprite.enabled = true;
            sprite.color = _baseColor;
        }

        void Update()
        {
            if (sprite == null) return;
            if (_machine.IsDead)
            {
                float t = (Time.time - _deadSince) / DeathFadeSeconds;
                var faded = _baseColor;
                faded.a = Mathf.Clamp01(1f - t);
                sprite.color = faded;
                if (t >= 1f) sprite.enabled = false;
                return;
            }
            _flashLeft = Mathf.Max(0f, _flashLeft - Time.deltaTime);
            sprite.color = _flashLeft > 0f ? Color.white : _tint.a > 0f ? _tint : _baseColor;
        }
    }
}
