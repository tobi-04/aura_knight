using AuraKnight.Core;
using UnityEngine;

namespace AuraKnight.Player
{
    /// <summary>Starting values for <see cref="PlayerStats"/>: from the live save when present, else GDD defaults (5 / 100 / 1).</summary>
    public readonly struct PlayerStatsSeed
    {
        public const int MaxHeartsLimit = 9;
        public const int MaxEnergyLimit = 200;
        public const int MaxSwordLevel = 3;

        public readonly int MaxHearts;
        public readonly int MaxEnergy;
        public readonly int SwordLevel;

        public PlayerStatsSeed(int maxHearts, int maxEnergy, int swordLevel)
        {
            MaxHearts = Mathf.Clamp(maxHearts, 1, MaxHeartsLimit);
            MaxEnergy = Mathf.Clamp(maxEnergy, 1, MaxEnergyLimit);
            SwordLevel = Mathf.Clamp(swordLevel, 1, MaxSwordLevel);
        }

        public static PlayerStatsSeed Defaults => new PlayerStatsSeed(5, 100, 1);

        /// <summary>Reads maxHearts / maxEnergy / swordLevel, clamping corrupt values; null state gives the defaults.</summary>
        public static PlayerStatsSeed From(GameState state) =>
            state == null ? Defaults : new PlayerStatsSeed(state.maxHearts, state.maxEnergy, state.swordLevel);
    }
}
