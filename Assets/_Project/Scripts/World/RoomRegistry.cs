using System.Collections.Generic;
using UnityEngine;

namespace AuraKnight.World
{
    /// <summary>
    /// All currently loaded rooms by id. Rooms register themselves, so exits can resolve targets
    /// across additively loaded region scenes without scene references.
    /// </summary>
    public static class RoomRegistry
    {
        static readonly Dictionary<string, Room> Rooms = new();

        public static IEnumerable<Room> All => Rooms.Values;

        public static void Register(Room room)
        {
            if (string.IsNullOrEmpty(room.RoomId))
            {
                Debug.LogError($"[RoomRegistry] Room '{room.name}' has no RoomId.", room);
                return;
            }
            if (Rooms.TryGetValue(room.RoomId, out var existing) && existing != room && existing != null)
                Debug.LogError($"[RoomRegistry] Duplicate room id '{room.RoomId}' ('{existing.name}' and '{room.name}').", room);
            Rooms[room.RoomId] = room;
        }

        public static void Unregister(Room room)
        {
            if (!string.IsNullOrEmpty(room.RoomId) && Rooms.TryGetValue(room.RoomId, out var r) && r == room)
                Rooms.Remove(room.RoomId);
        }

        public static bool TryGet(string roomId, out Room room)
        {
            room = null;
            return !string.IsNullOrEmpty(roomId) && Rooms.TryGetValue(roomId, out room) && room != null;
        }

        /// <summary>Finds the loaded room whose bounds contain the point (used after load/respawn).</summary>
        public static bool TryFindAt(Vector2 point, out Room room)
        {
            foreach (var r in Rooms.Values)
            {
                if (r != null && r.Contains(point)) { room = r; return true; }
            }
            room = null;
            return false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => Rooms.Clear();
    }
}
