using UnityEngine;

namespace AuraKnight.Player.States
{
    /// <summary>Pure swim helpers: 8-way stick quantising.</summary>
    public static class SwimMath
    {
        public const float Deadzone = 0.3f;

        /// <summary>Snaps a stick vector to one of the 8 compass directions (unit length), or zero inside the deadzone.</summary>
        public static Vector2 Quantize8(Vector2 stick)
        {
            if (stick.sqrMagnitude < Deadzone * Deadzone) return Vector2.zero;
            float angle = Mathf.Round(Mathf.Atan2(stick.y, stick.x) * Mathf.Rad2Deg / 45f) * 45f * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
    }

    /// <summary>
    /// Free 8-way swimming with the Water aura (normal speed, no gravity, no oxygen limit). Entered and left by
    /// <see cref="PlayerController.SwimMode"/>; swimming out through the surface gives a small hop onto the bank.
    /// </summary>
    public sealed class SwimState : PlayerStateBase
    {
        public const float Acceleration = 80f;
        public const float SurfaceHopSpeed = 12f;
        const float HopStickThreshold = 0.3f;

        public SwimState(PlayerController controller) : base(controller) { }

        public override void FixedTick() => Steer(C);

        /// <summary>One step of free swimming; also used by the air swing so attacking in water keeps swim physics.</summary>
        public static void Steer(PlayerController c)
        {
            var direction = c.ControlsEnabled ? SwimMath.Quantize8(c.Input.Move) : Vector2.zero;
            if (direction.x != 0f) c.Face(direction.x > 0f ? 1 : -1);
            var target = direction * (c.Config.RunSpeed * c.SpeedMultiplier);
            c.SetVelocity(Vector2.MoveTowards(c.Velocity, target, Acceleration * c.Dt));
        }

        public override void Exit()
        {
            if (!C.SwimMode && C.Input.Move.y > HopStickThreshold)
                C.SetVelocityY(Mathf.Max(C.Velocity.y, SurfaceHopSpeed));
        }
    }
}
