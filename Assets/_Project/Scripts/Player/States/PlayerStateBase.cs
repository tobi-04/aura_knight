namespace AuraKnight.Player.States
{
    /// <summary>Shared plumbing: states hold the controller and override only what they need.</summary>
    public abstract class PlayerStateBase : IPlayerState
    {
        protected readonly PlayerController C;

        protected PlayerStateBase(PlayerController controller) => C = controller;

        public virtual void Enter() { }
        public virtual void Tick() { }
        public virtual void FixedTick() { }
        public virtual void Exit() { }
    }
}
