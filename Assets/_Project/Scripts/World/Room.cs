using System.Collections.Generic;
using UnityEngine;

namespace AuraKnight.World
{
    /// <summary>
    /// One room prefab: stable id, camera bounds, exits and an enemies container that is only live
    /// while the player is inside. Rooms start with enemies disabled until RoomManager enters them.
    /// </summary>
    public sealed class Room : MonoBehaviour
    {
        [SerializeField] string roomId;
        [SerializeField] string regionId;
        [Tooltip("PolygonCollider2D used as the Cinemachine Confiner2D bounding shape.")]
        [SerializeField] PolygonCollider2D bounds;
        [SerializeField] List<RoomExit> exits = new();
        [SerializeField] GameObject enemiesContainer;
        [Tooltip("Gateway rooms: region scene to preload while the player is here (must neighbor this region).")]
        [SerializeField] string preloadRegionId;
        [SerializeField] List<Transform> spawnPoints = new();

        public string RoomId => roomId;
        public string RegionId => regionId;
        public PolygonCollider2D Bounds => bounds;
        public IReadOnlyList<RoomExit> Exits => exits;
        public string PreloadRegionId => preloadRegionId;

        void OnEnable()
        {
            RoomRegistry.Register(this);
            SetEnemiesActive(false);
        }

        void OnDisable() => RoomRegistry.Unregister(this);

        public bool Contains(Vector2 point) => bounds != null && bounds.OverlapPoint(point);

        public bool TryGetSpawn(string spawnName, out Transform spawn)
        {
            spawn = null;
            if (string.IsNullOrEmpty(spawnName)) return false;
            foreach (var s in spawnPoints)
            {
                if (s != null && s.name == spawnName) { spawn = s; return true; }
            }
            return false;
        }

        public void Activate() => SetEnemiesActive(true);

        /// <summary>Switches the room's enemies off and on again so components that reset in OnEnable start fresh (respawn into the current room).</summary>
        public void Restart()
        {
            SetEnemiesActive(false);
            SetEnemiesActive(true);
        }
        public void Deactivate() => SetEnemiesActive(false);

        void SetEnemiesActive(bool active)
        {
            if (enemiesContainer != null) enemiesContainer.SetActive(active);
        }
    }
}
