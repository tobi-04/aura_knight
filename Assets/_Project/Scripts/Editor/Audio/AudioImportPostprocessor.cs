using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Applies the project audio import rules to every clip under Audio/: SFX are mono ADPCM and decompress on load
    /// (tiny, zero latency); BGM are mono Vorbis streamed from disk (no RAM cost). Sample rates are preserved (sources are at most 44.1 kHz).
    /// Runs on import, so new clips never need manual settings. <see cref="ReimportAll"/> re-applies after changing the rules.
    /// </summary>
    sealed class AudioImportPostprocessor : AssetPostprocessor
    {
        public const string SfxFolder = "Assets/_Project/Audio/SFX/";
        public const string BgmFolder = "Assets/_Project/Audio/BGM/";

        void OnPreprocessAudio()
        {
            if (assetImporter is not AudioImporter importer) return;
            if (assetPath.StartsWith(SfxFolder)) Apply(importer, ImportKind.Sfx);
            else if (assetPath.StartsWith(BgmFolder)) Apply(importer, ImportKind.Bgm);
        }

        enum ImportKind { Sfx, Bgm }

        static void Apply(AudioImporter importer, ImportKind kind)
        {
            bool sfx = kind == ImportKind.Sfx;
            var settings = new AudioImporterSampleSettings
            {
                loadType = sfx ? AudioClipLoadType.DecompressOnLoad : AudioClipLoadType.Streaming,
                compressionFormat = sfx ? AudioCompressionFormat.ADPCM : AudioCompressionFormat.Vorbis,
                quality = 0.4f,
                preloadAudioData = sfx,
                sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate,
            };
            importer.forceToMono = true;
            importer.loadInBackground = !sfx;
            importer.defaultSampleSettings = settings;
            importer.SetOverrideSampleSettings("Android", settings);
        }

        [MenuItem("Aura/Audio/Reimport Audio")]
        public static void ReimportAll()
        {
            AssetDatabase.ImportAsset("Assets/_Project/Audio", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            Debug.Log("[Audio] Reimported Audio/ with project import settings.");
        }
    }
}
