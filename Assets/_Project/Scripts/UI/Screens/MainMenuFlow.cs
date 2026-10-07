using UnityEngine;

namespace AuraKnight.UI
{
    /// <summary>MainMenu scene entry: splash on the first launch of the session, then the main menu as the root screen.</summary>
    public sealed class MainMenuFlow : MonoBehaviour
    {
        [SerializeField] UIRouter router;
        [SerializeField] SplashScreen splash;
        [SerializeField] MainMenuScreen menu;

        public void Bind(UIRouter uiRouter, SplashScreen splashScreen, MainMenuScreen mainMenu)
        {
            router = uiRouter;
            splash = splashScreen;
            menu = mainMenu;
        }

        void Start()
        {
            GameSettings.ApplyAll();
            if (splash == null || SplashScreen.PlayedThisSession)
            {
                router.Push(menu);
                return;
            }
            splash.Finished += OnSplashFinished;
            router.Push(splash);
        }

        void OnDestroy()
        {
            if (splash != null) splash.Finished -= OnSplashFinished;
        }

        void OnSplashFinished()
        {
            splash.Finished -= OnSplashFinished;
            router.Remove(splash);
            router.Push(menu);
        }
    }
}
