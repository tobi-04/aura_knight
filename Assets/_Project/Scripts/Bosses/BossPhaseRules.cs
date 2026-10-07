using System.Collections.Generic;
using UnityEngine;

namespace AuraKnight.Bosses
{
    /// <summary>Which phase a boss is in for its current hit points. Phase i starts when HP falls to <c>fraction[i] * max</c> (inclusive).</summary>
    public static class BossPhaseRules
    {
        /// <summary>Hit points at (or below) which a phase starts: floor(fraction * max), so 50% of 30 is 15 and 25% of 70 is 17.</summary>
        public static int ThresholdHp(int maxHp, float fraction) => Mathf.FloorToInt(fraction * maxHp + 0.0001f);

        /// <summary>Highest phase whose threshold the HP has reached; 0 when nothing was reached or the list is empty.</summary>
        public static int IndexFor(int hp, int maxHp, IReadOnlyList<float> startFractions)
        {
            int index = 0;
            for (int i = 1; i < startFractions.Count; i++)
                if (hp <= ThresholdHp(maxHp, startFractions[i])) index = i;
            return index;
        }
    }
}
