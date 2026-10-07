using System;
using UnityEngine;

namespace AuraKnight.Enemies
{
    public enum ZapPhase
    {
        Idle,
        Telegraph,
        Fire
    }

    /// <summary>
    /// Repeating zap timeline for the static zapper: a cycle of <c>interval</c> seconds ends with a telegraph (warning
    /// flash) followed by the discharge, so the zap fires exactly once per interval (GDD: every 3 s).
    /// </summary>
    public sealed class ZapCycle
    {
        readonly float _interval;
        readonly float _telegraphStart;
        readonly float _fireStart;
        float _elapsed;
        bool _fired;

        public ZapPhase Phase { get; private set; } = ZapPhase.Idle;
        /// <summary>Raised once when the discharge starts.</summary>
        public event Action Fired;

        public ZapCycle(float interval, float telegraphSeconds, float fireSeconds)
        {
            _interval = Mathf.Max(0.1f, interval);
            float fire = Mathf.Clamp(fireSeconds, 0.01f, _interval);
            float telegraph = Mathf.Clamp(telegraphSeconds, 0f, _interval - fire);
            _fireStart = _interval - fire;
            _telegraphStart = _fireStart - telegraph;
        }

        public void Reset()
        {
            _elapsed = 0f;
            _fired = false;
            Phase = ZapPhase.Idle;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            _elapsed += deltaTime;
            while (true)
            {
                if (!_fired && _elapsed >= _fireStart)
                {
                    _fired = true;
                    Fired?.Invoke();
                }
                if (_elapsed < _interval) break;
                _elapsed -= _interval;
                _fired = false;
            }
            Phase = _elapsed >= _fireStart ? ZapPhase.Fire : _elapsed >= _telegraphStart ? ZapPhase.Telegraph : ZapPhase.Idle;
        }
    }
}
