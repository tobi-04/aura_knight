namespace AuraKnight.Enemies
{
    /// <summary>
    /// Published on the EventBus whenever coins are picked up (Amount > 0), before any wallet exists. The UI and audio
    /// can listen to it for pickup feedback; the running total is announced separately through Core.CoinsChanged.
    /// </summary>
    public readonly struct CoinsCollected
    {
        public readonly int Amount;
        public CoinsCollected(int amount) { Amount = amount; }
    }
}
