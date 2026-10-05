using System.Collections;
using AuraKnight.Core;
using Unity.Cinemachine;
using UnityEngine;

namespace AuraKnight.World
{
    /// <summary>
    /// Core-scene singleton that switches the active room: swaps the camera confiner shape with a short
    /// damped blend, toggles room enemies, records the visit and publishes RoomEntered.
    /// All repositioning (room spawns, respawn, continue) goes through <see cref="Warp"/> so the camera is told.
    /// </summary>
    public sealed class RoomManager : MonoBehaviour
    {
        public static RoomManager Instance { get; private set; }

        [SerializeField] CinemachineCamera virtualCamera;
        [SerializeField] CinemachineConfiner2D confiner;
        [Tooltip("Minimum seconds the confiner eases to the new room bounds (held at least as long as the damping time).")]
        [SerializeField] float blendSeconds = 0.3f;
        [Tooltip("Confiner damping applied during the blend (0 = snap).")]
        [SerializeField] float blendDamping = 1f;

        [Tooltip("On Start, enter the room containing the object tagged Player (test scenes; Core flow enters rooms explicitly).")]
        [SerializeField] bool enterPlayerRoomOnStart;

        Coroutine blendRoutine;
        float restDamping;

        public Room Current { get; private set; }

        void Awake()
        {
            if (Singleton.IsDuplicate(Instance, this)) return;
            Instance = this;
            if (confiner != null) restDamping = confiner.Damping;
        }

        void Start()
        {
            if (!enterPlayerRoomOnStart) return;
            var player = GameObject.FindGameObjectWithTag(WorldTags.Player);
            if (player != null) EnterRoomAt(player.transform.position, player.transform);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Points the virtual camera at a (newly spawned) target.</summary>
        public void SetFollowTarget(Transform target)
        {
            if (virtualCamera != null) virtualCamera.Follow = target;
        }

        /// <summary>
        /// Enters a room by id. The mover keeps its velocity; it is only repositioned if a spawn is named.
        /// With <paramref name="restartIfCurrent"/> (respawn) entering the room Leo is already in restarts it and publishes RoomEntered again.
        /// </summary>
        public bool EnterRoom(string roomId, string spawnName = null, Transform mover = null, bool restartIfCurrent = false)
        {
            if (!RoomRegistry.TryGet(roomId, out var room))
            {
                Debug.LogWarning($"[RoomManager] Room '{roomId}' is not loaded.", this);
                return false;
            }
            if (mover != null && room.TryGetSpawn(spawnName, out var spawn)) Warp(mover, spawn.position);
            if (room == Current)
            {
                if (!restartIfCurrent) return true;
                room.Restart();
                EventBus.Publish(new RoomEntered(room.RoomId, room.RegionId));
                return true;
            }

            var previous = Current;
            Current = room;
            float hold = RoomBlendTiming.HoldSeconds(blendSeconds, blendDamping);
            if (previous != null) StartCoroutine(DeactivateAfter(previous, hold));
            room.Activate();
            SwapConfiner(room, hold);
            RecordVisit(room);
            EventBus.Publish(new RoomEntered(room.RoomId, room.RegionId));
            return true;
        }

        /// <summary>Enters whichever loaded room contains the point (after load or respawn).</summary>
        public bool EnterRoomAt(Vector2 point, Transform mover = null)
        {
            return RoomRegistry.TryFindAt(point, out var room) && EnterRoom(room.RoomId, null, mover);
        }

        /// <summary>Moves a body and tells the camera it was warped (no lerp across the map). Respawns pass keepVelocity false.</summary>
        public void Warp(Transform mover, Vector3 target, bool keepVelocity = true)
        {
            var delta = target - mover.position;
            WorldTags.Teleport(mover, target, keepVelocity);
            if (virtualCamera != null) virtualCamera.OnTargetObjectWarped(mover, delta);
        }

        void SwapConfiner(Room room, float hold)
        {
            if (confiner == null) return;
            if (blendRoutine != null) StopCoroutine(blendRoutine);
            confiner.BoundingShape2D = room.Bounds;
            confiner.Damping = Mathf.Max(restDamping, blendDamping);
            blendRoutine = StartCoroutine(RestoreDampingAfter(hold));
        }

        IEnumerator RestoreDampingAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (confiner != null) confiner.Damping = restDamping;
            blendRoutine = null;
        }

        IEnumerator DeactivateAfter(Room room, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (room != null && room != Current) room.Deactivate();
        }

        static void RecordVisit(Room room)
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.State.MarkRoomVisited(room.RoomId);
        }
    }
}
