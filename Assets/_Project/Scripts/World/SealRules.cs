using AuraKnight.Aura;

namespace AuraKnight.World
{
    /// <summary>Pure rules of the Castle gate (GDD 7.2): three seals, one per Aura, all lit before it opens.</summary>
    public static class SealRules
    {
        /// <summary>The Auras that light the three seals, in the order the seals stand in the hub.</summary>
        public static readonly AuraId[] SealAuras = { AuraId.Wind, AuraId.Fire, AuraId.Water };

        /// <summary>A seal lights only while Leo wears exactly its Aura (which also means he has unlocked it).</summary>
        public static bool Lights(AuraId sealAura, AuraId worn) => sealAura != AuraId.None && sealAura == worn;

        /// <summary>The gate opens when every seal is lit.</summary>
        public static bool AllLit(System.Collections.Generic.IReadOnlyList<bool> lit)
        {
            if (lit == null || lit.Count == 0) return false;
            for (int i = 0; i < lit.Count; i++)
                if (!lit[i]) return false;
            return true;
        }
    }
}
