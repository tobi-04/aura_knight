using AuraKnight.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static AuraKnight.Editor.ScreenParts;

namespace AuraKnight.Editor
{
    /// <summary>The slide 8-10 layout: Aura info (3 tabs, opened from Pause) and the full-screen Aura unlock popup.</summary>
    static class AuraScreensBuilder
    {
        static readonly string[] Ids = { "Wind", "Fire", "Water" };

        public static AuraInfoScreen Info(Transform canvas, UIRouter router)
        {
            var screen = NewScreen<AuraInfoScreen>(canvas, "AuraInfo", UIColorToken.Night, 1f, true, out var safe);
            var panel = Panel(screen.transform, safe);
            var tabs = new UIButton[3];
            var bars = new Image[3];
            string[] keys = { "aura.tab_wind", "aura.tab_fire", "aura.tab_water" };
            for (int i = 0; i < 3; i++)
            {
                tabs[i] = Button(safe, "Tab" + Ids[i], keys[i], BL, new Vector2(96f + i * 264f, 130f), new Vector2(250f, ButtonHeight));
                var bar = UiFactory.AccentBar(tabs[i].transform, "TabBar", UIColorToken.TextMuted, true);
                var rect = bar.rectTransform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                rect.sizeDelta = new Vector2(0f, 8f);
                bars[i] = bar;
                bars[i].GetComponent<ThemedAccentBar>().enabled = false; // colour is driven by the screen
            }
            var back = Button(safe, "Back", "aura.back", BL, new Vector2(96f + 3 * 264f, 130f), new Vector2(250f, ButtonHeight), true);
            Footer(safe, "footer.p05");
            screen.Bind(router, panel, tabs, bars, back);
            Finish(screen);
            return screen;
        }

        public static AuraUnlockPopup Popup(Transform canvas)
        {
            var screen = NewScreen<AuraUnlockPopup>(canvas, "AuraUnlockPopup", UIColorToken.Night, 1f, true, out var safe);
            var panel = Panel(screen.transform, safe);
            var cont = Button(safe, "Continue", "aura.continue", TL, new Vector2(150f, -800f), new Vector2(520f, ButtonHeight), true);
            Footer(safe, "footer.p07");
            screen.Bind(panel, cont);
            Finish(screen);
            return screen;
        }

        /// <summary>Left column (tag, name, four abilities, accent bar) plus key art on the right.</summary>
        static AuraPanelView Panel(Transform screenRoot, RectTransform safe)
        {
            var art = KeyArt(screenRoot, "KeyArt", null, 0.55f);
            art.transform.SetSiblingIndex(1);
            screenRoot.Find("KeyArtFade").SetSiblingIndex(2);
            var view = safe.gameObject.AddComponent<AuraPanelView>();
            var bar = Bar(safe, "AccentBar", UIColorToken.Wind, TL, new Vector2(96f, -128f), new Vector2(6f, 640f));
            var tag = Text(safe, "Tag", "aura.wind.tag", UIFontRole.Mono, UIColorToken.Wind, 36f, TL, new Vector2(150f, -128f), new Vector2(900f, 48f));
            var title = Text(safe, "Name", "aura.wind.name", UIFontRole.Display, UIColorToken.TextPrimary, 120f, TL, new Vector2(150f, -184f),
                new Vector2(900f, 150f));
            title.GetComponent<ThemedText>().SetUppercase(false); // slide 8: "Aura Gió", not AURA GIÓ
            var numbers = new ThemedText[AuraPanelView.SkillCount];
            var skills = new ThemedText[AuraPanelView.SkillCount];
            for (int i = 0; i < AuraPanelView.SkillCount; i++)
            {
                float y = -380f - i * 90f;
                numbers[i] = Text(safe, $"Number{i + 1}", null, UIFontRole.Mono, UIColorToken.Wind, 40f, TL, new Vector2(150f, y), new Vector2(90f, 70f))
                    .GetComponent<ThemedText>();
                skills[i] = Text(safe, $"Skill{i + 1}", $"aura.wind.s{i + 1}", UIFontRole.Body, UIColorToken.TextPrimary, 44f, TL,
                    new Vector2(250f, y), new Vector2(800f, 70f)).GetComponent<ThemedText>();
            }
            var arts = new[]
            {
                LoadTexture(UiAssetPaths.AuraArt("wind")), LoadTexture(UiAssetPaths.AuraArt("fire")), LoadTexture(UiAssetPaths.AuraArt("water"))
            };
            view.Bind(tag.GetComponent<ThemedText>(), title.GetComponent<ThemedText>(), bar.GetComponent<ThemedAccentBar>(),
                numbers, skills, art, arts);
            return view;
        }
    }
}
