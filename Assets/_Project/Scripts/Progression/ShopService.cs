using AuraKnight.Audio;
using AuraKnight.Core;
using AuraKnight.Player;
using UnityEngine;

namespace AuraKnight.Progression
{
    public enum ShopResult
    {
        Bought,
        NotEnoughCoins,
        MaxedOut,
        AtStatLimit,
        InvalidItem,
        NoGame,
    }

    /// <summary>
    /// Buying: <see cref="TryBuy"/> is the pure rule on a GameState (price tier by purchase count, limit, never negative coins);
    /// <see cref="Buy"/> runs it on the live game and then refreshes Leo's stats, saves and plays the sound.
    /// </summary>
    public static class ShopService
    {
        public static ShopResult TryBuy(GameState state, ShopItem item)
        {
            var quote = ShopRules.Quote(state, item);
            switch (quote.Status)
            {
                case ShopStatus.Invalid: return ShopResult.InvalidItem;
                case ShopStatus.MaxedOut: return ShopResult.MaxedOut;
                case ShopStatus.AtStatLimit: return ShopResult.AtStatLimit;
                case ShopStatus.Unaffordable: return ShopResult.NotEnoughCoins;
            }
            var wallet = new Wallet(state);
            if (!wallet.TrySpend(quote.Price)) return ShopResult.NotEnoughCoins;
            ShopEffects.Apply(state, item.Effect, item.Amount);
            state.AddPurchase(item.Id);
            return ShopResult.Bought;
        }

        /// <summary>Live purchase: the upgrade shows up on Leo and the save immediately.</summary>
        public static ShopResult Buy(ShopItem item)
        {
            var manager = GameManager.Instance;
            if (manager == null) return ShopResult.NoGame;
            var result = TryBuy(manager.State, item);
            if (result != ShopResult.Bought)
            {
                Sfx.Play(SfxId.UiBack);
                return result;
            }
            RefreshPlayer();
            if (!manager.Save()) Debug.LogWarning($"[Shop] Bought '{item.Id}' but the save could not be written.");
            Sfx.Play(SfxId.Coin);
            return result;
        }

        /// <summary>Makes the live PlayerStats take the new maxima from the save (keeps current hearts/energy, adds the gained amount).</summary>
        public static void RefreshPlayer()
        {
            var stats = Object.FindFirstObjectByType<PlayerStats>();
            if (stats != null) stats.ApplyUpgrades();
        }
    }
}
