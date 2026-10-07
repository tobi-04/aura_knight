using UnityEngine;
using AuraKnight.Combat;

namespace AuraKnight.Enemies.Modifiers
{
    /// <summary>Bat: heals itself when its contact hit actually hurts Leo (blocked, dodged and i-frame hits heal nothing).</summary>
    [RequireComponent(typeof(EnemyBase))]
    public sealed class LifeSteal : MonoBehaviour
    {
        [SerializeField, Min(1)] int healAmount = 1;

        EnemyBase _enemy;
        Hitbox _hitbox;

        public int HealAmount => healAmount;

        void Awake() => _enemy = GetComponent<EnemyBase>();

        void OnEnable()
        {
            _hitbox = _enemy.ContactHitbox;
            if (_hitbox != null) _hitbox.Hit += OnHit;
        }

        void OnDisable()
        {
            if (_hitbox != null) _hitbox.Hit -= OnHit;
            _hitbox = null;
        }

        void OnHit(HitReport report)
        {
            if (!report.Outcome.DealtDamage() || !_enemy.IsAlive) return;
            _enemy.Health.Heal(healAmount);
        }
    }
}
