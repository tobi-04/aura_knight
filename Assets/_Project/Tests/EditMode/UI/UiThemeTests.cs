using AuraKnight.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace AuraKnight.Tests.UI
{
    public sealed class UiThemeTests
    {
        static Color Hex(string hex)
        {
            Assert.IsTrue(ColorUtility.TryParseHtmlString(hex, out var color));
            return color;
        }

        [Test]
        public void ThemeAssetCarriesEveryGddToken()
        {
            var theme = Resources.Load<UITheme>(UITheme.ResourceName);
            Assert.IsNotNull(theme, "Data/UI/Resources/UITheme.asset");
            Assert.AreEqual(Hex("#070D1F"), theme.GetColor(UIColorToken.Night));
            Assert.AreEqual(Hex("#0E1A2C"), theme.GetColor(UIColorToken.Panel));
            Assert.AreEqual(Hex("#F1F0E5"), theme.GetColor(UIColorToken.Paper));
            Assert.AreEqual(Hex("#D9D3C7"), theme.GetColor(UIColorToken.PaperAlt));
            Assert.AreEqual(Hex("#FFC857"), theme.GetColor(UIColorToken.Gold));
            Assert.AreEqual(Hex("#27D38C"), theme.GetColor(UIColorToken.Wind));
            Assert.AreEqual(Hex("#FF5C57"), theme.GetColor(UIColorToken.Fire));
            Assert.AreEqual(Hex("#27B5F7"), theme.GetColor(UIColorToken.Water));
            Assert.AreEqual(Color.white, theme.GetColor(UIColorToken.TextPrimary));
            Assert.AreEqual(Hex("#8A93A6"), theme.GetColor(UIColorToken.TextMuted));
            Assert.AreEqual(Hex("#0C1824"), theme.GetColor(UIColorToken.TextInk));
        }

        [Test]
        public void LayoutAndMotionTokensMatchTheDesignLanguage()
        {
            var theme = Resources.Load<UITheme>(UITheme.ResourceName);
            Assert.AreEqual(6f, theme.AccentBarWidth);
            Assert.AreEqual(4f, theme.CardBorderWidth);
            Assert.AreEqual(15f, theme.MonoSpacing, "+15% tracking");
            Assert.AreEqual(0.2f, theme.TransitionSeconds);
            Assert.AreEqual(16f, theme.SlidePixels);
            Assert.AreEqual(0.95f, theme.PressScale);
        }

        [Test]
        public void EveryFontRoleHasADynamicAssetWithTheRightFace()
        {
            var theme = Resources.Load<UITheme>(UITheme.ResourceName);
            var expected = new[]
            {
                (UIFontRole.Display, "ChakraPetch-Bold"), (UIFontRole.Mono, "IBMPlexMono-Bold"), (UIFontRole.MonoRegular, "IBMPlexMono-Regular"),
                (UIFontRole.Body, "BeVietnamPro-Regular"), (UIFontRole.Number, "BarlowCondensed-Bold")
            };
            foreach (var (role, file) in expected)
            {
                var font = theme.GetFont(role);
                Assert.IsNotNull(font, role.ToString());
                Assert.AreEqual(AtlasPopulationMode.Dynamic, font.atlasPopulationMode, role + " must be Dynamic for Vietnamese");
                Assert.AreEqual(file, font.sourceFontFile.name, role.ToString());
            }
        }

        [Test]
        public void AuraAndRegionColoursFollowTheGdd()
        {
            Assert.AreEqual(UIColorToken.Wind, UITheme.AuraToken("Wind"));
            Assert.AreEqual(UIColorToken.Fire, UITheme.AuraToken("Fire"));
            Assert.AreEqual(UIColorToken.Water, UITheme.AuraToken("Water"));
            Assert.AreEqual(UIColorToken.TextMuted, UITheme.AuraToken("None"));
            Assert.AreEqual(UIColorToken.Wind, UITheme.RegionToken("forest"));
            Assert.AreEqual(UIColorToken.Gold, UITheme.RegionToken("cave"));
            Assert.AreEqual(UIColorToken.Water, UITheme.RegionToken("city"));
            Assert.AreEqual(UIColorToken.Fire, UITheme.RegionToken("castle"));
            Assert.AreEqual(UIColorToken.Gold, UITheme.RegionToken(null));
            Assert.AreEqual(UIColorToken.Gold, UITheme.RegionToken("hub"));
        }

        [Test]
        public void ThemedTextAppliesFontColourAndMonoTracking()
        {
            var go = new GameObject("t");
            try
            {
                go.SetActive(false);
                var tmp = go.AddComponent<TextMeshProUGUI>();
                var themed = go.AddComponent<ThemedText>();
                themed.Configure(UIFontRole.Mono, UIColorToken.Gold);
                var theme = UITheme.Active;
                Assert.AreEqual(theme.GetFont(UIFontRole.Mono), tmp.font);
                Assert.AreEqual(theme.GetColor(UIColorToken.Gold), tmp.color);
                Assert.AreEqual(theme.MonoSpacing, tmp.characterSpacing);
                Assert.IsTrue((tmp.fontStyle & FontStyles.UpperCase) != 0, "mono labels are uppercase");
                themed.Configure(UIFontRole.Body, UIColorToken.TextPrimary);
                Assert.AreEqual(0f, tmp.characterSpacing);
                Assert.IsFalse((tmp.fontStyle & FontStyles.UpperCase) != 0);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
