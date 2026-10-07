using System.Collections.Generic;
using AuraKnight.Core;

namespace AuraKnight.Progression
{
    public enum ShopStatus
    {
        Available,
        Unaffordable,
        MaxedOut,
        /// <summary>The stat is already at its hard cap (cannot happen with GDD counts, guards edited saves).</summary>
        AtStatLimit,
        Invalid,
    }

    /// <summary>What the next purchase of an item looks like for the current save.</summary>
    public readonly struct ShopQuote
    {
        public readonly ShopStatus Status;
        public readonly int Price;
        public readonly int Purchased;
        public readonly int Max;

        public ShopQuote(ShopStatus status, int price, int purchased, int max)
        {
            Status = status;
            Price = price;
            Purchased = purchased;
            Max = max;
        }

        public bool CanBuy => Status == ShopStatus.Available;
    }

    /// <summary>Pure shop rules: price tier by purchase count, purchase limit, affordability.</summary>
    public static class ShopRules
    {
        /// <summary>Price of the next purchase when <paramref name="purchased"/> were already bought; the last tier repeats. -1 for an empty list.</summary>
        public static int PriceFor(IReadOnlyList<int> prices, int purchased)
        {
            if (prices == null || prices.Count == 0) return -1;
            int tier = purchased < 0 ? 0 : purchased;
            if (tier >= prices.Count) tier = prices.Count - 1;
            return prices[tier];
        }

        public static ShopQuote Quote(GameState state, ShopItem item)
        {
            if (state == null || item == null || string.IsNullOrEmpty(item.Id)) return new ShopQuote(ShopStatus.Invalid, -1, 0, 0);
            int purchased = state.GetPurchaseCount(item.Id);
            int price = PriceFor(item.Prices, purchased);
            if (price < 0) return new ShopQuote(ShopStatus.Invalid, -1, purchased, item.MaxPurchases);
            if (purchased >= item.MaxPurchases) return new ShopQuote(ShopStatus.MaxedOut, price, purchased, item.MaxPurchases);
            if (!ShopEffects.CanApply(state, item.Effect)) return new ShopQuote(ShopStatus.AtStatLimit, price, purchased, item.MaxPurchases);
            var status = state.coins >= price ? ShopStatus.Available : ShopStatus.Unaffordable;
            return new ShopQuote(status, price, purchased, item.MaxPurchases);
        }

        /// <summary>Total price of buying an item every time it is allowed (economy checks).</summary>
        public static int TotalCost(ShopItem item, int count)
        {
            int total = 0;
            for (int i = 0; i < count && i < item.MaxPurchases; i++) total += PriceFor(item.Prices, i);
            return total;
        }
    }
}
