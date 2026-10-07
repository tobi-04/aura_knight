using AuraKnight.Core;
using AuraKnight.Enemies;
using AuraKnight.Progression;

namespace AuraKnight.World.Pickups
{
    /// <summary>
    /// The single place coins are picked up: credits the live <see cref="Wallet"/> (which announces CoinsChanged) and publishes
    /// <see cref="CoinsCollected"/>. Callers only use <see cref="Collect"/>.
    /// </summary>
    public static class CoinCollector
    {
        /// <summary>Adds without overflowing int; a negative result is never produced.</summary>
        public static int AddSaturating(int current, int amount) => Wallet.AddSaturating(current, amount);

        /// <summary>
        /// Credits <paramref name="amount"/> coins. Without a GameManager (test scenes) only <see cref="CoinsCollected"/> is
        /// published. Returns the new total, or -1 when no GameState was updated or the amount was not positive.
        /// </summary>
        public static int Collect(int amount)
        {
            if (amount <= 0) return -1;
            int total = Wallet.Live?.Add(amount) ?? -1;
            EventBus.Publish(new CoinsCollected(amount));
            return total;
        }
    }
}
