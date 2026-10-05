using System.Collections;
using AuraKnight.Core;
using UnityEngine;

namespace AuraKnight.World
{
    /// <summary>
    /// Tracks the active altar and puts the player back on it after death. The destination is the saved
    /// <c>lastAltarId</c>, which may live in another region: the region is loaded first, then the player is warped
    /// (camera notified) and the room entered. Without a resolvable destination it falls back to the Hub start altar
    /// and keeps retrying; Leo is never revived in place.
    /// </summary>
    public sealed class CheckpointService : MonoBehaviour
    {
        public const float RetrySeconds = 1f;

        public static CheckpointService Instance { get; private set; }

        Transform player;
        bool respawning;

        public SunAltar CurrentAltar { get; private set; }
        public bool IsRespawning => respawning;

        void Awake()
        {
            if (Singleton.IsDuplicate(Instance, this)) return;
            Instance = this;
        }

        // Unity stops this component's coroutines when it is disabled; the flag must not outlive them.
        void OnDisable() => respawning = false;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void RegisterPlayer(Transform playerTransform) => player = playerTransform;

        public void SetCheckpoint(SunAltar altar)
        {
            if (altar == null) return;
            CurrentAltar = altar;
            EventBus.Publish(new CheckpointReached(altar.AltarId));
        }

        /// <summary>Remembers the altar the world was entered at (no heal, no save).</summary>
        public void SetCurrentQuietly(SunAltar altar) => CurrentAltar = altar;

        /// <summary>
        /// Starts moving the player to the checkpoint. PlayerRespawned is published on arrival. Returns false only when
        /// there is no player to move; an unresolved destination is retried until it exists.
        /// </summary>
        public bool Respawn()
        {
            if (player == null) player = FindPlayer();
            if (player == null)
            {
                Debug.LogWarning("[CheckpointService] Respawn without a player.", this);
                return false;
            }
            if (respawning) return true;
            respawning = true; // set before StartCoroutine: a routine that finishes synchronously clears it in its finally
            StartCoroutine(RespawnRoutine());
            return true;
        }

        IEnumerator RespawnRoutine()
        {
            bool arrived = false;
            try
            {
                var search = new AltarSearch();
                for (int failures = 0; player != null; failures++)
                {
                    yield return FindDestination(search);
                    if (search.Altar != null)
                    {
                        CurrentAltar = search.Altar;
                        AltarArrival.Place(player, search.Altar);
                        arrived = true;
                        break;
                    }
                    if (failures % 5 == 0) Debug.LogWarning("[CheckpointService] No altar available yet; Leo stays down and the respawn is retried.", this);
                    yield return new WaitForSecondsRealtime(RetrySeconds);
                }
            }
            finally { respawning = false; }
            if (arrived) EventBus.Publish(new PlayerRespawned());
        }

        IEnumerator FindDestination(AltarSearch search)
        {
            string target = TargetAltarId();
            yield return Locate(target, search);
            if (search.Altar != null || target == GameState.StartAltarId) yield break;

            Debug.LogWarning($"[CheckpointService] Altar '{target}' cannot be resolved; falling back to '{GameState.StartAltarId}'.", this);
            yield return Locate(GameState.StartAltarId, search);
            if (search.Altar == null || GameManager.Instance == null) yield break;
            // Persist the fallback so the saved altar id is never left pointing at something unresolvable.
            GameManager.Instance.State.lastAltarId = GameState.StartAltarId;
            GameManager.Instance.Save();
        }

        IEnumerator Locate(string altarId, AltarSearch search)
        {
            search.Altar = null;
            if (WorldEntry.Instance != null) return WorldEntry.Instance.FindAltar(altarId, search);
            if (SunAltar.TryFind(altarId, out var local)) search.Altar = local; // test scenes: altar already in the loaded scene
            return Empty();
        }

        static IEnumerator Empty() { yield break; }

        string TargetAltarId()
        {
            var gm = GameManager.Instance;
            if (gm != null && !string.IsNullOrEmpty(gm.State.lastAltarId)) return gm.State.lastAltarId;
            return CurrentAltar != null ? CurrentAltar.AltarId : GameState.StartAltarId;
        }

        static Transform FindPlayer()
        {
            var go = GameObject.FindGameObjectWithTag(WorldTags.Player);
            return go != null ? go.transform : null;
        }
    }
}
