namespace AuraKnight.Player.States
{
    /// <summary>Rising phase. The launch itself happens in <see cref="PlayerController.StartJump"/>.</summary>
    public sealed class JumpState : PlayerStateBase
    {
        public JumpState(PlayerController controller) : base(controller) { }

        public override void FixedTick()
        {
            if (PlayerTransitions.TryAirActions(C)) return;
            if (C.Velocity.y <= 0f) { C.StateMachine.TryChange(PlayerStateId.Fall); return; }

            C.RunHorizontal();
            C.ApplyGravity(C.Config.MaxFallSpeed);
            C.SetVerticalSpeed(C.JumpCut.Update(C.Velocity.y, C.Dt, C.Input.JumpHeld));
        }
    }
}
