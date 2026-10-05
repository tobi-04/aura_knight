namespace AuraKnight.Player.States
{
    /// <summary>Kicks off the wall with (+-11, 22) and ignores horizontal input for 0.15 s.</summary>
    public sealed class WallJumpState : PlayerStateBase
    {
        float _lockRemaining;

        public WallJumpState(PlayerController controller) : base(controller) { }

        public override void Enter()
        {
            int dir = C.WallJumpDirection;
            var v = C.Config.WallJumpVelocity;
            C.Face(dir);
            C.SetVelocity(new UnityEngine.Vector2(dir * v.x, v.y));
            C.ApplyGravity(C.Config.MaxFallSpeed);
            C.JumpBuffer.Cancel();
            C.Coyote.Cancel();
            C.JumpCut.Cancel();
            _lockRemaining = C.Config.WallJumpInputLock;
        }

        public override void FixedTick()
        {
            _lockRemaining -= C.Dt;
            if (C.Grounded) { C.Land(); return; }
            if (PlayerTransitions.TryAirActions(C)) return;
            if (C.Velocity.y <= 0f) { C.StateMachine.TryChange(PlayerStateId.Fall); return; }

            if (_lockRemaining <= 0f) C.RunHorizontal();
            C.ApplyGravity(C.Config.MaxFallSpeed);
        }
    }
}
