using System.Collections.Generic;
using System.IO;
using AuraKnight.Core;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Core
{
    public sealed class SaveSystemTests
    {
        sealed class MemoryStorage : ISaveStorage
        {
            public string Text;
            public bool FailWrite;
            public bool Exists() => Text != null;
            public string ReadText() => Text;
            public void WriteText(string text)
            {
                if (FailWrite) throw new IOException("disk full");
                Text = text;
            }
            public void Delete() => Text = null;
        }

        static GameState Sample()
        {
            var s = GameState.NewGame();
            s.lastAltarId = "forest_altar_02";
            s.maxHearts = 6; s.maxEnergy = 125; s.coins = 340;
            s.unlockedAuras.Add("Wind"); s.currentAura = "Wind";
            s.MarkBossDefeated("RootTree");
            s.MarkRoomVisited("hub_01"); s.MarkRoomVisited("forest_01");
            s.openedChests.Add("forest_chest_a");
            s.MarkShortcutOpened("forest_sc_1");
            s.AddPurchase("heart"); s.AddPurchase("map_forest");
            s.playTimeSeconds = 1834.5f;
            return s;
        }

        [Test]
        public void SaveThenLoad_RoundTripsAllFields()
        {
            var sys = new SaveSystem(new MemoryStorage());
            Assert.IsTrue(sys.Save(Sample()));
            Assert.IsTrue(sys.TryLoad(out var loaded));
            Assert.AreEqual("forest_altar_02", loaded.lastAltarId);
            Assert.AreEqual(6, loaded.maxHearts);
            Assert.AreEqual(125, loaded.maxEnergy);
            Assert.AreEqual(340, loaded.coins);
            CollectionAssert.AreEqual(new[] { "Wind" }, loaded.unlockedAuras);
            Assert.AreEqual("Wind", loaded.currentAura);
            CollectionAssert.AreEqual(new[] { "RootTree" }, loaded.defeatedBosses);
            CollectionAssert.AreEqual(new[] { "hub_01", "forest_01" }, loaded.visitedRooms);
            CollectionAssert.AreEqual(new[] { "forest_chest_a" }, loaded.openedChests);
            CollectionAssert.AreEqual(new[] { "forest_sc_1" }, loaded.openedShortcuts);
            Assert.AreEqual(1, loaded.GetPurchaseCount("heart"));
            Assert.AreEqual(1, loaded.GetPurchaseCount("map_forest"));
            Assert.AreEqual(1834.5f, loaded.playTimeSeconds, 0.001f);
        }

        [Test]
        public void Save_WritesCurrentVersion()
        {
            var storage = new MemoryStorage();
            new SaveSystem(storage).Save(GameState.NewGame());
            StringAssert.Contains("\"version\":1", storage.Text);
        }

        [Test]
        public void TryLoad_NoFile_ReturnsFalseWithoutWarning()
        {
            Assert.IsFalse(new SaveSystem(new MemoryStorage()).TryLoad(out var s));
            Assert.IsNull(s);
        }

        [TestCase("{not json")]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("[1,2,3]")]
        public void TryLoad_CorruptFile_ReturnsFalseAndWarns(string garbage)
        {
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Save"));
            var sys = new SaveSystem(new MemoryStorage { Text = garbage });
            Assert.IsFalse(sys.TryLoad(out var s));
            Assert.IsNull(s);
        }

        [Test]
        public void TryLoad_UnknownVersion_ReturnsFalseAndWarns()
        {
            var future = GameState.NewGame();
            future.version = 99;
            var storage = new MemoryStorage { Text = JsonUtility.ToJson(future) };
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("version"));
            Assert.IsFalse(new SaveSystem(storage).TryLoad(out _));
        }

        [Test]
        public void TryLoad_MissingFieldsInJson_YieldsUsableState()
        {
            var storage = new MemoryStorage { Text = "{\"version\":1,\"coins\":7}" };
            Assert.IsTrue(new SaveSystem(storage).TryLoad(out var s));
            Assert.AreEqual(7, s.coins);
            Assert.IsNotNull(s.visitedRooms);
            Assert.IsNotNull(s.shopPurchases);
            Assert.AreEqual(0, s.GetPurchaseCount("x"));
        }

        [Test]
        public void Save_StorageFailure_ReturnsFalseAndLogsWarning()
        {
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Save"));
            var sys = new SaveSystem(new MemoryStorage { FailWrite = true });
            Assert.IsFalse(sys.Save(GameState.NewGame()));
        }

        [Test]
        public void Save_Null_ReturnsFalse()
        {
            Assert.IsFalse(new SaveSystem(new MemoryStorage()).Save(null));
        }

        internal static string ValidJson(int coins)
        {
            var s = GameState.NewGame();
            s.coins = coins;
            return JsonUtility.ToJson(s);
        }

        [Test]
        public void FileStorage_AtomicWriteReplacesAndLeavesNoTempFile()
        {
            string dir = Path.Combine(Path.GetTempPath(), "aura_save_test_" + System.Guid.NewGuid().ToString("N"));
            string path = Path.Combine(dir, "save_0.json");
            try
            {
                var storage = new FileSaveStorage(path);
                Assert.IsFalse(storage.Exists());
                storage.WriteText(ValidJson(1));
                storage.WriteText(ValidJson(2));
                Assert.IsTrue(storage.Exists());
                Assert.AreEqual(ValidJson(2), storage.ReadText());
                CollectionAssert.AreEquivalent(new[] { path, path + ".bak" }, Directory.GetFiles(dir)); // no .tmp left behind
                storage.Delete();
                Assert.IsFalse(storage.Exists());
                Assert.IsEmpty(Directory.GetFiles(dir));
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        }
    }
}
