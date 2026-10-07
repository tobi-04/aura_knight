using System.Collections.Generic;
using UnityEngine;

namespace AuraKnight.UI
{
    /// <summary>
    /// Full-screen "new Aura" popup (slide 8-10 layout). Pauses the game while open; several unlocks queue up and show
    /// one after another. Back or the button moves on.
    /// </summary>
    public sealed class AuraUnlockPopup : UIScreen
    {
        [SerializeField] AuraPanelView panel;
        [SerializeField] UIButton continueButton;

        readonly Queue<string> queue = new();
        bool ownsPause;

        public string CurrentAuraId => panel != null ? panel.AuraId : null;
        public int QueuedCount => queue.Count;

        public void Bind(AuraPanelView panelView, UIButton button)
        {
            panel = panelView;
            continueButton = button;
        }

        protected override void Awake()
        {
            base.Awake();
            continueButton.onClick.AddListener(Next);
        }

        /// <summary>Shows now, or queues behind the Aura currently on screen.</summary>
        public void Present(string auraId)
        {
            if (IsVisible)
            {
                queue.Enqueue(auraId);
                return;
            }
            panel.Present(auraId, true);
            Show();
        }

        protected override void OnShowing()
        {
            var pause = PauseController.Instance;
            ownsPause = pause != null && pause.Pause(false);
        }

        protected override void OnHiding()
        {
            if (ownsPause) PauseController.Instance?.Resume();
            ownsPause = false;
        }

        void Next()
        {
            if (queue.Count > 0) panel.Present(queue.Dequeue(), true);
            else if (UIRouter.Instance != null && UIRouter.Instance.Contains(this)) UIRouter.Instance.Remove(this);
            else Hide();
        }

        public override bool HandleBack()
        {
            Next();
            return true;
        }
    }
}
