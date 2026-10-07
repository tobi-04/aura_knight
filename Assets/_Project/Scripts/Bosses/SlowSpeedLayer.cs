using UnityEngine;

namespace AuraKnight.Bosses
{
    /// <summary>
    /// Layers a multiplicative slow on top of a speed value that somebody else (the Aura binder) keeps overwriting.
    /// When the other writer's value is known (<c>PlayerAuraBinder.Modifiers.SpeedMultiplier</c>) it is used as the base. Otherwise the layer
    /// infers it: it remembers the last value it wrote, and if the observed value differs the other writer changed the base. The inference
    /// cannot see a rewrite that happens to equal the layer's own last value, which is why the known base is preferred.
    /// </summary>
    public sealed class SlowSpeedLayer
    {
        float _base = 1f;
        float _written = 1f;

        /// <summary>The speed without the slow, as last observed.</summary>
        public float Base => _base;

        float Observe(float current)
        {
            if (!Mathf.Approximately(current, _written)) _base = current;
            return _base;
        }

        /// <summary>Value to write while slowed by <paramref name="factor"/> (0..1).</summary>
        public float Apply(float current, float factor, float? knownBase = null)
        {
            _base = knownBase ?? Observe(current);
            _written = _base * factor;
            return _written;
        }

        /// <summary>Value to write when the slow ends (the other writer's base).</summary>
        public float Release(float current, float? knownBase = null)
        {
            _base = knownBase ?? Observe(current);
            _written = _base;
            return _written;
        }
    }
}
