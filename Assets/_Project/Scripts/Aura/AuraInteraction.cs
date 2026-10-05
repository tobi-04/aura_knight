namespace AuraKnight.Aura
{
    /// <summary>How an Aura acts on the environment (GDD sections 6 and 7.2).</summary>
    public enum AuraInteraction
    {
        /// <summary>Fire: thorn bushes, old wooden barricades.</summary>
        Burn,
        /// <summary>Water: fire traps.</summary>
        Extinguish,
        /// <summary>Water: lava becomes a standable platform for a few seconds.</summary>
        Freeze,
        /// <summary>Wind: updraft zones, only usable while the Wind Aura is active.</summary>
        WindLift,
        /// <summary>Fire: heat vents hurt unless the Aura grants heat immunity.</summary>
        HeatVent
    }

    /// <summary>Implemented by world objects that react to an Aura skill (gates, freezable lava).</summary>
    public interface IAuraInteractable
    {
        /// <summary>True when the object accepted the interaction and changed state.</summary>
        bool TryInteract(AuraInteraction interaction, AuraId source);
    }

    /// <summary>The one Aura that may trigger each interaction.</summary>
    public static class AuraInteractionRules
    {
        public static AuraId RequiredAura(AuraInteraction interaction)
        {
            switch (interaction)
            {
                case AuraInteraction.Burn:
                case AuraInteraction.HeatVent:
                    return AuraId.Fire;
                case AuraInteraction.Extinguish:
                case AuraInteraction.Freeze:
                    return AuraId.Water;
                case AuraInteraction.WindLift:
                    return AuraId.Wind;
                default:
                    return AuraId.None;
            }
        }

        /// <summary>A gate wanting <paramref name="required"/>/<paramref name="wanted"/> accepts only the same interaction from the same Aura.</summary>
        public static bool Accepts(AuraId required, AuraInteraction wanted, AuraInteraction offered, AuraId source) =>
            required != AuraId.None && source == required && offered == wanted;
    }
}
