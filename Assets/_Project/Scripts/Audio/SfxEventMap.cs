using AuraKnight.Aura;
using AuraKnight.Player;

namespace AuraKnight.Audio
{
    /// <summary>Pure mapping from gameplay facts to sound ids, kept free of scene state so it can be unit tested.</summary>
    public static class SfxEventMap
    {
        /// <summary>Sound for switching to the Aura with this event id (None and unknown ids are silent).</summary>
        public static SfxId ForAuraChanged(string auraKey) => ForAura(AuraIds.ParseOrNone(auraKey), SfxId.AuraWind, SfxId.AuraFire, SfxId.AuraWater);

        /// <summary>Sound for casting the skill of this Aura.</summary>
        public static SfxId ForSkill(AuraId aura) => ForAura(aura, SfxId.SkillWind, SfxId.SkillFire, SfxId.SkillWater);

        /// <summary>Sound when the player's state machine enters <paramref name="to"/>. Landing, footsteps and double jumps are detected elsewhere.</summary>
        public static SfxId ForPlayerState(PlayerStateId to)
        {
            switch (to)
            {
                case PlayerStateId.Jump:
                case PlayerStateId.WallJump: return SfxId.Jump;
                case PlayerStateId.Dash: return SfxId.Dash;
                case PlayerStateId.Slide: return SfxId.Slide;
                case PlayerStateId.WallSlide: return SfxId.WallSlide;
                case PlayerStateId.Attack:
                case PlayerStateId.AirAttack: return SfxId.SwordSwing;
                default: return SfxId.None;
            }
        }

        /// <summary>Energy only ever drops when a skill is cast (sword hits add energy), so a drop means a cast.</summary>
        public static bool IsEnergySpent(float previous, float current) => previous >= 0f && current < previous - 0.001f;

        /// <summary>A coin pickup raises the total; spending in a shop lowers it silently. <paramref name="previous"/> -1 means "not seen yet".</summary>
        public static bool IsCoinGain(int previous, int current) => previous >= 0 && current > previous;

        static SfxId ForAura(AuraId aura, SfxId wind, SfxId fire, SfxId water)
        {
            switch (aura)
            {
                case AuraId.Wind: return wind;
                case AuraId.Fire: return fire;
                case AuraId.Water: return water;
                default: return SfxId.None;
            }
        }
    }
}
