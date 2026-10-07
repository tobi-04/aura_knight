using AuraKnight.Core;
using AuraKnight.UI;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.UI
{
    /// <summary>Touches PlayerPrefs: the developer's own settings are saved first and restored after every test.</summary>
    public sealed class GameSettingsTests
    {
        int fps;

        [SetUp]
        public void Backup()
        {
            EventBus.Clear();
            fps = Application.targetFrameRate;
            RawBackup();
            GameSettings.ResetToDefaults();
        }

        float[] floats;
        int[] ints;
        string lang;

        void RawBackup()
        {
            floats = new[]
            {
                PlayerPrefs.GetFloat(SettingsKeys.MusicVolume, -1f), PlayerPrefs.GetFloat(SettingsKeys.SfxVolume, -1f),
                PlayerPrefs.GetFloat(SettingsKeys.ButtonScale, -1f), PlayerPrefs.GetFloat(SettingsKeys.ButtonOpacity, -1f)
            };
            ints = new[] { PlayerPrefs.GetInt(SettingsKeys.Haptics, -1), PlayerPrefs.GetInt(SettingsKeys.PowerSaving, -1) };
            lang = PlayerPrefs.GetString(SettingsKeys.Language, null);
        }

        [TearDown]
        public void Restore()
        {
            string[] floatKeys = { SettingsKeys.MusicVolume, SettingsKeys.SfxVolume, SettingsKeys.ButtonScale, SettingsKeys.ButtonOpacity };
            for (int i = 0; i < floatKeys.Length; i++)
                if (floats[i] < 0f) PlayerPrefs.DeleteKey(floatKeys[i]); else PlayerPrefs.SetFloat(floatKeys[i], floats[i]);
            string[] intKeys = { SettingsKeys.Haptics, SettingsKeys.PowerSaving };
            for (int i = 0; i < intKeys.Length; i++)
                if (ints[i] < 0) PlayerPrefs.DeleteKey(intKeys[i]); else PlayerPrefs.SetInt(intKeys[i], ints[i]);
            if (string.IsNullOrEmpty(lang)) PlayerPrefs.DeleteKey(SettingsKeys.Language); else PlayerPrefs.SetString(SettingsKeys.Language, lang);
            PlayerPrefs.Save();
            Haptics.Refresh();
            Application.targetFrameRate = fps;
            EventBus.Clear();
        }

        [Test]
        public void KeysMatchTheDocumentedNames()
        {
            Assert.AreEqual("settings.musicVolume", SettingsKeys.MusicVolume);
            Assert.AreEqual("settings.sfxVolume", SettingsKeys.SfxVolume);
            Assert.AreEqual("settings.haptics", SettingsKeys.Haptics);
            Assert.AreEqual(Haptics.PrefsKey, SettingsKeys.Haptics);
            Assert.AreEqual("settings.buttonScale", SettingsKeys.ButtonScale);
            Assert.AreEqual("settings.buttonOpacity", SettingsKeys.ButtonOpacity);
            Assert.AreEqual("settings.powerSaving", SettingsKeys.PowerSaving);
            Assert.AreEqual("settings.language", SettingsKeys.Language);
        }

        [Test]
        public void DefaultsAreSensible()
        {
            Assert.AreEqual(GameSettings.DefaultVolume, GameSettings.MusicVolume);
            Assert.AreEqual(GameSettings.DefaultVolume, GameSettings.SfxVolume);
            Assert.AreEqual(1f, GameSettings.ButtonScale);
            Assert.AreEqual(GameSettings.DefaultButtonOpacity, GameSettings.ButtonOpacity);
            Assert.IsTrue(GameSettings.HapticsEnabled);
            Assert.IsFalse(GameSettings.PowerSaving);
            Assert.AreEqual("vi", GameSettings.Language);
        }

        [Test]
        public void ValuesRoundTripAndAreClampedToTheDocumentedRanges()
        {
            GameSettings.MusicVolume = 0.25f;
            Assert.AreEqual(0.25f, GameSettings.MusicVolume);
            GameSettings.MusicVolume = 9f;
            Assert.AreEqual(1f, GameSettings.MusicVolume);
            GameSettings.ButtonScale = 0.1f;
            Assert.AreEqual(0.8f, GameSettings.ButtonScale);
            GameSettings.ButtonScale = 5f;
            Assert.AreEqual(1.3f, GameSettings.ButtonScale);
            GameSettings.ButtonOpacity = 0f;
            Assert.AreEqual(0.3f, GameSettings.ButtonOpacity);
        }

        [Test]
        public void CorruptStoredValuesFallBackToTheDefault()
        {
            PlayerPrefs.SetFloat(SettingsKeys.SfxVolume, float.NaN);
            PlayerPrefs.SetFloat(SettingsKeys.ButtonScale, float.PositiveInfinity);
            Assert.AreEqual(GameSettings.DefaultVolume, GameSettings.SfxVolume);
            Assert.AreEqual(1f, GameSettings.ButtonScale);
            Assert.AreEqual(0.5f, GameSettings.Sanitize(float.NaN, 0f, 1f, 0.5f));
        }

        [Test]
        public void HapticsToggleWritesTheKeyHapticsReads()
        {
            GameSettings.HapticsEnabled = false;
            Assert.AreEqual(0, PlayerPrefs.GetInt(Haptics.PrefsKey));
            Assert.IsFalse(Haptics.Enabled);
            GameSettings.HapticsEnabled = true;
            Assert.IsTrue(Haptics.Enabled);
        }

        [Test]
        public void PowerSavingDropsTheFrameRateToThirty()
        {
            GameSettings.PowerSaving = true;
            Assert.AreEqual(30, Application.targetFrameRate);
            GameSettings.PowerSaving = false;
            Assert.AreEqual(60, Application.targetFrameRate);
        }

        [Test]
        public void TheFrameRateDefaultsToSixtyAndFollowsTheStoredPowerSavingChoice()
        {
            GameSettings.ResetToDefaults();
            Application.targetFrameRate = -1;
            GameSettings.ApplyFrameRate();
            Assert.AreEqual(GameSettings.NormalFps, Application.targetFrameRate, "no stored choice: 60");
            PlayerPrefs.SetInt(SettingsKeys.PowerSaving, 1); // what the previous session left behind
            GameSettings.ApplyFrameRate();
            Assert.AreEqual(GameSettings.PowerSavingFps, Application.targetFrameRate, "boot picks the stored choice up");
        }

        [Test]
        public void UnsupportedLanguageFallsBackToVietnamese()
        {
            GameSettings.Language = "xx";
            Assert.AreEqual("vi", GameSettings.Language);
            PlayerPrefs.SetString(SettingsKeys.Language, "zz");
            Assert.AreEqual("vi", GameSettings.Language);
        }

        [Test]
        public void EveryWritePublishesSettingsChangedWithItsKey()
        {
            var keys = new System.Collections.Generic.List<string>();
            void Handler(SettingsChanged e) => keys.Add(e.Key);
            EventBus.Subscribe<SettingsChanged>(Handler);
            GameSettings.MusicVolume = 0.5f;
            GameSettings.ButtonOpacity = 0.7f;
            GameSettings.PowerSaving = true;
            GameSettings.HapticsEnabled = false;
            EventBus.Unsubscribe<SettingsChanged>(Handler);
            CollectionAssert.AreEqual(new[]
            {
                SettingsKeys.MusicVolume, SettingsKeys.ButtonOpacity, SettingsKeys.PowerSaving, SettingsKeys.Haptics
            }, keys);
        }
    }
}
