using System;
using System.Collections;
using System.IO;
using AuraKnight.Aura;
using AuraKnight.Combat;
using AuraKnight.Core;
using AuraKnight.Player;
using AuraKnight.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AuraKnight.Tests.PlayMode
{
    /// <summary>
    /// Loads the real Core scene (managers, camera, WorldEntry) and the real region scenes through the build settings.
    /// The save goes to a temp file so the developer's own save is never touched.
    /// </summary>
    public abstract class WorldPlayTestBase
    {
        const float DefaultTimeout = 30f;

        string _dir;
        protected GameManager Manager => GameManager.Instance;
        protected WorldEntry Entry => WorldEntry.Instance;
        protected string SavePath => Path.Combine(_dir, "save_0.json");

        [UnitySetUp]
        public IEnumerator LoadCore()
        {
            EventBus.Clear(); // before Core loads: its managers subscribe in OnEnable
            _dir = Path.Combine(Application.temporaryCachePath, "aura_playmode_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            yield return SceneManager.LoadSceneAsync("Core", LoadSceneMode.Single);
            yield return null;
            Assert.IsNotNull(Manager, "Core scene has a GameManager");
            Assert.IsNotNull(Entry, "Core scene has a WorldEntry");
            Manager.UseSaveSystem(new SaveSystem(new FileSaveStorage(SavePath)));
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1f;
            EventBus.Clear();
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
            yield return null;
        }

        protected static IEnumerator WaitUntil(Func<bool> condition, string what, float timeout = DefaultTimeout)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline) Assert.Fail($"Timed out after {timeout}s waiting for: {what}");
                yield return null;
            }
        }

        /// <summary>Starts the entry flow and waits until it reports back; returns through <paramref name="result"/>.</summary>
        protected IEnumerator Enter(bool newGame, Action<bool> result)
        {
            bool? done = null;
            Action<bool> handler = ok => done = ok;
            Entry.Entered += handler;
            bool started = newGame ? Entry.StartNewGame() : Entry.Continue();
            if (!started)
            {
                Entry.Entered -= handler;
                result(false);
                yield break;
            }
            yield return WaitUntil(() => done.HasValue, "world entry to finish");
            Entry.Entered -= handler;
            yield return null; // let the player's Start() methods run
            result(done.Value);
        }

        /// <summary>
        /// Lets physics run so the altar trigger Leo spawned inside has reported (it sets the checkpoint); tests that
        /// rewrite lastAltarId must do so afterwards or the trigger would overwrite their value.
        /// </summary>
        protected static IEnumerator SettlePhysics()
        {
            float until = Time.realtimeSinceStartup + 0.5f;
            while (Time.realtimeSinceStartup < until) yield return null;
        }

        protected static GameObject Player => GameObject.FindGameObjectWithTag(WorldTags.Player);

        protected static Vector2 PlayerPosition => Player.GetComponent<KinematicMotor2D>().Position;

        protected static Vector2 AltarSpawn(string altarId)
        {
            Assert.IsTrue(SunAltar.TryFind(altarId, out var altar), $"altar '{altarId}' is loaded");
            return altar.SpawnPosition;
        }

        protected static void AssertNear(Vector2 expected, Vector2 actual, string what, float tolerance = 1.0f) =>
            Assert.Less(Vector2.Distance(expected, actual), tolerance, $"{what}: expected near {expected}, was {actual}");
    }
}
