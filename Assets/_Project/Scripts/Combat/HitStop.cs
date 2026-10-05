using AuraKnight.Core;
using UnityEngine;

namespace AuraKnight.Combat
{
    /// <summary>
    /// Freezes the game for a few frames when a hit lands. Uses unscaled time, extends instead of stacking,
    /// never fights a pause, and does nothing outside play mode. Call <see cref="Request"/> from gameplay code.
    /// </summary>
    public static class HitStop
    {
        public const float DefaultDuration = 0.05f;
        static readonly HitStopTimer Timer = new HitStopTimer();
        static Runner runner;

        /// <summary>True while a hit-stop freeze is running.</summary>
        public static bool IsFrozen => Timer.Active;

        /// <summary>The scale gameplay runs at: pause menus resume to this so a freeze in progress is never mistaken for a pause.</summary>
        public static float GameplayScale => Timer.Active ? Timer.SavedScale : Time.timeScale;

        public static void Request(float duration = DefaultDuration)
        {
            if (!Application.isPlaying) return;
            var scale = Timer.Request(duration, Time.timeScale);
            if (!scale.HasValue) return;
            Time.timeScale = scale.Value;
            EnsureRunner();
        }

        static void EnsureRunner()
        {
            if (runner != null) return;
            // HideInHierarchy only: DontSave would keep the runner alive across play-mode exits when domain reload is off.
            var go = new GameObject("[HitStop]") { hideFlags = HideFlags.HideInHierarchy };
            Object.DontDestroyOnLoad(go);
            runner = go.AddComponent<Runner>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Timer.Abort();
            runner = null;
        }

        sealed class Runner : MonoBehaviour
        {
            void Update()
            {
                var manager = GameManager.Instance;
                bool paused = manager != null && manager.Mode == GameMode.Paused;
                var scale = Timer.Tick(Time.unscaledDeltaTime, Time.timeScale, paused);
                if (scale.HasValue) Time.timeScale = scale.Value;
            }

            void OnDestroy()
            {
                if (Timer.Active) Time.timeScale = Timer.Abort();
            }
        }
    }
}
