using UnityEngine;

namespace AuraKnight.Player
{
    /// <summary>Vertical motion math shared by every airborne state.</summary>
    public static class AirPhysics
    {
        /// <summary>Applies one step of gravity (heavier when falling) and clamps to the fall limit.</summary>
        public static float Step(PlayerMovementConfig config, float velocityY, float deltaTime, float maxFallSpeed)
        {
            float gravity = velocityY > 0f ? config.JumpGravity : config.FallGravity;
            return Mathf.Max(velocityY - gravity * deltaTime, -maxFallSpeed);
        }

        /// <summary>Trapezoidal displacement: exact for constant acceleration, so jump height matches h = v0^2 / 2g.</summary>
        public static float Displacement(float previousVelocityY, float velocityY, float deltaTime) =>
            0.5f * (previousVelocityY + velocityY) * deltaTime;
    }
}
