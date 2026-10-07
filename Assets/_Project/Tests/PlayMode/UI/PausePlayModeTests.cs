using System.Collections;
using System.Linq;
using AuraKnight.Combat;
using AuraKnight.Core;
using AuraKnight.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace AuraKnight.Tests.PlayMode.UI
{
    public sealed class PausePlayModeTests : UiPlayModeBase
    {
        IEnumerator StartGame()
        {
            bool ok = false;
            yield return Enter(true, r => ok = r);
            Assert.IsTrue(ok, "new game entered");
            yield return SettlePhysics();
        }

        [UnityTest]
        public IEnumerator PauseFreezesTimeAndResumeRestoresTheCapturedScale()
        {
            yield return StartGame();
            var pause = PauseController.Instance;
            Time.timeScale = 0.5f; // gameplay running at a non-default scale
            Assert.IsTrue(pause.Pause());
            Assert.AreEqual(0f, Time.timeScale);
            Assert.AreEqual(GameMode.Paused, Manager.Mode);
            Assert.IsTrue(UIRouter.Instance.Top is PauseScreen);
            Assert.IsFalse(pause.Pause(), "already paused");
            Assert.IsTrue(pause.Resume());
            Assert.AreEqual(0.5f, Time.timeScale);
            Assert.AreEqual(GameMode.Playing, Manager.Mode);
            Assert.AreEqual(0, UIRouter.Instance.Depth);
        }

        [UnityTest]
        public IEnumerator PausingDuringAHitStopResumesToGameplaySpeedNotTheFreezeValue()
        {
            yield return StartGame();
            Time.timeScale = 1f;
            HitStop.Request(2f);
            yield return null;
            Assert.IsTrue(HitStop.IsFrozen);
            Assert.Less(Time.timeScale, 1f, "hit-stop lowers the scale");
            Assert.IsTrue(PauseController.Instance.Pause());
            yield return null;
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(PauseController.Instance.Resume());
            Assert.AreEqual(1f, Time.timeScale, "resumes to the scale captured before the freeze");
        }

        [UnityTest]
        public IEnumerator CannotPauseOutsideGameplay()
        {
            Assert.AreNotEqual(GameMode.Playing, Manager.Mode);
            Assert.IsFalse(PauseController.Instance.CanPause);
            Assert.IsFalse(PauseController.Instance.Pause());
            Assert.AreEqual(1f, Time.timeScale);
            yield break;
        }

        [UnityTest]
        public IEnumerator BackWithNothingOpenPausesAndBackPopsOneScreenAtATime()
        {
            yield return StartGame();
            var router = UIRouter.Instance;
            router.Back();
            Assert.AreEqual(GameMode.Paused, Manager.Mode, "Back on an empty stack opens Pause");
            Assert.AreEqual(1, router.Depth);

            var pauseScreen = Find<PauseScreen>();
            Button(pauseScreen, "Settings").onClick.Invoke();
            Assert.AreEqual(2, router.Depth);
            Assert.IsTrue(router.Top is SettingsScreen);

            router.Back();
            Assert.AreEqual(1, router.Depth, "Back on Settings pops it");
            Assert.IsTrue(router.Top is PauseScreen);
            Assert.AreEqual(GameMode.Paused, Manager.Mode);

            Button(pauseScreen, "Aura").onClick.Invoke();
            Assert.IsTrue(router.Top is AuraInfoScreen);
            router.Back();
            Assert.IsTrue(router.Top is PauseScreen);

            router.Back();
            Assert.AreEqual(0, router.Depth, "Back on Pause resumes");
            Assert.AreEqual(GameMode.Playing, Manager.Mode);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator TheBackKeyIsEscapeOnAndroidAndWorksLikeBack()
        {
            yield return StartGame();
            // The batch-mode editor has no focus; without this the keyboard would drop the queued events.
            var settings = InputSystem.settings;
            var background = settings.backgroundBehavior;
            var editorBehavior = settings.editorInputBehaviorInPlayMode;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            Assert.AreEqual(GameMode.Paused, Manager.Mode, "Back with nothing open pauses");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            Assert.AreEqual(GameMode.Playing, Manager.Mode, "Back on the pause screen resumes");
            settings.backgroundBehavior = background;
            settings.editorInputBehaviorInPlayMode = editorBehavior;
        }

        [UnityTest]
        public IEnumerator PauseButtonsResumeAndReturnToTheMenu()
        {
            yield return StartGame();
            PauseController.Instance.Pause();
            Button(Find<PauseScreen>(), "Resume").onClick.Invoke();
            Assert.AreEqual(GameMode.Playing, Manager.Mode);

            PauseController.Instance.Pause();
            Button(Find<PauseScreen>(), "Menu").onClick.Invoke();
            yield return WaitUntil(() => SceneLoader.IsLoaded("MainMenu"), "main menu to load");
            yield return null;
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(SceneLoader.IsLoaded("Core"), "the world is unloaded with Core");
        }

        [UnityTest]
        public IEnumerator AuraUnlockPopupPausesAndQueuesSeveralUnlocks()
        {
            yield return StartGame();
            var popup = Find<AuraUnlockPopup>();
            EventBus.Publish(new AuraUnlocked("Wind"));
            Assert.IsTrue(popup.IsVisible);
            Assert.AreEqual("Wind", popup.CurrentAuraId);
            Assert.AreEqual(GameMode.Paused, Manager.Mode);
            Assert.AreEqual(0f, Time.timeScale);
            StringAssert.Contains("Aura Gió", popup.GetComponentsInChildren<TMPro.TMP_Text>(true).First(t => t.name == "Name").text);

            EventBus.Publish(new AuraUnlocked("Fire"));
            Assert.AreEqual(1, popup.QueuedCount);
            Assert.AreEqual("Wind", popup.CurrentAuraId, "still showing the first one");

            Button(popup, "Continue").onClick.Invoke();
            Assert.AreEqual("Fire", popup.CurrentAuraId);
            Assert.AreEqual(GameMode.Paused, Manager.Mode);

            Button(popup, "Continue").onClick.Invoke();
            Assert.IsFalse(popup.IsVisible);
            Assert.AreEqual(GameMode.Playing, Manager.Mode);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator AuraInfoShowsLockedAurasAsUnknownAndUnlockedOnesWithTheirAbilities()
        {
            yield return StartGame();
            Manager.State.unlockedAuras.Add("Fire");
            Manager.State.currentAura = "Fire";
            PauseController.Instance.Pause();
            Button(Find<PauseScreen>(), "Aura").onClick.Invoke();
            var info = Find<AuraInfoScreen>();
            Assert.AreEqual(1, info.SelectedIndex, "opens on the current Aura");
            Assert.IsFalse(info.Panel.ShowingLocked);
            Assert.AreEqual("Aura Hỏa", info.Panel.TitleText);
            Button(info, "TabWind").onClick.Invoke();
            Assert.IsTrue(info.Panel.ShowingLocked);
            Assert.AreEqual("???", info.Panel.TitleText);
            Button(info, "TabWater").onClick.Invoke();
            Assert.AreEqual(2, info.SelectedIndex);
            PauseController.Instance.Resume();
        }

        [UnityTest]
        public IEnumerator SettingsScreenWritesPlayerPrefsAndTogglesHaptics()
        {
            yield return StartGame();
            PauseController.Instance.Pause();
            Button(Find<PauseScreen>(), "Settings").onClick.Invoke();
            var settings = Find<SettingsScreen>();
            settings.MusicSlider.value = 0.3f;
            Assert.AreEqual(0.3f, PlayerPrefs.GetFloat(SettingsKeys.MusicVolume), 1e-4f);
            settings.ButtonScaleSlider.value = 1.2f;
            Assert.AreEqual(1.2f, GameSettings.ButtonScale, 1e-4f);
            bool before = GameSettings.HapticsEnabled;
            Button(settings, "Value").onClick.Invoke(); // first "Value" button is the haptics toggle
            Assert.AreNotEqual(before, GameSettings.HapticsEnabled);
            PauseController.Instance.Resume();
        }

        [UnityTest]
        public IEnumerator EndingScreenOpensWhenTheGameIsCompleted()
        {
            yield return StartGame();
            EventBus.Publish(new GameCompleted());
            var ending = Find<CreditsScreen>();
            Assert.IsTrue(ending.EndingMode);
            Assert.IsTrue(ending.IsVisible);
            Assert.IsTrue(UIRouter.Instance.Top is CreditsScreen);
            StringAssert.Contains("AURA KNIGHT", ending.BodyText);
        }
    }
}
