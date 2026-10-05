namespace AuraKnight.World
{
    /// <summary>Pure on/off cycle of a heat vent (GDD section 7.2: 2 s on, 2 s off).</summary>
    public sealed class VentCycle
    {
        readonly float _onSeconds;
        readonly float _offSeconds;
        float _elapsed;

        public VentCycle(float onSeconds = 2f, float offSeconds = 2f)
        {
            _onSeconds = System.Math.Max(0.05f, onSeconds);
            _offSeconds = System.Math.Max(0.05f, offSeconds);
        }

        /// <summary>Starts in the "on" half.</summary>
        public bool IsOn => _elapsed < _onSeconds;

        public void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            float period = _onSeconds + _offSeconds;
            if (_elapsed >= period) _elapsed %= period;
        }
    }
}
