using AuraKnight.Core;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Combat
{
    public sealed class HapticsTests
    {
        int _saved;
        bool _hadKey;

        [SetUp]
        public void SetUp()
        {
            _hadKey = PlayerPrefs.HasKey(Haptics.PrefsKey);
            _saved = PlayerPrefs.GetInt(Haptics.PrefsKey, 1);
            PlayerPrefs.DeleteKey(Haptics.PrefsKey);
            Haptics.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            if (_hadKey) PlayerPrefs.SetInt(Haptics.PrefsKey, _saved);
            else PlayerPrefs.DeleteKey(Haptics.PrefsKey);
            Haptics.Refresh();
        }

        [Test] public void KeyMatchesSettingsContract() => Assert.AreEqual("settings.haptics", Haptics.PrefsKey);

        [Test] public void EnabledByDefault() => Assert.IsTrue(Haptics.Enabled);

        [Test]
        public void ToggleRoundTripsThroughPlayerPrefs()
        {
            Haptics.Enabled = false;
            Assert.IsFalse(Haptics.Enabled);
            Assert.AreEqual(0, PlayerPrefs.GetInt(Haptics.PrefsKey, 1));
            Haptics.Enabled = true;
            Assert.IsTrue(Haptics.Enabled);
        }

        [Test]
        public void SetEnabledPersistsAndRefreshesTheCache()
        {
            Assert.IsTrue(Haptics.Enabled);
            Haptics.SetEnabled(false);
            Assert.IsFalse(Haptics.Enabled);
            Assert.AreEqual(0, PlayerPrefs.GetInt(Haptics.PrefsKey, 1));
        }

        [Test]
        public void EnabledIsCachedUntilRefreshed()
        {
            Assert.IsTrue(Haptics.Enabled);
            PlayerPrefs.SetInt(Haptics.PrefsKey, 0);
            Assert.IsTrue(Haptics.Enabled, "cached, no PlayerPrefs read per pulse");
            Haptics.Refresh();
            Assert.IsFalse(Haptics.Enabled);
        }

        [Test]
        public void PulseIsNoOpInEditor() => Assert.IsFalse(Haptics.Pulse());
    }
}
