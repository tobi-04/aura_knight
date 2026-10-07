namespace AuraKnight.UI
{
    // EventBus payloads owned by the UI layer. Gameplay systems (Boss, Progression, Audio) publish or consume these;
    // the UI never references them directly.

    /// <summary>Boss health changed. Published by the Boss system on every hit (and once when the fight starts).</summary>
    public readonly struct BossHealthChanged
    {
        public readonly string BossId;
        public readonly int Current;
        public readonly int Max;
        public BossHealthChanged(string bossId, int current, int max) { BossId = bossId; Current = current; Max = max; }
    }

    /// <summary>A boss fight begins: shows the intro banner (BOSS / DisplayName) and the boss HP bar.</summary>
    public readonly struct BossEncounterStarted
    {
        public readonly string BossId;
        public readonly string DisplayName;
        public BossEncounterStarted(string bossId, string displayName) { BossId = bossId; DisplayName = displayName; }
    }

    /// <summary>The fight is over (boss defeated or the player died): hides the boss HP bar.</summary>
    public readonly struct BossEncounterEnded
    {
        public readonly string BossId;
        public readonly string DisplayName;
        public BossEncounterEnded(string bossId, string displayName) { BossId = bossId; DisplayName = displayName; }
    }

    /// <summary>The final boss is down: the UI shows the ending and credits. Published by the progression system.</summary>
    public readonly struct GameCompleted { }

    /// <summary>The MAP button (or gamepad Select) was pressed. Phase 12's map screen listens for it.</summary>
    public readonly struct MapRequested { }

    /// <summary>A personal setting changed; <see cref="Key"/> is one of <see cref="SettingsKeys"/>.</summary>
    public readonly struct SettingsChanged
    {
        public readonly string Key;
        public SettingsChanged(string key) { Key = key; }
    }

    /// <summary>The game was paused or resumed through <see cref="PauseController"/>.</summary>
    public readonly struct PauseStateChanged
    {
        public readonly bool Paused;
        public PauseStateChanged(bool paused) { Paused = paused; }
    }
}
