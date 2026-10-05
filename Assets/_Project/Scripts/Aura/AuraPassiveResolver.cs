using UnityEngine;

namespace AuraKnight.Aura
{
    /// <summary>The player-facing values derived from the active Aura and the environment.</summary>
    public readonly struct PlayerModifiers
    {
        public readonly bool CanDoubleJump;
        public readonly bool CanGlide;
        public readonly float SpeedMultiplier;
        public readonly float JumpMultiplier;
        public readonly bool SwimMode;
        /// <summary>In water without the swim passive: the oxygen bar drains.</summary>
        public readonly bool Drowning;
        public readonly bool HeatImmune;
        public readonly bool AcidImmune;

        public PlayerModifiers(bool canDoubleJump, bool canGlide, float speedMultiplier, float jumpMultiplier,
            bool swimMode, bool drowning, bool heatImmune, bool acidImmune)
        {
            CanDoubleJump = canDoubleJump;
            CanGlide = canGlide;
            SpeedMultiplier = speedMultiplier;
            JumpMultiplier = jumpMultiplier;
            SwimMode = swimMode;
            Drowning = drowning;
            HeatImmune = heatImmune;
            AcidImmune = acidImmune;
        }
    }

    /// <summary>Pure mapping from passives and "is in water" to <see cref="PlayerModifiers"/> (GDD section 4 water table).</summary>
    public static class AuraPassiveResolver
    {
        public const float WaterSpeedMultiplier = 0.5f;
        public const float WaterJumpMultiplier = 0.6f;

        public static PlayerModifiers Resolve(in AuraPassives passives, bool inWater)
        {
            bool wading = inWater && !passives.swim;
            return new PlayerModifiers(
                passives.doubleJump,
                passives.glide,
                passives.SpeedOrOne * (wading ? WaterSpeedMultiplier : 1f),
                wading ? WaterJumpMultiplier : 1f,
                inWater && passives.swim,
                wading,
                passives.heatImmune,
                passives.acidImmune);
        }
    }
}
