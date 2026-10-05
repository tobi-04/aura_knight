using UnityEngine;

namespace AuraKnight.Core
{
    /// <summary>
    /// Core-scene singleton: owns the live GameState, the high-level mode, play time and autosave
    /// (altar checkpoint, boss defeat, app backgrounded - Android may kill the process after that).
    /// Runs before everything else so other components can read <see cref="State"/> in Awake/Start.
    /// StartNewGame/Continue replace the state and publish <see cref="GameStateLoaded"/>; the world flow
    /// (<c>WorldEntry</c>) then places the player and sets the mode to Playing.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        /// <summary>True when there is no GameManager (test scenes) or the game is in the Playing mode (not paused, in a menu or a cutscene).</summary>
        public static bool ModeAllowsControl => Instance == null || Instance.Mode == GameMode.Playing;

        SaveSystem saveSystem;
        // True when the live state is known to be on disk (loaded from it or written to it). A new game starts false,
        // so its first altar overwrites an old save instead of being skipped.
        bool stateSaved;

        public GameState State { get; private set; } = GameState.NewGame();
        public GameMode Mode { get; private set; } = GameMode.Menu;

        void Awake()
        {
            if (Singleton.IsDuplicate(Instance, this)) return;
            Instance = this;
            saveSystem ??= new SaveSystem();
        }

        void OnEnable()
        {
            EventBus.Subscribe<CheckpointReached>(OnCheckpointReached);
            EventBus.Subscribe<BossDefeated>(OnBossDefeated);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<CheckpointReached>(OnCheckpointReached);
            EventBus.Unsubscribe<BossDefeated>(OnBossDefeated);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (Mode == GameMode.Playing) State.playTimeSeconds += Time.unscaledDeltaTime;
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && Mode != GameMode.Menu) Save();
        }

        public void SetMode(GameMode mode) => Mode = mode;

        /// <summary>Replaces the save backend (tests, alternative slots). Must be called before the first save/load.</summary>
        public void UseSaveSystem(SaveSystem system) => saveSystem = system ?? new SaveSystem();

        public bool HasSave => Backend.HasSave;

        SaveSystem Backend => saveSystem ??= new SaveSystem();

        public void StartNewGame()
        {
            State = GameState.NewGame();
            stateSaved = false;
            EventBus.Publish(new GameStateLoaded(true));
        }

        /// <summary>Loads the save slot; returns false (state untouched) if absent, corrupt or wrong version.</summary>
        public bool Continue()
        {
            if (!Backend.TryLoad(out var loaded)) return false;
            State = loaded;
            stateSaved = true;
            EventBus.Publish(new GameStateLoaded(false));
            return true;
        }

        public bool Save()
        {
            if (!Backend.Save(State)) return false;
            stateSaved = true;
            EventBus.Publish(new GameSaved());
            return true;
        }

        void OnCheckpointReached(CheckpointReached evt)
        {
            // Touching the same altar again only heals (PlayerStats listens too); no disk write unless this state was never saved.
            if (State.lastAltarId == evt.AltarId && stateSaved) return;
            State.lastAltarId = evt.AltarId;
            Save();
        }

        void OnBossDefeated(BossDefeated evt)
        {
            State.MarkBossDefeated(evt.BossId);
            Save();
        }
    }
}
