namespace AuraKnight.Core
{
    /// <summary>Text persistence backend so SaveSystem can be tested with in-memory storage.</summary>
    public interface ISaveStorage
    {
        bool Exists();
        string ReadText();
        /// <summary>Must be atomic: a crash mid-write may never leave a half-written save behind.</summary>
        void WriteText(string text);
        void Delete();

        /// <summary>True when a previous-generation copy exists (kept by <see cref="WriteText"/> for crash/corruption recovery).</summary>
        bool BackupExists() => false;
        string ReadBackupText() => null;
    }
}
