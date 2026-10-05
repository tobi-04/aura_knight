using UnityEngine;

namespace AuraKnight.World
{
    /// <summary>
    /// Implemented by movers that keep their own position state (e.g. the kinematic player motor),
    /// so world code can warp them without the next physics step snapping them back.
    /// </summary>
    public interface ITeleportable
    {
        void TeleportTo(Vector2 position, bool keepVelocity);
    }
}
