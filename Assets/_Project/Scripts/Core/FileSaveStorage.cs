using System;
using System.IO;
using UnityEngine;

namespace AuraKnight.Core
{
    /// <summary>
    /// Single-slot file storage at persistentDataPath/save_0.json. A write goes to a tmp file first and then replaces
    /// the save through File.Replace, which keeps the previous generation as save_0.json.bak. File.Replace is not
    /// guaranteed on every Android filesystem, so any failure falls back to a plain overwrite copy.
    /// </summary>
    public sealed class FileSaveStorage : ISaveStorage
    {
        public const string DefaultFileName = "save_0.json";
        readonly string path;
        readonly bool copyOnly;

        public FileSaveStorage() : this(Path.Combine(Application.persistentDataPath, DefaultFileName)) { }
        public FileSaveStorage(string path) : this(path, false) { }

        /// <param name="copyOnly">Skips File.Replace; lets tests exercise the fallback path.</param>
        internal FileSaveStorage(string path, bool copyOnly)
        {
            this.path = path;
            this.copyOnly = copyOnly;
        }

        string TempPath => path + ".tmp";
        string BackupPath => path + ".bak";

        public bool Exists() => File.Exists(path);

        public string ReadText() => File.ReadAllText(path);

        public bool BackupExists() => File.Exists(BackupPath);

        public string ReadBackupText() => File.ReadAllText(BackupPath);

        public void WriteText(string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(TempPath, text);
            if (!File.Exists(path))
            {
                CommitFirstWrite();
                return;
            }
            if (!copyOnly && TryReplace()) return;
            CommitCopy();
        }

        public void Delete()
        {
            foreach (var file in new[] { path, TempPath, BackupPath })
                if (File.Exists(file)) File.Delete(file);
        }

        bool TryReplace()
        {
            try
            {
                // Only a main file that still parses becomes the backup; a damaged one must not overwrite a good .bak.
                File.Replace(TempPath, path, MainIsValid() ? BackupPath : null);
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException ||
                                      e is PlatformNotSupportedException || e is NotSupportedException)
            {
                Debug.LogWarning($"[FileSaveStorage] File.Replace failed ({e.GetType().Name}); using copy fallback.");
                return false;
            }
        }

        void CommitFirstWrite()
        {
            try { File.Move(TempPath, path); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is NotSupportedException)
            {
                File.Copy(TempPath, path, true);
                File.Delete(TempPath);
            }
        }

        /// <summary>Overwrites the save with the tmp file; the previous save is kept as .bak only when it is still a valid save.</summary>
        void CommitCopy()
        {
            if (MainIsValid()) File.Copy(path, BackupPath, true);
            File.Copy(TempPath, path, true);
            File.Delete(TempPath);
        }

        bool MainIsValid()
        {
            try
            {
                var state = JsonUtility.FromJson<GameState>(File.ReadAllText(path));
                return state != null && state.version == GameState.CurrentVersion;
            }
            catch (Exception) { return false; }
        }
    }
}
