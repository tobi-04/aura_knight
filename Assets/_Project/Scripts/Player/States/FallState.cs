namespace AuraKnight.Player.States
{
    public sealed class FallState : PlayerStateBase
    {
        public FallState(PlayerController controller) : base(controller) { }

        public override void FixedTick()
        {
            if (C.Grounded) { C.Land(); return; }
            if (PlayerTransitions.TryAirActions(C)) return;
            if (C.Velocity.y < 0f && C.WallContactToward() != 0)
            {
                C.StateMachine.TryChange(PlayerStateId.WallSlide);
                return;
            }

            C.RunHorizontal();
            bool gliding = C.CanGlide && C.Input.JumpHeld;
            C.ApplyGravity(gliding ? C.Config.GlideMaxFallSpeed : C.Config.MaxFallSpeed);
        }
    }
}
