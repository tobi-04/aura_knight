namespace AuraKnight.Player.States
{
    /// <summary>Buffered-input transitions shared by several states (jump, double jump, dash, slide).</summary>
    public static class PlayerTransitions
    {
        /// <summary>Jump from ground or inside the coyote window.</summary>
        public static bool TryJump(PlayerController c)
        {
            if (!c.Coyote.IsActive || !c.JumpBuffer.IsActive) return false;
            c.StartJump(c.Config.JumpVelocity);
            return true;
        }

        /// <summary>Wind aura: one extra jump while airborne and past the coyote window.</summary>
        public static bool TryDoubleJump(PlayerController c)
        {
            if (!c.CanDoubleJump || c.DoubleJumpUsed || c.Coyote.IsActive || !c.JumpBuffer.IsActive) return false;
            c.DoubleJumpUsed = true;
            c.StartJump(c.Config.DoubleJumpVelocity);
            return true;
        }

        public static bool TryDash(PlayerController c)
        {
            if (!c.DashBuffer.IsActive || !c.Dash.CanDash(c.Grounded)) return false;
            c.DashBuffer.Cancel();
            return c.StateMachine.TryChange(PlayerStateId.Dash);
        }

        public static bool TrySlide(PlayerController c)
        {
            if (!c.Grounded || !c.SlideBuffer.IsActive) return false;
            c.SlideBuffer.Cancel();
            return c.StateMachine.TryChange(PlayerStateId.Slide);
        }

        public static bool TryGroundActions(PlayerController c) =>
            TryJump(c) || TryDash(c) || TrySlide(c);

        public static bool TryAirActions(PlayerController c) =>
            TryJump(c) || TryDoubleJump(c) || TryDash(c);
    }
}
