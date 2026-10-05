using UnityEngine;

namespace AuraKnight.World
{
    /// <summary>Shared tag/physics helpers for world triggers.</summary>
    public static class WorldTags
    {
        public const string Player = "Player";

        public static bool IsPlayer(Collider2D other) => other != null && other.CompareTag(Player);

        /// <summary>
        /// Moves a body. Room changes keep velocity so momentum survives; respawns pass keepVelocity false.
        /// </summary>
        public static void Teleport(Transform mover, Vector3 position, bool keepVelocity = true)
        {
            if (mover.TryGetComponent<ITeleportable>(out var teleportable))
            {
                teleportable.TeleportTo(position, keepVelocity);
                return;
            }
            mover.position = position;
            if (!mover.TryGetComponent<Rigidbody2D>(out var rb)) return;
            rb.position = position;
            if (!keepVelocity) rb.linearVelocity = Vector2.zero;
        }
    }
}
