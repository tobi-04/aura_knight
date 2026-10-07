using System.Collections.Generic;
using AuraKnight.Core;
using AuraKnight.Progression;
using UnityEngine;

namespace AuraKnight.UI
{
    /// <summary>
    /// Shop of Tư Tế Sol (slide 12 cards). Pauses the game while open. Cards are built from the <see cref="ShopItem"/> list on first
    /// show and refreshed on every purchase and every CoinsChanged; buying goes through <see cref="ShopService"/>.
    /// </summary>
    public sealed class ShopScreen : UIScreen
    {
        static readonly Vector2 CardSize = new Vector2(852f, 130f);
        const float Gap = 16f;

        [SerializeField] UIRouter router;
        [SerializeField] ShopItem[] items = new ShopItem[0];
        [SerializeField] RectTransform list;
        [SerializeField] ThemedText dialogue;
        [SerializeField] ThemedText coins;
        [SerializeField] UIButton backButton;

        readonly List<ShopItemView> cards = new();
        bool ownsPause;

        public IReadOnlyList<ShopItemView> Cards => cards;
        public ShopResult LastResult { get; private set; }
        /// <summary>Localization key of the line Sol currently says.</summary>
        public string DialogueKey => dialogue.LocKey;

        public void Bind(UIRouter uiRouter, ShopItem[] shopItems, RectTransform listRoot, ThemedText dialogueText, ThemedText coinsText, UIButton back)
        {
            router = uiRouter;
            items = shopItems;
            list = listRoot;
            dialogue = dialogueText;
            coins = coinsText;
            backButton = back;
        }

        protected override void Awake()
        {
            base.Awake();
            backButton.onClick.AddListener(Close);
        }

        void OnEnable() => EventBus.Subscribe<CoinsChanged>(OnCoinsChanged);

        void OnDisable() => EventBus.Unsubscribe<CoinsChanged>(OnCoinsChanged);

        /// <summary>The line Sol says on top of the list (a localization key; empty keeps the default greeting).</summary>
        public void SetDialogue(string key) => dialogue.SetKey(string.IsNullOrEmpty(key) ? "dialogue.sol.default" : key);

        protected override void OnShowing()
        {
            var pause = PauseController.Instance;
            ownsPause = pause != null && pause.Pause(false);
            EnsureCards();
            RefreshAll();
        }

        protected override void OnHiding()
        {
            if (ownsPause) PauseController.Instance?.Resume();
            ownsPause = false;
        }

        public override bool HandleBack()
        {
            Close();
            return true;
        }

        void Close()
        {
            if (router != null && router.Contains(this)) router.Remove(this);
            else Hide();
        }

        void EnsureCards()
        {
            if (cards.Count > 0) return;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null) continue;
                var card = ShopItemView.Create(list, items[i], CardSize);
                var rect = (RectTransform)card.transform;
                int index = cards.Count;
                UiFactory.Place(rect, new Vector2(0f, 1f), new Vector2((index % 2) * (CardSize.x + Gap), -(index / 2) * (CardSize.y + Gap)), CardSize);
                card.BuyClicked += OnBuy;
                cards.Add(card);
            }
        }

        void OnBuy(ShopItem item)
        {
            LastResult = ShopService.Buy(item);
            RefreshAll();
        }

        void OnCoinsChanged(CoinsChanged evt) => RefreshAll();

        void RefreshAll()
        {
            var state = GameManager.Instance != null ? GameManager.Instance.State : null;
            coins.SetText(Localization.Format("shop.coins", state != null ? state.coins : 0));
            foreach (var card in cards) card.Refresh(ShopRules.Quote(state, card.Item));
        }
    }
}
