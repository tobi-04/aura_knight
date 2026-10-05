using UnityEngine;

namespace AuraKnight.World
{
    /// <summary>
    /// Pure oxygen rules (GDD section 4): 8 s of air under water without the Water aura, then 1 heart every 2 s.
    /// Leaving the water (or gaining the swim passive) refills the bar at once.
    /// </summary>
    public sealed class OxygenTimer
    {
        public const float DefaultMaxSeconds = 8f;
        public const float DefaultDamageInterval = 2f;

        readonly float _damageInterval;
        float _sinceDamage;

        public OxygenTimer(float maxSeconds = DefaultMaxSeconds, float damageInterval = DefaultDamageInterval)
        {
            Max = Mathf.Max(0.1f, maxSeconds);
            _damageInterval = Mathf.Max(0.1f, damageInterval);
            Remaining = Max;
        }

        public float Max { get; }
        public float Remaining { get; private set; }
        public float Fraction => Remaining / Max;
        public bool IsEmpty => Remaining <= 0f;

        /// <summary>Advances time; returns the number of hearts to remove during this step.</summary>
        public int Tick(float deltaTime, bool drowning)
        {
            if (!drowning)
            {
                Remaining = Max;
                _sinceDamage = 0f;
                return 0;
            }
            if (Remaining > 0f)
            {
                Remaining = Mathf.Max(0f, Remaining - deltaTime);
                if (Remaining > 0f) return 0;
                _sinceDamage = _damageInterval; // first heart is lost the moment the air runs out
            }
            else _sinceDamage += deltaTime;

            int hearts = 0;
            while (_sinceDamage >= _damageInterval)
            {
                hearts++;
                _sinceDamage -= _damageInterval;
            }
            return hearts;
        }
    }
}
