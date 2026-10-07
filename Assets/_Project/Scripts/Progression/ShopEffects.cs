using AuraKnight.Core;
using AuraKnight.Player;
using UnityEngine;

namespace AuraKnight.Progression
{
    /// <summary>Applies an upgrade to the save and respects the same caps as <see cref="PlayerStatsSeed"/> (9 hearts, 200 energy, sword 3).</summary>
    public static class ShopEffects
    {
        /// <summary>False when the stat is already at its cap, so a purchase would be wasted coins.</summary>
        public static bool CanApply(GameState state, ShopEffect effect)
        {
            switch (effect)
            {
                case ShopEffect.Heart: return state.maxHearts < PlayerStatsSeed.MaxHeartsLimit;
                case ShopEffect.Energy: return state.maxEnergy < PlayerStatsSeed.MaxEnergyLimit;
                case ShopEffect.Sword: return state.swordLevel < PlayerStatsSeed.MaxSwordLevel;
                default: return true; // a map has no stat to cap
            }
        }

        /// <summary>Raises the stat (clamped to its cap). Returns false when nothing changed. Map effects only count as a purchase record.</summary>
        public static bool Apply(GameState state, ShopEffect effect, int amount)
        {
            if (!CanApply(state, effect)) return false;
            switch (effect)
            {
                case ShopEffect.Heart:
                    state.maxHearts = Mathf.Min(PlayerStatsSeed.MaxHeartsLimit, state.maxHearts + amount);
                    return true;
                case ShopEffect.Energy:
                    state.maxEnergy = Mathf.Min(PlayerStatsSeed.MaxEnergyLimit, state.maxEnergy + amount);
                    return true;
                case ShopEffect.Sword:
                    state.swordLevel = Mathf.Min(PlayerStatsSeed.MaxSwordLevel, state.swordLevel + amount);
                    return true;
                default:
                    return true;
            }
        }
    }
}
