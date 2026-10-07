using AuraKnight.Combat;
using UnityEngine;

namespace AuraKnight.World.Hazards
{
    /// <summary>
    /// Floor/pit hazard on the Hazard layer: a contact <see cref="Hitbox"/> that hurts Leo (1 heart, re-hits every 0.5 s). With
    /// <see cref="lethal"/> it kills outright, which is how bottomless pits send Leo back to the last altar. Pairs with a Hazard
    /// <see cref="Hurtbox"/> on the same object so a downward sword strike pogoes off visible spikes.
    /// </summary>
    [RequireComponent(typeof(Hitbox))]
    public sealed class Spikes : MonoBehaviour
    {
        /// <summary>Damage of a lethal hazard: more than any heart count Leo can reach (GDD 8 caps hearts at 9).</summary>
        public const int LethalDamage = 99;

        [SerializeField, Min(1)] int damage = 1;
        [SerializeField] bool lethal;
        [SerializeField, Min(0.1f)] float rearmSeconds = 0.5f;

        public bool Lethal => lethal;
        public int EffectiveDamage => lethal ? LethalDamage : damage;

        void Awake()
        {
            var hitbox = GetComponent<Hitbox>();
            hitbox.Team = Team.Hazard;
            hitbox.Damage = EffectiveDamage;
            hitbox.RearmInterval = rearmSeconds;
        }
    }
}
