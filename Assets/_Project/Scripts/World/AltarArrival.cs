using UnityEngine;

namespace AuraKnight.World
{
    /// <summary>Out-parameter for the altar lookup coroutines (a coroutine cannot return a value).</summary>
    public sealed class AltarSearch
    {
        public SunAltar Altar;
    }

    /// <summary>Shared "put the player on an altar" step for respawn and continue-from-save.</summary>
    public static class AltarArrival
    {
        /// <summary>
        /// Warps the player to the altar through <see cref="RoomManager.Warp"/> (so the camera gets OnTargetObjectWarped,
        /// velocity zeroed) and enters the altar's room, which activates its enemies and publishes RoomEntered.
        /// </summary>
        public static void Place(Transform player, SunAltar altar)
        {
            var rooms = RoomManager.Instance;
            if (rooms == null)
            {
                WorldTags.Teleport(player, altar.SpawnPosition, keepVelocity: false);
                return;
            }
            rooms.Warp(player, altar.SpawnPosition, keepVelocity: false);
            var room = altar.Room;
            if (room != null) rooms.EnterRoom(room.RoomId, restartIfCurrent: true);
            else rooms.EnterRoomAt(altar.SpawnPosition);
        }
    }
}
