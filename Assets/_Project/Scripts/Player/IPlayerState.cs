namespace AuraKnight.Player
{
    /// <summary>One behaviour of the player state machine.</summary>
    public interface IPlayerState
    {
        void Enter();
        /// <summary>Per rendered frame (Update).</summary>
        void Tick();
        /// <summary>Per physics step; velocity decisions go here.</summary>
        void FixedTick();
        void Exit();
    }
}
