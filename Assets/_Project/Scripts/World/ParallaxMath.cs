namespace AuraKnight.World
{
    /// <summary>
    /// Pure parallax placement (GDD 10): a layer with factor f scrolls f times as fast as the camera moves, so its offset from
    /// its anchor is (camera - anchor) * (1 - f). f = 1 stays glued to the world, f &lt; 1 trails behind (far away), f &gt; 1 slides
    /// against the motion (foreground).
    /// </summary>
    public static class ParallaxMath
    {
        public const float Background = 0.1f;
        public const float Midground = 0.5f;
        public const float Action = 1f;
        public const float Foreground = 1.2f;

        /// <summary>The four layer factors, back to front: background, midground, action decor, foreground.</summary>
        public static readonly float[] Factors = { Background, Midground, Action, Foreground };

        /// <summary>World x of a layer whose anchor is <paramref name="anchorX"/> while the camera is at <paramref name="cameraX"/>.</summary>
        public static float PositionX(float anchorX, float cameraX, float factor) => anchorX + (cameraX - anchorX) * (1f - factor);
    }
}
