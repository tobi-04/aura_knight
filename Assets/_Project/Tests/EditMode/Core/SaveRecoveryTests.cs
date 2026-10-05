using System.IO;
using AuraKnight.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AuraKnight.Tests.Core
{
    /// <summary>Backup generation, corruption recovery and HasSave semantics of the save pipeline.</summary>
    public sealed class SaveRecoveryTests
    {
        sealed class MemoryStorage : ISaveStorage
        {
            public string Main, Backup;
            public bool Exists() => Main != null;
            public string ReadText() => Main;
            public void WriteText(string text) { if (Main != null) Backup = Main; Main = text; }
            public void Delete() { Main = null; Backup = null; }
            public bool BackupExists() => Backup != null;
            public string ReadBackupText() => Backup;
        }

        string _dir;

        [SetUp] public void SetUp() => _dir = Path.Combine(Path.GetTempPath(), "aura_save_rec_" + System.Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        static GameState WithCoins(int coins)
        {
            var s = GameState.NewGame();
            s.coins = coins;
            return s;
        }

        [Test]
        public void HasSave_FalseWhenNothingSaved() => Assert.IsFalse(new SaveSystem(new MemoryStorage()).HasSave);

        [Test]
        public void HasSave_TrueAfterSave()
        {
            var sys = new SaveSystem(new MemoryStorage());
            sys.Save(WithCoins(1));
            Assert.IsTrue(sys.HasSave);
        }

        [Test]
        public void HasSave_FalseForCorruptFileWithoutBackup()
        {
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("SaveSystem"));
            Assert.IsFalse(new SaveSystem(new MemoryStorage { Main = "{broken" }).HasSave);
        }

        [Test]
        public void TryLoad_CorruptMainFallsBackToBackup()
        {
            var storage = new MemoryStorage();
            var sys = new SaveSystem(storage);
            sys.Save(WithCoins(10));
            sys.Save(WithCoins(20));
            storage.Main = "{broken";
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("SaveSystem"));
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("recovered"));
            Assert.IsTrue(sys.TryLoad(out var loaded));
            Assert.AreEqual(10, loaded.coins, "backup holds the previous generation");
        }

        [Test]
        public void TryLoad_MissingMainFallsBackToBackup()
        {
            var storage = new MemoryStorage();
            var sys = new SaveSystem(storage);
            sys.Save(WithCoins(10));
            sys.Save(WithCoins(20));
            storage.Main = null;
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("recovered"));
            Assert.IsTrue(sys.TryLoad(out var loaded));
            Assert.AreEqual(10, loaded.coins);
        }

        [Test]
        public void TryLoad_BothDamaged_ReturnsFalse()
        {
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("SaveSystem"));
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("SaveSystem"));
            var sys = new SaveSystem(new MemoryStorage { Main = "x{", Backup = "y{" });
            Assert.IsFalse(sys.TryLoad(out var loaded));
            Assert.IsNull(loaded);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FileStorage_KeepsPreviousGenerationAsBackup(bool copyOnly)
        {
            var path = Path.Combine(_dir, "save_0.json");
            var storage = new FileSaveStorage(path, copyOnly);
            storage.WriteText(SaveSystemTests.ValidJson(1));
            Assert.IsFalse(storage.BackupExists(), "first write has nothing to back up");
            storage.WriteText(SaveSystemTests.ValidJson(2));
            storage.WriteText(SaveSystemTests.ValidJson(3));
            Assert.AreEqual(SaveSystemTests.ValidJson(3), storage.ReadText());
            Assert.AreEqual(SaveSystemTests.ValidJson(2), storage.ReadBackupText());
            Assert.IsFalse(File.Exists(path + ".tmp"));
        }

        [Test]
        public void FileStorage_SaveSystemRecoversWhenMainIsTruncated()
        {
            var path = Path.Combine(_dir, "save_0.json");
            var sys = new SaveSystem(new FileSaveStorage(path));
            sys.Save(WithCoins(5));
            sys.Save(WithCoins(6));
            File.WriteAllText(path, "{not json");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("SaveSystem"));
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("recovered"));
            Assert.IsTrue(sys.HasSave);
        }

        [Test]
        public void FileStorage_DeleteRemovesAllGenerations()
        {
            var path = Path.Combine(_dir, "save_0.json");
            var storage = new FileSaveStorage(path);
            storage.WriteText(SaveSystemTests.ValidJson(1));
            storage.WriteText(SaveSystemTests.ValidJson(2));
            storage.Delete();
            Assert.IsEmpty(Directory.GetFiles(_dir));
        }

        [Test]
        public void TryLoad_WrongVersionMainIsNotReplacedByAnOlderBackup()
        {
            var storage = new MemoryStorage();
            var sys = new SaveSystem(storage);
            sys.Save(WithCoins(10));
            sys.Save(WithCoins(20));
            var future = GameState.NewGame();
            future.version = 99;
            storage.Main = JsonUtility.ToJson(future);
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("version"));
            Assert.IsFalse(sys.TryLoad(out var loaded), "a newer save must not silently turn into the older backup");
            Assert.IsNull(loaded);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FileStorage_DamagedMainDoesNotOverwriteAGoodBackup(bool copyOnly)
        {
            var path = Path.Combine(_dir, "save_0.json");
            var storage = new FileSaveStorage(path, copyOnly);
            storage.WriteText(SaveSystemTests.ValidJson(1));
            storage.WriteText(SaveSystemTests.ValidJson(2)); // bak = 1
            File.WriteAllText(path, "{broken");
            storage.WriteText(SaveSystemTests.ValidJson(3));
            Assert.AreEqual(SaveSystemTests.ValidJson(3), storage.ReadText());
            Assert.AreEqual(SaveSystemTests.ValidJson(1), storage.ReadBackupText(), "the damaged main must not have become the backup");
        }

        [Test]
        public void FileStorage_FirstWriteLeavesOnlyTheSaveFile()
        {
            var path = Path.Combine(_dir, "save_0.json");
            new FileSaveStorage(path).WriteText(SaveSystemTests.ValidJson(1));
            CollectionAssert.AreEqual(new[] { path }, Directory.GetFiles(_dir));
        }
    }
}
