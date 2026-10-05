namespace AuraKnight.Aura.Skills
{
    /// <summary>Pure rules of the water shield: up for 6 s, absorbs exactly one hit, then gone.</summary>
    public sealed class ShieldState
    {
        public const float DefaultDuration = 6f;

        public ShieldState(float duration = DefaultDuration) => Duration = duration;

        public float Duration { get; }
        public float Remaining { get; private set; }
        public bool IsActive => Remaining > 0f;

        public void Activate() => Remaining = Duration;

        public void Tick(float deltaTime)
        {
            if (Remaining > 0f) Remaining = System.Math.Max(0f, Remaining - deltaTime);
        }

        /// <summary>True (and the shield is spent) for the first hit while active; false afterwards.</summary>
        public bool TryAbsorb()
        {
            if (!IsActive) return false;
            Remaining = 0f;
            return true;
        }
    }
}
