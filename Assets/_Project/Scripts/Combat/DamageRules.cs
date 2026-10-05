namespace AuraKnight.Combat
{
    /// <summary>Pure rules deciding whether a hit may land.</summary>
    public static class DamageRules
    {
        /// <summary>Hitboxes never hurt their own team.</summary>
        public static bool CanDamage(Team attacker, Team target) => attacker != target;
    }
}
