using UnityEngine;

namespace AuraKnight.Combat
{
    /// <summary>
    /// Pure bookkeeping for hit stop: decides the time scale to apply and when to restore it.
    /// Returns the scale to set, or null when nothing should change. Ticked with unscaled time.
    /// The timer owns the scale only while it is frozen at <see cref="FrozenScale"/>: if somebody else changed it,
    /// or the game was paused during the freeze, the pause/other owner keeps control and nothing is restored.
    /// </summary>
    public sealed class HitStopTimer
    {
        public const float FrozenScale = 0f;
        float _remaining;
        float _savedScale = 1f;

        public bool Active { get; private set; }

        /// <summary>The scale to restore after the freeze; pause menus must resume to this, never to a value read while frozen.</summary>
        public float SavedScale => _savedScale;

        /// <summary>Starts a freeze (scale 0) or extends a running one. Ignored while the game is already paused.</summary>
        public float? Request(float duration, float currentScale)
        {
            if (duration <= 0f) return null;
            if (Active)
            {
                if (duration > _remaining) _remaining = duration;
                return null;
            }
            if (currentScale <= 0f) return null;
            Active = true;
            _savedScale = currentScale;
            _remaining = duration;
            return FrozenScale;
        }

        /// <summary>
        /// Counts down; when finished returns the saved scale, unless someone else changed the scale meanwhile or
        /// <paramref name="paused"/> (a pause that began during the freeze owns the scale and resumes it itself).
        /// </summary>
        public float? Tick(float unscaledDeltaTime, float currentScale, bool paused = false)
        {
            if (!Active) return null;
            _remaining -= unscaledDeltaTime;
            if (_remaining > 0f) return null;
            Active = false;
            if (paused) return null;
            return Mathf.Approximately(currentScale, FrozenScale) ? _savedScale : (float?)null;
        }

        /// <summary>Cancels a running freeze and returns the scale to restore.</summary>
        public float Abort()
        {
            Active = false;
            _remaining = 0f;
            return _savedScale;
        }
    }
}
