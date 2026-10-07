using UnityEngine;

namespace AuraKnight.UI
{
    /// <summary>Splash timing: studio logo fades in, holds, fades out; then the game title fades in and holds. Pure math.</summary>
    public static class SplashTimeline
    {
        public const float StudioFade = 0.3f, StudioHold = 0.9f, TitleFade = 0.4f, TitleHold = 0.9f;
        public const float TitleStart = StudioFade * 2f + StudioHold;
        public const float Total = TitleStart + TitleFade + TitleHold;

        public static float StudioAlpha(float t)
        {
            if (t < StudioFade) return Mathf.Clamp01(t / StudioFade);
            if (t < StudioFade + StudioHold) return 1f;
            return Mathf.Clamp01(1f - (t - StudioFade - StudioHold) / StudioFade);
        }

        public static float TitleAlpha(float t) => Mathf.Clamp01((t - TitleStart) / TitleFade);

        public static bool IsDone(float t) => t >= Total;
    }
}
