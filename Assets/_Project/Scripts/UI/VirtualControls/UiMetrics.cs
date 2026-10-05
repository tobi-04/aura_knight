using UnityEngine;

namespace AuraKnight.UI
{
    /// <summary>Density-independent pixel helpers (1 dp = 1 px at 160 dpi).</summary>
    public static class UiMetrics
    {
        public const float BaselineDpi = 160f;

        public static float DpToPixels(float dp, float dpi) =>
            dp * (dpi > 0f ? dpi : BaselineDpi) / BaselineDpi;

        public static float DpToPixels(float dp) => DpToPixels(dp, Screen.dpi);
    }
}
