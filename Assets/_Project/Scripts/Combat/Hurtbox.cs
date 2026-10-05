using AuraKnight.Core;
using UnityEngine;

namespace AuraKnight.Combat
{
    /// <summary>
    /// Receiving end of a hit. Put it on the same GameObject as the collider a <see cref="Hitbox"/> should
    /// find (trigger or solid). Without a <see cref="Combat.Health"/> it acts as a pogo-able, damage-free surface (spikes).
    /// </summary>
    public sealed class Hurtbox : MonoBehaviour
    {
        [SerializeField] Team team = Team.Enemy;
        [SerializeField] Health health;
        [Tooltip("A downward sword strike on this bounces Leo up (enemies, spikes).")]
        [SerializeField] bool allowsPogo = true;

        /// <summary>Changing the team also moves the object to that team's physics layer.</summary>
        public Team Team
        {
            get => team;
            set
            {
                team = value;
                ApplyLayer();
            }
        }

        void Awake() => ApplyLayer();

        /// <summary>Puts this object on its team's layer (Player / Enemy / Hazard).</summary>
        public void ApplyLayer() => PhysicsLayers.Apply(gameObject, HitMasks.HurtboxLayer(team));
        public bool AllowsPogo { get => allowsPogo; set => allowsPogo = value; }

        public Health Health
        {
            get
            {
                if (health == null) health = GetComponentInParent<Health>(true);
                return health;
            }
            set => health = value;
        }

        public HitOutcome Receive(in DamageInfo info)
        {
            if (!DamageRules.CanDamage(info.Team, team)) return HitOutcome.Ignored;
            var target = Health;
            return target == null ? HitOutcome.Touched : target.TakeDamage(info);
        }
    }
}
