using System;
using AuraKnight.Core;
using AuraKnight.Enemies;

namespace AuraKnight.Progression
{
    public enum ChestRewardKind
    {
        Coins = 0,
        Upgrade = 1,
    }

    /// <summary>What a chest holds (GDD §8: 100-150 coins or one free upgrade). Set per chest in its prefab instance.</summary>
    [Serializable]
    public struct ChestReward
    {
        public ChestRewardKind kind;
        public int coinsMin;
        public int coinsMax;
        public ShopEffect upgrade;

        public static ChestReward Coins(int min = 100, int max = 150) =>
            new ChestReward { kind = ChestRewardKind.Coins, coinsMin = min, coinsMax = max };

        public static ChestReward FreeUpgrade(ShopEffect effect) =>
            new ChestReward { kind = ChestRewardKind.Upgrade, upgrade = effect, coinsMin = 100, coinsMax = 150 };
    }

    public readonly struct ChestOutcome
    {
        public readonly bool Opened;
        public readonly int Coins;
        public readonly bool UpgradeGranted;
        public readonly ShopEffect Upgrade;

        public ChestOutcome(bool opened, int coins, bool upgradeGranted, ShopEffect upgrade)
        {
            Opened = opened;
            Coins = coins;
            UpgradeGranted = upgradeGranted;
            Upgrade = upgrade;
        }

        public static ChestOutcome None => new ChestOutcome(false, 0, false, ShopEffect.Heart);
    }

    /// <summary>Pure chest rules: a chest id opens once per save; the reward goes straight into the GameState.</summary>
    public static class ChestLogic
    {
        public static bool IsOpen(GameState state, string chestId) =>
            state != null && !string.IsNullOrEmpty(chestId) && state.openedChests.Contains(chestId);

        /// <summary>
        /// Opens the chest once: records it in <c>openedChests</c> and pays coins through the <see cref="Wallet"/>, or applies the
        /// free upgrade (falls back to coins when that stat is already capped). Already open or an empty id gives <see cref="ChestOutcome.None"/>.
        /// </summary>
        public static ChestOutcome Open(GameState state, string chestId, ChestReward reward, IRandomSource random)
        {
            if (state == null || string.IsNullOrEmpty(chestId) || state.openedChests.Contains(chestId)) return ChestOutcome.None;
            state.openedChests.Add(chestId);
            if (reward.kind == ChestRewardKind.Upgrade && ShopEffects.Apply(state, reward.upgrade, UpgradeAmount(reward.upgrade)))
                return new ChestOutcome(true, 0, true, reward.upgrade);

            int min = Math.Max(0, reward.coinsMin);
            int max = Math.Max(min, reward.coinsMax);
            int coins = random == null ? min : random.Range(min, max + 1);
            if (coins > 0) new Wallet(state).Add(coins);
            return new ChestOutcome(true, Math.Max(0, coins), false, reward.upgrade);
        }

        /// <summary>Same amount one shop purchase gives (1 heart, 25 energy, 1 sword level).</summary>
        public static int UpgradeAmount(ShopEffect effect) => effect == ShopEffect.Energy ? 25 : 1;
    }
}
