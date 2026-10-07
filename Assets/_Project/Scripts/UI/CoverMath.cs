using UnityEngine;

namespace AuraKnight.UI
{
    /// <summary>UV window that makes a texture cover a rect (crop, never stretch), whatever the screen aspect.</summary>
    public static class CoverMath
    {
        /// <param name="focus">Which part to keep when cropping: (0.5,0.5) centre, y=1 top.</param>
        public static Rect UvRect(float textureAspect, float rectAspect, Vector2 focus)
        {
            if (textureAspect <= 0f || rectAspect <= 0f) return new Rect(0f, 0f, 1f, 1f);
            if (rectAspect > textureAspect)
            {
                float h = textureAspect / rectAspect;
                return new Rect(0f, (1f - h) * Mathf.Clamp01(focus.y), 1f, h);
            }
            float w = rectAspect / textureAspect;
            return new Rect((1f - w) * Mathf.Clamp01(focus.x), 0f, w, 1f);
        }
    }
}
