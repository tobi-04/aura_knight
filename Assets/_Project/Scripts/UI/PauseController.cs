using AuraKnight.Combat;
using AuraKnight.Core;
using UnityEngine;

namespace AuraKnight.UI
{
    /// <summary>
    /// Pauses gameplay: GameMode.Paused + Time.timeScale 0, and on resume restores the scale captured when pausing
    /// (<see cref="HitStop.GameplayScale"/>, so a hit-stop freeze in progress is never mistaken for the normal speed).
    /// The pause screen is optional: popups (Aura unlock) pause without it.
    /// </summary>
    public sealed class PauseController : MonoBehaviour
    {
        [SerializeField] UIScreen pauseScreen;
        [SerializeField] UIRouter router;

        readonly PauseGate gate = new();

        public static PauseController Instance { get; private set; }
        public bool IsPaused => gate.IsPaused;

        /// <summary>Only while playing (or in a test scene without a GameManager).</summary>
        public bool CanPause => !gate.IsPaused && GameManager.ModeAllowsControl;

        public void Bind(UIScreen screen, UIRouter uiRouter)
        {
            pauseScreen = screen;
            router = uiRouter;
        }

        void OnEnable() => Instance = this;

        void OnDisable()
        {
            if (Instance == this) Instance = null;
            if (gate.IsPaused) Resume(); // never leave the game frozen when this object goes away
        }

        public bool Pause(bool showScreen = true)
        {
            if (!CanPause || !gate.Begin(HitStop.GameplayScale, CurrentMode())) return false;
            Time.timeScale = 0f;
            GameManager.Instance?.SetMode(GameMode.Paused);
            EventBus.Publish(new PauseStateChanged(true));
            if (showScreen && router != null && pauseScreen != null) router.Push(pauseScreen);
            return true;
        }

        public bool Resume()
        {
            if (!gate.End(out float scale, out GameMode mode)) return false;
            Time.timeScale = scale;
            GameManager.Instance?.SetMode(mode);
            if (router != null && pauseScreen != null) router.Remove(pauseScreen);
            EventBus.Publish(new PauseStateChanged(false));
            return true;
        }

        /// <summary>Start button: pauses, or resumes when the pause screen itself is on top (not while Settings sits above it).</summary>
        public void Toggle()
        {
            if (!gate.IsPaused) Pause();
            else if (router == null || pauseScreen == null || ReferenceEquals(router.Top, pauseScreen)) Resume();
        }

        static GameMode CurrentMode() => GameManager.Instance != null ? GameManager.Instance.Mode : GameMode.Playing;
    }
}
