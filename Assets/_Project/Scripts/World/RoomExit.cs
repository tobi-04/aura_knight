using UnityEngine;

namespace AuraKnight.World
{
    /// <summary>
    /// Trigger on a room edge. When the player touches it, RoomManager enters the target room.
    /// With no target spawn the hand-off is seamless (rooms overlap); with one, the player is placed there.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class RoomExit : MonoBehaviour
    {
        [SerializeField] string targetRoomId;
        [Tooltip("Optional: name of a spawn point inside the target room.")]
        [SerializeField] string targetSpawnName;

        public string TargetRoomId => targetRoomId;
        public string TargetSpawnName => targetSpawnName;

        void Reset() => GetComponent<Collider2D>().isTrigger = true;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!WorldTags.IsPlayer(other)) return;
            if (RoomManager.Instance == null)
            {
                Debug.LogWarning("[RoomExit] No RoomManager in the loaded scenes.", this);
                return;
            }
            RoomManager.Instance.EnterRoom(targetRoomId, targetSpawnName, other.attachedRigidbody != null
                ? other.attachedRigidbody.transform : other.transform);
        }
    }
}
