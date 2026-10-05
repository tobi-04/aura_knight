using AuraKnight.Combat;
using UnityEngine;

namespace AuraKnight.Player.States
{
    /// <summary>
    /// Stagger after a hit: pushed 3 tiles away from the attacker over 0.25 s, no control. The 1.0 s i-frames
    /// (<see cref="PlayerController.ExternalInvulnerable"/>) outlast the stagger so Leo can act while still protected.
    /// </summary>
    public sealed class HurtState : PlayerStateBase
    {
        const float Epsilon = 1e-4f;
        readonly PlayerCombat _combat;
        float _elapsed;
        float _velocityX;

        public HurtState(PlayerController controller, PlayerCombat combat) : base(controller) => _combat = combat;

        public override void Enter()
        {
            var info = _combat.LastDamage;
            _elapsed = 0f;
            _velocityX = Knockback.Sign(info, -C.Facing) * Knockback.Speed(info.KnockbackTiles);
            C.SetVelocity(new Vector2(_velocityX, Mathf.Min(C.Velocity.y, 0f)));
        }

        public override void FixedTick()
        {
            _elapsed += C.Dt;
            if (_elapsed >= Knockback.Duration - Epsilon)
            {
                C.SetVelocityX(0f);
                AttackExit.Leave(C);
                return;
            }
            C.SetVelocityX(_velocityX);
            C.ApplyGravity(C.Config.MaxFallSpeed);
        }
    }
}
