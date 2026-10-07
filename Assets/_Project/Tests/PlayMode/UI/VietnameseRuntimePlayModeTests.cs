using System.Linq;
using AuraKnight.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace AuraKnight.Tests.PlayMode.UI
{
    public sealed class VietnameseRuntimePlayModeTests
    {
        const string Pangram = "Ăn quả nhớ kẻ trồng cây. Tôi yêu Việt Nam: đất nước tươi đẹp, ƯU TIÊN ĐỘC LẬP, QUẢN LÝ HỆ THỐNG, ĐỨNG DẬY!";

        [Test]
        public void EveryThemeFontRendersTheVietnamesePangramWithoutMissingGlyphs()
        {
            var theme = UITheme.Active;
            foreach (UIFontRole role in System.Enum.GetValues(typeof(UIFontRole)))
            {
                var font = theme.GetFont(role);
                Assert.IsNotNull(font, role.ToString());
                Assert.IsTrue(font.HasCharacters(Pangram, out uint[] missing, true, true),
                    $"{role}: missing {(missing == null ? "?" : string.Concat(missing.Select(c => char.ConvertFromUtf32((int)c))))}");
            }
        }

        [Test]
        public void ATextObjectLaysOutEveryVietnameseCharacterAsAVisibleGlyph()
        {
            var canvasGo = new GameObject("canvas", typeof(Canvas));
            var go = new GameObject("vi", typeof(RectTransform));
            go.transform.SetParent(canvasGo.transform, false);
            ((RectTransform)go.transform).sizeDelta = new Vector2(2000f, 200f);
            try
            {
                go.SetActive(false);
                var text = go.AddComponent<TextMeshProUGUI>();
                text.font = UITheme.Active.GetFont(UIFontRole.Body);
                go.SetActive(true);
                text.text = "Tiếng Việt: ắằẳẵặ ếềểễệ ốồổỗộ ớờởỡợ ứừửữự";
                text.ForceMeshUpdate();
                int visible = 0;
                for (int i = 0; i < text.textInfo.characterCount; i++)
                    if (text.textInfo.characterInfo[i].isVisible) visible++;
                Assert.AreEqual(text.text.Count(c => !char.IsWhiteSpace(c)), visible, "each non-space character produced a glyph");
            }
            finally { Object.Destroy(canvasGo); }
        }

        [Test]
        public void LocalizedStringsRoundTripWithDiacritics()
        {
            Localization.Reload();
            Assert.AreEqual("ÁNH SÁNG LỤI TẮT", Localization.Get("gameover.title"));
            Assert.AreEqual("CÀI ĐẶT", Localization.Get("menu.settings"));
        }
    }
}
