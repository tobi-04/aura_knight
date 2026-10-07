using System.Collections.Generic;
using AuraKnight.Core;
using UnityEngine;

namespace AuraKnight.UI
{
    /// <summary>
    /// The 3-button Aura ring (Wind / Fire / Water): locked buttons are grey with a padlock, the current Aura is bright.
    /// It only restyles the existing on-screen buttons; touches still reach AuraManager through the virtual gamepad.
    /// Listens to AuraChanged, AuraUnlocked, GameStateLoaded and the button opacity setting.
    /// </summary>
    public sealed class AuraRingView : MonoBehaviour
    {
        [SerializeField] AuraButtonView[] buttons = new AuraButtonView[0];

        readonly HashSet<string> unlocked = new();
        string current = GameState.NoAura;

        public IReadOnlyList<AuraButtonView> Buttons => buttons;

        public void Bind(AuraButtonView[] views) => buttons = views;

        void OnEnable()
        {
            EventBus.Subscribe<AuraChanged>(OnChanged);
            EventBus.Subscribe<AuraUnlocked>(OnUnlocked);
            EventBus.Subscribe<GameStateLoaded>(OnStateLoaded);
            EventBus.Subscribe<SettingsChanged>(OnSettings);
            ReadState();
            Refresh();
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<AuraChanged>(OnChanged);
            EventBus.Unsubscribe<AuraUnlocked>(OnUnlocked);
            EventBus.Unsubscribe<GameStateLoaded>(OnStateLoaded);
            EventBus.Unsubscribe<SettingsChanged>(OnSettings);
        }

        void OnChanged(AuraChanged e)
        {
            current = e.AuraId;
            Refresh();
        }

        void OnUnlocked(AuraUnlocked e)
        {
            unlocked.Add(e.AuraId);
            Refresh();
        }

        void OnStateLoaded(GameStateLoaded e)
        {
            ReadState();
            Refresh();
        }

        void OnSettings(SettingsChanged e)
        {
            if (e.Key == SettingsKeys.ButtonOpacity) Refresh();
        }

        void ReadState()
        {
            unlocked.Clear();
            var gm = GameManager.Instance;
            if (gm == null) return;
            current = gm.State.currentAura;
            foreach (string id in gm.State.unlockedAuras) unlocked.Add(id);
        }

        void Refresh()
        {
            float opacity = GameSettings.ButtonOpacity;
            foreach (var view in buttons)
                if (view != null) view.Apply(unlocked.Contains(view.AuraId), view.AuraId == current, opacity);
        }
    }
}
