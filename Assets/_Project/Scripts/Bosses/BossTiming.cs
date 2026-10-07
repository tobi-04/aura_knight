using UnityEngine;

namespace AuraKnight.Bosses
{
    /// <summary>Timing rules shared by every boss attack. The telegraph never drops below the minimum, whatever the phase speed.</summary>
    public static class BossTiming
    {
        /// <summary>Mobile reaction floor: every attack announces itself at least this long (seconds).</summary>
        public const float MinTelegraph = 0.5f;

        /// <summary>Telegraph length at a phase speed: faster phases shorten it, but never below <paramref name="minimum"/>.</summary>
        public static float Telegraph(float baseSeconds, float speed, float minimum = MinTelegraph) =>
            Mathf.Max(minimum, baseSeconds / Mathf.Max(0.01f, speed));

        /// <summary>Execute / recover / think durations simply shrink with the phase speed.</summary>
        public static float Scaled(float seconds, float speed) => seconds / Mathf.Max(0.01f, speed);
    }
}
