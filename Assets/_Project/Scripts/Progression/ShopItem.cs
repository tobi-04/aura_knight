using System.Collections.Generic;
using UnityEngine;

namespace AuraKnight.Progression
{
    /// <summary>
    /// One shop item as data (GDD §8): its tiered prices, how often it can be bought and what it does. The shop screen builds
    /// its cards from a list of these. <see cref="Id"/> is the key in <c>GameState.shopPurchases</c>: never rename after release.
    /// </summary>
    [CreateAssetMenu(menuName = "Aura/Shop Item", fileName = "ShopItem")]
    public sealed class ShopItem : ScriptableObject
    {
        /// <summary>Id prefix of the per-region map items (<c>map_forest</c>); the map screen reads purchases by this id.</summary>
        public const string MapIdPrefix = "map_";

        [SerializeField] string itemId;
        [SerializeField] string nameKey;
        [SerializeField] string descriptionKey;
        [SerializeField] ShopEffect effect;
        [Tooltip("How much one purchase adds (1 heart, 25 energy, 1 sword level).")]
        [SerializeField, Min(1)] int amount = 1;
        [Tooltip("Price of the 1st, 2nd... purchase. Past the last tier the last price repeats.")]
        [SerializeField] List<int> prices = new();
        [SerializeField, Min(1)] int maxPurchases = 1;
        [Tooltip("MapRegion only: the region whose unvisited rooms the map reveals.")]
        [SerializeField] string regionId;

        public string Id => itemId;
        public string NameKey => nameKey;
        public string DescriptionKey => descriptionKey;
        public ShopEffect Effect => effect;
        public int Amount => amount;
        public IReadOnlyList<int> Prices => prices;
        public int MaxPurchases => maxPurchases;
        public string RegionId => regionId;

        public static string MapItemId(string regionId) => MapIdPrefix + regionId;

        /// <summary>Fills every field (generators, tests); an SO has no constructor.</summary>
        public ShopItem Configure(string id, string name, string description, ShopEffect shopEffect, int effectAmount,
            IEnumerable<int> tierPrices, int purchaseLimit, string region = null)
        {
            itemId = id;
            nameKey = name;
            descriptionKey = description;
            effect = shopEffect;
            amount = Mathf.Max(1, effectAmount);
            prices = new List<int>(tierPrices ?? System.Array.Empty<int>());
            maxPurchases = Mathf.Max(1, purchaseLimit);
            regionId = region;
            return this;
        }
    }
}
