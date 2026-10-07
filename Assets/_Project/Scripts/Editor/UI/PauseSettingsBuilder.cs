using AuraKnight.UI;
using TMPro;
using UnityEngine;
using static AuraKnight.Editor.ScreenParts;

namespace AuraKnight.Editor
{
    /// <summary>Pause panel and the Settings screen.</summary>
    static class PauseSettingsBuilder
    {
        public static PauseScreen Pause(Transform canvas, UIRouter router, UIScreen auraInfo, UIScreen settings)
        {
            var screen = NewScreen<PauseScreen>(canvas, "Pause", UIColorToken.Night, 0.85f, true, out var safe);
            Bar(safe, "AccentBar", UIColorToken.Gold, TL, new Vector2(96f, -120f), new Vector2(6f, 900f));
            Text(safe, "Label", "pause.label", UIFontRole.Mono, UIColorToken.Gold, 34f, TL, new Vector2(150f, -120f), new Vector2(900f, 46f));
            Text(safe, "Title", "pause.title", UIFontRole.Display, UIColorToken.TextPrimary, 110f, TL, new Vector2(150f, -172f),
                new Vector2(1100f, 150f)).GetComponent<ThemedText>().SetUppercase(false);
            var size = new Vector2(640f, ButtonHeight);
            var resume = Button(safe, "Resume", "pause.resume", TL, new Vector2(150f, -350f), size, true);
            var aura = Button(safe, "Aura", "pause.aura", TL, new Vector2(150f, -516f), size);
            var settingsButton = Button(safe, "Settings", "pause.settings", TL, new Vector2(150f, -682f), size);
            var menu = Button(safe, "Menu", "pause.menu", TL, new Vector2(150f, -848f), size);
            Footer(safe, "footer.p03");
            screen.Bind(router, resume, aura, settingsButton, menu, auraInfo, settings);
            Finish(screen);
            return screen;
        }

        public static SettingsScreen Settings(Transform canvas, UIRouter router)
        {
            var screen = NewScreen<SettingsScreen>(canvas, "Settings", UIColorToken.Night, 1f, true, out var safe);
            Header(safe, "settings.label", "settings.title");
            const float colW = 840f, rowH = ButtonHeight, gap = 16f, top = -300f;
            float RowY(int i) => top - i * (rowH + gap);
            float left = 96f, right = 984f;

            var music = SliderRow(safe, "Music", "settings.music", left, RowY(0), colW, rowH);
            var sfx = SliderRow(safe, "Sfx", "settings.sfx", left, RowY(1), colW, rowH);
            var scale = SliderRow(safe, "ButtonSize", "settings.button_size", left, RowY(2), colW, rowH);
            var opacity = SliderRow(safe, "ButtonOpacity", "settings.button_opacity", left, RowY(3), colW, rowH);
            scale.slider.minValue = GameSettings.MinButtonScale;
            scale.slider.maxValue = GameSettings.MaxButtonScale;
            opacity.slider.minValue = GameSettings.MinButtonOpacity;
            opacity.slider.maxValue = GameSettings.MaxButtonOpacity;

            var haptics = ToggleRow(safe, "Haptics", "settings.haptics", right, RowY(0), colW, rowH);
            var power = ToggleRow(safe, "PowerSaving", "settings.power_saving", right, RowY(1), colW, rowH);
            var language = ToggleRow(safe, "Language", "settings.language", right, RowY(2), colW, rowH);
            var back = Button(safe, "Back", "settings.back", TL, new Vector2(right, RowY(3)), new Vector2(colW, rowH), true);
            Footer(safe, "footer.p04");
            screen.Bind(router, music, sfx, scale, opacity, haptics, haptics.Label, power, power.Label, language, language.Label, back);
            Finish(screen);
            return screen;
        }

        static SettingsScreen.SliderRow SliderRow(RectTransform parent, string name, string labelKey, float x, float y, float width, float height)
        {
            var row = RowBack(parent, name, labelKey, x, y, width, height);
            var slider = NewSlider(row, "Slider", ML, new Vector2(300f, 0f), new Vector2(width - 300f - 140f, height));
            var value = Text(row, "Value", null, UIFontRole.Mono, UIColorToken.Gold, 34f, MR, new Vector2(-24f, 0f), new Vector2(120f, height),
                TextAlignmentOptions.MidlineRight);
            return new SettingsScreen.SliderRow { slider = slider, value = value.GetComponent<ThemedText>() };
        }

        /// <summary>A row with a BẬT/TẮT-style button; the button's own label shows the current value.</summary>
        static UIButton ToggleRow(RectTransform parent, string name, string labelKey, float x, float y, float width, float height)
        {
            var row = RowBack(parent, name, labelKey, x, y, width, height);
            return Button(row, "Value", "settings.on", MR, new Vector2(-24f, 0f), new Vector2(300f, height - 30f));
        }

        static RectTransform RowBack(RectTransform parent, string name, string labelKey, float x, float y, float width, float height)
        {
            var row = UiFactory.Place(UiFactory.Rect(parent, name), TL, new Vector2(x, y), new Vector2(width, height));
            var back = UiFactory.Panel(row, "Back", UIColorToken.Panel);
            UiFactory.Stretch(back.rectTransform);
            Bar(row, "Accent", UIColorToken.Gold, ML, Vector2.zero, new Vector2(4f, height)).GetComponent<ThemedAccentBar>()
                .Configure(UIColorToken.Gold, false, true);
            Text(row, "Label", labelKey, UIFontRole.Body, UIColorToken.TextPrimary, 38f, ML, new Vector2(28f, 0f),
                new Vector2(280f, height), TextAlignmentOptions.MidlineLeft);
            return row;
        }
    }
}
