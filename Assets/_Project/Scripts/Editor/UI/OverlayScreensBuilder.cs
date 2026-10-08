using AuraKnight.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static AuraKnight.Editor.ScreenParts;

namespace AuraKnight.Editor
{
    /// <summary>Boss intro banner, Game Over, Loading cover, the New Game confirmation and the Credits/Ending screen.</summary>
    static class OverlayScreensBuilder
    {
        public static BossIntroBanner BossBanner(Transform canvas)
        {
            var screen = NewScreen<BossIntroBanner>(canvas, "BossIntroBanner", UIColorToken.Night, 0f, false, out var safe);
            var plate = UiFactory.Panel(safe, "Plate", UIColorToken.Night, 0.92f);
            UiFactory.Place(plate.rectTransform, TC, new Vector2(0f, -70f), new Vector2(1100f, 170f));
            var bar = Bar(plate.transform, "Accent", UIColorToken.Gold, ML, Vector2.zero, new Vector2(8f, 170f));
            var label = Text(plate.transform, "Boss", "boss.label", UIFontRole.Mono, UIColorToken.Gold, 34f, TL, new Vector2(40f, -18f),
                new Vector2(600f, 44f));
            var name = Text(plate.transform, "Name", null, UIFontRole.Display, UIColorToken.TextPrimary, 74f, TL, new Vector2(40f, -64f),
                new Vector2(1020f, 96f));
            screen.Bind(label.GetComponent<ThemedText>(), name.GetComponent<ThemedText>(), bar.GetComponent<ThemedAccentBar>());
            Finish(screen);
            return screen;
        }

        public static GameOverScreen GameOver(Transform canvas)
        {
            var screen = NewScreen<GameOverScreen>(canvas, "GameOver", UIColorToken.Night, 0.88f, true, out var safe);
            Text(safe, "Title", "gameover.title", UIFontRole.Display, UIColorToken.Fire, 150f, C, new Vector2(0f, 60f), new Vector2(1700f, 200f),
                TextAlignmentOptions.Center);
            Bar(safe, "Line", UIColorToken.Gold, C, new Vector2(0f, -70f), new Vector2(360f, 6f), true);
            Text(safe, "Respawn", "gameover.respawn", UIFontRole.Mono, UIColorToken.Gold, 44f, C, new Vector2(0f, -150f), new Vector2(1200f, 70f),
                TextAlignmentOptions.Center);
            Finish(screen);
            return screen;
        }

        /// <summary>Centred Yes/No plate over a dimmed screen ("Bắt đầu lại?" before New Game overwrites a save).</summary>
        public static ConfirmDialog ConfirmNewGame(Transform canvas, UIRouter router)
        {
            var screen = NewScreen<ConfirmDialog>(canvas, "ConfirmNewGame", UIColorToken.Night, 0.85f, true, out var safe);
            var plate = UiFactory.Panel(safe, "Plate", UIColorToken.Panel);
            UiFactory.Place(plate.rectTransform, C, Vector2.zero, new Vector2(1240f, 780f));
            Bar(plate.transform, "Accent", UIColorToken.Gold, TL, new Vector2(56f, -56f), new Vector2(6f, 276f));
            Text(plate.transform, "Label", "confirm.label", UIFontRole.Mono, UIColorToken.Gold, 34f, TL, new Vector2(96f, -56f), new Vector2(1000f, 46f));
            Text(plate.transform, "Title", "confirm.new_game.title", UIFontRole.Display, UIColorToken.TextPrimary, 92f, TL, new Vector2(96f, -100f),
                new Vector2(1080f, 110f)).GetComponent<ThemedText>().SetUppercase(false);
            Text(plate.transform, "Body", "confirm.new_game.body", UIFontRole.Body, UIColorToken.TextMuted, 40f, TL, new Vector2(96f, -222f),
                new Vector2(1060f, 110f)); // buttons may grow to 64 dp (MinTouchTarget): keep ~440 units clear below
            var size = new Vector2(500f, ButtonHeight);
            var cancel = Button(plate.transform, "Cancel", "confirm.no", BL, new Vector2(96f, 56f), size, true);
            var yes = Button(plate.transform, "Confirm", "confirm.yes", BR, new Vector2(-96f, 56f), size);
            screen.Bind(router, yes, cancel);
            Finish(screen);
            return screen;
        }

        public static UIScreen Loading(Transform canvas)
        {
            var screen = NewScreen<UIScreen>(canvas, "Loading", UIColorToken.Night, 1f, true, out var safe);
            Bar(safe, "Accent", UIColorToken.Gold, C, new Vector2(-300f, 0f), new Vector2(6f, 120f));
            Text(safe, "Label", "loading.label", UIFontRole.Mono, UIColorToken.Gold, 56f, C, new Vector2(80f, 0f), new Vector2(700f, 120f),
                TextAlignmentOptions.MidlineLeft);
            Finish(screen);
            return screen;
        }

        public static CreditsScreen Credits(Transform canvas, UIRouter router, bool ending)
        {
            var screen = NewScreen<CreditsScreen>(canvas, ending ? "Ending" : "Credits", UIColorToken.Paper, 1f, true, out var safe);
            Bar(safe, "AccentBar", UIColorToken.Gold, TL, new Vector2(96f, -96f), new Vector2(6f, 200f));
            Text(safe, "Label", "credits.label", UIFontRole.Mono, UIColorToken.TextMuted, 34f, TL,
                new Vector2(150f, -96f), new Vector2(900f, 46f));
            var title = Text(safe, "Title", "credits.title", UIFontRole.Display, UIColorToken.TextInk, 80f, TL, new Vector2(150f, -146f),
                new Vector2(1500f, 130f));
            title.GetComponent<ThemedText>().SetUppercase(false);

            var scrollRect = UiFactory.Place(UiFactory.Rect(safe, "Scroll"), TL, new Vector2(150f, -330f), new Vector2(1500f, 540f));
            scrollRect.gameObject.AddComponent<Image>().color = Color.clear;
            scrollRect.gameObject.AddComponent<RectMask2D>();
            var probe = scrollRect.gameObject.AddComponent<CreditsDragProbe>();
            var body = UiFactory.Text(scrollRect, "Content", null, UIFontRole.Body, UIColorToken.TextInk, 34f);
            var bodyRect = body.rectTransform;
            bodyRect.anchorMin = new Vector2(0f, 1f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 1f);
            bodyRect.anchoredPosition = Vector2.zero;
            bodyRect.sizeDelta = new Vector2(0f, 0f);
            body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.content = bodyRect;
            scroll.viewport = scrollRect;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var close = Button(safe, "Close", ending ? "credits.close_ending" : "credits.close", BL, new Vector2(150f, 100f),
                new Vector2(520f, ButtonHeight));
            Footer(safe, "footer.p06", true);
            screen.Bind(router, scroll, title.GetComponent<ThemedText>(), body.GetComponent<ThemedText>(), probe, close, ending);
            Finish(screen);
            return screen;
        }
    }
}
