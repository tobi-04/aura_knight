namespace AuraKnight.Core
{
    // Event payloads for EventBus. Keep them small, immutable-by-convention structs.

    public readonly struct RoomEntered
    {
        public readonly string RoomId;
        public readonly string RegionId;
        public RoomEntered(string roomId, string regionId) { RoomId = roomId; RegionId = regionId; }
    }

    public readonly struct PlayerDamaged
    {
        public readonly int Amount;
        public readonly int CurrentHearts;
        public PlayerDamaged(int amount, int currentHearts) { Amount = amount; CurrentHearts = currentHearts; }
    }

    public readonly struct PlayerDied { }

    public readonly struct PlayerRespawned { }

    public readonly struct HeartsChanged
    {
        public readonly int Current;
        public readonly int Max;
        public HeartsChanged(int current, int max) { Current = current; Max = max; }
    }

    public readonly struct EnergyChanged
    {
        public readonly float Current;
        public readonly float Max;
        public EnergyChanged(float current, float max) { Current = current; Max = max; }
    }

    public readonly struct CoinsChanged
    {
        public readonly int Coins;
        public CoinsChanged(int coins) { Coins = coins; }
    }

    public readonly struct AuraChanged
    {
        public readonly string AuraId;
        public AuraChanged(string auraId) { AuraId = auraId; }
    }

    public readonly struct AuraUnlocked
    {
        public readonly string AuraId;
        public AuraUnlocked(string auraId) { AuraId = auraId; }
    }

    public readonly struct BossDefeated
    {
        public readonly string BossId;
        public BossDefeated(string bossId) { BossId = bossId; }
    }

    public readonly struct CheckpointReached
    {
        public readonly string AltarId;
        public CheckpointReached(string altarId) { AltarId = altarId; }
    }

    public readonly struct GameSaved { }

    /// <summary>
    /// The live GameState was replaced (new game or continue). Components that cached values from it at
    /// startup (hearts, energy, Auras, opened gates) re-read from GameManager.State when they get this.
    /// </summary>
    public readonly struct GameStateLoaded
    {
        public readonly bool IsNewGame;
        public GameStateLoaded(bool isNewGame) { IsNewGame = isNewGame; }
    }
}
