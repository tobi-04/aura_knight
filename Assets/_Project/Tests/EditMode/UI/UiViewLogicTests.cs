using AuraKnight.Editor;
using AuraKnight.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AuraKnight.Tests.UI
{
    public sealed class UiViewLogicTests
    {
        [Test]
        public void AuraButtonShowsLockedUnlockedAndSelectedStates()
        {
            var go = new GameObject("aura", typeof(RectTransform), typeof(Image));
            var padlock = new GameObject("lock");
            padlock.transform.SetParent(go.transform);
            var labelGo = new GameObject("label");
            labelGo.transform.SetParent(go.transform);
            try
            {
                var view = go.AddComponent<AuraButtonView>();
                var image = go.GetComponent<Image>();
                view.Bind("Fire", image, null, padlock);
                var theme = UITheme.Active;

                view.Apply(false, false, 0.6f);
                Assert.IsTrue(view.Locked);
                Assert.IsTrue(padlock.activeSelf, "locked shows the padlock");
                Assert.AreEqual(theme.GetColor(UIColorToken.TextMuted).r, image.color.r, 1e-4f, "locked is grey");

                view.Apply(true, false, 0.6f);
                Assert.IsFalse(view.Locked);
                Assert.IsFalse(padlock.activeSelf);
                Assert.AreEqual(theme.AuraColor("Fire").r, image.color.r, 1e-4f);
                float idleAlpha = image.color.a;

                view.Apply(true, true, 0.6f);
                Assert.IsTrue(view.Selected);
                Assert.Greater(image.color.a, idleAlpha, "the current Aura is brighter");

                view.Apply(false, true, 0.6f);
                Assert.IsFalse(view.Selected, "a locked Aura cannot be selected");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void CreditsTextAggregatesSourcesAndToleratesMissingOnes()
        {
            string text = CreditsTextBuilder.Compose(path => path.EndsWith("Fonts/LICENSES.md")
                ? "# Fonts\n\n| File | Role |\n|---|---|\n| `A.ttf` | Display |\n"
                : path.EndsWith("Audio/LICENSES.md") ? "Sound by someone, CC0\n" : null);
            StringAssert.StartsWith("AURA KNIGHT", text);
            StringAssert.Contains("PHÔNG CHỮ", text);
            StringAssert.Contains("A.ttf  -  Display", text);
            StringAssert.Contains("ÂM THANH", text);
            StringAssert.Contains("Sound by someone, CC0", text);
            StringAssert.DoesNotContain("HÌNH ẢNH", text, "missing Art/LICENSES.md is skipped");
            StringAssert.DoesNotContain("---", text);
            StringAssert.DoesNotContain("|", text);
        }

        [Test]
        public void CreditsTextWithNoSourcesIsJustTheHeader()
        {
            string text = CreditsTextBuilder.Compose(_ => null);
            Assert.AreEqual("AURA KNIGHT: MẢNH VỠ ÁNH SÁNG\n", text.Replace("\r", ""));
        }

        [Test]
        public void ShippedCreditsTextMentionsTheFonts()
        {
            var asset = Resources.Load<TextAsset>(CreditsScreen.ResourceName);
            Assert.IsNotNull(asset, "Resources/CreditsText.txt");
            StringAssert.Contains("Chakra Petch", asset.text);
        }
    }
}
