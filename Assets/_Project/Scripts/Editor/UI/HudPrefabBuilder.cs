using AuraKnight.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static AuraKnight.Editor.ScreenParts;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Builds Prefabs/UI/Hud.prefab: hearts, Aura-coloured energy bar, coins and the boss HP bar. Two canvases: a static one
    /// (plate, frames, icons: never changes after build) and a dynamic one (values), so value updates rebuild only small meshes.
    /// MAP / PAUSE and the Aura ring are on the virtual controls prefab (they are on-screen buttons).
    /// </summary>
    static class HudPrefabBuilder
    {
        public static void Build()
        {
            var root = new GameObject("Hud", typeof(RectTransform));
            try
            {
                var staticCanvas = UiFactory.CreateCanvas("HudStatic", 4, root.transform);
                staticCanvas.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
                var dynamicCanvas = UiFactory.CreateCanvas("HudDynamic", 6, root.transform);
                dynamicCanvas.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
                var back = UiFactory.SafeArea(staticCanvas.transform);
                var front = UiFactory.SafeArea(dynamicCanvas.transform);

                var plate = UiFactory.Panel(back, "Plate", UIColorToken.Night, 0.55f);
                UiFactory.Place(plate.rectTransform, TL, new Vector2(16f, -16f), new Vector2(840f, 180f));
                Bar(back, "Accent", UIColorToken.Gold, TL, new Vector2(16f, -16f), new Vector2(6f, 180f));
                var frame = UiFactory.Panel(back, "EnergyFrame", UIColorToken.Panel);
                UiFactory.Place(frame.rectTransform, TL, new Vector2(48f, -118f), new Vector2(330f, 36f));
                var sun = UiFactory.Panel(back, "CoinIcon", UIColorToken.Gold, 1f, false, LoadSprite(UiAssetPaths.SunSprite));
                UiFactory.Place(sun.rectTransform, TL, new Vector2(430f, -40f), new Vector2(60f, 60f));

                BuildHearts(front);
                BuildEnergy(front);
                BuildCoins(front);
                BuildBossBar(front);

                PrefabUtility.SaveAsPrefabAsset(root, UiAssetPaths.HudPrefab);
            }
            finally { Object.DestroyImmediate(root); }
        }

        static void BuildHearts(RectTransform front)
        {
            var container = UiFactory.Place(UiFactory.Rect(front, "Hearts"), TL, new Vector2(48f, -34f), new Vector2(330f, 64f));
            var view = container.gameObject.AddComponent<HeartsView>();
            view.Bind(container, LoadSprite(UiAssetPaths.HeartSprite), 56f, 8f);
            view.Preview(5); // five full hearts baked in, so the prefab is never empty
        }

        static void BuildEnergy(RectTransform front)
        {
            var fill = UiFactory.Panel(front, "EnergyFill", UIColorToken.TextMuted, 1f, false, LoadSprite(UiAssetPaths.WhiteSprite));
            UiFactory.Place(fill.rectTransform, TL, new Vector2(52f, -122f), new Vector2(322f, 28f));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 1f;
            Object.DestroyImmediate(fill.GetComponent<ThemedImage>()); // colour is the current Aura's, set at runtime
            fill.color = UITheme.Active.EnergyColor("None");
            fill.gameObject.AddComponent<EnergyBarView>().Bind(fill);
        }

        static void BuildCoins(RectTransform front)
        {
            var holder = UiFactory.Place(UiFactory.Rect(front, "Coins"), TL, new Vector2(506f, -34f), new Vector2(330f, 70f));
            var text = Text(holder, "Value", null, UIFontRole.Mono, UIColorToken.Gold, 56f, ML, Vector2.zero, new Vector2(330f, 70f),
                TextAlignmentOptions.MidlineLeft);
            var themed = text.GetComponent<ThemedText>();
            themed.SetText("0");
            holder.gameObject.AddComponent<CoinsView>().Bind(themed);
        }

        static void BuildBossBar(RectTransform front)
        {
            var holder = UiFactory.Place(UiFactory.Rect(front, "BossBar"), TC, new Vector2(0f, -40f), new Vector2(900f, 90f));
            var group = holder.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f; // hidden until BossEncounterStarted, also in the serialized state
            var label = Text(holder, "Name", null, UIFontRole.Mono, UIColorToken.TextPrimary, 32f, TL, Vector2.zero, new Vector2(900f, 42f));
            var frame = UiFactory.Panel(holder, "Frame", UIColorToken.Panel);
            UiFactory.Place(frame.rectTransform, BL, Vector2.zero, new Vector2(900f, 34f));
            var fill = UiFactory.Panel(holder, "Fill", UIColorToken.Gold, 1f, false, LoadSprite(UiAssetPaths.WhiteSprite));
            UiFactory.Place(fill.rectTransform, BL, new Vector2(4f, 4f), new Vector2(892f, 26f));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            Object.DestroyImmediate(fill.GetComponent<ThemedImage>());
            holder.gameObject.AddComponent<BossHealthBarView>().Bind(group, fill, label.GetComponent<ThemedText>());
        }
    }
}
