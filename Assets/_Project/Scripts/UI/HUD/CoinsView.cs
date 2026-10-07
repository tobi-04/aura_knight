using AuraKnight.Core;
using UnityEngine;

namespace AuraKnight.UI
{
    /// <summary>Coin counter (mono gold). Listens to CoinsChanged and re-reads the save on GameStateLoaded.</summary>
    public sealed class CoinsView : MonoBehaviour
    {
        [SerializeField] ThemedText value;

        public int Coins { get; private set; }
        public string DisplayedText => value != null ? value.Text.text : string.Empty;

        public void Bind(ThemedText text) => value = text;

        void OnEnable()
        {
            EventBus.Subscribe<CoinsChanged>(OnCoins);
            EventBus.Subscribe<GameStateLoaded>(OnStateLoaded);
            Set(GameManager.Instance != null ? GameManager.Instance.State.coins : 0);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<CoinsChanged>(OnCoins);
            EventBus.Unsubscribe<GameStateLoaded>(OnStateLoaded);
        }

        void OnCoins(CoinsChanged e) => Set(e.Coins);

        void OnStateLoaded(GameStateLoaded e) => Set(GameManager.Instance != null ? GameManager.Instance.State.coins : 0);

        void Set(int coins)
        {
            Coins = Mathf.Max(0, coins);
            if (value != null) value.SetText(Coins.ToString());
        }
    }
}
