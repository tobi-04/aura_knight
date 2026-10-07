using System.Collections.Generic;
using UnityEngine;

namespace AuraKnight.World
{
    /// <summary>
    /// Pure rule behind <see cref="LightBudgetController"/>: at most N local 2D lights are lit at once, the N nearest to the player
    /// (GDD 12.5, URP 2D pays per light per pixel on weak GPUs). Power saving halves the budget.
    /// </summary>
    public static class LightBudget
    {
        public const int Normal = 8;
        public const int PowerSaving = 4;

        public static int Limit(bool powerSaving) => powerSaving ? PowerSaving : Normal;

        /// <summary>Marks the <paramref name="limit"/> positions nearest to <paramref name="center"/> in <paramref name="allowed"/> (one slot per position, ties go to the lower index).</summary>
        public static void Select(IReadOnlyList<Vector2> positions, Vector2 center, int limit, IList<bool> allowed)
        {
            int count = positions.Count;
            for (int i = 0; i < count; i++) allowed[i] = false;
            int picks = Mathf.Min(Mathf.Max(limit, 0), count);
            for (int pick = 0; pick < picks; pick++)
            {
                int best = -1;
                float bestDistance = float.PositiveInfinity;
                for (int i = 0; i < count; i++)
                {
                    if (allowed[i]) continue;
                    float distance = (positions[i] - center).sqrMagnitude;
                    if (best >= 0 && !(distance < bestDistance)) continue;
                    best = i;
                    bestDistance = distance;
                }
                allowed[best] = true;
            }
        }
    }
}
