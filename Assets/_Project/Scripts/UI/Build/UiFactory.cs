using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AuraKnight.UI
{
    /// <summary>
    /// Code-side builders shared by the editor generators and <see cref="VirtualControlsBuilder"/>: rects, themed images,
    /// themed TMP texts and buttons. Runtime code on purpose (no AssetDatabase), so any caller can use it.
    /// </summary>
    public static class UiFactory
    {
        public const float ReferenceWidth = 1920f, ReferenceHeight = 1080f;

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Stretch(RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        /// <summary>Fixed-size rect anchored (and pivoted) at one point of its parent.</summary>
        public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static Canvas CreateCanvas(string name, int sortingOrder, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform SafeArea(Transform parent, string name = "SafeArea")
        {
            var rect = Stretch(Rect(parent, name));
            rect.gameObject.AddComponent<SafeAreaFitter>();
            return rect;
        }

        /// <summary>Plain themed rectangle (Image without a sprite). Raycasts off unless it must catch touches.</summary>
        public static Image Panel(Transform parent, string name, UIColorToken token, float alpha = 1f, bool raycast = false, Sprite sprite = null)
        {
            var image = Rect(parent, name).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = raycast;
            image.gameObject.AddComponent<ThemedImage>().Configure(token, alpha);
            return image;
        }

        public static Image AccentBar(Transform parent, string name, UIColorToken token = UIColorToken.Gold, bool horizontal = false, bool cardBorder = false)
        {
            var image = Rect(parent, name).gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.gameObject.AddComponent<ThemedAccentBar>().Configure(token, horizontal, cardBorder);
            return image;
        }

        /// <summary>Themed TMP text. Built inactive so the font is assigned before TMP's Awake runs.</summary>
        public static TextMeshProUGUI Text(Transform parent, string name, string locKey, UIFontRole role, UIColorToken color,
            float size, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.SetActive(false);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            var font = UITheme.Active.GetFont(role);
            if (font != null) tmp.font = font;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            go.AddComponent<ThemedText>().Configure(role, color, locKey);
            go.SetActive(true);
            return tmp;
        }

        /// <summary>
        /// Flat button: panel (or gold, for the primary action) background, 6 px accent bar on the left, Display label.
        /// </summary>
        public static UIButton Button(Transform parent, string name, string locKey, Vector2 size, bool primary = false)
        {
            var rect = Rect(parent, name);
            rect.sizeDelta = size;
            var background = rect.gameObject.AddComponent<Image>();
            background.raycastTarget = true;
            background.gameObject.AddComponent<ThemedImage>().Configure(primary ? UIColorToken.Gold : UIColorToken.Panel);
            var accent = AccentBar(rect, "Accent", primary ? UIColorToken.TextInk : UIColorToken.Gold);
            var accentRect = accent.rectTransform;
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.offsetMin = accentRect.offsetMax = Vector2.zero;
            accentRect.sizeDelta = new Vector2(UITheme.Active.AccentBarWidth, 0f);
            var label = Text(rect, "Label", locKey, UIFontRole.Display, primary ? UIColorToken.TextInk : UIColorToken.TextPrimary,
                size.y * 0.3f, TextAlignmentOptions.MidlineLeft);
            Stretch(label.rectTransform, 36f, 0f, 16f, 0f);
            var button = rect.gameObject.AddComponent<UIButton>();
            button.Bind(label.GetComponent<ThemedText>(), background, primary ? UIColorToken.TextInk : UIColorToken.TextPrimary);
            rect.gameObject.AddComponent<PressScale>();
            rect.gameObject.AddComponent<MinTouchTarget>();
            return button;
        }
    }
}
