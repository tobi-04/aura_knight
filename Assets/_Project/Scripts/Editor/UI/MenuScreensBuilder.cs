using AuraKnight.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static AuraKnight.Editor.ScreenParts;

namespace AuraKnight.Editor
{
    /// <summary>Splash, Main Menu (slide 1) and the intro cutscene (slide 3).</summary>
    static class MenuScreensBuilder
    {
        public static SplashScreen Splash(Transform canvas, out Button skip)
        {
            var screen = NewScreen<SplashScreen>(canvas, "Splash", UIColorToken.Night, 1f, true, out var safe);
            var studio = Group(safe, "Studio");
            Text(studio, "Name", "splash.studio", UIFontRole.Display, UIColorToken.TextPrimary, 120f, C, Vector2.zero,
                new Vector2(1400f, 180f), TextAlignmentOptions.Center);
            Bar(studio, "Line", UIColorToken.Gold, C, new Vector2(0f, -110f), new Vector2(240f, 6f), true);
            var title = Group(safe, "Title");
            Text(title, "Name", "splash.title", UIFontRole.Display, UIColorToken.TextPrimary, 210f, C, new Vector2(0f, 20f),
                new Vector2(1700f, 280f), TextAlignmentOptions.Center);
            Bar(title, "Line", UIColorToken.Gold, C, new Vector2(0f, -150f), new Vector2(360f, 6f), true);
            var tap = UiFactory.Stretch(UiFactory.Rect(safe, "TapToSkip"));
            var tapImage = tap.gameObject.AddComponent<Image>();
            tapImage.color = Color.clear;
            skip = tap.gameObject.AddComponent<Button>();
            skip.transition = Selectable.Transition.None;
            screen.Bind(studio.GetComponent<CanvasGroup>(), title.GetComponent<CanvasGroup>(), skip);
            Finish(screen);
            return screen;
        }

        public static MainMenuScreen MainMenu(Transform canvas, UIRouter router, UIScreen settings, CreditsScreen credits,
            IntroCutscene intro)
        {
            var screen = NewScreen<MainMenuScreen>(canvas, "MainMenu", UIColorToken.Night, 1f, true, out var safe);
            var root = (RectTransform)screen.transform;
            var hero = KeyArt(root, "KeyArt", LoadTexture(UiAssetPaths.HeroArt), 0.6f);
            hero.transform.SetSiblingIndex(1); // above the background, below the safe-area content
            root.Find("KeyArtFade").SetSiblingIndex(2);
            Bar(safe, "AccentBar", UIColorToken.Gold, TL, new Vector2(96f, -128f), new Vector2(6f, 770f));
            Text(safe, "Label", "menu.label", UIFontRole.Mono, UIColorToken.Gold, 34f, TL, new Vector2(150f, -128f), new Vector2(900f, 46f));
            Text(safe, "Title", "menu.title", UIFontRole.Display, UIColorToken.TextPrimary, 168f, TL, new Vector2(150f, -184f),
                new Vector2(720f, 400f));
            Text(safe, "Subtitle", "menu.subtitle", UIFontRole.Mono, UIColorToken.Wind, 38f, TL, new Vector2(150f, -600f), new Vector2(900f, 50f));
            var size = new Vector2(486f, ButtonHeight);
            var cont = Button(safe, "Continue", "menu.continue", TL, new Vector2(150f, -690f), size, true);
            var fresh = Button(safe, "NewGame", "menu.new_game", TL, new Vector2(660f, -690f), size);
            var settingsButton = Button(safe, "Settings", "menu.settings", TL, new Vector2(150f, -864f), size);
            var about = Button(safe, "About", "menu.about", TL, new Vector2(660f, -864f), size);
            Footer(safe, "footer.p01");
            screen.Bind(router, cont, fresh, settingsButton, about, settings, credits, intro);
            Finish(screen);
            return screen;
        }

        public static IntroCutscene Intro(Transform canvas)
        {
            var screen = NewScreen<IntroCutscene>(canvas, "Intro", UIColorToken.Night, 1f, true, out var safe);
            var advance = UiFactory.Stretch(UiFactory.Rect(safe, "Advance")).gameObject;
            advance.AddComponent<Image>().color = Color.clear;
            var advanceButton = advance.AddComponent<Button>();
            advanceButton.transition = Selectable.Transition.None;
            Header(safe, "intro.label", "intro.title");
            const int count = 4;
            const float cardW = 390f, cardH = 470f, gap = 56f;
            var groups = new CanvasGroup[count];
            var bodies = new ThemedText[count];
            for (int i = 0; i < count; i++)
            {
                var card = Group(safe, $"Card{i + 1}");
                UiFactory.Place((RectTransform)card, TL, new Vector2(96f + i * (cardW + gap), -330f), new Vector2(cardW, cardH));
                var back = UiFactory.Panel(card, "Back", i == count - 1 ? UIColorToken.Panel : UIColorToken.Panel);
                UiFactory.Stretch(back.rectTransform);
                Text(card, "Number", null, UIFontRole.Mono, UIColorToken.Gold, 46f, TL, new Vector2(28f, -26f), new Vector2(200f, 60f))
                    .GetComponent<ThemedText>().SetText($"{i + 1:00}");
                var body = Text(card, "Body", null, UIFontRole.Body, UIColorToken.TextPrimary, 40f, TL, new Vector2(28f, -120f),
                    new Vector2(cardW - 56f, cardH - 150f));
                groups[i] = card.GetComponent<CanvasGroup>();
                bodies[i] = body.GetComponent<ThemedText>();
            }
            var skip = Button(safe, "Skip", "intro.skip", BR, new Vector2(-96f, 110f), new Vector2(420f, ButtonHeight));
            Footer(safe, "footer.p02");
            screen.Bind(groups, bodies, skip, advanceButton);
            Finish(screen);
            return screen;
        }

        static RectTransform Group(Transform parent, string name)
        {
            var rect = UiFactory.Stretch(UiFactory.Rect(parent, name));
            rect.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
            return rect;
        }
    }
}
