using AuraKnight.Core;
using AuraKnight.Enemies;

namespace AuraKnight.World.Pickups
{
    /// <summary>
    /// The single place coins are credited: adds to the live GameState and announces it. Phase 12 builds the Wallet on top
    /// of this; callers only use <see cref="Collect"/> so the swap touches this class alone.
    /// </summary>
    public static class CoinCollector
    {
        /// <summary>Adds without overflowing int; a negative result is never produced.</summary>
        public static int AddSaturating(int current, int amount)
        {
            if (amount <= 0) return current < 0 ? 0 : current;
            long total = (long)(current < 0 ? 0 : current) + amount;
            return total > int.MaxValue ? int.MaxValue : (int)total;
        }

        /// <summary>
        /// Credits <paramref name="amount"/> coins. Without a GameManager (test scenes) only <see cref="CoinsCollected"/> is
        /// published. Returns the new total, or -1 when no GameState was updated or the amount was not positive.
        /// </summary>
        public static int Collect(int amount)
        {
            if (amount <= 0) return -1;
            int total = -1;
            var manager = GameManager.Instance;
            if (manager != null)
            {
                manager.State.coins = AddSaturating(manager.State.coins, amount);
                total = manager.State.coins;
                EventBus.Publish(new CoinsChanged(total));
            }
            EventBus.Publish(new CoinsCollected(amount));
            return total;
        }
    }
}
