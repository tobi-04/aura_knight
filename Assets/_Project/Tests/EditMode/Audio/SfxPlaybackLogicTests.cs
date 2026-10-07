using AuraKnight.Audio;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Audio
{
    /// <summary>Clip choice, throttling, voice stealing and library lookup (the pure parts of SFX playback).</summary>
    public sealed class SfxPlaybackLogicTests
    {
        [Test]
        public void ClipPicker_SingleClipIsAlwaysZero()
        {
            Assert.AreEqual(0, ClipPicker.Pick(1, -1, 0.9f));
            Assert.AreEqual(0, ClipPicker.Pick(1, 0, 0.2f));
            Assert.AreEqual(0, ClipPicker.Pick(0, -1, 0.2f));
        }

        [Test]
        public void ClipPicker_NeverRepeatsTheLastClip()
        {
            for (int count = 2; count <= 5; count++)
                for (int last = 0; last < count; last++)
                    for (float roll = 0f; roll <= 1f; roll += 0.05f)
                    {
                        int pick = ClipPicker.Pick(count, last, roll);
                        Assert.That(pick, Is.InRange(0, count - 1));
                        Assert.AreNotEqual(last, pick, $"count {count} last {last} roll {roll}");
                    }
        }

        [Test]
        public void ClipPicker_CoversEveryClipWithoutHistory()
        {
            var seen = new System.Collections.Generic.HashSet<int>();
            for (float roll = 0f; roll <= 1f; roll += 0.01f) seen.Add(ClipPicker.Pick(4, -1, roll));
            Assert.AreEqual(4, seen.Count);
        }

        [Test]
        public void Throttle_DropsRepeatsInsideTheIntervalPerId()
        {
            var throttle = new SfxThrottle(0.05f);
            Assert.IsTrue(throttle.TryAcquire(SfxId.Coin, 1.00f));
            Assert.IsFalse(throttle.TryAcquire(SfxId.Coin, 1.02f));
            Assert.IsTrue(throttle.TryAcquire(SfxId.Jump, 1.02f), "another id is independent");
            Assert.IsTrue(throttle.TryAcquire(SfxId.Coin, 1.06f));
        }

        [Test]
        public void Throttle_RejectsOutOfRangeIds() =>
            Assert.IsFalse(new SfxThrottle().TryAcquire((SfxId)9999, 1f));

        [Test]
        public void VoiceSelector_PrefersAFreeVoice()
        {
            var busyUntil = new[] { 5f, 0.5f, 5f };
            var started = new[] { 1f, 0.2f, 0.1f };
            Assert.AreEqual(1, VoiceSelector.Choose(busyUntil, started, 1f));
        }

        [Test]
        public void VoiceSelector_StealsTheOldestWhenAllAreBusy()
        {
            var busyUntil = new[] { 5f, 5f, 5f };
            var started = new[] { 0.9f, 0.2f, 0.5f };
            Assert.AreEqual(1, VoiceSelector.Choose(busyUntil, started, 1f));
        }

        [Test]
        public void Library_FindsEntriesByIdAndIgnoresNoneAndDuplicates()
        {
            var library = ScriptableObject.CreateInstance<SfxLibrary>();
            try
            {
                var first = new SfxEntry { id = SfxId.Jump, volume = 0.5f };
                library.SetEntries(new[] { first, new SfxEntry { id = SfxId.Jump, volume = 1f }, new SfxEntry { id = SfxId.None }, null });
                Assert.IsTrue(library.TryGet(SfxId.Jump, out var found));
                Assert.AreSame(first, found, "first row wins");
                Assert.IsFalse(library.TryGet(SfxId.None, out _));
                Assert.IsFalse(library.TryGet(SfxId.Coin, out _));
                Assert.IsFalse(found.HasClips);
            }
            finally { Object.DestroyImmediate(library); }
        }

        [Test]
        public void Library_SetEntriesRebuildsTheLookup()
        {
            var library = ScriptableObject.CreateInstance<SfxLibrary>();
            try
            {
                library.SetEntries(new[] { new SfxEntry { id = SfxId.Coin } });
                Assert.IsTrue(library.TryGet(SfxId.Coin, out _));
                library.SetEntries(null);
                Assert.IsFalse(library.TryGet(SfxId.Coin, out _));
            }
            finally { Object.DestroyImmediate(library); }
        }
    }
}
