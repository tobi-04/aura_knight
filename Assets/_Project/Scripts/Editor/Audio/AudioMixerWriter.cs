using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Writes Audio/AuraKnight.mixer as text (Unity has no public API to create mixers): Master with the children
    /// Music, SFX and UI, each exposing MusicVolume / SfxVolume / UiVolume. Ids are derived from names, so reruns
    /// produce identical bytes and the file (and its .meta guid) stays stable.
    /// </summary>
    static class AudioMixerWriter
    {
        public const string MixerPath = "Assets/_Project/Audio/AuraKnight.mixer";

        const long ControllerId = 24100000, SnapshotId = 24500000;
        static readonly string[] Groups = { "Master", "Music", "SFX", "UI" };
        static readonly string[] Exposed = { null, "MusicVolume", "SfxVolume", "UiVolume" };

        public static bool Ensure()
        {
            string text = Build();
            string full = Path.GetFullPath(MixerPath);
            if (File.Exists(full) && File.ReadAllText(full) == text) return false;
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, text);
            AssetDatabase.ImportAsset(MixerPath, ImportAssetOptions.ForceUpdate);
            return true;
        }

        static string Guid(string seed)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes("aura-knight-mixer/" + seed));
            var sb = new StringBuilder();
            foreach (byte b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        static long GroupFileId(int i) => 24300000 + i * 2;
        static long EffectFileId(int i) => 24400000 + i * 2;
        const string Header = "  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n";

        static string Build()
        {
            var sb = new StringBuilder("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n");
            sb.Append($"--- !u!241 &{ControllerId}\nAudioMixerController:\n{Header}  m_Name: AuraKnight\n  m_OutputGroup: {{fileID: 0}}\n");
            sb.Append($"  m_MasterGroup: {{fileID: {GroupFileId(0)}}}\n  m_Snapshots:\n  - {{fileID: {SnapshotId}}}\n  m_StartSnapshot: {{fileID: {SnapshotId}}}\n");
            sb.Append("  m_SuspendThreshold: -80\n  m_EnableSuspend: 1\n  m_UpdateMode: 0\n  m_ExposedParameters:\n");
            for (int i = 1; i < Groups.Length; i++)
                sb.Append($"  - guid: {Guid(Groups[i] + "/volume")}\n    name: {Exposed[i]}\n");
            sb.Append("  m_AudioMixerGroupViews:\n  - guids:\n");
            for (int i = 0; i < Groups.Length; i++) sb.Append($"    - {Guid(Groups[i] + "/id")}\n");
            sb.Append("    name: View\n  m_CurrentViews: 0\n  m_TargetPlatformGroupSnapshots: []\n");
            for (int i = 0; i < Groups.Length; i++) AppendGroup(sb, i);
            for (int i = 0; i < Groups.Length; i++) AppendEffect(sb, i);
            sb.Append($"--- !u!245 &{SnapshotId}\nAudioMixerSnapshotController:\n{Header}  m_Name: Snapshot\n  m_AudioMixer: {{fileID: {ControllerId}}}\n");
            sb.Append($"  m_SnapshotID: {Guid("snapshot")}\n  m_FloatValues:\n");
            for (int i = 0; i < Groups.Length; i++) sb.Append($"    {Guid(Groups[i] + "/volume")}: 0\n");
            sb.Append("  m_TransitionOverrides: {}\n");
            return sb.ToString();
        }

        static void AppendGroup(StringBuilder sb, int i)
        {
            string name = Groups[i];
            sb.Append($"--- !u!243 &{GroupFileId(i)}\nAudioMixerGroupController:\n{Header}  m_Name: {name}\n  m_AudioMixer: {{fileID: {ControllerId}}}\n");
            sb.Append($"  m_GroupID: {Guid(name + "/id")}\n");
            if (i == 0)
            {
                sb.Append("  m_Children:\n");
                for (int c = 1; c < Groups.Length; c++) sb.Append($"  - {{fileID: {GroupFileId(c)}}}\n");
            }
            else sb.Append("  m_Children: []\n");
            sb.Append($"  m_Volume: {Guid(name + "/volume")}\n  m_Pitch: {Guid(name + "/pitch")}\n  m_Send: 00000000000000000000000000000000\n");
            sb.Append($"  m_Effects:\n  - {{fileID: {EffectFileId(i)}}}\n  m_UserColorIndex: 0\n  m_Mute: 0\n  m_Solo: 0\n  m_BypassEffects: 0\n");
        }

        static void AppendEffect(StringBuilder sb, int i)
        {
            string name = Groups[i];
            sb.Append($"--- !u!244 &{EffectFileId(i)}\nAudioMixerEffectController:\n{Header}  m_Name: \n  m_EffectID: {Guid(name + "/effect")}\n");
            sb.Append($"  m_EffectName: Attenuation\n  m_MixLevel: {Guid(name + "/mix")}\n  m_Parameters: []\n  m_SendTarget: {{fileID: 0}}\n  m_EnableWetMix: 0\n  m_Bypass: 0\n");
        }
    }
}
