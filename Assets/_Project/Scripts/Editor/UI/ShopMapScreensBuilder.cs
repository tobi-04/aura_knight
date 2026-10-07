using System;
using System.Collections.Generic;
using AuraKnight.Progression;
using AuraKnight.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static AuraKnight.Editor.ScreenParts;

namespace AuraKnight.Editor
{
    /// <summary>
    /// The slide 12 shop (header, Sol's line, two columns of cards) and the slide 11 map (viewport with zoom buttons). The cards and
    /// room cells are drawn at runtime from <see cref="ShopItem"/> / <see cref="RoomMapData"/> assets, which are looked up here by type.
    /// Run <c>ProgressionAssetGenerator.GenerateAll</c> before the UI generator so those assets exist.
    /// </summary>
    static class ShopMapScreensBuilder
    {
        const string ShopItemsDir = "Assets/_Project/Data/ShopItems";
        const string MapDir = "Assets/_Project/Data/Map";

        public static ShopScreen Shop(Transform canvas, UIRouter router)
        {
            var screen = NewScreen<ShopScreen>(canvas, "Shop", UIColorToken.Night, 1f, true, out var safe);
            Header(safe, "shop.label", "shop.title");
            Bar(safe, "DialogueBar", UIColorToken.Gold, TL, new Vector2(96f, -250f), new Vector2(6f, 96f));
            var line = Text(safe, "Dialogue", "dialogue.sol.default", UIFontRole.Body, UIColorToken.TextPrimary, 36f, TL,
                new Vector2(130f, -250f), new Vector2(1660f, 96f), TextAlignmentOptions.MidlineLeft);
            var coins = Text(safe, "Coins", "shop.coins", UIFontRole.Mono, UIColorToken.Gold, 40f, TR, new Vector2(-440f, -70f),
                new Vector2(380f, 50f), TextAlignmentOptions.MidlineRight);
            var back = Button(safe, "Back", "shop.back", TR, new Vector2(-96f, -56f), new Vector2(300f, 120f), true);
            var list = UiFactory.Place(UiFactory.Rect(safe, "List"), TL, new Vector2(96f, -370f), new Vector2(1728f, 590f));
            Footer(safe, "footer.p12");
            screen.Bind(router, Load<ShopItem>(ShopItemsDir), list, line.GetComponent<ThemedText>(), coins.GetComponent<ThemedText>(), back);
            Finish(screen);
            return screen;
        }

        public static MapScreen Map(Transform canvas, UIRouter router)
        {
            var screen = NewScreen<MapScreen>(canvas, "Map", UIColorToken.Night, 1f, true, out var safe);
            Header(safe, "map.label", "map.title");
            var area = UiFactory.Panel(safe, "Scroll", UIColorToken.Panel, 1f, true);
            UiFactory.Stretch(area.rectTransform, 96f, 130f, 96f, 250f);
            area.gameObject.AddComponent<RectMask2D>();
            var input = area.gameObject.AddComponent<MapInput>();
            var content = UiFactory.Rect(area.rectTransform, "Content");
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 0.5f);
            content.anchoredPosition = Vector2.zero;

            var zoomIn = Button(safe, "ZoomIn", "map.zoom_in", TR, new Vector2(-130f, -290f), new Vector2(110f, 110f));
            var zoomOut = Button(safe, "ZoomOut", "map.zoom_out", TR, new Vector2(-130f, -410f), new Vector2(110f, 110f));
            var center = Button(safe, "Center", "map.center", TR, new Vector2(-130f, -530f), new Vector2(110f, 110f));
            var back = Button(safe, "Back", "shop.back", TR, new Vector2(-96f, -56f), new Vector2(300f, 120f), true);
            Text(safe, "Legend", "map.legend", UIFontRole.MonoRegular, UIColorToken.TextMuted, 26f, BL, new Vector2(560f, 36f),
                new Vector2(1100f, 40f), TextAlignmentOptions.BottomLeft);
            Footer(safe, "footer.p11");
            screen.Bind(router, Load<RoomMapData>(MapDir), area.rectTransform, content, input, zoomIn, zoomOut, center, back);
            Finish(screen);
            return screen;
        }

        static T[] Load<T>(string dir) where T : UnityEngine.Object
        {
            var paths = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { dir })) paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            paths.Sort(StringComparer.Ordinal);
            if (paths.Count == 0) Debug.LogWarning($"[UI] No {typeof(T).Name} assets in {dir}; run Aura/Progression/Generate All first.");
            var assets = new T[paths.Count];
            for (int i = 0; i < assets.Length; i++) assets[i] = AssetDatabase.LoadAssetAtPath<T>(paths[i]);
            return assets;
        }
    }
}
