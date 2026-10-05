namespace AuraKnight.Player.States
{
    public sealed class IdleState : PlayerStateBase
    {
        public IdleState(PlayerController controller) : base(controller) { }

        public override void FixedTick()
        {
            if (!C.Grounded) { C.StateMachine.TryChange(PlayerStateId.Fall); return; }
            if (PlayerTransitions.TryGroundActions(C)) return;
            if (C.MoveDirection != 0) { C.StateMachine.TryChange(PlayerStateId.Run); return; }

            C.RunHorizontal();
            C.ApplyGravity(C.Config.MaxFallSpeed);
        }
    }
}
