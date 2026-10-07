using UnityEngine;

namespace AuraKnight.Player
{
    /// <summary>Movement API used by states and other phases (velocity, facing, jumps).</summary>
    public sealed partial class PlayerController
    {
        // ---- API for states and other phases ----

        public void Teleport(Vector2 position) => TeleportTo(position, keepVelocity: false);

        /// <summary>Warps motor + transform together; room transitions keep velocity, respawns zero it.</summary>
        public void TeleportTo(Vector2 position, bool keepVelocity)
        {
            Motor.Teleport(position);
            if (!keepVelocity) SetVelocity(Vector2.zero);
        }

        /// <summary>Instant velocity change; the next integration uses it without averaging with the old value.</summary>
        public void SetVelocity(Vector2 velocity)
        {
            Velocity = velocity;
            _previousVelocityY = velocity.y;
        }

        public void SetVelocityX(float x) => Velocity = new Vector2(x, Velocity.y);

        public void SetVelocityY(float y) => SetVelocity(new Vector2(Velocity.x, y));

        /// <summary>Gravity-driven vertical change (not an impulse).</summary>
        public void ApplyGravity(float maxFallSpeed) =>
            Velocity = new Vector2(Velocity.x, AirPhysics.Step(config, Velocity.y, Dt, maxFallSpeed));

        /// <summary>Overrides vertical speed without breaking displacement averaging (used by jump cut).</summary>
        public void SetVerticalSpeed(float y) => Velocity = new Vector2(Velocity.x, y);

        public void Face(int direction)
        {
            if (direction != 0) Facing = direction;
        }

        /// <summary>Accelerates toward the input direction at run speed and updates facing.</summary>
        public void RunHorizontal()
        {
            int dir = MoveDirection;
            Face(dir);
            SetVelocityX(HorizontalMotion.Step(config, Velocity.x, dir * RunSpeed, Dt, SpeedMultiplier));
        }

        /// <summary>The wall side (-1/+1) the player grips (touches AND pushes toward; smooth walls cannot be gripped), else 0.</summary>
        public int WallContactToward()
        {
            int dir = MoveDirection;
            return dir != 0 && Motor.GripsWall(dir) ? dir : 0;
        }

        /// <summary>Starts a jump with the given launch speed from any state (ground, coyote, double jump).</summary>
        public void StartJump(float velocity)
        {
            JumpBuffer.Cancel();
            Coyote.Cancel();
            SetVelocityY(velocity * JumpMultiplier);
            ApplyGravity(config.MaxFallSpeed); // first step already feels gravity so apex height is exact
            JumpCut.Begin();
            Grounded = false;
            StateMachine.TryChange(PlayerStateId.Jump);
        }

        /// <summary>Back to a grounded locomotion state.</summary>
        public void Land() =>
            StateMachine.TryChange(MoveDirection != 0 ? PlayerStateId.Run : PlayerStateId.Idle);
    }
}
