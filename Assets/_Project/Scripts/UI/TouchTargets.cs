using UnityEngine;

namespace AuraKnight.UI
{
    /// <summary>Touch target sizing: GDD 3.2 asks for at least 64 dp on every tappable element.</summary>
    public static class TouchTargets
    {
        public const float MinDp = 64f;

        /// <summary>Smallest size in canvas units that is at least <see cref="MinDp"/> on this device.</summary>
        public static float MinCanvasUnits(float dpi, float canvasScaleFactor)
        {
            float pixels = UiMetrics.DpToPixels(MinDp, dpi);
            return canvasScaleFactor > 0f ? pixels / canvasScaleFactor : pixels;
        }
    }
}
