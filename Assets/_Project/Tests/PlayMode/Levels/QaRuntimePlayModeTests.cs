using System.Collections;
using System.IO;
using System.Linq;
using AuraKnight.Core;
using AuraKnight.UI;
using AuraKnight.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace AuraKnight.Tests.PlayMode.Levels
{
    /// <summary>Phase 13 runtime checks in the real Core scene: app lifecycle save, listener, light budget, frame cap.</summary>
    public sealed class QaRuntimePlayModeTests : LevelsPlayModeBase
    {
        [TearDown]
        public void ResetPowerSaving() => GameSettings.PowerSaving = false;

        [UnityTest]
        public IEnumerator SendingTheAppToTheBackgroundSavesProgress()
        {
            yield return NewGame();
            Manager.State.coins = 77;
            Manager.State.lastAltarId = "hub_altar_01";
            Manager.SendMessage("OnApplicationPause", true);
            Assert.IsTrue(new SaveSystem(new FileSaveStorage(SavePath)).TryLoad(out var saved), "the pause wrote a loadable save");
            Assert.AreEqual(77, saved.coins);
            Assert.AreEqual("hub_altar_01", saved.lastAltarId);
        }

        [UnityTest]
        public IEnumerator ProgressSurvivesTheAppBeingKilledAfterTheBackgroundSave()
        {
            yield return NewGame();
            Manager.State.coins = 41;
            Manager.SendMessage("OnApplicationPause", true);
            Manager.State.coins = 999; // later play that never reached a save: the app is killed in the background
            Manager.StartNewGame();    // a fresh process knows nothing in memory
            Assert.IsTrue(Manager.Continue());
            Assert.AreEqual(41, Manager.State.coins, "the player resumes from the last save, not from nothing");
        }

        [UnityTest]
        public IEnumerator TheMenuDoesNotWriteASaveWhenTheAppGoesToTheBackground()
        {
            yield return null;
            Manager.SetMode(GameMode.Menu);
            Manager.SendMessage("OnApplicationPause", true);
            Assert.IsFalse(File.Exists(SavePath), "nothing to save before a game exists");
        }

        [UnityTest]
        public IEnumerator CoreHasOneAudioListenerOnTheMainCamera()
        {
            yield return null;
            var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
            Assert.AreEqual(1, listeners.Length);
            Assert.AreSame(Camera.main.gameObject, listeners[0].gameObject);
        }

        Light2D AddPointLight(Vector2 position)
        {
            var light = new GameObject("TestLight").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.transform.position = position;
            return light;
        }

        [UnityTest]
        public IEnumerator OnlyTheNearestLocalLightsStayLitAndPowerSavingTightensTheBudget()
        {
            yield return NewGame();
            var controller = Object.FindAnyObjectByType<LightBudgetController>();
            Assert.IsNotNull(controller, "Core has the light budget");
            var glow = Player.GetComponentInChildren<Light2D>();
            Assert.IsNotNull(glow, "Leo carries the Aura glow");
            Vector2 leo = PlayerPosition;
            var near = Enumerable.Range(0, 10).Select(i => AddPointLight(leo + new Vector2(2f + i, 1f))).ToArray();
            var far = Enumerable.Range(0, 5).Select(i => AddPointLight(leo + new Vector2(300f + i, 0f))).ToArray();
            try
            {
                controller.Refresh();
                Assert.LessOrEqual(LocalLit(), LightBudget.Normal, "never more than the budget");
                Assert.IsTrue(glow.enabled, "Leo's own light is always among the nearest");
                Assert.IsTrue(far.All(l => !l.enabled), "far lights are off");
                Assert.IsTrue(near[0].enabled && near[1].enabled, "the lights next to Leo stay on");

                GameSettings.PowerSaving = true;
                controller.Refresh();
                Assert.LessOrEqual(LocalLit(), LightBudget.PowerSaving, "power saving halves it");
                Assert.IsTrue(glow.enabled);

                GameSettings.PowerSaving = false;
                controller.Refresh();
                Assert.Greater(LocalLit(), LightBudget.PowerSaving, "the budget widens again");
                Assert.LessOrEqual(LocalLit(), LightBudget.Normal);
            }
            finally
            {
                foreach (var l in near.Concat(far)) if (l != null) Object.Destroy(l.gameObject);
            }
        }

        static int LocalLit() => Object.FindObjectsByType<Light2D>().Count(l => l.lightType != Light2D.LightType.Global && l.enabled);

        [UnityTest]
        public IEnumerator PowerSavingCapsTheFrameRateAtThirty()
        {
            yield return null;
            GameSettings.ApplyFrameRate();
            Assert.AreEqual(60, Application.targetFrameRate);
            GameSettings.PowerSaving = true;
            Assert.AreEqual(30, Application.targetFrameRate);
            GameSettings.PowerSaving = false;
            Assert.AreEqual(60, Application.targetFrameRate);
        }
    }
}
