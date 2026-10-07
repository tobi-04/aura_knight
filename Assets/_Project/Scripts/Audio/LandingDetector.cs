namespace AuraKnight.Audio
{
    /// <summary>Detects a landing after real air time (a short hop over a seam or a step down is not a landing).</summary>
    public sealed class LandingDetector
    {
        public const float DefaultMinAirTime = 0.12f;

        readonly float _minAirTime;
        float _airTime;

        public LandingDetector(float minAirTime = DefaultMinAirTime) => _minAirTime = minAirTime;

        /// <summary>Feeds one physics step; true on the step that touches ground after at least the minimum air time.</summary>
        public bool Update(bool grounded, float deltaTime)
        {
            if (!grounded)
            {
                _airTime += deltaTime;
                return false;
            }
            bool landed = _airTime >= _minAirTime;
            _airTime = 0f;
            return landed;
        }
    }
}
