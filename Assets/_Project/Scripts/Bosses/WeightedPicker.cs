using System.Collections.Generic;
using AuraKnight.Enemies;

namespace AuraKnight.Bosses
{
    /// <summary>Weighted random choice with an injectable source. Zero or negative weights are never picked.</summary>
    public static class WeightedPicker
    {
        /// <summary>
        /// Picks an index proportionally to its weight. <paramref name="avoid"/> (the previous pick) is skipped unless it is the only
        /// candidate left, so a boss does not repeat itself. Returns -1 when nothing can be picked.
        /// </summary>
        public static int Pick(IReadOnlyList<float> weights, IRandomSource random, int avoid = -1)
        {
            float total = Total(weights, avoid);
            if (total <= 0f)
            {
                avoid = -1;
                total = Total(weights, avoid);
                if (total <= 0f) return -1;
            }
            float roll = random.Value() * total;
            int last = -1;
            for (int i = 0; i < weights.Count; i++)
            {
                if (i == avoid || weights[i] <= 0f) continue;
                last = i;
                roll -= weights[i];
                if (roll < 0f) return i;
            }
            return last;
        }

        static float Total(IReadOnlyList<float> weights, int avoid)
        {
            float total = 0f;
            for (int i = 0; i < weights.Count; i++)
                if (i != avoid && weights[i] > 0f) total += weights[i];
            return total;
        }
    }
}
