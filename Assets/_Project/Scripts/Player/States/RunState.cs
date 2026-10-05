using UnityEngine;

namespace AuraKnight.Player.States
{
    public sealed class RunState : PlayerStateBase
    {
        const float StopThreshold = 0.05f;

        public RunState(PlayerController controller) : base(controller) { }

        public override void FixedTick()
        {
            if (!C.Grounded) { C.StateMachine.TryChange(PlayerStateId.Fall); return; }
            if (PlayerTransitions.TryGroundActions(C)) return;

            C.RunHorizontal();
            C.ApplyGravity(C.Config.MaxFallSpeed);
            if (C.MoveDirection == 0 && Mathf.Abs(C.Velocity.x) < StopThreshold)
                C.StateMachine.TryChange(PlayerStateId.Idle);
        }
    }
}
