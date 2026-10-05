using System.Collections.Generic;
using AuraKnight.Core;
using UnityEngine;

namespace AuraKnight.World
{
    /// <summary>
    /// Checkpoint + save point. Touching it (player tag) sets the checkpoint, which publishes
    /// CheckpointReached; GameManager autosaves and the combat phase restores hearts/energy from that event.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class SunAltar : MonoBehaviour
    {
        static readonly Dictionary<string, SunAltar> Altars = new();

        [SerializeField] string altarId;
        [SerializeField] Transform spawnPoint;

        public string AltarId => altarId;
        public Vector3 SpawnPosition => spawnPoint != null ? spawnPoint.position : transform.position;
        /// <summary>Room containing this altar, or null if it is not placed inside a Room prefab.</summary>
        public Room Room => GetComponentInParent<Room>();

        public static bool TryFind(string id, out SunAltar altar)
        {
            altar = null;
            return !string.IsNullOrEmpty(id) && Altars.TryGetValue(id, out altar) && altar != null;
        }

        void Reset() => GetComponent<Collider2D>().isTrigger = true;

        void OnEnable()
        {
            if (!string.IsNullOrEmpty(altarId)) Altars[altarId] = this;
        }

        void OnDisable()
        {
            if (!string.IsNullOrEmpty(altarId) && Altars.TryGetValue(altarId, out var a) && a == this)
                Altars.Remove(altarId);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!WorldTags.IsPlayer(other)) return;
            if (CheckpointService.Instance == null)
            {
                Debug.LogWarning("[SunAltar] No CheckpointService in the loaded scenes.", this);
                return;
            }
            CheckpointService.Instance.SetCheckpoint(this);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRegistry() => Altars.Clear();
    }
}
