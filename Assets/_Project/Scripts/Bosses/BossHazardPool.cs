using System.Collections.Generic;
using UnityEngine;

namespace AuraKnight.Bosses
{
    /// <summary>
    /// Free list for damaging <see cref="BossHazard"/>s (projectiles, waves, spikes). Boss fights spawn a few of them every attack; reusing
    /// the objects keeps allocation and GC out of the fight (GDD 12.5). Entries destroyed with their scene are skipped when renting.
    /// </summary>
    public static class BossHazardPool
    {
        const int MaxFree = 64;
        static readonly Stack<BossHazard> Free = new Stack<BossHazard>();

        public static int FreeCount => Free.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlayModeEnter() => Free.Clear();

        /// <summary>Forgets every parked hazard (their objects are left to their scenes).</summary>
        public static void Clear() => Free.Clear();

        /// <summary>A parked hazard, or null when none is free (the caller builds a new one).</summary>
        internal static BossHazard Rent()
        {
            while (Free.Count > 0)
            {
                var hazard = Free.Pop();
                if (hazard != null) return hazard;
            }
            return null;
        }

        /// <summary>Hides the hazard and keeps it for reuse; a full pool just destroys the extra.</summary>
        internal static void Return(BossHazard hazard)
        {
            if (hazard == null) return;
            if (Free.Count >= MaxFree)
            {
                Object.Destroy(hazard.gameObject);
                return;
            }
            hazard.gameObject.SetActive(false);
            Free.Push(hazard);
        }
    }
}
