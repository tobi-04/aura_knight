using System;
using AuraKnight.Combat;
using UnityEngine;

namespace AuraKnight.Enemies.Modifiers
{
    /// <summary>
    /// Night Knight: hits arriving from the facing side are negated through <see cref="Health.DamageFilter"/> (outcome Absorbed).
    /// Hits from behind, from above (pogo) and non-player damage go through. The knight is slow to turn (EnemyStats.turnDelaySeconds),
    /// so sliding past it opens its back.
    /// </summary>
    [RequireComponent(typeof(EnemyBase))]
    public sealed class FrontShield : MonoBehaviour
    {
        EnemyBase _enemy;
        Health _health;
        Func<DamageInfo, bool> _filter;

        /// <summary>Raised when a hit was blocked (spark VFX / clang SFX hook).</summary>
        public event Action Blocked;

        void Awake()
        {
            _enemy = GetComponent<EnemyBase>();
            _health = GetComponent<Health>();
            _filter = Filter;
        }

        void OnEnable() => _health.DamageFilter = _filter;

        void OnDisable()
        {
            if (_health != null && _health.DamageFilter == _filter) _health.DamageFilter = null;
        }

        bool Filter(DamageInfo info)
        {
            if (!ShieldRule.Blocks(_enemy.Facing, info)) return false;
            Blocked?.Invoke();
            return true;
        }
    }
}
