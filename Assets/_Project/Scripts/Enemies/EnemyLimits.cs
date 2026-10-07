namespace AuraKnight.Enemies
{
    /// <summary>
    /// Level-design budget: at most <see cref="MaxActivePerRoom"/> enemies live in one room's Enemies container
    /// (keeps CPU and readability sane on mobile). Checked by the editor menu "Aura/Enemies/Validate Room Limits".
    /// </summary>
    public static class EnemyLimits
    {
        public const int MaxActivePerRoom = 6;

        public static bool IsWithinLimit(int enemyCount) => enemyCount <= MaxActivePerRoom;
    }
}
