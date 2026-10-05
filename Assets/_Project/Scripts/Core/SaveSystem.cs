using System;
using UnityEngine;

namespace AuraKnight.Core
{
    /// <summary>
    /// Serializes GameState via JsonUtility. Never throws: failures are logged and reported by return value,
    /// so a corrupt or newer-version file can never crash the game at boot.
    /// </summary>
    public sealed class SaveSystem
    {
        readonly ISaveStorage storage;

        public SaveSystem() : this(new FileSaveStorage()) { }
        public SaveSystem(ISaveStorage storage) { this.storage = storage; }

        /// <summary>True only when a loadable save exists (main file, or its backup when the main one is damaged).</summary>
        public bool HasSave => TryLoad(out _);

        public bool Save(GameState state)
        {
            if (state == null) return false;
            try
            {
                state.version = GameState.CurrentVersion;
                storage.WriteText(JsonUtility.ToJson(state));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Save failed: {e.Message}");
                return false;
            }
        }

        public bool TryLoad(out GameState state)
        {
            state = null;
            var main = SafeExists() ? TryParse(Guarded(storage.ReadText), "save file", out state) : ParseResult.Unusable;
            if (main == ParseResult.Ok) return true;
            // A save from another version is not damage: do not quietly replace it with an older backup.
            if (main == ParseResult.WrongVersion) return false;
            if (SafeBackupExists() && TryParse(Guarded(storage.ReadBackupText), "backup save", out state) == ParseResult.Ok)
            {
                Debug.LogWarning("[SaveSystem] Main save unusable; recovered from the backup.");
                return true;
            }
            state = null;
            return false;
        }

        static string Guarded(Func<string> read)
        {
            try { return read(); }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Save file unreadable: {e.Message}");
                return null;
            }
        }

        enum ParseResult { Ok, Unusable, WrongVersion }

        static ParseResult TryParse(string json, string what, out GameState state)
        {
            state = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                if (json != null) Debug.LogWarning($"[SaveSystem] The {what} is empty; ignoring it.");
                return ParseResult.Unusable;
            }
            try
            {
                var loaded = JsonUtility.FromJson<GameState>(json);
                if (loaded == null || loaded.version != GameState.CurrentVersion)
                {
                    Debug.LogWarning($"[SaveSystem] Unsupported {what} version {(loaded == null ? "?" : loaded.version.ToString())} " +
                                     $"(expected {GameState.CurrentVersion}); ignoring it.");
                    return loaded == null ? ParseResult.Unusable : ParseResult.WrongVersion;
                }
                loaded.Normalize();
                state = loaded;
                return ParseResult.Ok;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] The {what} is corrupt: {e.Message}");
                return ParseResult.Unusable;
            }
        }

        public void Delete()
        {
            try { storage.Delete(); }
            catch (Exception e) { Debug.LogWarning($"[SaveSystem] Delete failed: {e.Message}"); }
        }

        bool SafeBackupExists()
        {
            try { return storage.BackupExists(); }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Backup check failed: {e.Message}");
                return false;
            }
        }

        bool SafeExists()
        {
            try { return storage.Exists(); }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Storage check failed: {e.Message}");
                return false;
            }
        }
    }
}
