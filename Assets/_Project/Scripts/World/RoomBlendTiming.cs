using UnityEngine;

namespace AuraKnight.World
{
    /// <summary>Pure timing of the room-to-room camera blend (confiner damping).</summary>
    public static class RoomBlendTiming
    {
        /// <summary>
        /// How long the temporary confiner damping stays applied. Confiner damping is a time constant in seconds, so it is
        /// held for at least that long: dropping back to the rest value earlier would make the camera snap visibly.
        /// The old room's enemies are switched off after the same time, so they never vanish while still on screen.
        /// </summary>
        public static float HoldSeconds(float blendSeconds, float blendDamping) =>
            Mathf.Max(0f, Mathf.Max(blendSeconds, blendDamping));
    }
}
