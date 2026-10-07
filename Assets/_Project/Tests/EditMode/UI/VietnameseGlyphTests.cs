using System.Linq;
using System.Text;
using AuraKnight.Editor;
using AuraKnight.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace AuraKnight.Tests.UI
{
    public sealed class VietnameseGlyphTests
    {
        const string Pangram = "Tôi yêu Việt Nam: đất nước tươi đẹp, người dân thân thiện. ƯU TIÊN ĐỘC LẬP, QUẢN LÝ HỆ THỐNG.";

        static string AllVietnameseLetters()
        {
            const string lower = "aàáảãạăằắẳẵặâầấẩẫậđeèéẻẽẹêềếểễệiìíỉĩịoòóỏõọôồốổỗộơờớởỡợuùúủũụưừứửữựyỳýỷỹỵ";
            return lower + lower.ToUpperInvariant();
        }

        [Test]
        public void EveryFontAssetCoversAllVietnameseLettersAndTheGameStrings()
        {
            var theme = Resources.Load<UITheme>(UITheme.ResourceName);
            var strings = Resources.Load<TextAsset>("Strings_vi");
            StringTable.TryParse(strings.text, out var table);
            string all = AllVietnameseLetters() + Pangram + string.Concat(table.Values);
            foreach (UIFontRole role in System.Enum.GetValues(typeof(UIFontRole)))
            {
                var font = theme.GetFont(role);
                bool ok = font.HasCharacters(all, out uint[] missing, true, true);
                Assert.IsTrue(ok, $"{role} ({font.name}) misses: {Describe(missing)}");
            }
        }

        [Test]
        public void BeVietnamProIsTheTmpDefaultAndTheFallbackOfTheOthers()
        {
            var theme = Resources.Load<UITheme>(UITheme.ResourceName);
            var body = theme.GetFont(UIFontRole.Body);
            Assert.AreSame(body, TMP_Settings.defaultFontAsset);
            foreach (var role in new[] { UIFontRole.Display, UIFontRole.Mono, UIFontRole.MonoRegular, UIFontRole.Number })
                CollectionAssert.Contains(theme.GetFont(role).fallbackFontAssetTable, body, role.ToString());
        }

        [Test]
        public void LicenseFileListsEveryFontAndTheOflNotice()
        {
            string text = System.IO.File.ReadAllText(UiAssetPaths.FontsDir + "/LICENSES.md");
            foreach (string file in new[] { "ChakraPetch-Bold", "IBMPlexMono-Bold", "IBMPlexMono-Regular", "BeVietnamPro-Regular", "BarlowCondensed-Bold" })
                StringAssert.Contains(file, text);
            StringAssert.Contains("SIL Open Font License", text);
        }

        static string Describe(uint[] missing) =>
            missing == null ? "(font not readable)" : string.Concat(missing.Select(c => char.ConvertFromUtf32((int)c)));
    }
}
