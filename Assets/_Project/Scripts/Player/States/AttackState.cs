namespace AuraKnight.Player.States
{
    /// <summary>Grounded sword swing (combo step 1 or 2, or an up-slash). Leo plants his feet for the 0.25 s swing.</summary>
    public sealed class AttackState : PlayerStateBase
    {
        readonly PlayerCombat _combat;

        public AttackState(PlayerController controller, PlayerCombat combat) : base(controller) => _combat = combat;

        public override void Enter() => _combat.BeginSwing();

        public override void FixedTick()
        {
            bool finished = _combat.TickSwing(C.Dt);
            C.SetVelocityX(HorizontalMotion.Step(C.Config, C.Velocity.x, 0f, C.Dt, C.SpeedMultiplier));
            C.ApplyGravity(C.Config.MaxFallSpeed);
            if (finished) AttackExit.Leave(C);
        }

        public override void Exit() => _combat.EndSwing();
    }

    /// <summary>Picks the locomotion state to resume once a swing is over.</summary>
    static class AttackExit
    {
        public static void Leave(PlayerController c)
        {
            if (c.SwimMode) c.StateMachine.TryChange(PlayerStateId.Swim);
            else if (c.Grounded) c.Land();
            else c.StateMachine.TryChange(c.Velocity.y > 0f ? PlayerStateId.Jump : PlayerStateId.Fall);
        }
    }
}
