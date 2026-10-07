using System.Linq;
using AuraKnight.Editor;
using AuraKnight.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace AuraKnight.Tests.UI
{
    /// <summary>Structure of the generated UI prefabs and scenes: design rules from the phase spec, enforced.</summary>
    public sealed class GeneratedUiAssetsTests
    {
        static GameObject Load(string path)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(go, path + " (run Aura/UI/Generate All)");
            return go;
        }

        static GameObject[] ScreenPrefabs() => new[]
        {
            Load(UiAssetPaths.HudPrefab), Load(UiAssetPaths.GameScreensPrefab), Load(UiAssetPaths.MenuScreensPrefab)
        };

        [Test]
        public void EveryCanvasScalesFrom1920x1080AtMatchHalfAndRespectsTheSafeArea()
        {
            foreach (var prefab in ScreenPrefabs())
            {
                var canvases = prefab.GetComponentsInChildren<Canvas>(true);
                Assert.IsNotEmpty(canvases, prefab.name);
                foreach (var canvas in canvases)
                {
                    var scaler = canvas.GetComponent<CanvasScaler>();
                    Assert.IsNotNull(scaler, $"{prefab.name}/{canvas.name}");
                    Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
                    Assert.AreEqual(new Vector2(1920f, 1080f), scaler.referenceResolution);
                    Assert.AreEqual(0.5f, scaler.matchWidthOrHeight);
                    Assert.IsNotNull(canvas.GetComponentInChildren<SafeAreaFitter>(true), $"{canvas.name} content must sit in a safe area");
                }
            }
        }

        [Test]
        public void HudSplitsStaticAndDynamicCanvasesAndCarriesEveryView()
        {
            var hud = Load(UiAssetPaths.HudPrefab);
            Assert.AreEqual(2, hud.GetComponentsInChildren<Canvas>(true).Length, "static + dynamic");
            Assert.IsNotNull(hud.GetComponentInChildren<HeartsView>(true));
            Assert.IsNotNull(hud.GetComponentInChildren<EnergyBarView>(true));
            Assert.IsNotNull(hud.GetComponentInChildren<CoinsView>(true));
            Assert.IsNotNull(hud.GetComponentInChildren<BossHealthBarView>(true));
            var dynamicCanvas = hud.transform.Find("HudDynamic");
            Assert.IsNotNull(dynamicCanvas.GetComponentInChildren<EnergyBarView>(true), "values live on the dynamic canvas");
            Assert.IsNull(hud.transform.Find("HudStatic").GetComponentInChildren<EnergyBarView>(true));
        }

        [Test]
        public void OnlyInteractiveOrBlockingGraphicsReceiveRaycasts()
        {
            string[] blockers = { "Background", "TapToSkip", "Advance", "Scroll", "Handle" };
            foreach (var prefab in ScreenPrefabs())
                foreach (var graphic in prefab.GetComponentsInChildren<Graphic>(true))
                {
                    if (!graphic.raycastTarget) continue;
                    bool interactive = graphic.GetComponentInParent<Selectable>(true) != null || graphic.GetComponentInParent<ScrollRect>(true) != null;
                    Assert.IsTrue(interactive || blockers.Contains(graphic.name),
                        $"{prefab.name}/{graphic.name} has Raycast Target on but is not interactive");
                    Assert.IsFalse(graphic is TMP_Text, $"{prefab.name}/{graphic.name}: text must not take raycasts");
                }
        }

        [Test]
        public void EveryButtonMeetsTheSixtyFourDpTarget()
        {
            foreach (var prefab in ScreenPrefabs())
                foreach (var button in prefab.GetComponentsInChildren<UIButton>(true))
                {
                    var size = ((RectTransform)button.transform).sizeDelta;
                    Assert.GreaterOrEqual(size.x, 64f, $"{prefab.name}/{button.name} width");
                    Assert.GreaterOrEqual(size.y, 64f, $"{prefab.name}/{button.name} height");
                    Assert.IsNotNull(button.GetComponent<MinTouchTarget>(), button.name);
                    Assert.IsNotNull(button.GetComponent<PressScale>(), button.name);
                }
        }

        [Test]
        public void EveryScreenIsAUiScreenThatStartsInactive()
        {
            foreach (var prefab in new[] { Load(UiAssetPaths.GameScreensPrefab), Load(UiAssetPaths.MenuScreensPrefab) })
                foreach (var screen in prefab.GetComponentsInChildren<UIScreen>(true))
                {
                    Assert.IsFalse(screen.gameObject.activeSelf, screen.name + " starts hidden");
                    Assert.IsNotNull(screen.GetComponent<CanvasGroup>());
                }
        }

        [Test]
        public void ScreenSetsContainWhatThePhaseRequires()
        {
            var game = Load(UiAssetPaths.GameScreensPrefab);
            foreach (var type in new[]
            {
                typeof(PauseScreen), typeof(SettingsScreen), typeof(AuraInfoScreen), typeof(AuraUnlockPopup), typeof(BossIntroBanner),
                typeof(GameOverScreen), typeof(CreditsScreen), typeof(UIRouter), typeof(PauseController), typeof(GameUiDirector), typeof(HudInput)
            })
                Assert.IsNotNull(game.GetComponentInChildren(type, true), "GameScreens misses " + type.Name);
            Assert.IsTrue(game.GetComponentsInChildren<CreditsScreen>(true).Single().EndingMode);

            var menu = Load(UiAssetPaths.MenuScreensPrefab);
            foreach (var type in new[]
            {
                typeof(SplashScreen), typeof(MainMenuScreen), typeof(IntroCutscene), typeof(SettingsScreen), typeof(CreditsScreen),
                typeof(UIRouter), typeof(MainMenuFlow)
            })
                Assert.IsNotNull(menu.GetComponentInChildren(type, true), "MenuScreens misses " + type.Name);
            Assert.IsFalse(menu.GetComponentInChildren<CreditsScreen>(true).EndingMode);
        }

        [Test]
        public void IntroHasFourCards()
        {
            var intro = Load(UiAssetPaths.MenuScreensPrefab).GetComponentInChildren<IntroCutscene>(true);
            Assert.AreEqual(4, new SerializedObject(intro).FindProperty("cards").arraySize);
        }

        [Test]
        public void VirtualControlsUseThemedTmpLabelsAndAnAuraRing()
        {
            var prefab = Load(UiAssetPaths.VirtualControlsPrefab);
            Assert.IsEmpty(prefab.GetComponentsInChildren<Text>(true), "no legacy Text left");
            var buttons = prefab.GetComponentsInChildren<OnScreenButton>(true);
            Assert.AreEqual(9, buttons.Length);
            foreach (var b in buttons)
            {
                var label = b.GetComponentInChildren<ThemedText>(true);
                Assert.IsNotNull(label, b.name);
                Assert.IsFalse(string.IsNullOrEmpty(label.LocKey), b.name + " label goes through Localization");
                Assert.IsNotNull(b.GetComponent<PressScale>(), b.name);
            }
            Assert.AreEqual(3, prefab.GetComponentsInChildren<AuraButtonView>(true).Length);
            Assert.IsNotNull(prefab.GetComponent<AuraRingView>());
            Assert.IsNotNull(prefab.GetComponent<VirtualControlsStyler>());
            Assert.IsNotNull(prefab.GetComponent<CanvasGroup>());
        }

        [Test]
        public void EveryLocalizationKeyUsedByAPrefabExists()
        {
            Assert.IsTrue(StringTable.TryParse(Resources.Load<TextAsset>("Strings_vi").text, out var table));
            foreach (var prefab in ScreenPrefabs().Append(Load(UiAssetPaths.VirtualControlsPrefab)))
                foreach (var text in prefab.GetComponentsInChildren<ThemedText>(true))
                    if (!string.IsNullOrEmpty(text.LocKey))
                        Assert.IsTrue(table.TryGet(text.LocKey, out _), $"{prefab.name}/{text.name}: key '{text.LocKey}' missing in Strings_vi.json");
        }

        [Test]
        public void CoreSceneHasOnlyTheUiRootFromThisPhaseAndMainMenuHasTheMenu()
        {
            string core = System.IO.File.ReadAllText(UiAssetPaths.CoreScene);
            StringAssert.Contains("m_Name: UI_Root", core);
            StringAssert.Contains("m_Name: Managers", core, "the world generator's objects are still there");
            string menu = System.IO.File.ReadAllText(UiAssetPaths.MainMenuScene);
            StringAssert.Contains("MenuScreens", menu);
            StringAssert.Contains("EventSystem", menu);
        }
    }
}
