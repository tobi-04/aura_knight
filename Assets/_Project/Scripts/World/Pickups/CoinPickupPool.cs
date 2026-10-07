using System.Collections.Generic;
using UnityEngine;

namespace AuraKnight.World.Pickups
{
    /// <summary>
    /// Free list for <see cref="CoinPickup"/> (one coin prefab game-wide). Coins popped from enemies are reused instead of
    /// destroyed; entries destroyed with their scene are skipped when popping.
    /// </summary>
    public static class CoinPickupPool
    {
        static readonly Stack<CoinPickup> Free = new Stack<CoinPickup>();

        public static int FreeCount => Free.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlayModeEnter() => Free.Clear();

        public static CoinPickup Spawn(CoinPickup prefab, Vector2 position, Vector2 velocity)
        {
            if (prefab == null) return null;
            CoinPickup coin = null;
            while (Free.Count > 0 && coin == null) coin = Free.Pop();
            if (coin == null) coin = Object.Instantiate(prefab, position, Quaternion.identity);
            coin.Launch(position, velocity);
            return coin;
        }

        /// <summary>Hides the coin and keeps it for reuse.</summary>
        public static void Release(CoinPickup coin)
        {
            if (coin == null) return;
            coin.gameObject.SetActive(false);
            Free.Push(coin);
        }
    }
}
