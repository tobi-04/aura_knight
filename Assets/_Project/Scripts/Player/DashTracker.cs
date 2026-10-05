namespace AuraKnight.Player
{
    /// <summary>Dash availability rules: cooldown, a single air dash, and the early i-frame window.</summary>
    public sealed class DashTracker
    {
        readonly PlayerMovementConfig _config;
        readonly Countdown _cooldown = new Countdown();
        readonly Countdown _invulnerable = new Countdown();
        bool _airDashUsed;

        public DashTracker(PlayerMovementConfig config) => _config = config;

        public bool IsInvulnerable => _invulnerable.IsActive;

        public bool CanDash(bool grounded) => !_cooldown.IsActive && (grounded || !_airDashUsed);

        public void Begin(bool grounded)
        {
            _cooldown.Start(_config.DashCooldown);
            _invulnerable.Start(_config.DashInvulnerableTime);
            if (!grounded) _airDashUsed = true;
        }

        /// <summary>Called on landing and on wall contact.</summary>
        public void ResetAir() => _airDashUsed = false;

        public void Tick(float deltaTime)
        {
            _cooldown.Tick(deltaTime);
            _invulnerable.Tick(deltaTime);
        }
    }
}
