using UnityEngine;

namespace AuraKnight.Enemies
{
    /// <summary>
    /// Constant-speed walk along authored waypoints (crawlers on walls and ceilings). Loops, or ping-pongs when not
    /// looping. Following drawn points instead of sticking to surfaces avoids the convex/concave corner failures.
    /// </summary>
    public sealed class WaypointPath
    {
        const float Epsilon = 1e-5f;
        readonly Vector2[] _points;
        readonly bool _loop;
        int _direction = 1;

        /// <summary>Index of the waypoint currently being walked toward.</summary>
        public int TargetIndex { get; private set; }

        public WaypointPath(Vector2[] points, bool loop)
        {
            _points = points ?? new Vector2[0];
            _loop = loop;
            TargetIndex = _points.Length > 1 ? 1 : 0;
        }

        public int Count => _points.Length;

        /// <summary>Restarts the walk toward the second waypoint.</summary>
        public void Reset()
        {
            _direction = 1;
            TargetIndex = _points.Length > 1 ? 1 : 0;
        }

        /// <summary>Position after moving <paramref name="speed"/> * <paramref name="deltaTime"/> along the path; never overshoots a waypoint it should turn at.</summary>
        public Vector2 Advance(Vector2 position, float speed, float deltaTime)
        {
            if (_points.Length == 0) return position;
            float remaining = Mathf.Max(0f, speed * deltaTime);
            int guard = _points.Length * 2 + 2;
            while (remaining > Epsilon && guard-- > 0)
            {
                var target = _points[TargetIndex];
                float distance = Vector2.Distance(position, target);
                if (distance > remaining) return Vector2.MoveTowards(position, target, remaining);
                position = target;
                remaining -= distance;
                NextTarget();
            }
            return position;
        }

        void NextTarget()
        {
            if (_points.Length < 2) return;
            int next = TargetIndex + _direction;
            if (_loop) { TargetIndex = (next + _points.Length) % _points.Length; return; }
            if (next >= _points.Length || next < 0)
            {
                _direction = -_direction;
                next = TargetIndex + _direction;
            }
            TargetIndex = next;
        }
    }
}
