using AuraKnight.Aura;

namespace AuraKnight.World
{
    /// <summary>Thorn bush / old wooden barricade: the Fire skill (fireball) burns it open for good.</summary>
    public sealed class BurnableGate : OneTimeAuraGate
    {
        public override AuraId RequiredAura => AuraId.Fire;
        public override AuraInteraction Interaction => AuraInteraction.Burn;
    }
}
