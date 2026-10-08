using System;
using UnityEngine;

namespace AuraKnight.UI
{
    /// <summary>
    /// Yes/No overlay pushed on top of the current screen. <see cref="Ask"/> shows it and calls back once:
    /// true for the confirm button, false for cancel or Back. The dialog pops itself before the callback runs.
    /// </summary>
    public sealed class ConfirmDialog : UIScreen
    {
        [SerializeField] UIRouter router;
        [SerializeField] UIButton confirmButton, cancelButton;

        Action<bool> pending;

        public void Bind(UIRouter uiRouter, UIButton confirm, UIButton cancel)
        {
            router = uiRouter;
            confirmButton = confirm;
            cancelButton = cancel;
        }

        protected override void Awake()
        {
            base.Awake();
            confirmButton.onClick.AddListener(() => Answer(true));
            cancelButton.onClick.AddListener(() => Answer(false));
        }

        public void Ask(Action<bool> answered)
        {
            pending = answered;
            router.Push(this);
        }

        void Answer(bool confirmed)
        {
            var callback = pending;
            pending = null;
            if (callback == null) return; // a second tap during the hide tween
            router.Remove(this);
            callback(confirmed);
        }

        public override bool HandleBack()
        {
            if (pending == null) return false; // already answered: let the router pop it
            Answer(false);
            return true;
        }
    }
}
