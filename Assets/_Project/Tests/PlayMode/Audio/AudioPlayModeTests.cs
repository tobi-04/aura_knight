using System.Collections;
using AuraKnight.Audio;
using AuraKnight.Core;
using AuraKnight.Aura;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AuraKnight.Tests.PlayMode
{
    /// <summary>The audio system inside the real Core scene: pooled SFX, event listener, music switching and crossfades.</summary>
    public sealed class AudioPlayModeTests : WorldPlayTestBase
    {
        static AudioManager Audio => AudioManager.Instance;
        static MusicLayerController Music => MusicLayerController.Instance;

        [UnityTest]
        public IEnumerator CoreScene_HasTheAudioSystem()
        {
            yield return null;
            Assert.IsNotNull(Audio, "AudioManager in Core");
            Assert.IsNotNull(Music, "MusicLayerController in Core");
            Assert.IsNotNull(Audio.Mixer, "mixer assigned");
            Assert.IsNotNull(Audio.MusicGroup, "Music group found");
            Assert.IsNotNull(Object.FindAnyObjectByType<AudioEventListener>(), "listener in Core");
            Assert.AreEqual(AudioManager.PoolSize, Audio.GetComponentsInChildren<AudioSource>().Length - 4 /* music decks */);
        }

        [UnityTest]
        public IEnumerator SfxPlay_UsesAPooledVoice()
        {
            yield return null;
            int before = Audio.PlayCount;
            Sfx.Play(SfxId.UiTap);
            Assert.AreEqual(before + 1, Audio.PlayCount);
            Assert.AreEqual(SfxId.UiTap, Audio.LastPlayed);
            Assert.GreaterOrEqual(Audio.ActiveVoices, 1);
        }

        [UnityTest]
        public IEnumerator ManySimultaneousSounds_AreNotDroppedAndStealTheOldestVoice()
        {
            yield return null;
            int before = Audio.PlayCount;
            int played = 0;
            foreach (SfxId id in System.Enum.GetValues(typeof(SfxId)))
            {
                if (id == SfxId.None) continue;
                if (Audio.Play(id, new Vector3(3f, 1f, 0f))) played++;
            }
            Assert.Greater(played, AudioManager.PoolSize, "more sounds than voices");
            Assert.AreEqual(before + played, Audio.PlayCount);
            Assert.AreEqual(AudioManager.PoolSize, Audio.ActiveVoices, "every voice busy, none lost");
        }

        [UnityTest]
        public IEnumerator SameSoundWithinFortyMilliseconds_IsThrottled()
        {
            yield return null;
            Assert.IsTrue(Audio.Play(SfxId.Coin));
            Assert.IsFalse(Audio.Play(SfxId.Coin));
        }

        [UnityTest]
        public IEnumerator EventBusEvents_BecomeSounds()
        {
            yield return null;
            EventBus.Publish(new PlayerDied());
            Assert.AreEqual(SfxId.PlayerDie, Audio.LastPlayed);
            EventBus.Publish(new PlayerDamaged(1, 2));
            Assert.AreEqual(SfxId.PlayerHurt, Audio.LastPlayed);
            EventBus.Publish(new CheckpointReached("hub_altar_01"));
            Assert.AreEqual(SfxId.Altar, Audio.LastPlayed);
            EventBus.Publish(new AuraUnlocked("Wind"));
            Assert.AreEqual(SfxId.AuraUnlock, Audio.LastPlayed);
        }

        [UnityTest]
        public IEnumerator CoinsAndAuraSwitches_PlayOnlyOnRealChanges()
        {
            yield return null;
            EventBus.Publish(new CoinsChanged(10)); // seeds the tracker silently
            int count = Audio.PlayCount;
            yield return new WaitForSecondsRealtime(0.1f);
            EventBus.Publish(new CoinsChanged(10));
            EventBus.Publish(new CoinsChanged(4));
            Assert.AreEqual(count, Audio.PlayCount, "no sound without a gain");
            EventBus.Publish(new CoinsChanged(7));
            Assert.AreEqual(SfxId.Coin, Audio.LastPlayed);

            EventBus.Publish(new AuraChanged("Fire")); // seeds
            yield return new WaitForSecondsRealtime(0.1f);
            EventBus.Publish(new AuraChanged("Water"));
            Assert.AreEqual(SfxId.AuraWater, Audio.LastPlayed);
        }

        [UnityTest]
        public IEnumerator RoomEntered_SwitchesTheRegionTrackWithACrossfade()
        {
            yield return null;
            Music.AutoCombat = false;
            EventBus.Publish(new RoomEntered("forest_01", "forest"));
            Assert.AreEqual("forest", Music.CurrentTrackId);
            Assert.AreEqual(0f, Music.CurrentGain, 0.2f, "starts silent and fades in");

            yield return new WaitForSecondsRealtime(1.3f);
            Assert.AreEqual(1f, Music.CurrentGain, 1e-3f, "fade-in finished");

            EventBus.Publish(new RoomEntered("cave_01", "cave"));
            Assert.AreEqual("cave", Music.CurrentTrackId);
            Assert.IsTrue(Music.IsCrossfading, "the forest track is still fading out");
            yield return new WaitForSecondsRealtime(1.3f);
            Assert.IsFalse(Music.IsCrossfading, "old track stopped");
            Assert.AreEqual(1f, Music.CurrentGain, 1e-3f);
        }

        [UnityTest]
        public IEnumerator SameRegionOrUnknownRegion_KeepsThePlayingTrack()
        {
            yield return null;
            Music.AutoCombat = false;
            EventBus.Publish(new RoomEntered("hub_01", "hub"));
            EventBus.Publish(new RoomEntered("hub_02", "hub"));
            EventBus.Publish(new RoomEntered("x_01", "test"));
            Assert.AreEqual("hub", Music.CurrentTrackId);
            Assert.IsFalse(Music.IsCrossfading);
        }

        [UnityTest]
        public IEnumerator CombatIntensity_CrossfadesToTheCombatLayerInOneSecond()
        {
            yield return null;
            Music.AutoCombat = false;
            EventBus.Publish(new RoomEntered("forest_01", "forest"));
            Music.SetCombatIntensity(1f);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(Music.CombatMix, Is.InRange(0.2f, 0.8f), "halfway through the fade");
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.AreEqual(1f, Music.CombatMix, 1e-3f);
            Music.SetCombatIntensity(0f);
            yield return new WaitForSecondsRealtime(1.3f);
            Assert.AreEqual(0f, Music.CombatMix, 1e-3f);
        }

        [UnityTest]
        public IEnumerator AutoCombat_ReactsToEnemyLayerCollidersNearThePlayer()
        {
            yield return null;
            var player = new GameObject("AudioTestPlayer") { tag = "Player" };
            var enemy = new GameObject("AudioTestEnemy");
            PhysicsLayers.Apply(enemy, PhysicsLayers.Enemy);
            enemy.AddComponent<BoxCollider2D>();
            try
            {
                player.transform.position = new Vector3(500f, 500f, 0f);
                enemy.transform.position = new Vector3(504f, 500f, 0f);
                Music.AutoCombat = true;
                EventBus.Publish(new RoomEntered("forest_01", "forest"));
                yield return WaitUntil(() => Music.CombatMix > 0.99f, "combat mix to rise with an enemy within 8 units", 6f);

                enemy.transform.position = new Vector3(540f, 500f, 0f); // out of range
                yield return WaitUntil(() => Music.CombatMix < 0.01f, "combat mix to fall after the enemy leaves (2 s linger + 1 s fade)", 8f);
            }
            finally
            {
                Object.Destroy(player);
                Object.Destroy(enemy);
            }
        }

        [UnityTest]
        public IEnumerator BossTrack_OverridesRegionMusicUntilReleased()
        {
            yield return null;
            Music.AutoCombat = false;
            EventBus.Publish(new RoomEntered("forest_01", "forest"));
            Music.PlayBoss();
            Assert.AreEqual(MusicLayerController.BossId, Music.CurrentTrackId);
            EventBus.Publish(new RoomEntered("forest_02", "forest"));
            EventBus.Publish(new RoomEntered("cave_01", "cave"));
            Assert.AreEqual(MusicLayerController.BossId, Music.CurrentTrackId, "rooms do not interrupt the boss track");
            Music.ReleaseOverride();
            Assert.AreEqual("cave", Music.CurrentTrackId, "back to the latest region");
            Music.PlayEnding();
            Assert.AreEqual(MusicLayerController.EndingId, Music.CurrentTrackId);
        }

        [UnityTest]
        public IEnumerator VolumeSettings_ReachTheMixerAsDecibels()
        {
            yield return null;
            PlayerPrefs.DeleteKey(AudioVolumeSettings.MusicKey);
            PlayerPrefs.DeleteKey(AudioVolumeSettings.SfxKey);
            Assert.AreEqual(0.8f, AudioVolumeSettings.MusicVolume, 1e-5f, "default 0.8");
            try
            {
                Assert.IsTrue(AudioVolumeSettings.Set(0.5f, 0.25f));
                Assert.IsTrue(Audio.Mixer.GetFloat(AudioVolumeSettings.MusicParam, out float music));
                Assert.IsTrue(Audio.Mixer.GetFloat(AudioVolumeSettings.SfxParam, out float sfx));
                Assert.IsTrue(Audio.Mixer.GetFloat(AudioVolumeSettings.UiParam, out float ui));
                Assert.AreEqual(-6.02f, music, 0.05f);
                Assert.AreEqual(-12.04f, sfx, 0.05f);
                Assert.AreEqual(sfx, ui, 1e-4f, "UI follows the SFX slider");
            }
            finally
            {
                PlayerPrefs.DeleteKey(AudioVolumeSettings.MusicKey);
                PlayerPrefs.DeleteKey(AudioVolumeSettings.SfxKey);
            }
        }
    }
}
