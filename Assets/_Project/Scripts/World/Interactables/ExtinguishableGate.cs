using AuraKnight.Aura;

namespace AuraKnight.World
{
    /// <summary>Fire trap: the Water skill (shield cast, proximity) puts it out for good.</summary>
    public sealed class ExtinguishableGate : OneTimeAuraGate
    {
        public override AuraId RequiredAura => AuraId.Water;
        public override AuraInteraction Interaction => AuraInteraction.Extinguish;
    }
}
