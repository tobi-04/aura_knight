using UnityEngine;

namespace AuraKnight.UI
{
    /// <summary>Pure joystick maths, kept apart from the pointer plumbing so it can be unit-tested.</summary>
    public static class JoystickMath
    {
        public static Vector2 ClampToRadius(Vector2 offset, float radius) =>
            radius <= 0f ? Vector2.zero : Vector2.ClampMagnitude(offset, radius);

        /// <summary>Offset from the touch origin to a stick value: length 0..1, zero inside the deadzone.</summary>
        public static Vector2 Normalize(Vector2 offset, float radius, float deadzone)
        {
            if (radius <= 0f) return Vector2.zero;
            var v = Vector2.ClampMagnitude(offset / radius, 1f);
            return v.magnitude < deadzone ? Vector2.zero : v;
        }
    }
}
