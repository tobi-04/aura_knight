using AuraKnight.Core;

namespace AuraKnight.Bosses
{
    /// <summary>Reads the save: a defeated boss stays defeated across reloads.</summary>
    public static class BossProgress
    {
        /// <summary>True when the live GameState lists the boss. Without a GameManager (test scenes) nothing is defeated.</summary>
        public static bool IsDefeated(string bossId)
        {
            var manager = GameManager.Instance;
            return manager != null && !string.IsNullOrEmpty(bossId) && manager.State.defeatedBosses.Contains(bossId);
        }
    }
}
