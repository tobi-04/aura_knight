using UnityEngine;

namespace AuraKnight.Core
{
    /// <summary>
    /// Light vibration on hit / being hit: a 20 ms one-shot (VibrationEffect, Android API 26+; minSdk is 26).
    /// The Settings screen calls <see cref="SetEnabled"/> (PlayerPrefs "settings.haptics", on by default); the flag is
    /// cached so a pulse never touches PlayerPrefs. A no-op everywhere except Android devices.
    /// The VIBRATE permission is added by Unity's manifest generator because <c>Handheld.Vibrate</c> is referenced
    /// (legacy fallback below); verified in the built APK.
    /// </summary>
    public static class Haptics
    {
        public const string PrefsKey = "settings.haptics";
        public const int PulseMilliseconds = 20;
        const float MinInterval = 0.1f;
#if UNITY_ANDROID && !UNITY_EDITOR
        static float lastPulseTime = float.NegativeInfinity;
#endif
        static int cached = -1; // -1 = not read yet

        public static bool Enabled
        {
            get
            {
                if (cached < 0) cached = PlayerPrefs.GetInt(PrefsKey, 1) != 0 ? 1 : 0;
                return cached == 1;
            }
            set => SetEnabled(value);
        }

        /// <summary>Persists the setting and refreshes the cache. Call from the Settings toggle.</summary>
        public static void SetEnabled(bool enabled)
        {
            cached = enabled ? 1 : 0;
            PlayerPrefs.SetInt(PrefsKey, cached);
            PlayerPrefs.Save();
        }

        /// <summary>Drops the cache so the next read comes from PlayerPrefs (setting changed elsewhere).</summary>
        public static void Refresh() => cached = -1;

        /// <summary>Vibrates once if enabled and not throttled. Returns true when a vibration was requested.</summary>
        public static bool Pulse()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Enabled) return false;
            float now = Time.realtimeSinceStartup;
            if (now - lastPulseTime < MinInterval) return false;
            lastPulseTime = now;
            return AndroidVibrator.OneShot(PulseMilliseconds);
#else
            return false;
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            lastPulseTime = float.NegativeInfinity;
#endif
            cached = -1;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        static class AndroidVibrator
        {
            const int DefaultAmplitude = -1; // VibrationEffect.DEFAULT_AMPLITUDE
            static AndroidJavaObject vibrator;
            static AndroidJavaClass effects;
            static bool initialised, jniBroken;

            public static bool OneShot(int milliseconds)
            {
                if (!jniBroken)
                {
                    try
                    {
                        Init();
                        using (var effect = effects.CallStatic<AndroidJavaObject>("createOneShot", (long)milliseconds, DefaultAmplitude))
                            vibrator.Call("vibrate", effect);
                        return true;
                    }
                    catch (System.Exception e)
                    {
                        jniBroken = true;
                        Debug.LogWarning($"[Haptics] VibrationEffect unavailable ({e.Message}); using Handheld.Vibrate.");
                    }
                }
                Handheld.Vibrate(); // long buzz, but still better than nothing; also what makes Unity add android.permission.VIBRATE
                return true;
            }

            static void Init()
            {
                if (initialised) return;
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                effects = new AndroidJavaClass("android.os.VibrationEffect");
                initialised = true;
                if (vibrator == null) throw new System.InvalidOperationException("no vibrator service");
            }
        }
#endif
    }
}
