using System;

namespace AuraKnight.World.Hazards
{
    public enum PistonPhase
    {
        Retracted,
        Telegraph,
        Striking,
        Retracting,
    }

    /// <summary>
    /// Pure timing of a crushing piston (GDD 7.2): rests retracted, shakes for the telegraph, slams down (<see cref="Extension"/>
    /// goes 0 to 1 within <see cref="StrikeSeconds"/> and holds), then retracts. <see cref="IsDangerous"/> is true while the head is
    /// mostly out. A start offset staggers neighbouring pistons.
    /// </summary>
    public sealed class PistonCycle
    {
        public const float DefaultRest = 1.4f;
        public const float DefaultTelegraph = 0.6f;
        public const float DefaultStrike = 0.15f;
        public const float DefaultHold = 0.25f;
        public const float DefaultRetract = 0.45f;
        public const float DangerFrom = 0.5f;

        readonly float _rest, _telegraph, _strike, _hold, _retract;
        float _clock;

        public PistonCycle(float rest = DefaultRest, float telegraph = DefaultTelegraph, float strike = DefaultStrike,
                           float hold = DefaultHold, float retract = DefaultRetract, float startOffset = 0f)
        {
            _rest = Math.Max(0.05f, rest);
            _telegraph = Math.Max(0.5f, telegraph); // never an unreadable slam: the boss telegraph floor is 0.5 s too
            _strike = Math.Max(0.02f, strike);
            _hold = Math.Max(0f, hold);
            _retract = Math.Max(0.05f, retract);
            Period = _rest + _telegraph + _strike + _hold + _retract;
            _clock = Positive(startOffset, Period);
        }

        public float Period { get; }

        public PistonPhase Phase
        {
            get
            {
                float t = _clock;
                if (t < _rest) return PistonPhase.Retracted;
                t -= _rest;
                if (t < _telegraph) return PistonPhase.Telegraph;
                t -= _telegraph;
                if (t < _strike + _hold) return PistonPhase.Striking;
                return PistonPhase.Retracting;
            }
        }

        /// <summary>Head position: 0 fully retracted, 1 fully extended.</summary>
        public float Extension
        {
            get
            {
                float t = _clock - _rest - _telegraph;
                if (t < 0f) return 0f;
                if (t < _strike) return t / _strike;
                t -= _strike;
                if (t < _hold) return 1f;
                t -= _hold;
                return Math.Max(0f, 1f - t / _retract);
            }
        }

        public bool IsDangerous => Extension >= DangerFrom;

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            _clock = Positive(_clock + deltaTime, Period);
        }

        static float Positive(float value, float period)
        {
            float v = value % period;
            return v < 0f ? v + period : v;
        }
    }
}
