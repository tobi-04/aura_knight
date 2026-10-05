namespace AuraKnight.Player
{
    /// <summary>Pure rules for which player actions are allowed in which state (Dead/Hurt/paused).</summary>
    public static class PlayerActionRules
    {
        /// <summary>Aura buttons and the HUD ring: only blocked while dead or when controls are off (pause, cutscene).</summary>
        public static bool CanSwitchAura(PlayerStateId state, bool controlsEnabled) =>
            controlsEnabled && state != PlayerStateId.Dead;

        /// <summary>Skills additionally wait for the hurt stagger to end.</summary>
        public static bool CanCastSkill(PlayerStateId state, bool controlsEnabled) =>
            controlsEnabled && state != PlayerStateId.Dead && state != PlayerStateId.Hurt;

        /// <summary>The sword may start from free movement, including swimming.</summary>
        public static bool CanStartAttack(PlayerStateId state, bool controlsEnabled)
        {
            if (!controlsEnabled) return false;
            switch (state)
            {
                case PlayerStateId.Idle:
                case PlayerStateId.Run:
                case PlayerStateId.Jump:
                case PlayerStateId.Fall:
                case PlayerStateId.Swim:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Swimming swings use the airborne attack so water physics (no gravity) can continue during the swing.</summary>
        public static PlayerStateId AttackStateFor(PlayerStateId state, bool grounded) =>
            state == PlayerStateId.Swim || !grounded ? PlayerStateId.AirAttack : PlayerStateId.Attack;
    }
}
