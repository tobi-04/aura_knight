using UnityEngine;

namespace AuraKnight.UI
{
    /// <summary>Detects one fast downward swipe (default 60 dp within 0.25 s). Plain class for testability.</summary>
    public sealed class SwipeGesture
    {
        float _minDistance;
        readonly float _maxDuration;
        Vector2 _start;
        float _startTime;
        bool _tracking;

        public SwipeGesture(float minDistancePixels, float maxDurationSeconds)
        {
            _minDistance = minDistancePixels;
            _maxDuration = maxDurationSeconds;
        }

        /// <summary>Starts tracking. The distance threshold is re-read per gesture (screen DPI can change), so one instance serves every touch.</summary>
        public void Begin(Vector2 position, float time, float minDistancePixels)
        {
            _minDistance = minDistancePixels;
            Begin(position, time);
        }

        public void Begin(Vector2 position, float time)
        {
            _start = position;
            _startTime = time;
            _tracking = true;
        }

        public void End() => _tracking = false;

        /// <summary>Returns true exactly once per gesture, when the swipe qualifies.</summary>
        public bool Update(Vector2 position, float time)
        {
            if (!_tracking) return false;
            if (time - _startTime > _maxDuration) { _tracking = false; return false; }

            var d = position - _start;
            float down = -d.y;
            if (down < _minDistance || down <= Mathf.Abs(d.x)) return false;
            _tracking = false;
            return true;
        }
    }
}
