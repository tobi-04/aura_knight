using System;

namespace AuraKnight.World.Hazards
{
    public enum StalactitePhase
    {
        Hanging,
        Shaking,
        Falling,
        Resting,
    }

    /// <summary>
    /// Pure phases of a falling stalactite (GDD 7.2): it shakes 0.5 s once Leo is under it, falls until it lands, then rests
    /// <see cref="RespawnSeconds"/> before it hangs again.
    /// </summary>
    public sealed class StalactiteCycle
    {
        public const float DefaultShake = 0.5f;
        public const float DefaultRespawn = 4f;

        public StalactiteCycle(float shakeSeconds = DefaultShake, float respawnSeconds = DefaultRespawn)
        {
            ShakeSeconds = Math.Max(0.05f, shakeSeconds);
            RespawnSeconds = Math.Max(0.05f, respawnSeconds);
        }

        public float ShakeSeconds { get; }
        public float RespawnSeconds { get; }
        public StalactitePhase Phase { get; private set; }
        public float Elapsed { get; private set; }

        /// <summary>Leo is below: only a hanging stalactite starts shaking.</summary>
        public bool Arm()
        {
            if (Phase != StalactitePhase.Hanging) return false;
            Enter(StalactitePhase.Shaking);
            return true;
        }

        /// <summary>Advances the shake and rest timers; true when the phase changed.</summary>
        public bool Tick(float deltaTime)
        {
            if (deltaTime <= 0f || Phase == StalactitePhase.Hanging || Phase == StalactitePhase.Falling) return false;
            Elapsed += deltaTime;
            if (Phase == StalactitePhase.Shaking && Elapsed >= ShakeSeconds) { Enter(StalactitePhase.Falling); return true; }
            if (Phase == StalactitePhase.Resting && Elapsed >= RespawnSeconds) { Enter(StalactitePhase.Hanging); return true; }
            return false;
        }

        /// <summary>It hit the ground (or left the room): rest until the respawn time.</summary>
        public bool Land()
        {
            if (Phase != StalactitePhase.Falling) return false;
            Enter(StalactitePhase.Resting);
            return true;
        }

        public void Reset() => Enter(StalactitePhase.Hanging);

        void Enter(StalactitePhase phase)
        {
            Phase = phase;
            Elapsed = 0f;
        }
    }
}
