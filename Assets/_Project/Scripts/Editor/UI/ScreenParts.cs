using AuraKnight.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AuraKnight.Editor
{
    /// <summary>Shared scaffolding of the screen builders: screen shell, anchored texts/buttons, footer, key art, slider.</summary>
    static class ScreenParts
    {
        public static readonly Vector2 TL = new Vector2(0f, 1f), TR = new Vector2(1f, 1f), BL = new Vector2(0f, 0f),
            BR = new Vector2(1f, 0f), TC = new Vector2(0.5f, 1f), C = new Vector2(0.5f, 0.5f), ML = new Vector2(0f, 0.5f),
            MR = new Vector2(1f, 0.5f);

        public const float ButtonHeight = 150f;

        /// <summary>Full-screen UIScreen shell: CanvasGroup, background panel and a safe-area content root. Left inactive.</summary>
        public static T NewScreen<T>(Transform parent, string name, UIColorToken background, float alpha, bool blocksTouches,
            out RectTransform safe) where T : UIScreen
        {
            var rect = UiFactory.Stretch(UiFactory.Rect(parent, name));
            rect.gameObject.AddComponent<CanvasGroup>();
            var bg = UiFactory.Panel(rect, "Background", background, alpha, blocksTouches);
            UiFactory.Stretch(bg.rectTransform);
            safe = UiFactory.SafeArea(rect);
            var screen = rect.gameObject.AddComponent<T>();
            return screen;
        }

        public static void Finish(UIScreen screen) => screen.gameObject.SetActive(false);

        public static TextMeshProUGUI Text(Transform parent, string name, string key, UIFontRole role, UIColorToken color,
            float size, Vector2 anchor, Vector2 position, Vector2 box, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            var text = UiFactory.Text(parent, name, key, role, color, size, align);
            UiFactory.Place(text.rectTransform, anchor, position, box);
            return text;
        }

        public static UIButton Button(Transform parent, string name, string key, Vector2 anchor, Vector2 position, Vector2 size,
            bool primary = false)
        {
            var button = UiFactory.Button(parent, name, key, size, primary);
            UiFactory.Place((RectTransform)button.transform, anchor, position, size);
            return button;
        }

        public static Image Bar(Transform parent, string name, UIColorToken token, Vector2 anchor, Vector2 position, Vector2 size,
            bool horizontal = false)
        {
            var bar = UiFactory.AccentBar(parent, name, token, horizontal);
            UiFactory.Place(bar.rectTransform, anchor, position, size);
            bar.GetComponent<ThemedAccentBar>().Apply();
            return bar;
        }

        /// <summary>"AURA KNIGHT / NN" bottom-left and the short accent line bottom-right (GDD 9.3).</summary>
        public static void Footer(RectTransform safe, string pageKey, bool onPaper = false)
        {
            Text(safe, "FooterLabel", pageKey, UIFontRole.MonoRegular, UIColorToken.TextMuted, 26f, BL, new Vector2(96f, 36f),
                new Vector2(600f, 40f), TextAlignmentOptions.BottomLeft);
            Bar(safe, "FooterLine", onPaper ? UIColorToken.TextInk : UIColorToken.Gold, BR, new Vector2(-96f, 56f), new Vector2(120f, 6f), true);
        }

        /// <summary>Header of the list-style screens: mono gold label + big display title (slide 3 / 12).</summary>
        public static void Header(RectTransform safe, string labelKey, string titleKey, bool onPaper = false)
        {
            Text(safe, "Label", labelKey, UIFontRole.Mono, onPaper ? UIColorToken.TextMuted : UIColorToken.Gold, 34f, TL,
                new Vector2(96f, -56f), new Vector2(1200f, 46f));
            Text(safe, "Title", titleKey, UIFontRole.Display, onPaper ? UIColorToken.TextInk : UIColorToken.TextPrimary, 92f, TL,
                new Vector2(96f, -108f), new Vector2(1500f, 130f)).GetComponent<ThemedText>().SetUppercase(false);
        }

        public static Texture2D LoadTexture(string path)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) Debug.LogWarning($"[UI] Missing texture {path}; run the art import first.");
            return tex;
        }

        public static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        /// <summary>Key art that covers a right-hand region of the screen, with a Night gradient melting its left edge.</summary>
        public static RawImage KeyArt(Transform screenRoot, string name, Texture2D texture, float leftAnchor)
        {
            var rect = UiFactory.Rect(screenRoot, name);
            rect.anchorMin = new Vector2(leftAnchor, 0f);
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var raw = rect.gameObject.AddComponent<RawImage>();
            raw.texture = texture;
            raw.raycastTarget = false;
            rect.gameObject.AddComponent<CoverFit>();
            var fade = UiFactory.Panel(screenRoot, name + "Fade", UIColorToken.Night, 1f, false, LoadSprite(UiAssetPaths.GradientSprite));
            fade.rectTransform.anchorMin = new Vector2(leftAnchor, 0f);
            fade.rectTransform.anchorMax = new Vector2(leftAnchor + 0.14f, 1f);
            fade.rectTransform.offsetMin = fade.rectTransform.offsetMax = Vector2.zero;
            return raw;
        }

        /// <summary>Horizontal slider: invisible full-height hit area (touch target), thin gold track, square handle.</summary>
        public static Slider NewSlider(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = UiFactory.Place(UiFactory.Rect(parent, name), anchor, position, size);
            var hit = rect.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var track = UiFactory.Panel(rect, "Track", UIColorToken.TextMuted, 0.35f);
            Track(track.rectTransform, 12f, 28f);
            var fillArea = UiFactory.Stretch(UiFactory.Rect(rect, "FillArea"), 28f, 0f, 28f, 0f);
            var fill = UiFactory.Panel(fillArea, "Fill", UIColorToken.Gold);
            fill.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            fill.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            fill.rectTransform.sizeDelta = new Vector2(0f, 12f);
            var handleArea = UiFactory.Stretch(UiFactory.Rect(rect, "HandleArea"), 28f, 0f, 28f, 0f);
            var handle = UiFactory.Panel(handleArea, "Handle", UIColorToken.TextPrimary, 1f, true);
            handle.rectTransform.sizeDelta = new Vector2(36f, 96f);
            var slider = rect.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            rect.gameObject.AddComponent<MinTouchTarget>();
            return slider;
        }

        static void Track(RectTransform rect, float thickness, float margin)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.offsetMin = new Vector2(margin, -thickness / 2f);
            rect.offsetMax = new Vector2(-margin, thickness / 2f);
        }
    }
}
