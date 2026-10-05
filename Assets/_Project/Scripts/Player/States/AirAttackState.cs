namespace AuraKnight.Player.States
{
    /// <summary>
    /// Airborne swing, including the down-slash and any swing while swimming. Air control and gravity continue; a down-slash that
    /// connects with an enemy or spikes pogoes Leo upward (handled by <see cref="PlayerCombat"/>).
    /// </summary>
    public sealed class AirAttackState : PlayerStateBase
    {
        readonly PlayerCombat _combat;

        public AirAttackState(PlayerController controller, PlayerCombat combat) : base(controller) => _combat = combat;

        public override void Enter() => _combat.BeginSwing();

        public override void FixedTick()
        {
            bool finished = _combat.TickSwing(C.Dt);
            if (C.SwimMode) SwimState.Steer(C); // swinging in water: no gravity, swim steering continues
            else Steer();
            if (finished) AttackExit.Leave(C);
        }

        void Steer()
        {
            float target = C.MoveDirection * C.RunSpeed;
            C.SetVelocityX(HorizontalMotion.Step(C.Config, C.Velocity.x, target, C.Dt, C.SpeedMultiplier));
            C.ApplyGravity(C.Config.MaxFallSpeed);
        }

        public override void Exit() => _combat.EndSwing();
    }
}
