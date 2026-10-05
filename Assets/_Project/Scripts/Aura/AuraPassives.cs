using System;
using UnityEngine;

namespace AuraKnight.Aura
{
    /// <summary>Passive abilities granted while an Aura is active (GDD section 6). Pure data, edited in <see cref="AuraDefinition"/>.</summary>
    [Serializable]
    public struct AuraPassives
    {
        public bool doubleJump;
        public bool glide;
        [Min(0.1f)] public float speedMultiplier;
        public bool swim;
        public bool heatImmune;
        public bool acidImmune;

        /// <summary>No abilities, normal run speed.</summary>
        public static AuraPassives None => new AuraPassives { speedMultiplier = 1f };

        /// <summary>Treats an unset (zero) multiplier from a hand-made asset as 1.</summary>
        public float SpeedOrOne => speedMultiplier > 0f ? speedMultiplier : 1f;
    }
}
