namespace AuraKnight.Combat
{
    /// <summary>
    /// Two-swing sword combo. After a swing ends a <see cref="Window"/> opens; a new swing inside it is the
    /// next step, otherwise the combo restarts at step 1. Pure logic, driven by <see cref="Tick"/>.
    /// </summary>
    public sealed class ComboTracker
    {
        public const int MaxSteps = 2;
        public const float Window = 0.3f;

        float _window;

        /// <summary>Last swing started (0 = none).</summary>
        public int Step { get; private set; }

        public bool CanContinue => Step > 0 && Step < MaxSteps && _window > 0f;

        /// <summary>Starts a swing and returns its step number (1 or 2).</summary>
        public int Begin()
        {
            Step = CanContinue ? Step + 1 : 1;
            _window = 0f;
            return Step;
        }

        /// <summary>Opens the follow-up window; finishing the last step starts the combo over.</summary>
        public void EndSwing()
        {
            if (Step >= MaxSteps) Reset();
            else if (Step > 0) _window = Window;
        }

        public void Tick(float deltaTime)
        {
            if (_window <= 0f) return;
            _window -= deltaTime;
            if (_window <= 0f) Reset();
        }

        public void Reset()
        {
            Step = 0;
            _window = 0f;
        }
    }
}
