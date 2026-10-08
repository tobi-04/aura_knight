using System.Collections;
using System.IO;
using AuraKnight.Core;
using AuraKnight.UI;
using AuraKnight.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AuraKnight.Tests.PlayMode.UI
{
    /// <summary>The real MainMenu scene: splash, menu buttons, intro, and the hand-off into Core.</summary>
    public sealed class MainMenuPlayModeTests
    {
        PrefsSnapshot prefs;
        byte[] savedGame;
        string savePath;

        [UnitySetUp]
        public IEnumerator LoadMenu()
        {
            prefs = PrefsSnapshot.Take();
            savePath = Path.Combine(Application.persistentDataPath, FileSaveStorage.DefaultFileName);
            savedGame = File.Exists(savePath) ? File.ReadAllBytes(savePath) : null;
            EventBus.Clear();
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            EventBus.Clear();
            Time.timeScale = 1f;
            prefs.Restore();
            if (savedGame != null) File.WriteAllBytes(savePath, savedGame);
            else if (File.Exists(savePath)) File.Delete(savePath);
            yield return null;
        }

        static T Find<T>() where T : Object => Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);

        static UIButton ButtonOf(Component screen, string name)
        {
            foreach (var b in screen.GetComponentsInChildren<UIButton>(true))
                if (b.name == name) return b;
            Assert.Fail("no button " + name);
            return null;
        }

        static IEnumerator ShowMenu()
        {
            var splash = Find<SplashScreen>();
            if (splash.IsVisible) splash.Finish();
            yield return null;
        }

        [UnityTest]
        public IEnumerator FirstLaunchPlaysTheSplashThenTheMenu()
        {
            var router = UIRouter.Instance;
            Assert.IsNotNull(router);
            // The splash plays once per app session; the test scene may have been entered before, so force the first-launch path.
            if (SplashScreen.PlayedThisSession) Assert.Pass("splash already played in this session; covered by SplashTimeline tests");
            Assert.IsTrue(router.Top is SplashScreen);
            Find<SplashScreen>().Finish();
            yield return null;
            Assert.IsTrue(router.Top is MainMenuScreen);
            Assert.AreEqual(1, router.Depth);
        }

        [UnityTest]
        public IEnumerator ContinueIsDisabledWithoutASaveAndEnabledWithOne()
        {
            yield return ShowMenu();
            var menu = Find<MainMenuScreen>();
            menu.UseHooks(() => false, null);
            Assert.IsFalse(menu.ContinueEnabled);
            menu.UseHooks(() => true, null);
            Assert.IsTrue(menu.ContinueEnabled);
        }

        [UnityTest]
        public IEnumerator ContinueFollowsTheRealSaveFile()
        {
            yield return ShowMenu();
            if (File.Exists(savePath)) File.Delete(savePath);
            var menu = Find<MainMenuScreen>();
            menu.RefreshContinue();
            Assert.IsFalse(menu.ContinueEnabled, "no save file: Continue is disabled");
            Assert.IsTrue(new SaveSystem().Save(GameState.NewGame()));
            menu.RefreshContinue();
            Assert.IsTrue(menu.ContinueEnabled, "a loadable save enables Continue");
            File.WriteAllText(savePath, "garbage");
            menu.RefreshContinue();
            Assert.IsFalse(menu.ContinueEnabled, "an unreadable save is not a save");
        }

        [UnityTest]
        public IEnumerator MenuButtonsOpenSettingsAboutAndBackReturns()
        {
            yield return ShowMenu();
            var router = UIRouter.Instance;
            var menu = Find<MainMenuScreen>();
            ButtonOf(menu, "Settings").onClick.Invoke();
            Assert.IsTrue(router.Top is SettingsScreen);
            Assert.AreEqual(2, router.Depth);
            router.Back();
            Assert.IsTrue(router.Top is MainMenuScreen);
            ButtonOf(menu, "About").onClick.Invoke();
            Assert.IsTrue(router.Top is CreditsScreen);
            router.Back();
            Assert.AreEqual(1, router.Depth);
        }

        [UnityTest]
        public IEnumerator NewGamePlaysTheIntroThenLaunchesAndSkipWorks()
        {
            yield return ShowMenu();
            var menu = Find<MainMenuScreen>();
            bool? launched = null;
            menu.UseHooks(() => false, newGame => launched = newGame); // no save: nothing to confirm
            ButtonOf(menu, "NewGame").onClick.Invoke();
            var intro = Find<IntroCutscene>();
            Assert.IsTrue(intro.IsVisible);
            Assert.IsNull(launched, "the intro comes before the game");
            yield return null;
            ButtonOf(intro, "Skip").onClick.Invoke();
            Assert.AreEqual(true, launched);
            Assert.IsTrue(intro.IsFinished);
        }

        [UnityTest]
        public IEnumerator NewGameOverASaveAsksFirstAndCancelKeepsTheMenu()
        {
            yield return ShowMenu();
            var router = UIRouter.Instance;
            var menu = Find<MainMenuScreen>();
            bool? launched = null;
            menu.UseHooks(() => true, newGame => launched = newGame);
            ButtonOf(menu, "NewGame").onClick.Invoke();
            var confirm = Find<ConfirmDialog>();
            Assert.IsTrue(router.Top is ConfirmDialog, "a save exists: ask before starting over");
            Assert.IsFalse(Find<IntroCutscene>().IsVisible);
            ButtonOf(confirm, "Cancel").onClick.Invoke();
            Assert.IsTrue(router.Top is MainMenuScreen);
            Assert.AreEqual(1, router.Depth);
            Assert.IsNull(launched);

            ButtonOf(menu, "NewGame").onClick.Invoke();
            router.Back(); // Android Back is a cancel too
            Assert.IsTrue(router.Top is MainMenuScreen);
            Assert.IsNull(launched);

            ButtonOf(menu, "NewGame").onClick.Invoke();
            ButtonOf(confirm, "Confirm").onClick.Invoke();
            var intro = Find<IntroCutscene>();
            Assert.IsTrue(router.Top is IntroCutscene, "confirmed: the intro plays");
            Assert.IsFalse(router.Contains(confirm));
            yield return null;
            ButtonOf(intro, "Skip").onClick.Invoke();
            Assert.AreEqual(true, launched);
        }

        [UnityTest]
        public IEnumerator IntroTypesAllFourCardsWhenTapped()
        {
            yield return ShowMenu();
            var menu = Find<MainMenuScreen>();
            menu.UseHooks(() => false, _ => { });
            ButtonOf(menu, "NewGame").onClick.Invoke();
            var intro = Find<IntroCutscene>();
            yield return null;
            for (int i = 0; i < 12 && !intro.IsFinished; i++) intro.Tap();
            Assert.IsTrue(intro.IsFinished);
        }

        [UnityTest]
        public IEnumerator ContinueLaunchesWithoutTheIntro()
        {
            yield return ShowMenu();
            var menu = Find<MainMenuScreen>();
            bool? launched = null;
            menu.UseHooks(() => true, newGame => launched = newGame);
            ButtonOf(menu, "Continue").onClick.Invoke();
            Assert.AreEqual(false, launched);
        }

        [UnityTest]
        public IEnumerator NewGameFromTheMenuLoadsCoreAndEntersTheWorld()
        {
            yield return ShowMenu();
            Assert.IsTrue(GameLauncher.Begin(true));
            float deadline = Time.realtimeSinceStartup + 40f;
            while ((GameManager.Instance == null || GameManager.Instance.Mode != GameMode.Playing) && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.IsNotNull(GameManager.Instance, "Core is loaded");
            Assert.AreEqual(GameMode.Playing, GameManager.Instance.Mode, "CoreLauncher started the new game");
            Assert.IsNotNull(GameObject.FindGameObjectWithTag(WorldTags.Player));
            Assert.AreEqual(GameLauncher.Request.None, GameLauncher.Pending, "the request was consumed");
            Assert.IsFalse(SceneLoader.IsLoaded("MainMenu"));
        }
    }
}
