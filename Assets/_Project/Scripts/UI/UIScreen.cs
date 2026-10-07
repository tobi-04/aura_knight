using System;
using UnityEngine;

namespace AuraKnight.UI
{
    /// <summary>
    /// Base of every full screen / popup: fade + 16 px slide in 200 ms on unscaled time (works while paused).
    /// Screens start inactive; <see cref="Show"/> activates them and <see cref="Hide"/> deactivates after the fade.
    /// A hidden screen never blocks touches. Subclasses override <see cref="OnShowing"/> / <see cref="OnHidden"/> / <see cref="HandleBack"/>.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIScreen : MonoBehaviour, IRoutedScreen
    {
        CanvasGroup group;
        RectTransform rect;
        Vector2 restPosition;
        float progress;
        int direction; // +1 showing, -1 hiding, 0 settled

        public bool IsVisible { get; private set; }
        public bool IsTransitioning => direction != 0;

        /// <summary>Raised when the screen starts showing.</summary>
        public event Action<UIScreen> Shown;
        /// <summary>Raised once the screen is fully hidden and deactivated.</summary>
        public event Action<UIScreen> Closed;

        protected CanvasGroup Group => group != null ? group : (group = GetComponent<CanvasGroup>());

        protected virtual void Awake()
        {
            rect = (RectTransform)transform;
            restPosition = rect.anchoredPosition;
        }

        public void Show(bool instant = false)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            bool wasVisible = IsVisible;
            IsVisible = true;
            Group.interactable = true;
            Group.blocksRaycasts = BlocksTouches;
            if (instant || !Application.isPlaying)
            {
                Settle(1f);
            }
            else
            {
                direction = 1;
                Apply(progress);
            }
            if (wasVisible) return;
            OnShowing();
            Shown?.Invoke(this);
        }

        public void Hide(bool instant = false)
        {
            if (!IsVisible)
            {
                if (instant && gameObject.activeSelf) FinishHide();
                return;
            }
            IsVisible = false;
            OnHiding();
            Group.interactable = false;
            Group.blocksRaycasts = false;
            if (instant || !Application.isPlaying || !isActiveAndEnabled)
            {
                FinishHide();
                return;
            }
            direction = -1;
        }

        public virtual bool HandleBack() => false;

        /// <summary>False for HUD-like overlays (boss banner) that must let touches through to the controls below.</summary>
        protected virtual bool BlocksTouches => true;

        protected virtual void OnShowing() { }
        /// <summary>Hide started (the fade-out has not run yet): the moment to give control back.</summary>
        protected virtual void OnHiding() { }
        protected virtual void OnHidden() { }

        protected virtual void Update()
        {
            if (direction == 0) return;
            progress = UITween.Step(progress, Time.unscaledDeltaTime, direction > 0);
            Apply(progress);
            if (direction > 0 && progress >= 1f) direction = 0;
            else if (direction < 0 && progress <= 0f) FinishHide();
        }

        void Settle(float value)
        {
            progress = value;
            direction = 0;
            Apply(value);
        }

        void FinishHide()
        {
            Settle(0f);
            gameObject.SetActive(false);
            OnHidden();
            Closed?.Invoke(this);
        }

        void Apply(float value)
        {
            if (rect == null) Awake();
            Group.alpha = UITween.EaseOut(value);
            rect.anchoredPosition = restPosition + new Vector2(0f, UITween.SlideOffset(value, UITheme.Active.SlidePixels));
        }
    }
}
