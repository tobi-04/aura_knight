using UnityEngine;

namespace AuraKnight.UI
{
    public static class SafeAreaMath
    {
        /// <summary>Converts a pixel safe-area rect into normalised anchors; falls back to the full screen.</summary>
        public static void ToAnchors(Rect safeArea, float screenWidth, float screenHeight, out Vector2 min, out Vector2 max)
        {
            if (screenWidth <= 0f || screenHeight <= 0f)
            {
                min = Vector2.zero;
                max = Vector2.one;
                return;
            }
            min = new Vector2(safeArea.xMin / screenWidth, safeArea.yMin / screenHeight);
            max = new Vector2(safeArea.xMax / screenWidth, safeArea.yMax / screenHeight);
        }
    }
}
