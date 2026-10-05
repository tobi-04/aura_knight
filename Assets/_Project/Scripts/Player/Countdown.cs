using UnityEngine;

namespace AuraKnight.Player
{
    /// <summary>
    /// Simple down-counting timer used for coyote time, input buffers and cooldowns.
    /// Driven explicitly by <see cref="Tick"/> so it stays testable without a scene.
    /// </summary>
    public sealed class Countdown
    {
        public float Remaining { get; private set; }
        public bool IsActive => Remaining > 0f;

        public void Start(float duration) => Remaining = Mathf.Max(0f, duration);
        public void Cancel() => Remaining = 0f;

        public void Tick(float deltaTime)
        {
            if (Remaining > 0f) Remaining = Mathf.Max(0f, Remaining - deltaTime);
        }

        /// <summary>Returns true (and deactivates) if the timer was still running.</summary>
        public bool Consume()
        {
            if (!IsActive) return false;
            Remaining = 0f;
            return true;
        }
    }
}
