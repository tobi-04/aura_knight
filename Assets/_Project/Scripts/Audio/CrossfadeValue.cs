using UnityEngine;

namespace AuraKnight.Audio
{
    /// <summary>A 0..1 value that glides to a target in a fixed time (used for the combat mix and the track-to-track fade).</summary>
    public struct CrossfadeValue
    {
        public float Value;
        public float Target;

        public CrossfadeValue(float value) { Value = Mathf.Clamp01(value); Target = Value; }

        public bool IsSettled => Mathf.Approximately(Value, Target);

        /// <summary>Moves toward the target so a full 0 to 1 swing takes <paramref name="duration"/> seconds. Returns the new value.</summary>
        public float Step(float deltaTime, float duration)
        {
            Value = duration <= 0f
                ? Target
                : Mathf.MoveTowards(Value, Target, Mathf.Max(0f, deltaTime) / duration);
            return Value;
        }

        public void SetTarget(float target) => Target = Mathf.Clamp01(target);
    }
}
