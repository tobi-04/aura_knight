using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using AuraKnight.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Builds the audio data assets from the clips on disk: the mixer, Data/Audio/SfxLibrary.asset (clips named
    /// "&lt;SfxId&gt;_&lt;n&gt;.wav" or "&lt;SfxId&gt;.wav") and one RegionMusic asset per "&lt;track&gt;_explore.ogg" (+ "_combat.ogg").
    /// Idempotent: existing assets are updated in place, so references and guids survive reruns.
    /// Clips come from tools/audio (generate_sfx.py, generate_bgm.py).
    /// </summary>
    public static class AudioAssetGenerator
    {
        public const string DataFolder = "Assets/_Project/Data/Audio";
        public const string LibraryPath = DataFolder + "/SfxLibrary.asset";
        static readonly string[] TrackIds = { "hub", "forest", "cave", "city", "castle", MusicLayerController.BossId, MusicLayerController.EndingId };

        // id -> (volume, pitch variance); anything not listed gets 0.7 / 5 %
        static readonly Dictionary<SfxId, (float volume, float pitch)> Tuning = new()
        {
            { SfxId.Footstep, (0.45f, 0.08f) }, { SfxId.UiTap, (0.7f, 0f) }, { SfxId.UiBack, (0.7f, 0f) },
            { SfxId.SwordHit, (0.85f, 0.05f) }, { SfxId.PlayerHurt, (0.9f, 0.03f) }, { SfxId.PlayerDie, (0.9f, 0f) },
            { SfxId.BossRoar, (1f, 0.03f) }, { SfxId.AuraUnlock, (0.9f, 0f) }, { SfxId.Altar, (0.8f, 0f) },
            { SfxId.Chest, (0.8f, 0.03f) }, { SfxId.Coin, (0.6f, 0.04f) }, { SfxId.SwordSwing, (0.6f, 0.06f) },
            { SfxId.WallSlide, (0.4f, 0.05f) },
        };

        [MenuItem("Aura/Audio/Generate Audio Assets")]
        public static void Generate()
        {
            AudioMixerWriter.Ensure();
            Directory.CreateDirectory(DataFolder);
            AssetDatabase.Refresh();
            BuildLibrary();
            foreach (var id in TrackIds) BuildRegionMusic(id);
            AssetDatabase.SaveAssets();
            Debug.Log("[AudioAssetGenerator] Mixer, SfxLibrary and RegionMusic assets are up to date.");
        }

        /// <summary>Everything: assets, the "Audio" object in Core.unity, and the probe on the Player prefab.</summary>
        public static void GenerateAll()
        {
            Generate();
            AudioSceneGenerator.PopulateCore();
            PlayerSfxProbeInstaller.Apply();
        }

        static void BuildLibrary()
        {
            var clipsById = new Dictionary<SfxId, List<AudioClip>>();
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioImportPostprocessor.SfxFolder.TrimEnd('/') }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string stem = Regex.Replace(Path.GetFileNameWithoutExtension(path), "_\\d+$", "");
                if (!Enum.TryParse(stem, out SfxId id) || id == SfxId.None)
                {
                    Debug.LogWarning($"[AudioAssetGenerator] '{path}' does not match an SfxId; skipped.");
                    continue;
                }
                if (!clipsById.TryGetValue(id, out var list)) clipsById[id] = list = new List<AudioClip>();
                list.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(path));
            }

            var entries = new List<SfxEntry>();
            foreach (SfxId id in Enum.GetValues(typeof(SfxId)))
            {
                if (id == SfxId.None) continue;
                if (!clipsById.TryGetValue(id, out var list))
                {
                    Debug.LogWarning($"[AudioAssetGenerator] No clip file for {id}.");
                    continue;
                }
                list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
                var (volume, pitch) = Tuning.TryGetValue(id, out var t) ? t : (0.7f, AudioMath.DefaultPitchVariance);
                entries.Add(new SfxEntry { id = id, clips = list.ToArray(), volume = volume, pitchVariance = pitch });
            }

            var library = LoadOrCreate<SfxLibrary>(LibraryPath);
            library.SetEntries(entries.ToArray());
            EditorUtility.SetDirty(library);
        }

        static void BuildRegionMusic(string id)
        {
            string folder = AudioImportPostprocessor.BgmFolder;
            var explore = AssetDatabase.LoadAssetAtPath<AudioClip>($"{folder}{id}_explore.ogg");
            if (explore == null)
            {
                Debug.LogWarning($"[AudioAssetGenerator] No explore clip for track '{id}'.");
                return;
            }
            var music = LoadOrCreate<RegionMusic>($"{DataFolder}/RegionMusic_{id}.asset");
            music.trackId = id;
            music.explore = explore;
            music.combat = AssetDatabase.LoadAssetAtPath<AudioClip>($"{folder}{id}_combat.ogg");
            music.volume = 0.7f;
            EditorUtility.SetDirty(music);
        }

        internal static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        internal static AudioMixer LoadMixer() => AssetDatabase.LoadAssetAtPath<AudioMixer>(AudioMixerWriter.MixerPath);

        internal static RegionMusic[] LoadAllRegionMusic()
        {
            var result = new List<RegionMusic>();
            foreach (string guid in AssetDatabase.FindAssets("t:RegionMusic", new[] { DataFolder }))
                result.Add(AssetDatabase.LoadAssetAtPath<RegionMusic>(AssetDatabase.GUIDToAssetPath(guid)));
            result.Sort((a, b) => string.CompareOrdinal(a.trackId, b.trackId));
            return result.ToArray();
        }
    }
}
