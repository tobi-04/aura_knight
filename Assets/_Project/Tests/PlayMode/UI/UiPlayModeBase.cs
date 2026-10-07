using System.Collections;
using AuraKnight.Tests.PlayMode;
using AuraKnight.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AuraKnight.Tests.PlayMode.UI
{
    /// <summary>Core scene with the real UI_Root; settings prefs are restored after each test.</summary>
    public abstract class UiPlayModeBase : WorldPlayTestBase
    {
        PrefsSnapshot prefs;

        [UnitySetUp]
        public IEnumerator SnapshotPrefs()
        {
            prefs = PrefsSnapshot.Take();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator RestorePrefs()
        {
            prefs?.Restore();
            Time.timeScale = 1f;
            yield return null;
        }

        protected static T Find<T>() where T : Object => Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);

        protected static UIButton Button(Component screen, string name)
        {
            foreach (var button in screen.GetComponentsInChildren<UIButton>(true))
                if (button.name == name) return button;
            Assert.Fail($"{screen.name} has no button '{name}'");
            return null;
        }

        protected static IEnumerator WaitFrames(int count)
        {
            for (int i = 0; i < count; i++) yield return null;
        }

        /// <summary>Waits real time (screens animate on unscaled time).</summary>
        protected static IEnumerator WaitSeconds(float seconds)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until) yield return null;
        }
    }

    /// <summary>Saves and restores the personal-settings PlayerPrefs so tests never change the developer's settings.</summary>
    public sealed class PrefsSnapshot
    {
        float[] floats;
        int[] ints;
        string language;
        static readonly string[] FloatKeys = { SettingsKeys.MusicVolume, SettingsKeys.SfxVolume, SettingsKeys.ButtonScale, SettingsKeys.ButtonOpacity };
        static readonly string[] IntKeys = { SettingsKeys.Haptics, SettingsKeys.PowerSaving };

        public static PrefsSnapshot Take()
        {
            var s = new PrefsSnapshot { floats = new float[FloatKeys.Length], ints = new int[IntKeys.Length] };
            for (int i = 0; i < FloatKeys.Length; i++) s.floats[i] = PlayerPrefs.HasKey(FloatKeys[i]) ? PlayerPrefs.GetFloat(FloatKeys[i]) : float.NaN;
            for (int i = 0; i < IntKeys.Length; i++) s.ints[i] = PlayerPrefs.HasKey(IntKeys[i]) ? PlayerPrefs.GetInt(IntKeys[i]) : -1;
            s.language = PlayerPrefs.HasKey(SettingsKeys.Language) ? PlayerPrefs.GetString(SettingsKeys.Language) : null;
            return s;
        }

        public void Restore()
        {
            for (int i = 0; i < FloatKeys.Length; i++)
                if (float.IsNaN(floats[i])) PlayerPrefs.DeleteKey(FloatKeys[i]); else PlayerPrefs.SetFloat(FloatKeys[i], floats[i]);
            for (int i = 0; i < IntKeys.Length; i++)
                if (ints[i] < 0) PlayerPrefs.DeleteKey(IntKeys[i]); else PlayerPrefs.SetInt(IntKeys[i], ints[i]);
            if (language == null) PlayerPrefs.DeleteKey(SettingsKeys.Language); else PlayerPrefs.SetString(SettingsKeys.Language, language);
            PlayerPrefs.Save();
            AuraKnight.Core.Haptics.Refresh();
        }
    }
}
