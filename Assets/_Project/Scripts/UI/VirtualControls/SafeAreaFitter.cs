using UnityEngine;

namespace AuraKnight.UI
{
    /// <summary>Stretches its RectTransform to the device safe area (notches, punch-holes, gesture bars).</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        RectTransform _rect;
        Rect _appliedArea;
        Vector2Int _appliedScreen;

        void OnEnable() => Apply();

        void Update()
        {
            if (Screen.safeArea != _appliedArea || Screen.width != _appliedScreen.x || Screen.height != _appliedScreen.y)
                Apply();
        }

        void Apply()
        {
            if (_rect == null) _rect = (RectTransform)transform;
            _appliedArea = Screen.safeArea;
            _appliedScreen = new Vector2Int(Screen.width, Screen.height);
            SafeAreaMath.ToAnchors(_appliedArea, _appliedScreen.x, _appliedScreen.y, out var min, out var max);
            _rect.anchorMin = min;
            _rect.anchorMax = max;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
