using UnityEngine;

namespace AuraKnight.Player.States
{
    /// <summary>Horizontal burst: 25 u/s for 0.2 s, no gravity, i-frames tracked by <see cref="DashTracker"/>.</summary>
    public sealed class DashState : PlayerStateBase
    {
        const float Epsilon = 1e-4f;
        int _direction;
        float _elapsed;

        public DashState(PlayerController controller) : base(controller) { }

        public override void Enter()
        {
            int input = C.MoveDirection;
            // Dashing out of a wall slide always goes away from the wall.
            _direction = C.StateMachine.PreviousId == PlayerStateId.WallSlide ? -C.Facing : (input != 0 ? input : C.Facing);
            C.Face(_direction);
            C.Dash.Begin(C.Grounded);
            C.DashBuffer.Cancel();
            _elapsed = 0f;
            C.SetVelocity(new Vector2(_direction * C.Config.DashSpeed, 0f));
        }

        public override void FixedTick()
        {
            _elapsed += C.Dt;
            if (_elapsed >= C.Config.DashDuration - Epsilon)
            {
                C.SetVelocityX(_direction * C.RunSpeed);
                if (C.Grounded) C.Land();
                else C.StateMachine.TryChange(PlayerStateId.Fall);
                return;
            }
            C.SetVelocity(new Vector2(_direction * C.Config.DashSpeed, 0f));
        }
    }
}
