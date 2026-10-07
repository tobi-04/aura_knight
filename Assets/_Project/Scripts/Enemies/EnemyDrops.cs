using AuraKnight.World.Pickups;
using UnityEngine;

namespace AuraKnight.Enemies
{
    /// <summary>Turns a <see cref="DropResult"/> into world pickups: coins pop out in a fan, a Light Drop appears in place.</summary>
    public static class EnemyDrops
    {
        const float PopSpeedY = 5f;
        const float PopSpread = 2.4f;

        /// <summary>Missing prefabs are skipped (a variant without a coin prefab simply drops nothing visible).</summary>
        public static void Spawn(DropResult drop, Vector2 origin, CoinPickup coinPrefab, LightDropPickup lightPrefab, IRandomSource random)
        {
            if (coinPrefab != null)
            {
                for (int i = 0; i < drop.Coins; i++)
                {
                    float t = drop.Coins == 1 ? 0.5f : (float)i / (drop.Coins - 1);
                    float vx = Mathf.Lerp(-PopSpread, PopSpread, t) + (random.Value() - 0.5f) * 0.6f;
                    float vy = PopSpeedY + random.Value();
                    CoinPickupPool.Spawn(coinPrefab, origin, new Vector2(vx, vy));
                }
            }
            if (drop.LightDrop && lightPrefab != null) Object.Instantiate(lightPrefab, origin, Quaternion.identity);
        }
    }
}
