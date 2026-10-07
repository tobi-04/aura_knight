using AuraKnight.Core;
using UnityEngine;

namespace AuraKnight.UI
{
    /// <summary>
    /// Always-active listener in the Core scene that turns gameplay events into screens (screens themselves are inactive,
    /// so they cannot subscribe): death overlay, Aura unlock popup, boss banner, ending, loading cover, and Back with an
    /// empty stack opening the pause menu. It never touches player or manager objects, only the EventBus.
    /// </summary>
    public sealed class GameUiDirector : MonoBehaviour
    {
        [SerializeField] UIRouter router;
        [SerializeField] PauseController pause;
        [SerializeField] GameOverScreen gameOver;
        [SerializeField] AuraUnlockPopup unlockPopup;
        [SerializeField] BossIntroBanner bossBanner;
        [SerializeField] CreditsScreen ending;
        [SerializeField] UIScreen loading;

        string regionId;

        public void Bind(UIRouter uiRouter, PauseController pauseController, GameOverScreen over, AuraUnlockPopup popup,
            BossIntroBanner banner, CreditsScreen endingScreen, UIScreen loadingScreen)
        {
            router = uiRouter;
            pause = pauseController;
            gameOver = over;
            unlockPopup = popup;
            bossBanner = banner;
            ending = endingScreen;
            loading = loadingScreen;
        }

        void OnEnable()
        {
            EventBus.Subscribe<PlayerDied>(OnDied);
            EventBus.Subscribe<PlayerRespawned>(OnRespawned);
            EventBus.Subscribe<GameStateLoaded>(OnStateLoaded);
            EventBus.Subscribe<AuraUnlocked>(OnAuraUnlocked);
            EventBus.Subscribe<BossEncounterStarted>(OnBossStarted);
            EventBus.Subscribe<GameCompleted>(OnGameCompleted);
            EventBus.Subscribe<RoomEntered>(OnRoom);
            router.BackOnEmpty += OnBackOnEmpty;
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<PlayerDied>(OnDied);
            EventBus.Unsubscribe<PlayerRespawned>(OnRespawned);
            EventBus.Unsubscribe<GameStateLoaded>(OnStateLoaded);
            EventBus.Unsubscribe<AuraUnlocked>(OnAuraUnlocked);
            EventBus.Unsubscribe<BossEncounterStarted>(OnBossStarted);
            EventBus.Unsubscribe<GameCompleted>(OnGameCompleted);
            EventBus.Unsubscribe<RoomEntered>(OnRoom);
            if (router != null) router.BackOnEmpty -= OnBackOnEmpty;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            bool wantLoading = gm != null && gm.Mode == GameMode.Loading;
            if (wantLoading && !loading.IsVisibleScreen()) loading.Show();
            else if (!wantLoading && loading.IsVisibleScreen()) loading.Hide();
        }

        void OnDied(PlayerDied e) => gameOver.Show();
        void OnRespawned(PlayerRespawned e) => gameOver.Hide();
        void OnStateLoaded(GameStateLoaded e) => gameOver.Hide(true);
        void OnRoom(RoomEntered e) => regionId = e.RegionId;

        void OnAuraUnlocked(AuraUnlocked e)
        {
            bool wasVisible = unlockPopup.IsVisible;
            unlockPopup.Present(e.AuraId);
            if (!wasVisible) router.Push(unlockPopup);
        }

        void OnBossStarted(BossEncounterStarted e) =>
            bossBanner.Present(e.DisplayName, UITheme.Active.RegionColor(regionId));

        void OnGameCompleted(GameCompleted e) => router.Push(ending);

        void OnBackOnEmpty()
        {
            if (pause != null && pause.CanPause && !gameOver.IsVisible) pause.Pause();
        }
    }

    static class UIScreenExtensions
    {
        public static bool IsVisibleScreen(this UIScreen screen) => screen != null && screen.IsVisible;
    }
}
