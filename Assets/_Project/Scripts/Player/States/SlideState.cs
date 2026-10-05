using UnityEngine;

namespace AuraKnight.Player.States
{
    /// <summary>
    /// 0.45 s at 11 u/s with a 0.8 x 0.9 collider. While a ceiling blocks standing the slide keeps going
    /// (reversing at walls) until the player is clear.
    /// </summary>
    public sealed class SlideState : PlayerStateBase
    {
        const float Epsilon = 1e-4f;
        int _direction;
        float _elapsed;

        public SlideState(PlayerController controller) : base(controller) { }

        public override void Enter()
        {
            _direction = C.MoveDirection != 0 ? C.MoveDirection : C.Facing;
            C.Face(_direction);
            C.Motor.SetCrouched(true);
            C.SlideBuffer.Cancel();
            C.SetVelocityX(_direction * C.Config.SlideSpeed);
            _elapsed = 0f;
        }

        public override void Exit() => C.Motor.TrySetStanding();

        public override void FixedTick()
        {
            _elapsed += C.Dt;
            if (!C.Grounded) { C.StateMachine.TryChange(PlayerStateId.Fall); return; }

            bool canStand = C.Motor.CanStand();
            if (canStand && C.JumpBuffer.IsActive && PlayerTransitions.TryJump(C)) return;
            if (canStand && _elapsed >= C.Config.SlideDuration - Epsilon) { C.Land(); return; }
            if (!canStand && C.LastContacts.Horizontal) _direction = -_direction;

            C.Face(_direction);
            C.SetVelocityX(_direction * C.Config.SlideSpeed);
            C.ApplyGravity(C.Config.MaxFallSpeed);
        }
    }
}
