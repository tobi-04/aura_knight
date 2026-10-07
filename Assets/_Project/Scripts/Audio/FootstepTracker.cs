namespace AuraKnight.Audio
{
    /// <summary>Counts ground distance covered while running and says when a footstep is due.</summary>
    public sealed class FootstepTracker
    {
        public const float DefaultStride = 1.4f;

        readonly float _stride;
        float _travelled;

        public FootstepTracker(float stride = DefaultStride)
        {
            _stride = stride;
            _travelled = StartOffset;
        }

        float StartOffset => _stride * 0.6f; // first step lands soon after starting to run

        /// <summary>Feeds one physics step; true when a footstep should play now.</summary>
        public bool Advance(float distance, bool runningOnGround)
        {
            if (!runningOnGround)
            {
                _travelled = StartOffset;
                return false;
            }
            _travelled += distance < 0f ? -distance : distance;
            if (_travelled < _stride) return false;
            _travelled -= _stride;
            return true;
        }
    }
}
