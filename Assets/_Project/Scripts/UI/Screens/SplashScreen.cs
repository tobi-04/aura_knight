using System;
using UnityEngine;
using UnityEngine.UI;

namespace AuraKnight.UI
{
    /// <summary>"Aura Studio" then "AURA KNIGHT" fading in and out (see <see cref="SplashTimeline"/>). A tap skips it.</summary>
    public sealed class SplashScreen : UIScreen
    {
        [SerializeField] CanvasGroup studio;
        [SerializeField] CanvasGroup title;
        [SerializeField] Button skipArea;

        float elapsed;
        bool done;

        /// <summary>The splash plays once per app launch; coming back from a game goes straight to the menu.</summary>
        public static bool PlayedThisSession { get; private set; }

        public event Action Finished;

        public void Bind(CanvasGroup studioGroup, CanvasGroup titleGroup, Button skip)
        {
            studio = studioGroup;
            title = titleGroup;
            skipArea = skip;
        }

        protected override void Awake()
        {
            base.Awake();
            if (skipArea != null) skipArea.onClick.AddListener(Finish);
        }

        protected override void OnShowing()
        {
            elapsed = 0f;
            done = false;
            Paint();
        }

        protected override void Update()
        {
            base.Update();
            if (done || !IsVisible) return;
            elapsed += Time.unscaledDeltaTime;
            Paint();
            if (SplashTimeline.IsDone(elapsed)) Finish();
        }

        public void Finish()
        {
            if (done) return;
            done = true;
            PlayedThisSession = true;
            Finished?.Invoke();
        }

        void Paint()
        {
            if (studio != null) studio.alpha = SplashTimeline.StudioAlpha(elapsed);
            if (title != null) title.alpha = SplashTimeline.TitleAlpha(elapsed);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => PlayedThisSession = false;
    }
}
