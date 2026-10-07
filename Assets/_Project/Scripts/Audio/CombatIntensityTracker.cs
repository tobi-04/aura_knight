namespace AuraKnight.Audio
{
    /// <summary>
    /// Turns "how many enemies are near" into the combat target (0 or 1). Combat stays on for <see cref="Linger"/>
    /// seconds after the last enemy disappears so the music does not flap while a fight pauses.
    /// </summary>
    public sealed class CombatIntensityTracker
    {
        public const float DefaultLinger = 2f;

        readonly float _linger;
        float _remaining;

        public CombatIntensityTracker(float linger = DefaultLinger) => _linger = linger;

        public float Linger => _linger;

        /// <summary>Feeds one scan; returns the target intensity (1 while enemies are near or lingering, else 0).</summary>
        public float Update(int enemiesNearby, float deltaTime)
        {
            if (enemiesNearby > 0) _remaining = _linger;
            else if (_remaining > 0f) _remaining -= deltaTime;
            return enemiesNearby > 0 || _remaining > 0f ? 1f : 0f;
        }

        public void Reset() => _remaining = 0f;
    }
}
