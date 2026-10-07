using AuraKnight.Core;
using UnityEngine;

namespace AuraKnight.World
{
    /// <summary>
    /// Trigger that asks the RegionLoader to preload one more neighbouring region while Leo stands in it, for rooms with exits
    /// into several regions (the Hub's third room leads to the Cave, the City and the Castle but a room names only one default).
    /// Leaving the zone goes back to the room's own default preload.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class RegionPreloadZone : MonoBehaviour
    {
        [SerializeField] string regionId;

        static RegionLoader _loader;

        bool _restorePending;

        public string RegionId => regionId;

        void Reset() => GetComponent<Collider2D>().isTrigger = true;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!WorldTags.IsPlayer(other)) return;
            _restorePending = false;
            Request(regionId);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (WorldTags.IsPlayer(other)) _restorePending = true;
        }

        /// <summary>
        /// The restore waits until the end of the frame: stepping through an exit also ends the overlap in the same physics step, and by
        /// then RoomEntered has made the next room current, which owns the preload. Restoring at once would unload the region Leo is
        /// entering (and load it again), because the two trigger callbacks come in no fixed order.
        /// </summary>
        void LateUpdate()
        {
            if (!_restorePending) return;
            _restorePending = false;
            var room = GetComponentInParent<Room>();
            var rooms = RoomManager.Instance;
            if (room == null || (rooms != null && rooms.Current != room)) return;
            Request(room.PreloadRegionId);
        }

        void OnDisable() => _restorePending = false;

        static void Request(string region)
        {
            if (string.IsNullOrEmpty(region)) return;
            if (_loader == null) _loader = Object.FindAnyObjectByType<RegionLoader>();
            if (_loader != null) _loader.Preload(region);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() => _loader = null;
    }
}
