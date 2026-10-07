using AuraKnight.Combat;
using UnityEngine;

namespace AuraKnight.Bosses
{
    /// <summary>Damage scaling for a weak point: at least 1, otherwise amount x multiplier rounded.</summary>
    public static class WeakPointMath
    {
        public static int Scale(int amount, float multiplier) =>
            amount <= 0 ? 0 : Mathf.Max(1, Mathf.RoundToInt(amount * Mathf.Max(0f, multiplier)));
    }

    /// <summary>
    /// A secondary Hurtbox (Enemy layer, its own child object) that forwards every hit to the boss's Health multiplied by
    /// <see cref="Multiplier"/> (the boiler of the Rogue Machine, the core of the Root Tree). It owns a large stand-in Health so the normal
    /// Hitbox to Hurtbox to Health path keeps working unchanged; the stand-in is refilled after each hit and never dies.
    /// Place its collider so it does not overlap the body Hurtbox, otherwise one swing would hit both.
    /// </summary>
    [RequireComponent(typeof(Hurtbox), typeof(Health))]
    public sealed class WeakPointHurtbox : MonoBehaviour
    {
        const int StandInHealth = 1000;

        [SerializeField] Health target;
        [SerializeField, Min(0f)] float multiplier = 2f;

        Health _standIn;

        public float Multiplier { get => multiplier; set => multiplier = Mathf.Max(0f, value); }
        public Health Target { get => target; set => target = value; }

        void Awake()
        {
            _standIn = GetComponent<Health>();
            GetComponent<Hurtbox>().Health = _standIn;
            if (target == null) target = GetComponentInParent<BossBase>(true)?.Health;
            if (target == null)
            {
                Debug.LogError($"{name}: no boss Health to forward weak-point damage to.", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            _standIn.Initialize(StandInHealth);
            _standIn.Damaged += OnDamaged;
        }

        void OnDisable() => _standIn.Damaged -= OnDamaged;

        void OnDamaged(DamageInfo info, int applied)
        {
            int scaled = WeakPointMath.Scale(applied, multiplier);
            _standIn.Refill();
            target.TakeDamage(new DamageInfo(scaled, info.Team, info.Source, info.Direction, info.KnockbackTiles));
        }
    }
}
