using System;
using System.Collections;
using AuraKnight.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AuraKnight.World
{
    /// <summary>
    /// Core-scene entry into the world: new game or continue-from-save. Sets the GameManager state, loads the region of
    /// <c>lastAltarId</c> next to Core, spawns (or reuses) the player at that altar, enters its room and sets the mode to
    /// Playing. <see cref="FindAltar"/> is shared with <see cref="CheckpointService"/> for cross-region respawns.
    /// </summary>
    public sealed class WorldEntry : MonoBehaviour
    {
        public static WorldEntry Instance { get; private set; }

        [SerializeField] RegionGraph graph;
        [SerializeField] RegionLoader loader;
        [Tooltip("Instantiated into the Core scene when no object tagged Player exists yet.")]
        [SerializeField] GameObject playerPrefab;
        [SerializeField, Min(1f)] float loadTimeoutSeconds = 15f;

        Coroutine routine;

        public bool IsBusy => routine != null;

        /// <summary>Raised when an entry attempt finishes; true when the player is placed and the mode is Playing.</summary>
        public event Action<bool> Entered;

        void Awake()
        {
            if (Singleton.IsDuplicate(Instance, this)) return;
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Fresh save at the Hub start altar. False when busy or there is no GameManager.</summary>
        public bool StartNewGame() => Begin(newGame: true);

        /// <summary>Resumes from the save slot. False when busy, no GameManager, or no loadable save.</summary>
        public bool Continue() => Begin(newGame: false);

        bool Begin(bool newGame)
        {
            var gm = GameManager.Instance;
            if (routine != null || gm == null) return false;
            if (newGame) gm.StartNewGame();
            else if (!gm.Continue()) return false;
            gm.SetMode(GameMode.Loading); // no control, no play-time, until the player is placed
            routine = StartCoroutine(Run(gm));
            return true;
        }

        IEnumerator Run(GameManager gm)
        {
            bool ok = false;
            try
            {
                var search = new AltarSearch();
                yield return FindAltar(gm.State.lastAltarId, search);
                if (search.Altar == null && gm.State.lastAltarId != GameState.StartAltarId)
                {
                    Debug.LogWarning($"[WorldEntry] Altar '{gm.State.lastAltarId}' not found; starting at the Hub.", this);
                    gm.State.lastAltarId = GameState.StartAltarId;
                    yield return FindAltar(GameState.StartAltarId, search);
                    if (search.Altar != null) gm.Save(); // never leave an unresolvable altar id on disk
                }
                if (search.Altar == null) Debug.LogError("[WorldEntry] No start altar could be loaded.", this);
                else ok = Enter(gm, search.Altar);
            }
            finally
            {
                routine = null;
                if (!ok) gm.SetMode(GameMode.Menu);
                Entered?.Invoke(ok);
            }
        }

        bool Enter(GameManager gm, SunAltar altar)
        {
            var player = EnsurePlayer(altar);
            if (player == null) return false;
            CheckpointService.Instance?.RegisterPlayer(player);
            CheckpointService.Instance?.SetCurrentQuietly(altar);
            RoomManager.Instance?.SetFollowTarget(player);
            AltarArrival.Place(player, altar);
            // A player that survived from an earlier session (dead, hurt, mid-air) starts clean.
            var combat = player.GetComponent<AuraKnight.Player.PlayerCombat>();
            if (combat != null) combat.ResetToFreshStart();
            gm.SetMode(GameMode.Playing);
            return true;
        }

        Transform EnsurePlayer(SunAltar altar)
        {
            var existing = GameObject.FindGameObjectWithTag(WorldTags.Player);
            if (existing != null) return existing.transform;
            if (playerPrefab == null)
            {
                Debug.LogError("[WorldEntry] No player in the scene and no player prefab assigned.", this);
                return null;
            }
            var player = Instantiate(playerPrefab, altar.SpawnPosition, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(player, gameObject.scene);
            return player.transform;
        }

        /// <summary>
        /// Makes the altar's region current, waits until its scene is loaded and the altar is registered, and returns it
        /// through <paramref name="result"/> (null on unknown altar or timeout).
        /// </summary>
        public IEnumerator FindAltar(string altarId, AltarSearch result)
        {
            result.Altar = null;
            if (graph == null || loader == null || !graph.TryGetRegionOfAltar(altarId, out var region))
            {
                Debug.LogWarning($"[WorldEntry] Altar '{altarId}' is not listed in any region.", this);
                yield break;
            }
            loader.SetCurrentRegion(region);
            float deadline = Time.realtimeSinceStartup + loadTimeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (loader.IsRegionLoaded(region) && SunAltar.TryFind(altarId, out var altar))
                {
                    result.Altar = altar;
                    yield break;
                }
                yield return null;
            }
            Debug.LogWarning($"[WorldEntry] Timed out loading region '{region}' for altar '{altarId}'.", this);
        }
    }
}
