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
        const int TargetFrameRate = 60;

        void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        void Start()
        {
            SceneManager.LoadScene(MainMenuScene);
        }
    }
}
