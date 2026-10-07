using UnityEngine;
using UnityEngine.EventSystems;

namespace AuraKnight.UI
{
    /// <summary>Scales the element to 0.95 while pressed (GDD 9.3). Uses unscaled math only, so it works while paused.</summary>
    public sealed class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        Vector3 baseScale = Vector3.one;
        bool pressed;

        public bool IsPressed => pressed;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (pressed) return;
            pressed = true;
            baseScale = transform.localScale;
            transform.localScale = baseScale * UITheme.Active.PressScale;
        }

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) => Release();
        void OnDisable() => Release();

        void Release()
        {
            if (!pressed) return;
            pressed = false;
            transform.localScale = baseScale;
        }
    }
}
