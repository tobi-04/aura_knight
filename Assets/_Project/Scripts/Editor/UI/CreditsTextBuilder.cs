using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Aggregates every LICENSES.md (art, fonts, audio) into Resources/CreditsText.txt for the credits screen. Runs from the
    /// generator and before every player build, so the shipped credits never go stale. Missing files are skipped.
    /// </summary>
    public sealed class CreditsTextBuilder : IPreprocessBuildWithReport
    {
        static readonly (string title, string path)[] Sources =
        {
            ("HÌNH ẢNH", "Assets/_Project/Art/LICENSES.md"),
            ("PHÔNG CHỮ", "Assets/_Project/Art/Fonts/LICENSES.md"),
            ("ÂM THANH", "Assets/_Project/Audio/LICENSES.md")
        };

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) => Build();

        [MenuItem("Aura/UI/Build Credits Text")]
        public static void Build()
        {
            Directory.CreateDirectory(UiAssetPaths.ResourcesDir);
            string text = Compose(path => File.Exists(path) ? File.ReadAllText(path) : null);
            string existing = File.Exists(UiAssetPaths.CreditsText) ? File.ReadAllText(UiAssetPaths.CreditsText) : null;
            if (existing == text) return;
            File.WriteAllText(UiAssetPaths.CreditsText, text, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(UiAssetPaths.CreditsText);
        }

        /// <summary>Pure composition (testable): header plus one section per readable source.</summary>
        public static string Compose(System.Func<string, string> read)
        {
            var sb = new StringBuilder();
            sb.AppendLine("AURA KNIGHT: MẢNH VỠ ÁNH SÁNG").AppendLine();
            foreach (var (title, path) in Sources)
            {
                string content = read(path);
                if (string.IsNullOrWhiteSpace(content)) continue;
                sb.AppendLine(title).AppendLine();
                foreach (string line in content.Replace("\r", "").Split('\n')) AppendLine(sb, line);
                sb.AppendLine();
            }
            return sb.ToString().TrimEnd() + "\n";
        }

        static void AppendLine(StringBuilder sb, string line)
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith("|"))
            {
                if (trimmed.Replace("|", "").Replace("-", "").Replace(":", "").Trim().Length == 0) return; // table separator
                var cells = trimmed.Trim('|').Split('|').Select(c => c.Replace("`", "").Trim()).Where(c => c.Length > 0);
                sb.AppendLine(string.Join("  -  ", cells));
                return;
            }
            if (trimmed.StartsWith("#")) trimmed = trimmed.TrimStart('#').Trim().ToUpperInvariant();
            sb.AppendLine(trimmed.Replace("`", "").Replace("**", ""));
        }
    }
}
