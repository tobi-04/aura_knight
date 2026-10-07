using UnityEngine;

namespace AuraKnight.UI
{
    /// <summary>Grows a tappable RectTransform (never shrinks it) to at least 64 dp on this device and canvas scale.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class MinTouchTarget : MonoBehaviour
    {
        Canvas root;
        float appliedScale = -1f;

        void OnEnable() => Apply();

        void Update()
        {
            if (root != null && !Mathf.Approximately(root.scaleFactor, appliedScale)) Apply();
        }

        void Apply()
        {
            var canvas = GetComponentInParent<Canvas>();
            root = canvas != null ? canvas.rootCanvas : null;
            appliedScale = root != null ? root.scaleFactor : 1f;
            float min = TouchTargets.MinCanvasUnits(Screen.dpi, appliedScale);
            var rect = (RectTransform)transform;
            // Only fixed-size axes can be grown; stretched axes already follow the parent.
            var size = rect.sizeDelta;
            if (Mathf.Approximately(rect.anchorMin.x, rect.anchorMax.x)) size.x = Mathf.Max(size.x, min);
            if (Mathf.Approximately(rect.anchorMin.y, rect.anchorMax.y)) size.y = Mathf.Max(size.y, min);
            rect.sizeDelta = size;
        }
    }
}
