using System;
using AuraKnight.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AuraKnight.UI
{
    /// <summary>
    /// One shop card (slide 12): panel with a left border in the item's colour, name, description, mono gold price, purchase count
    /// and a MUA button. Built in code by <see cref="Create"/> so the list always follows the ShopItem assets.
    /// </summary>
    public sealed class ShopItemView : MonoBehaviour
    {
        ThemedText nameText, descriptionText, priceText, countText;
        UIButton buyButton;

        public ShopItem Item { get; private set; }
        public ShopStatus Status { get; private set; } = ShopStatus.Invalid;
        public bool CanBuy => buyButton != null && buyButton.interactable;
        public string PriceText => priceText != null ? priceText.Text.text : string.Empty;

        public event Action<ShopItem> BuyClicked;

        /// <summary>Colour of the left border: hearts red, energy blue, sword gold, a map the colour of its region.</summary>
        public static UIColorToken AccentFor(ShopItem item)
        {
            switch (item.Effect)
            {
                case ShopEffect.Heart: return UIColorToken.Fire;
                case ShopEffect.Energy: return UIColorToken.Water;
                case ShopEffect.MapRegion: return UITheme.RegionToken(item.RegionId);
                default: return UIColorToken.Gold;
            }
        }

        public static ShopItemView Create(Transform parent, ShopItem item, Vector2 size)
        {
            var rect = UiFactory.Rect(parent, "Card_" + item.Id);
            rect.sizeDelta = size;
            var background = UiFactory.Panel(rect, "Background", UIColorToken.Panel, 1f);
            UiFactory.Stretch(background.rectTransform);
            var border = UiFactory.AccentBar(rect, "Border", AccentFor(item), false, true);
            var borderRect = border.rectTransform;
            borderRect.anchorMin = new Vector2(0f, 0f);
            borderRect.anchorMax = new Vector2(0f, 1f);
            borderRect.pivot = new Vector2(0f, 0.5f);
            borderRect.offsetMin = borderRect.offsetMax = Vector2.zero;
            borderRect.sizeDelta = new Vector2(UITheme.Active.CardBorderWidth, 0f);

            var view = rect.gameObject.AddComponent<ShopItemView>();
            view.Item = item;
            view.nameText = Label(rect, "Name", item.NameKey, UIFontRole.Display, UIColorToken.TextPrimary, 38f,
                new Vector2(0f, 1f), new Vector2(32f, -14f), new Vector2(size.x - 470f, 52f), TextAlignmentOptions.TopLeft);
            view.nameText.SetUppercase(false);
            view.descriptionText = Label(rect, "Description", item.DescriptionKey, UIFontRole.Body, UIColorToken.TextMuted, 28f,
                new Vector2(0f, 1f), new Vector2(32f, -68f), new Vector2(size.x - 470f, 50f), TextAlignmentOptions.TopLeft);
            view.priceText = Label(rect, "Price", null, UIFontRole.Mono, UIColorToken.Gold, 38f,
                new Vector2(1f, 0.5f), new Vector2(-250f, 18f), new Vector2(190f, 48f), TextAlignmentOptions.MidlineRight);
            view.countText = Label(rect, "Count", null, UIFontRole.MonoRegular, UIColorToken.TextMuted, 26f,
                new Vector2(1f, 0.5f), new Vector2(-250f, -22f), new Vector2(190f, 36f), TextAlignmentOptions.MidlineRight);

            var buttonSize = new Vector2(200f, 90f);
            view.buyButton = UiFactory.Button(rect, "Buy", "shop.buy", buttonSize, true);
            UiFactory.Place((RectTransform)view.buyButton.transform, new Vector2(1f, 0.5f), new Vector2(-24f, 0f), buttonSize);
            view.buyButton.onClick.AddListener(view.OnBuy);
            return view;
        }

        public void Refresh(ShopQuote quote)
        {
            Status = quote.Status;
            bool maxed = quote.Status == ShopStatus.MaxedOut || quote.Status == ShopStatus.AtStatLimit;
            if (maxed || quote.Price < 0) priceText.SetText("-");
            else priceText.SetText(quote.Price.ToString());
            countText.SetText($"{quote.Purchased}/{quote.Max}");
            buyButton.Label.SetKey(maxed ? "shop.maxed" : "shop.buy");
            buyButton.interactable = quote.CanBuy;
            priceText.SetColor(quote.Status == ShopStatus.Unaffordable ? UIColorToken.TextMuted : UIColorToken.Gold);
        }

        /// <summary>Taps the buy button (tests, accessibility).</summary>
        public void Click() => buyButton.onClick.Invoke();

        void OnBuy()
        {
            if (CanBuy) BuyClicked?.Invoke(Item);
        }

        static ThemedText Label(Transform parent, string name, string key, UIFontRole role, UIColorToken color, float size,
            Vector2 anchor, Vector2 position, Vector2 box, TextAlignmentOptions align)
        {
            var text = UiFactory.Text(parent, name, key, role, color, size, align);
            UiFactory.Place(text.rectTransform, anchor, position, box);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text.GetComponent<ThemedText>();
        }
    }
}
