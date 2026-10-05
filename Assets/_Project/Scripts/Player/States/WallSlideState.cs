namespace AuraKnight.Player.States
{
    /// <summary>Pressing into a wall while falling: capped fall speed, wall jump available.</summary>
    public sealed class WallSlideState : PlayerStateBase
    {
        int _wallDirection;

        public WallSlideState(PlayerController controller) : base(controller) { }

        public override void Enter()
        {
            _wallDirection = C.WallContactToward();
            C.Face(_wallDirection);
            C.DoubleJumpUsed = false;
            C.Dash.ResetAir();
            C.SetVelocityX(0f);
        }

        public override void FixedTick()
        {
            if (C.Grounded) { C.Land(); return; }
            if (C.WallContactToward() != _wallDirection) { C.StateMachine.TryChange(PlayerStateId.Fall); return; }
            if (C.JumpBuffer.Consume())
            {
                C.WallJumpDirection = -_wallDirection;
                C.StateMachine.TryChange(PlayerStateId.WallJump);
                return;
            }
            if (PlayerTransitions.TryDash(C)) return;

            C.SetVelocityX(0f);
            C.ApplyGravity(C.Config.WallSlideMaxSpeed);
        }
    }
}
