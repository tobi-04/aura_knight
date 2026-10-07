using System;

namespace AuraKnight.Audio
{
    /// <summary>Drops a sound that repeats within a few milliseconds of itself (a coin burst must not stack into a click).</summary>
    public sealed class SfxThrottle
    {
        public const float DefaultMinInterval = 0.04f;

        readonly float[] _last = new float[Enum.GetValues(typeof(SfxId)).Length + 8];
        readonly float _minInterval;

        public SfxThrottle(float minInterval = DefaultMinInterval)
        {
            _minInterval = minInterval;
            Array.Fill(_last, float.NegativeInfinity);
        }

        /// <summary>True (and the time is recorded) when <paramref name="id"/> may play at <paramref name="now"/>.</summary>
        public bool TryAcquire(SfxId id, float now)
        {
            int i = (int)id;
            if (i < 0 || i >= _last.Length) return false;
            if (now - _last[i] < _minInterval) return false;
            _last[i] = now;
            return true;
        }
    }
}
