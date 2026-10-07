using UnityEngine;

namespace AuraKnight.UI
{
    /// <summary>Pause panel: TIẾP TỤC / AURA / CÀI ĐẶT / VỀ MENU. The pause itself is owned by <see cref="PauseController"/>.</summary>
    public sealed class PauseScreen : UIScreen
    {
        [SerializeField] UIRouter router;
        [SerializeField] UIButton resumeButton, auraButton, settingsButton, menuButton;
        [SerializeField] UIScreen auraInfo, settings;

        public void Bind(UIRouter uiRouter, UIButton resume, UIButton aura, UIButton settingsBtn, UIButton menu,
            UIScreen auraScreen, UIScreen settingsScreen)
        {
            router = uiRouter;
            resumeButton = resume;
            auraButton = aura;
            settingsButton = settingsBtn;
            menuButton = menu;
            auraInfo = auraScreen;
            settings = settingsScreen;
        }

        protected override void Awake()
        {
            base.Awake();
            resumeButton.onClick.AddListener(Resume);
            auraButton.onClick.AddListener(() => router.Push(auraInfo, true));
            settingsButton.onClick.AddListener(() => router.Push(settings, true));
            menuButton.onClick.AddListener(GameLauncher.ReturnToMenu);
        }

        void Resume() => PauseController.Instance?.Resume();

        /// <summary>Back on the pause panel resumes the game.</summary>
        public override bool HandleBack()
        {
            Resume();
            return true;
        }
    }
}
