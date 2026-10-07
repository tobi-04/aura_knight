using System;
using AuraKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AuraKnight.UI
{
    /// <summary>
    /// Slide-1 main menu: TIẾP TỤC (disabled without a loadable save) / TRÒ CHƠI MỚI (intro cutscene, then the world) /
    /// CÀI ĐẶT / GIỚI THIỆU. Back on the root menu quits (Android convention).
    /// </summary>
    public sealed class MainMenuScreen : UIScreen
    {
        [SerializeField] UIRouter router;
        [SerializeField] UIButton continueButton, newGameButton, settingsButton, aboutButton;
        [SerializeField] UIScreen settings;
        [SerializeField] CreditsScreen credits;
        [SerializeField] IntroCutscene intro;

        Func<bool> saveProbe = () => new SaveSystem().HasSave;
        Action<bool> launch = newGame => GameLauncher.Begin(newGame);

        public bool ContinueEnabled => continueButton != null && continueButton.interactable;

        public void Bind(UIRouter uiRouter, UIButton cont, UIButton newGame, UIButton settingsBtn, UIButton about,
            UIScreen settingsScreen, CreditsScreen creditsScreen, IntroCutscene introCutscene)
        {
            router = uiRouter;
            continueButton = cont;
            newGameButton = newGame;
            settingsButton = settingsBtn;
            aboutButton = about;
            settings = settingsScreen;
            credits = creditsScreen;
            intro = introCutscene;
        }

        /// <summary>Test seam: replaces the save check and the scene launch.</summary>
        public void UseHooks(Func<bool> hasSave, Action<bool> launchGame)
        {
            if (hasSave != null) saveProbe = hasSave;
            if (launchGame != null) launch = launchGame;
            RefreshContinue();
        }

        protected override void Awake()
        {
            base.Awake();
            continueButton.onClick.AddListener(() => launch(false));
            newGameButton.onClick.AddListener(StartNewGame);
            settingsButton.onClick.AddListener(() => router.Push(settings, true));
            aboutButton.onClick.AddListener(() => router.Push(credits, true));
        }

        protected override void OnShowing() => RefreshContinue();

        public void RefreshContinue()
        {
            if (continueButton != null) continueButton.interactable = saveProbe();
        }

        void StartNewGame()
        {
            if (intro == null)
            {
                launch(true);
                return;
            }
            intro.Completed += OnIntroCompleted;
            router.Push(intro, true);
        }

        void OnIntroCompleted()
        {
            intro.Completed -= OnIntroCompleted;
            launch(true);
        }

        public override bool HandleBack()
        {
            if (router != null && router.Depth > 1) return false;
            Application.Quit();
            return true;
        }
    }
}
