using System.Linq;
using AuraKnight.Core;
using AuraKnight.UI;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.UI
{
    public sealed class LocalizationTests
    {
        [TearDown]
        public void ReloadFromDisk() => Localization.Reload();

        static StringTable ViTable()
        {
            var asset = Resources.Load<TextAsset>("Strings_vi");
            Assert.IsNotNull(asset, "Data/UI/Resources/Strings_vi.json");
            Assert.IsTrue(StringTable.TryParse(asset.text, out var table));
            return table;
        }

        [Test]
        public void ParsesEntriesAndRejectsGarbage()
        {
            Assert.IsTrue(StringTable.TryParse("{\"entries\":[{\"key\":\"a\",\"value\":\"x\"}]}", out var table));
            Assert.IsTrue(table.TryGet("a", out var value));
            Assert.AreEqual("x", value);
            Assert.IsFalse(StringTable.TryParse("not json", out _));
            Assert.IsFalse(StringTable.TryParse("", out _));
            Assert.IsFalse(StringTable.TryParse("{\"entries\":[]}", out _));
            Assert.IsFalse(StringTable.TryParse(null, out _));
        }

        [Test]
        public void ReportsDuplicateKeys()
        {
            StringTable.TryParse("{\"entries\":[{\"key\":\"a\",\"value\":\"1\"},{\"key\":\"a\",\"value\":\"2\"}]}", out var table);
            CollectionAssert.AreEqual(new[] { "a" }, table.Duplicates);
        }

        [Test]
        public void VietnameseTableHasNoDuplicatesOrEmptyValues()
        {
            var table = ViTable();
            Assert.IsEmpty(table.Duplicates, "duplicate keys: " + string.Join(", ", table.Duplicates));
            foreach (string key in table.Keys)
            {
                table.TryGet(key, out var value);
                Assert.IsFalse(string.IsNullOrWhiteSpace(value), $"'{key}' is empty");
            }
        }

        [Test]
        public void MissingKeysAreVisibleNotBlank()
        {
            Localization.UseTable(new StringTable(), "vi");
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Missing string key"));
            Assert.AreEqual("!no.such.key!", Localization.Get("no.such.key"));
            Assert.AreEqual(string.Empty, Localization.Get(null));
        }

        [Test]
        public void FormatFillsPlaceholdersAndToleratesBadPatterns()
        {
            var table = new StringTable();
            table.Set("boss", "BOSS / {0}");
            table.Set("bad", "{0} {1}");
            Localization.UseTable(table, "vi");
            Assert.AreEqual("BOSS / NHỆN", Localization.Format("boss", "NHỆN"));
            Assert.AreEqual("{0} {1}", Localization.Format("bad", "only-one"));
        }

        [Test]
        public void SupportedLanguagesAreVietnameseOnlyForNow()
        {
            CollectionAssert.AreEqual(new[] { "vi" }, Localization.SupportedLanguages);
            Assert.IsTrue(Localization.IsSupported("vi"));
            Assert.IsFalse(Localization.IsSupported("en"));
        }

        [Test]
        public void SetLanguageRaisesTheChangeEventOnce()
        {
            int calls = 0;
            void Handler() => calls++;
            Localization.UseTable(new StringTable(), "other");
            Localization.LanguageChanged += Handler;
            Localization.SetLanguage("en"); // unsupported: reloads vi
            Localization.LanguageChanged -= Handler;
            Assert.AreEqual(1, calls);
            Assert.AreEqual("vi", Localization.CurrentLanguage);
        }

        [Test]
        public void EveryAuraHasANameTagAndFourAbilities()
        {
            var table = ViTable();
            foreach (string id in new[] { "wind", "fire", "water" })
            {
                Assert.IsTrue(table.TryGet($"aura.{id}.name", out _), id);
                Assert.IsTrue(table.TryGet($"aura.{id}.tag", out _), id);
                for (int i = 1; i <= 4; i++) Assert.IsTrue(table.TryGet($"aura.{id}.s{i}", out _), $"{id} s{i}");
            }
        }

        [Test]
        public void ViewsFindTheirKeysThroughTheGameplayFlow()
        {
            Localization.SetLanguage("vi");
            Assert.AreEqual("TIẾP TỤC", Localization.Get("menu.continue"));
            Assert.AreEqual("ÁNH SÁNG LỤI TẮT", Localization.Get("gameover.title"));
            Assert.IsTrue(Localization.Has("pause.menu"));
            Assert.IsFalse(ViTable().Keys.Any(k => k.Contains(" ")), "keys have no spaces");
        }
    }
}
