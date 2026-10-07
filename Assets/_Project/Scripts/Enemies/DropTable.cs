using AuraKnight.World.Pickups;
using UnityEngine;

namespace AuraKnight.Enemies
{
    /// <summary>
    /// Pure loot rule: an inclusive coin range (GDD 7.3: 3-6 depending on the variant) plus the 10% Light Drop shared
    /// with <see cref="LightDropPickup"/>. The random source is injected so tests and designers can drive it.
    /// </summary>
    public readonly struct DropTable
    {
        public readonly int CoinsMin;
        public readonly int CoinsMax;
        public readonly float LightChance;

        /// <summary>Negative amounts clamp to 0 and an inverted range collapses to its minimum.</summary>
        public DropTable(int coinsMin, int coinsMax, float lightChance = LightDropPickup.DropChance)
        {
            CoinsMin = Mathf.Max(0, coinsMin);
            CoinsMax = Mathf.Max(CoinsMin, coinsMax);
            LightChance = Mathf.Clamp01(lightChance);
        }

        public DropResult Roll(IRandomSource random)
        {
            int coins = CoinsMax == CoinsMin ? CoinsMin : random.Range(CoinsMin, CoinsMax + 1);
            bool light = random.Value() < LightChance;
            return new DropResult(coins, light);
        }
    }
}
