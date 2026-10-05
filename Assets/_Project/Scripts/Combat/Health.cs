using System;
using UnityEngine;

namespace AuraKnight.Combat
{
    /// <summary>
    /// Hit points for any entity (Leo, enemies, bosses). Damage arrives through <see cref="TakeDamage"/>
    /// (normally from a <see cref="Hurtbox"/>). Subscribe to the C# events for reactions.
    /// </summary>
    public sealed class Health : MonoBehaviour
    {
        [SerializeField, Min(1)] int maxHealth = 3;
        [Tooltip("I-frames granted after a non-lethal hit (player 1.0 s).")]
        [SerializeField, Min(0f)] float invulnerableAfterHit;

        int _current;
        bool _initialized;

        /// <summary>(info, applied amount). Fired for every hit that removed health, including the lethal one.</summary>
        public event Action<DamageInfo, int> Damaged;
        public event Action<DamageInfo> Died;
        /// <summary>(current, max) after any change: damage, heal, refill, re-initialise.</summary>
        public event Action<int, int> Changed;

        public Invulnerability Invulnerability { get; } = new Invulnerability();
        /// <summary>Extra source of invulnerability (the player's dash i-frames).</summary>
        public Func<bool> InvulnerabilityGate { get; set; }
        /// <summary>
        /// Optional interceptor consulted for hits that would otherwise land (after the invulnerability checks).
        /// Returning true negates the hit (outcome <see cref="HitOutcome.Absorbed"/>); the water shield uses it to eat exactly one hit.
        /// </summary>
        public Func<DamageInfo, bool> DamageFilter { get; set; }
        /// <summary>When true the i-frame timer runs on frame time; owners that tick it themselves turn this off.</summary>
        public bool SelfTicking { get; set; } = true;

        public int Max => maxHealth;
        public int Current { get { EnsureInitialized(); return _current; } }
        public bool IsDead => Current <= 0;
        public bool IsInvulnerable => Invulnerability.IsActive || (InvulnerabilityGate != null && InvulnerabilityGate());

        public float InvulnerableAfterHit
        {
            get => invulnerableAfterHit;
            set => invulnerableAfterHit = Mathf.Max(0f, value);
        }

        void Update()
        {
            if (SelfTicking) Invulnerability.Tick(Time.deltaTime);
        }

        /// <summary>Sets the maximum and the current value (default: full) and announces the change.</summary>
        public void Initialize(int max, int current = -1)
        {
            maxHealth = Mathf.Max(1, max);
            _current = current < 0 ? maxHealth : Mathf.Min(current, maxHealth);
            _initialized = true;
            Invulnerability.Clear();
            Changed?.Invoke(_current, maxHealth);
        }

        public HitOutcome TakeDamage(in DamageInfo info)
        {
            EnsureInitialized();
            if (info.Amount <= 0 || _current <= 0) return HitOutcome.Ignored;
            if (IsInvulnerable) return HitOutcome.Absorbed;
            if (DamageFilter != null && DamageFilter(info)) return HitOutcome.Absorbed;

            int applied = Mathf.Min(info.Amount, _current);
            _current -= applied;
            bool killed = _current == 0;
            if (!killed) Invulnerability.Begin(invulnerableAfterHit);

            Changed?.Invoke(_current, maxHealth);
            Damaged?.Invoke(info, applied);
            if (killed) Died?.Invoke(info);
            return killed ? HitOutcome.Killed : HitOutcome.Damaged;
        }

        /// <summary>Restores up to <paramref name="amount"/> and returns what was actually gained. Dead entities stay dead.</summary>
        public int Heal(int amount)
        {
            EnsureInitialized();
            if (amount <= 0 || _current <= 0) return 0;
            int gained = Mathf.Min(amount, maxHealth - _current);
            if (gained == 0) return 0;
            _current += gained;
            Changed?.Invoke(_current, maxHealth);
            return gained;
        }

        /// <summary>Full health, clears i-frames and revives.</summary>
        public void Refill()
        {
            EnsureInitialized();
            _current = maxHealth;
            Invulnerability.Clear();
            Changed?.Invoke(_current, maxHealth);
        }

        void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            _current = maxHealth;
        }
    }
}
