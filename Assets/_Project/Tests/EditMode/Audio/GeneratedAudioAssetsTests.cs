using System;
using System.IO;
using AuraKnight.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace AuraKnight.Tests.Audio
{
    /// <summary>The generated mixer, library, music assets and import settings are complete (run AudioAssetGenerator first).</summary>
    public sealed class GeneratedAudioAssetsTests
    {
        const string MixerPath = "Assets/_Project/Audio/AuraKnight.mixer";
        const string LibraryPath = "Assets/_Project/Data/Audio/SfxLibrary.asset";
        static readonly string[] Tracks = { "hub", "forest", "cave", "city", "castle", "boss", "ending" };
        static readonly string[] RegionTracks = { "hub", "forest", "cave", "city", "castle" };

        [Test]
        public void Mixer_HasMasterWithMusicSfxAndUiGroups()
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            Assert.IsNotNull(mixer, "mixer asset");
            Assert.AreEqual("Master", mixer.FindMatchingGroups("Master")[0].name);
            foreach (string path in new[] { "Master/Music", "Master/SFX", "Master/UI" })
                Assert.AreEqual(1, mixer.FindMatchingGroups(path).Length, path);
        }

        [Test]
        public void Mixer_ExposesTheThreeVolumeParameters()
        {
            // SetFloat only works while playing (AudioPlayModeTests covers the values); here the exposed names are checked.
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            var exposed = new SerializedObject(mixer).FindProperty("m_ExposedParameters");
            var names = new System.Collections.Generic.List<string>();
            for (int i = 0; i < exposed.arraySize; i++) names.Add(exposed.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue);
            CollectionAssert.AreEquivalent(
                new[] { AudioVolumeSettings.MusicParam, AudioVolumeSettings.SfxParam, AudioVolumeSettings.UiParam }, names);
        }

        [Test]
        public void Library_HasClipsForEverySfxId()
        {
            var library = AssetDatabase.LoadAssetAtPath<SfxLibrary>(LibraryPath);
            Assert.IsNotNull(library);
            foreach (SfxId id in Enum.GetValues(typeof(SfxId)))
            {
                if (id == SfxId.None) continue;
                Assert.IsTrue(library.TryGet(id, out var entry), $"{id} has a row");
                Assert.IsTrue(entry.HasClips, $"{id} has clips");
                foreach (var clip in entry.clips) Assert.IsNotNull(clip, $"{id} has a null clip");
                Assert.That(entry.volume, Is.InRange(0.05f, 1f), id.ToString());
                Assert.That(entry.pitchVariance, Is.InRange(0f, AudioMath.MaxPitchVariance), id.ToString());
            }
        }

        [Test]
        public void Library_FootstepHasTwoVariants()
        {
            var library = AssetDatabase.LoadAssetAtPath<SfxLibrary>(LibraryPath);
            Assert.IsTrue(library.TryGet(SfxId.Footstep, out var entry));
            Assert.AreEqual(2, entry.clips.Length);
        }

        [Test]
        public void SfxClips_AreMonoAdpcmDecompressOnLoadAndLightweight()
        {
            long totalBytes = 0;
            var library = AssetDatabase.LoadAssetAtPath<SfxLibrary>(LibraryPath);
            foreach (var entry in library.Entries)
                foreach (var clip in entry.clips)
                {
                    string path = AssetDatabase.GetAssetPath(clip);
                    var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                    Assert.IsTrue(importer.forceToMono, path);
                    Assert.AreEqual(AudioCompressionFormat.ADPCM, importer.defaultSampleSettings.compressionFormat, path);
                    Assert.AreEqual(AudioClipLoadType.DecompressOnLoad, importer.defaultSampleSettings.loadType, path);
                    Assert.LessOrEqual(clip.frequency, 44100, path);
                    Assert.LessOrEqual(clip.length, 2f, path + " is a short effect");
                    totalBytes += clip.samples * 2L; // decompressed mono 16-bit
                }
            Assert.Less(totalBytes, 8L * 1024 * 1024, "SFX in RAM stays far below the 40 MB audio budget");
        }

        [TestCaseSource(nameof(Tracks))]
        public void RegionMusic_ExistsAndStreamsAsVorbis(string track)
        {
            var music = AssetDatabase.LoadAssetAtPath<RegionMusic>($"Assets/_Project/Data/Audio/RegionMusic_{track}.asset");
            Assert.IsNotNull(music, track);
            Assert.AreEqual(track, music.trackId);
            Assert.IsNotNull(music.explore, track + " explore layer");
            AssertStreamedVorbis(music.explore);
            if (music.combat != null) AssertStreamedVorbis(music.combat);
        }

        [TestCaseSource(nameof(RegionTracks))]
        public void RegionTracks_HaveSyncedExploreAndCombatLayers(string track)
        {
            var music = AssetDatabase.LoadAssetAtPath<RegionMusic>($"Assets/_Project/Data/Audio/RegionMusic_{track}.asset");
            Assert.IsTrue(music.HasCombat, track + " needs a combat layer");
            Assert.AreEqual(music.explore.samples, music.combat.samples, track + " layers must be equally long to stay in sync");
            Assert.AreEqual(music.explore.frequency, music.combat.frequency, track);
        }

        [TestCaseSource(nameof(Tracks))]
        public void BgmFiles_StayUnderOneAndAHalfMegabytes(string track)
        {
            foreach (string layer in new[] { "explore", "combat" })
            {
                string path = $"Assets/_Project/Audio/BGM/{track}_{layer}.ogg";
                if (!File.Exists(path)) continue;
                Assert.Less(new FileInfo(path).Length, 1_500_000, path);
            }
        }

        [Test]
        public void LicensesFile_ListsEveryBgmAndSfxSource()
        {
            string text = File.ReadAllText("Assets/_Project/Audio/LICENSES.md");
            foreach (string needle in new[] { "SFX", "BGM", "generated, project-owned", "tools/audio" })
                StringAssert.Contains(needle, text);
        }

        static void AssertStreamedVorbis(AudioClip clip)
        {
            string path = AssetDatabase.GetAssetPath(clip);
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            Assert.AreEqual(AudioCompressionFormat.Vorbis, importer.defaultSampleSettings.compressionFormat, path);
            Assert.AreEqual(AudioClipLoadType.Streaming, importer.defaultSampleSettings.loadType, path);
            Assert.LessOrEqual(clip.frequency, 44100, path);
        }
    }
}
