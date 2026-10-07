using UnityEngine;
using UnityEngine.SceneManagement;

namespace AuraKnight.Core
{
    /// <summary>
    /// Entry point living in the Boot scene (build index 0).
    /// Applies global runtime settings, then hands over to the main menu.
    /// </summary>
    public sealed class Bootstrapper : MonoBehaviour
    {
        public const string MainMenuScene = "MainMenu";
        void Awake()
        {
            QualitySettings.vSyncCount = 0; // Android ignores targetFrameRate while vsync is on
            AuraKnight.UI.GameSettings.ApplyFrameRate(); // 60, or 30 when the player chose power saving last session
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        void Start()
        {
            SceneManager.LoadScene(MainMenuScene);
        }
    }
}
