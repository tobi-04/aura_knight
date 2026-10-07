using UnityEngine;

namespace AuraKnight.UI
{
    /// <summary>Pure easing math for screen motion (fade + 16 px slide in 200 ms, GDD 9.3). Unity-free so it is unit tested.</summary>
    public static class UITween
    {
        public const float Duration = 0.2f;
        public const float SlidePixels = 16f;

        /// <summary>Cubic ease-out of t in 0..1 (clamped).</summary>
        public static float EaseOut(float t)
        {
            t = Mathf.Clamp01(t);
            float inv = 1f - t;
            return 1f - inv * inv * inv;
        }

        /// <summary>Advances a 0..1 progress by dt towards 1 (showing) or 0 (hiding).</summary>
        public static float Step(float progress, float dt, bool showing, float duration = Duration)
        {
            if (duration <= 0f) return showing ? 1f : 0f;
            float delta = dt / duration;
            return Mathf.Clamp01(showing ? progress + delta : progress - delta);
        }

        /// <summary>Vertical offset in pixels for a progress value: -SlidePixels when hidden, 0 when fully shown.</summary>
        public static float SlideOffset(float progress, float slidePixels = SlidePixels) =>
            -slidePixels * (1f - EaseOut(progress));

        /// <summary>Characters visible in a typewriter effect after elapsed seconds (never above total).</summary>
        public static int Typewriter(float elapsed, float charsPerSecond, int total)
        {
            if (total <= 0 || charsPerSecond <= 0f) return Mathf.Max(0, total);
            return Mathf.Clamp(Mathf.FloorToInt(elapsed * charsPerSecond), 0, total);
        }
    }
}
