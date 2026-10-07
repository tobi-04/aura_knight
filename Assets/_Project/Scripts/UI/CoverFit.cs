using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AuraKnight.UI
{
    /// <summary>Keeps a RawImage covering its rect without distortion when the screen aspect (16:9 to 21:9) or texture changes.</summary>
    [RequireComponent(typeof(RawImage))]
    public sealed class CoverFit : UIBehaviour
    {
        [SerializeField] Vector2 focus = new Vector2(0.5f, 0.5f);

        public void SetFocus(Vector2 value)
        {
            focus = value;
            Apply();
        }

        protected override void OnEnable() => Apply();
        protected override void OnRectTransformDimensionsChange() => Apply();

        public void Apply()
        {
            var image = GetComponent<RawImage>();
            var rect = ((RectTransform)transform).rect;
            if (image.texture == null || rect.height <= 0f) return;
            image.uvRect = CoverMath.UvRect((float)image.texture.width / image.texture.height, rect.width / rect.height, focus);
        }
    }
}
