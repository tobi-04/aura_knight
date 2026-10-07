using AuraKnight.Core;

namespace AuraKnight.UI
{
    /// <summary>
    /// Remembers what a pause must undo: the gameplay time scale (HitStop.GameplayScale, never a hit-stop freeze value)
    /// and the mode before pausing. Pure state, so the resume contract is unit tested.
    /// </summary>
    public sealed class PauseGate
    {
        float savedScale = 1f;
        GameMode savedMode = GameMode.Playing;

        public bool IsPaused { get; private set; }

        /// <summary>Captures the state. False when already paused (the first capture is kept).</summary>
        public bool Begin(float gameplayScale, GameMode currentMode)
        {
            if (IsPaused) return false;
            savedScale = gameplayScale > 0f ? gameplayScale : 1f;
            savedMode = currentMode == GameMode.Paused ? GameMode.Playing : currentMode;
            IsPaused = true;
            return true;
        }

        public bool End(out float scale, out GameMode mode)
        {
            scale = savedScale;
            mode = savedMode;
            if (!IsPaused) return false;
            IsPaused = false;
            return true;
        }
    }
}
