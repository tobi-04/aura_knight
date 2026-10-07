using System;
using AuraKnight.Core;

namespace AuraKnight.Progression
{
    /// <summary>
    /// The only writer of <c>GameState.coins</c>: adds saturate at int.MaxValue, spends never go below zero, and every real change
    /// raises <see cref="OnCoinsChanged"/> and publishes <see cref="CoinsChanged"/> on the EventBus (HUD, audio, shop listen to that).
    /// </summary>
    public sealed class Wallet
    {
        static Wallet live;

        readonly GameState state;

        public Wallet(GameState state)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
        }

        /// <summary>Raised with the new total after every change of this wallet instance.</summary>
        public event Action<int> OnCoinsChanged;

        public int Coins => state.coins < 0 ? 0 : state.coins;

        /// <summary>
        /// The wallet of the live GameState, or null without a GameManager (test scenes). Rebuilt when the state object is replaced
        /// (new game / continue), so do not keep the instance across those; listen to the EventBus instead.
        /// </summary>
        public static Wallet Live
        {
            get
            {
                var manager = GameManager.Instance;
                if (manager == null) return null;
                if (live == null || !ReferenceEquals(live.state, manager.State)) live = new Wallet(manager.State);
                return live;
            }
        }

        /// <summary>Adds without overflowing int; a negative current value counts as zero.</summary>
        public static int AddSaturating(int current, int amount)
        {
            if (amount <= 0) return current < 0 ? 0 : current;
            long total = (long)(current < 0 ? 0 : current) + amount;
            return total > int.MaxValue ? int.MaxValue : (int)total;
        }

        /// <summary>Credits coins. Returns the new total, or -1 when the amount is not positive (nothing changes).</summary>
        public int Add(int amount)
        {
            if (amount <= 0) return -1;
            state.coins = AddSaturating(state.coins, amount);
            Announce();
            return state.coins;
        }

        public bool CanAfford(int price) => price >= 0 && price <= Coins;

        /// <summary>Spends exactly <paramref name="price"/> or nothing: false when short or the price is negative.</summary>
        public bool TrySpend(int price)
        {
            if (!CanAfford(price)) return false;
            if (price == 0) return true;
            state.coins = Coins - price;
            Announce();
            return true;
        }

        void Announce()
        {
            OnCoinsChanged?.Invoke(state.coins);
            EventBus.Publish(new CoinsChanged(state.coins));
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => live = null;
    }
}
