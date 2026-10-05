using UnityEngine;

namespace AuraKnight.Combat
{
    /// <summary>Immutable description of one hit, passed from <see cref="Hitbox"/> to <see cref="Health"/>.</summary>
    public readonly struct DamageInfo
    {
        /// <summary>Hearts (player) or hit points (enemy) to remove. Boss attacks may pass 2.</summary>
        public readonly int Amount;
        public readonly Team Team;
        /// <summary>Attacker's root object; may be null for environmental damage.</summary>
        public readonly GameObject Source;
        /// <summary>Points from the attacker toward the victim; only its sign on X drives knockback.</summary>
        public readonly Vector2 Direction;
        /// <summary>Knockback distance in tiles (1 tile = 1 unit).</summary>
        public readonly float KnockbackTiles;

        public DamageInfo(int amount, Team team, GameObject source = null, Vector2 direction = default,
            float knockbackTiles = Knockback.DefaultTiles)
        {
            Amount = amount;
            Team = team;
            Source = source;
            Direction = direction;
            KnockbackTiles = knockbackTiles;
        }
    }
}
