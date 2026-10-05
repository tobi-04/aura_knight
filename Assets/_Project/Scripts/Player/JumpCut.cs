namespace AuraKnight.Player
{
    /// <summary>
    /// Variable jump height: releasing JUMP after the minimum hold time multiplies upward speed
    /// by <see cref="PlayerMovementConfig.JumpCutMultiplier"/>. An earlier release is deferred until the minimum hold.
    /// </summary>
    public sealed class JumpCut
    {
        const float Epsilon = 1e-4f;
        readonly PlayerMovementConfig _config;
        float _heldTime;
        bool _active;

        public JumpCut(PlayerMovementConfig config) => _config = config;

        public void Begin()
        {
            _heldTime = 0f;
            _active = true;
        }

        public void Cancel() => _active = false;

        public float Update(float velocityY, float deltaTime, bool jumpHeld)
        {
            if (!_active) return velocityY;
            if (velocityY <= 0f) { _active = false; return velocityY; }

            _heldTime += deltaTime;
            if (jumpHeld || _heldTime < _config.JumpMinHoldTime - Epsilon) return velocityY;

            _active = false;
            return velocityY * _config.JumpCutMultiplier;
        }
    }
}
