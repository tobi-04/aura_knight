using System;

namespace AuraKnight.World.Hazards
{
    public enum CollapsePhase
    {
        Solid,
        Shaking,
        Collapsed,
    }

    /// <summary>
    /// Pure timing of a collapsing platform (GDD 7.2): stepped on, it shakes for <see cref="Delay"/> (0.6 s), is gone for
    /// <see cref="RespawnSeconds"/> (3 s), then solid again. <see cref="Tick"/> reports the phase changes.
    /// </summary>
    public sealed class CollapseCycle
    {
        public const float DefaultDelay = 0.6f;
        public const float DefaultRespawn = 3f;

        public CollapseCycle(float delay = DefaultDelay, float respawnSeconds = DefaultRespawn)
        {
            Delay = Math.Max(0.05f, delay);
            RespawnSeconds = Math.Max(0.05f, respawnSeconds);
        }

        public float Delay { get; }
        public float RespawnSeconds { get; }
        public CollapsePhase Phase { get; private set; }
        /// <summary>Seconds spent in the current phase.</summary>
        public float Elapsed { get; private set; }

        /// <summary>Starts the countdown. Only a solid platform reacts (a shaking or fallen one ignores further steps).</summary>
        public bool Trigger()
        {
            if (Phase != CollapsePhase.Solid) return false;
            Enter(CollapsePhase.Shaking);
            return true;
        }

        /// <summary>Advances time; true when the phase changed during this call.</summary>
        public bool Tick(float deltaTime)
        {
            if (Phase == CollapsePhase.Solid || deltaTime <= 0f) return false;
            Elapsed += deltaTime;
            if (Phase == CollapsePhase.Shaking && Elapsed >= Delay) { Enter(CollapsePhase.Collapsed); return true; }
            if (Phase == CollapsePhase.Collapsed && Elapsed >= RespawnSeconds) { Enter(CollapsePhase.Solid); return true; }
            return false;
        }

        /// <summary>Back to solid without waiting (room restart).</summary>
        public void Reset() => Enter(CollapsePhase.Solid);

        void Enter(CollapsePhase phase)
        {
            Phase = phase;
            Elapsed = 0f;
        }
    }
}
