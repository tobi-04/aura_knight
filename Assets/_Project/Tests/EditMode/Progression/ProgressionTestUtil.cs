using System.Collections.Generic;
using AuraKnight.Core;
using AuraKnight.Enemies;
using AuraKnight.Progression;
using UnityEngine;

namespace AuraKnight.Tests.Progression
{
    /// <summary>Items with the GDD §8 numbers, built in memory so the pure rules are tested without assets.</summary>
    static class ProgressionTestUtil
    {
        public static ShopItem Heart() => Make("heart", ShopEffect.Heart, 1, new[] { 100, 200, 300, 400 }, 4);
        public static ShopItem Energy() => Make("energy", ShopEffect.Energy, 25, new[] { 120, 240, 360, 480 }, 4);
        public static ShopItem Sword() => Make("sword", ShopEffect.Sword, 1, new[] { 300, 600 }, 2);
        public static ShopItem Map(string region) => Make(ShopItem.MapItemId(region), ShopEffect.MapRegion, 1, new[] { 50 }, 1, region);

        public static ShopItem Make(string id, ShopEffect effect, int amount, int[] prices, int max, string region = null) =>
            ScriptableObject.CreateInstance<ShopItem>().Configure(id, "n", "d", effect, amount, prices, max, region);

        public static GameState StateWithCoins(int coins)
        {
            var state = GameState.NewGame();
            state.coins = coins;
            return state;
        }

        /// <summary>Returns queued integers from Range (clamped into the requested interval) and the same value from Value.</summary>
        public sealed class FakeRandom : IRandomSource
        {
            readonly Queue<int> values;

            public FakeRandom(params int[] values) { this.values = new Queue<int>(values); }

            public float Value() => 0f;

            public int Range(int minInclusive, int maxExclusive)
            {
                int v = values.Count > 0 ? values.Dequeue() : minInclusive;
                return Mathf.Clamp(v, minInclusive, maxExclusive - 1);
            }
        }
    }
}
