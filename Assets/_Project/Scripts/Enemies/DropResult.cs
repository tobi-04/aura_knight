namespace AuraKnight.Enemies
{
    /// <summary>What one enemy death yields: a number of coins and whether a Light Drop (heart) falls too.</summary>
    public readonly struct DropResult
    {
        public readonly int Coins;
        public readonly bool LightDrop;

        public DropResult(int coins, bool lightDrop)
        {
            Coins = coins;
            LightDrop = lightDrop;
        }
    }
}
