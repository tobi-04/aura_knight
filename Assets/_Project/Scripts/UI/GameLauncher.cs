using AuraKnight.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AuraKnight.UI
{
    /// <summary>
    /// Menu to world hand-off. The menu records what the player chose and loads the Core scene; <see cref="CoreLauncher"/>
    /// (in Core) consumes it and calls WorldEntry.StartNewGame / Continue. Also the way back: <see cref="ReturnToMenu"/>.
    /// </summary>
    public static class GameLauncher
    {
        public const string CoreScene = "Core";
        public const string MenuScene = "MainMenu";

        public enum Request { None, NewGame, Continue }

        public static Request Pending { get; private set; }

        /// <summary>Remembers the choice and loads Core (single mode, so the menu scene goes away).</summary>
        public static bool Begin(bool newGame)
        {
            if (!Application.CanStreamedLevelBeLoaded(CoreScene))
            {
                Debug.LogError($"[GameLauncher] Scene '{CoreScene}' is not in Build Settings.");
                return false;
            }
            Pending = newGame ? Request.NewGame : Request.Continue;
            SceneManager.LoadScene(CoreScene);
            return true;
        }

        /// <summary>Takes the pending request (clearing it). None when the Core scene was opened directly.</summary>
        public static Request Consume()
        {
            var request = Pending;
            Pending = Request.None;
            return request;
        }

        /// <summary>Saves, restores time and the mode, and loads the menu (which unloads Core and every region).</summary>
        public static void ReturnToMenu()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                if (gm.Mode != GameMode.Menu && gm.Mode != GameMode.Loading) gm.Save();
                gm.SetMode(GameMode.Menu);
            }
            Time.timeScale = 1f;
            Pending = Request.None;
            if (Application.CanStreamedLevelBeLoaded(MenuScene)) SceneManager.LoadScene(MenuScene);
            else Debug.LogError($"[GameLauncher] Scene '{MenuScene}' is not in Build Settings.");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Pending = Request.None;
    }
}
