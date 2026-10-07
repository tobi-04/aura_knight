namespace AuraKnight.Enemies
{
    /// <summary>The shared enemy loop (GDD 7.3): Patrol, Detect, Attack, Cooldown, Hurt, Dead. Archetypes map their own phases onto it.</summary>
    public enum EnemyState
    {
        Patrol,
        Detect,
        Attack,
        Cooldown,
        Hurt,
        Dead
    }
}
