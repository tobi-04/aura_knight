using System;

namespace AuraKnight.Editor
{
    /// <summary>What Leo can do while a room is analysed: one flag per Aura-gated feature, plus the shortcut door state.</summary>
    [Flags]
    public enum LevelAbilities
    {
        None = 0,
        Wind = 1,
        Fire = 2,
        Water = 4,
        /// <summary>The three Castle seals are lit (needs all three Auras).</summary>
        Seals = 8,
        /// <summary>One-way shortcut doors are open.</summary>
        ShortcutOpen = 16,
        All = Wind | Fire | Water | Seals | ShortcutOpen,
    }

    /// <summary>Parsing and naming of <see cref="LevelAbilities"/> (the tokens used by the "requires" line of a room file).</summary>
    public static class LevelAbilityNames
    {
        public static bool TryParse(string token, out LevelAbilities ability)
        {
            switch (token)
            {
                case "wind": ability = LevelAbilities.Wind; return true;
                case "fire": ability = LevelAbilities.Fire; return true;
                case "water": ability = LevelAbilities.Water; return true;
                case "seals": ability = LevelAbilities.Seals; return true;
                default: ability = LevelAbilities.None; return false;
            }
        }

        /// <summary>The abilities Leo owns once he has the given Auras (the seals need all three).</summary>
        public static LevelAbilities FromAuras(bool wind, bool fire, bool water)
        {
            var ab = LevelAbilities.None;
            if (wind) ab |= LevelAbilities.Wind;
            if (fire) ab |= LevelAbilities.Fire;
            if (water) ab |= LevelAbilities.Water;
            if (wind && fire && water) ab |= LevelAbilities.Seals;
            return ab;
        }
    }
}
