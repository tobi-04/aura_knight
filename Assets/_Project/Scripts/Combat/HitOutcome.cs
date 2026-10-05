namespace AuraKnight.Combat
{
    /// <summary>What happened when a <see cref="DamageInfo"/> reached a target.</summary>
    public enum HitOutcome
    {
        /// <summary>Not applicable: same team, zero damage, or the target is already dead.</summary>
        Ignored,
        /// <summary>Target was invulnerable (i-frames, dash). Still counts as contact for pogo.</summary>
        Absorbed,
        /// <summary>Contact with a hurtbox that has no <see cref="Health"/> (spikes). Pogo-able, deals nothing.</summary>
        Touched,
        Damaged,
        Killed
    }

    public static class HitOutcomeExtensions
    {
        /// <summary>True when health actually went down (the hit "landed": energy gain, feedback).</summary>
        public static bool DealtDamage(this HitOutcome outcome) =>
            outcome == HitOutcome.Damaged || outcome == HitOutcome.Killed;
    }
}
