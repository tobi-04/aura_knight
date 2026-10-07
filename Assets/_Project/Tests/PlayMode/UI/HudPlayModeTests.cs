using System.Collections;
using System.Linq;
using AuraKnight.Core;
using AuraKnight.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.TestTools;

namespace AuraKnight.Tests.PlayMode.UI
{
    /// <summary>The HUD listens to the EventBus only: publish events in the real Core scene and watch the views react.</summary>
    public sealed class HudPlayModeTests : UiPlayModeBase
    {
        [UnityTest]
        public IEnumerator HeartsFollowHeartsChanged()
        {
            var hearts = Find<HeartsView>();
            EventBus.Publish(new HeartsChanged(3, 5));
            Assert.AreEqual(3, hearts.Current);
            Assert.AreEqual(5, hearts.Max);
            Assert.AreEqual(5, hearts.VisibleSlots);
            EventBus.Publish(new HeartsChanged(2, 3));
            Assert.AreEqual(3, hearts.VisibleSlots, "extra slots hide when max hearts shrink");
            EventBus.Publish(new HeartsChanged(9, 3));
            Assert.AreEqual(3, hearts.Current, "current is clamped to max");
            yield break;
        }

        [UnityTest]
        public IEnumerator EnergyBarUsesTheCurrentAuraColour()
        {
            var bar = Find<EnergyBarView>();
            var theme = UITheme.Active;
            EventBus.Publish(new EnergyChanged(40f, 100f));
            Assert.AreEqual(0.4f, bar.Fraction, 1e-4f);
            EventBus.Publish(new AuraChanged("Fire"));
            Assert.AreEqual(theme.AuraColor("Fire"), bar.FillColor);
            EventBus.Publish(new AuraChanged("Water"));
            Assert.AreEqual(theme.AuraColor("Water"), bar.FillColor);
            EventBus.Publish(new AuraChanged("None"));
            Assert.AreEqual(theme.GetColor(UIColorToken.TextMuted), bar.FillColor);
            EventBus.Publish(new EnergyChanged(5f, 0f));
            Assert.AreEqual(0f, bar.Fraction, "zero max never divides");
            yield break;
        }

        [UnityTest]
        public IEnumerator CoinsShowTheCountInGoldMono()
        {
            var coins = Find<CoinsView>();
            EventBus.Publish(new CoinsChanged(42));
            Assert.AreEqual(42, coins.Coins);
            Assert.AreEqual("42", coins.DisplayedText);
            var text = coins.GetComponentInChildren<ThemedText>();
            Assert.AreEqual(UIFontRole.Mono, text.Role);
            Assert.AreEqual(UITheme.Active.GetColor(UIColorToken.Gold), text.Text.color);
            yield break;
        }

        [UnityTest]
        public IEnumerator AuraRingGreysLockedAurasAndHighlightsTheCurrentOne()
        {
            var ring = Find<AuraRingView>();
            Assert.AreEqual(3, ring.Buttons.Count);
            Assert.IsTrue(ring.Buttons.All(b => b.Locked && b.PadlockVisible), "a fresh game has no Aura");
            EventBus.Publish(new AuraUnlocked("Wind"));
            var wind = ring.Buttons.Single(b => b.AuraId == "Wind");
            Assert.IsFalse(wind.Locked);
            Assert.IsFalse(wind.PadlockVisible);
            Assert.IsTrue(ring.Buttons.Where(b => b.AuraId != "Wind").All(b => b.Locked));
            EventBus.Publish(new AuraChanged("Wind"));
            Assert.IsTrue(wind.Selected);
            EventBus.Publish(new AuraUnlocked("Fire"));
            EventBus.Publish(new AuraChanged("Fire"));
            Assert.IsFalse(wind.Selected);
            Assert.IsTrue(ring.Buttons.Single(b => b.AuraId == "Fire").Selected);
            yield break;
        }

        [UnityTest]
        public IEnumerator AuraRingReadsTheSaveWhenAGameLoads()
        {
            var ring = Find<AuraRingView>();
            Manager.State.unlockedAuras.Add("Water");
            Manager.State.currentAura = "Water";
            EventBus.Publish(new GameStateLoaded(false));
            var water = ring.Buttons.Single(b => b.AuraId == "Water");
            Assert.IsFalse(water.Locked);
            Assert.IsTrue(water.Selected);
            yield break;
        }

        [UnityTest]
        public IEnumerator BossBarAppearsFollowsItsBossAndDisappears()
        {
            var bar = Find<BossHealthBarView>();
            Assert.IsFalse(bar.Visible);
            EventBus.Publish(new BossEncounterStarted("root", "GỐC CÂY MỤC"));
            Assert.IsTrue(bar.Visible);
            Assert.AreEqual(1f, bar.Fraction);
            EventBus.Publish(new BossHealthChanged("someone_else", 1, 20));
            Assert.AreEqual(1f, bar.Fraction, "other bosses are ignored");
            EventBus.Publish(new BossHealthChanged("root", 10, 20));
            Assert.AreEqual(0.5f, bar.Fraction, 1e-4f);
            EventBus.Publish(new BossEncounterEnded("root", "GỐC CÂY MỤC"));
            Assert.IsFalse(bar.Visible);
            yield break;
        }

        [UnityTest]
        public IEnumerator HudAndControlsShowOnlyWhilePlaying()
        {
            var controls = Find<VirtualControlsStyler>().GetComponent<CanvasGroup>();
            yield return null;
            Assert.AreEqual(0f, controls.alpha, "Core loaded straight from a test sits in Menu mode");
            bool ok = false;
            yield return Enter(true, r => ok = r);
            Assert.IsTrue(ok);
            yield return null;
            Assert.AreEqual(1f, controls.alpha);
            Assert.IsTrue(Find<HudController>().Visible);
            Assert.IsTrue(PauseController.Instance.Pause(false));
            yield return null;
            Assert.AreEqual(0f, controls.alpha, "hidden while paused");
            PauseController.Instance.Resume();
        }

        [UnityTest]
        public IEnumerator ButtonSettingsRestyleTheOnScreenButtons()
        {
            var buttons = Find<VirtualControlsStyler>().GetComponentsInChildren<OnScreenButton>(true);
            GameSettings.ButtonScale = 1.3f;
            GameSettings.ButtonOpacity = 0.9f;
            Assert.IsTrue(buttons.All(b => Mathf.Approximately(b.transform.localScale.x, 1.3f)));
            var jump = buttons.Single(b => b.name == "Jump").GetComponent<UnityEngine.UI.Image>();
            Assert.AreEqual(0.9f, jump.color.a, 1e-4f);
            GameSettings.ButtonScale = 0.8f;
            Assert.IsTrue(buttons.All(b => Mathf.Approximately(b.transform.localScale.x, 0.8f)));
            yield break;
        }

        [UnityTest]
        public IEnumerator PlayerDeathShowsGameOverUntilRespawn()
        {
            var screen = Find<GameOverScreen>();
            Assert.IsFalse(screen.IsVisible);
            EventBus.Publish(new PlayerDied());
            Assert.IsTrue(screen.IsVisible);
            yield return WaitSeconds(0.35f);
            Assert.IsTrue(screen.gameObject.activeInHierarchy);
            EventBus.Publish(new PlayerRespawned());
            Assert.IsFalse(screen.IsVisible);
            yield return WaitSeconds(0.35f);
            Assert.IsFalse(screen.gameObject.activeSelf, "fully hidden after the fade");
            StringAssert.Contains("ÁNH SÁNG LỤI TẮT", screen.GetComponentInChildren<TMPro.TMP_Text>(true).text);
        }

        [UnityTest]
        public IEnumerator BossBannerShowsTheNameInTheRegionColour()
        {
            var banner = Find<BossIntroBanner>();
            EventBus.Publish(new RoomEntered("forest_01", "forest"));
            EventBus.Publish(new BossEncounterStarted("root", "GỐC CÂY MỤC"));
            Assert.IsTrue(banner.IsVisible);
            Assert.AreEqual("GỐC CÂY MỤC", banner.DisplayedName);
            var label = banner.GetComponentsInChildren<ThemedText>(true).First(t => t.name == "Boss");
            Assert.AreEqual(UITheme.Active.GetColor(UIColorToken.Wind), label.Text.color);
            yield break;
        }
    }
}
