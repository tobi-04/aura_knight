using UnityEngine;

namespace AuraKnight.Player.States
{
    /// <summary>
    /// Leo is down: no control, gravity only. After <see cref="PlayerCombat.RespawnDelay"/> the checkpoint respawn is
    /// requested (again after each delay until a destination exists; the UI fades on PlayerDied / PlayerRespawned).
    /// Never revives in place. Coins and progress are untouched.
    /// </summary>
    public sealed class DeadState : PlayerStateBase
    {
        const float Epsilon = 1e-4f;
        readonly PlayerCombat _combat;
        float _elapsed;
        bool _requested;

        public DeadState(PlayerController controller, PlayerCombat combat) : base(controller) => _combat = combat;

        public override void Enter()
        {
            _elapsed = 0f;
            _requested = false;
            C.SetVelocityX(0f);
        }

        public override void FixedTick()
        {
            _elapsed += C.Dt;
            if (_requested)
            {
                C.SetVelocity(Vector2.zero); // respawn under way (maybe waiting on a region load): hold still, no falling into the void
                return;
            }
            C.SetVelocityX(0f);
            C.ApplyGravity(C.Config.MaxFallSpeed);
            if (_requested || _elapsed < PlayerCombat.RespawnDelay - Epsilon) return;
            if (_combat.RequestRespawn()) _requested = true;
            else _elapsed = 0f; // no destination yet: stay down and ask again after another delay
        }
    }
}
