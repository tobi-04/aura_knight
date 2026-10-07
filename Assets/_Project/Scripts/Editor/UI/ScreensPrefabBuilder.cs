using AuraKnight.UI;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>Assembles the two screen prefabs: GameScreens (in Core) and MenuScreens (in MainMenu). Each carries its own router.</summary>
    static class ScreensPrefabBuilder
    {
        public static void BuildGameScreens()
        {
            var root = new GameObject("GameScreens", typeof(RectTransform));
            try
            {
                var router = root.AddComponent<UIRouter>();
                var pause = root.AddComponent<PauseController>();
                var input = root.AddComponent<HudInput>();
                var director = root.AddComponent<GameUiDirector>();
                var canvas = UiFactory.CreateCanvas("ScreensCanvas", 20, root.transform).transform;

                var shop = ShopMapScreensBuilder.Shop(canvas, router);
                var map = ShopMapScreensBuilder.Map(canvas, router);
                root.AddComponent<ShopMapLauncher>().Bind(router, map, shop);
                var banner = OverlayScreensBuilder.BossBanner(canvas);
                var gameOver = OverlayScreensBuilder.GameOver(canvas);
                var settings = PauseSettingsBuilder.Settings(canvas, router);
                var auraInfo = AuraScreensBuilder.Info(canvas, router);
                var pauseScreen = PauseSettingsBuilder.Pause(canvas, router, auraInfo, settings);
                var popup = AuraScreensBuilder.Popup(canvas);
                var ending = OverlayScreensBuilder.Credits(canvas, router, true);
                var loading = OverlayScreensBuilder.Loading(canvas);

                // Pause sits under Settings / Aura info, which are pushed on top of it: keep that sibling order.
                pauseScreen.transform.SetSiblingIndex(settings.transform.GetSiblingIndex());
                pause.Bind(pauseScreen, router);
                input.Bind(pause);
                director.Bind(router, pause, gameOver, popup, banner, ending, loading);
                PrefabUtility.SaveAsPrefabAsset(root, UiAssetPaths.GameScreensPrefab);
            }
            finally { Object.DestroyImmediate(root); }
        }

        public static void BuildMenuScreens()
        {
            var root = new GameObject("MenuScreens", typeof(RectTransform));
            try
            {
                var router = root.AddComponent<UIRouter>();
                var flow = root.AddComponent<MainMenuFlow>();
                var canvas = UiFactory.CreateCanvas("MenuCanvas", 0, root.transform).transform;

                var settings = PauseSettingsBuilder.Settings(canvas, router);
                var credits = OverlayScreensBuilder.Credits(canvas, router, false);
                var intro = MenuScreensBuilder.Intro(canvas);
                var menu = MenuScreensBuilder.MainMenu(canvas, router, settings, credits, intro);
                var splash = MenuScreensBuilder.Splash(canvas, out _);
                flow.Bind(router, splash, menu);
                PrefabUtility.SaveAsPrefabAsset(root, UiAssetPaths.MenuScreensPrefab);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
