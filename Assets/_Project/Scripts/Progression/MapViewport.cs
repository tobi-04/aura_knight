using UnityEngine;

namespace AuraKnight.Progression
{
    /// <summary>
    /// Zoom and pan of the map as pure maths. The content is centred in the view; a content point <c>p</c> (relative to the content
    /// centre, unscaled) shows at <c>Pan + p * Zoom</c> relative to the view centre. Zooming keeps the point under the pivot fixed.
    /// </summary>
    public sealed class MapViewport
    {
        public const float DefaultMinZoom = 0.5f, DefaultMaxZoom = 4f;

        readonly float minZoom, maxZoom;
        Vector2 contentSize;

        public MapViewport(float minZoom = DefaultMinZoom, float maxZoom = DefaultMaxZoom)
        {
            this.minZoom = Mathf.Max(0.01f, minZoom);
            this.maxZoom = Mathf.Max(this.minZoom, maxZoom);
            Zoom = 1f;
        }

        public float Zoom { get; private set; }
        public Vector2 Pan { get; private set; }

        /// <summary>Content size in view units; pan is limited so the map cannot leave the view entirely.</summary>
        public void SetContentSize(Vector2 size)
        {
            contentSize = new Vector2(Mathf.Max(0f, size.x), Mathf.Max(0f, size.y));
            Pan = ClampPan(Pan);
        }

        public void Reset()
        {
            Zoom = 1f;
            Pan = Vector2.zero;
        }

        /// <summary>Multiplies the zoom (clamped) around <paramref name="pivot"/> (view-centre relative). Invalid factors are ignored.</summary>
        public void ZoomBy(float factor, Vector2 pivot)
        {
            if (!(factor > 0f) || float.IsInfinity(factor)) return;
            float next = Mathf.Clamp(Zoom * factor, minZoom, maxZoom);
            float ratio = next / Zoom;
            Zoom = next;
            Pan = ClampPan(pivot - (pivot - Pan) * ratio);
        }

        /// <summary>Two-finger pinch: the finger distance ratio is the zoom factor; zero or NaN distances are ignored.</summary>
        public void Pinch(float previousDistance, float distance, Vector2 pivot)
        {
            if (!(previousDistance > 0.01f) || !(distance > 0.01f)) return;
            ZoomBy(distance / previousDistance, pivot);
        }

        public void Drag(Vector2 delta) => Pan = ClampPan(Pan + delta);

        /// <summary>Centres the view on a content point (relative to the content centre, unscaled).</summary>
        public void FocusOn(Vector2 contentPoint) => Pan = ClampPan(-contentPoint * Zoom);

        Vector2 ClampPan(Vector2 pan)
        {
            float limitX = contentSize.x * Zoom * 0.5f, limitY = contentSize.y * Zoom * 0.5f;
            return new Vector2(Mathf.Clamp(pan.x, -limitX, limitX), Mathf.Clamp(pan.y, -limitY, limitY));
        }
    }
}
