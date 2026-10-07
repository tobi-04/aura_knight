using System.Collections;
using AuraKnight.Core;
using AuraKnight.Progression;
using AuraKnight.UI;
using AuraKnight.World.Pickups;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AuraKnight.Tests.PlayMode.Progression
{
    public sealed class ShopPlayModeTests : ProgressionPlayModeBase
    {
        static readonly int[] HeartPrices = { 100, 200, 300, 400 };

        [UnityTest]
        public IEnumerator BuyingAHeartRaisesMaxHeartsLiveAndSurvivesContinue()
        {
            yield return EnterWorld(true);
            Manager.State.coins = 500;
            Assert.AreEqual(5, Stats.Health.Max);
            var heart = Item("heart", ShopEffect.Heart, 1, HeartPrices, 4);

            Assert.AreEqual(ShopResult.Bought, ShopService.Buy(heart));

            Assert.AreEqual(6, Stats.Health.Max, "live max hearts");
            Assert.AreEqual(6, Stats.Health.Current, "the new heart arrives full");
            Assert.AreEqual(400, Manager.State.coins);
            Assert.IsTrue(new SaveSystem(new FileSaveStorage(SavePath)).TryLoad(out var onDisk), "the purchase was saved");
            Assert.AreEqual(6, onDisk.maxHearts);
            Assert.AreEqual(400, onDisk.coins);

            yield return EnterWorld(false);
            Assert.AreEqual(6, Manager.State.maxHearts);
            Assert.AreEqual(6, Stats.Health.Max, "max hearts after Continue");
            Assert.AreEqual(1, Manager.State.GetPurchaseCount("heart"));
        }

        [UnityTest]
        public IEnumerator EnergyAndSwordUpgradesReachTheLivePlayer()
        {
            yield return EnterWorld(true);
            Manager.State.coins = 1000;
            Assert.AreEqual(ShopResult.Bought, ShopService.Buy(Item("energy", ShopEffect.Energy, 25, new[] { 120 }, 4)));
            Assert.AreEqual(ShopResult.Bought, ShopService.Buy(Item("sword", ShopEffect.Sword, 1, new[] { 300, 600 }, 2)));
            Assert.AreEqual(125f, Stats.Energy.Max);
            Assert.AreEqual(2, Stats.SwordLevel);
            Assert.AreEqual(580, Manager.State.coins);
        }

        [UnityTest]
        public IEnumerator AFailedPurchaseChangesNothingLive()
        {
            yield return EnterWorld(true);
            Manager.State.coins = 99;
            Assert.AreEqual(ShopResult.NotEnoughCoins, ShopService.Buy(Item("heart", ShopEffect.Heart, 1, HeartPrices, 4)));
            Assert.AreEqual(5, Stats.Health.Max);
            Assert.AreEqual(99, Manager.State.coins);
        }

        [UnityTest]
        public IEnumerator PickedUpCoinsGoThroughTheWallet()
        {
            yield return EnterWorld(true);
            int before = Manager.State.coins;
            int events = 0, last = -1;
            EventBus.Subscribe<CoinsChanged>(e => { events++; last = e.Coins; });
            Assert.AreEqual(before + 7, CoinCollector.Collect(7));
            Assert.AreEqual(1, events);
            Assert.AreEqual(before + 7, last);
            Assert.AreEqual(-1, CoinCollector.Collect(0));
            Assert.AreEqual(1, events, "an empty pickup announces nothing");
        }

        [UnityTest]
        public IEnumerator TalkingToSolOpensTheShopWithTheCardsAndBuyingWorks()
        {
            yield return EnterWorld(true);
            Manager.State.coins = 150;
            var shop = Find<ShopScreen>();
            EventBus.Publish(new ShopRequested("dialogue.sol.start"));
            yield return null;
            Assert.IsTrue(shop.IsVisible, "shop opened");
            Assert.AreEqual("dialogue.sol.start", shop.DialogueKey);
            Assert.IsTrue(PauseController.Instance.IsPaused, "game paused behind the shop");
            Assert.AreEqual(7, shop.Cards.Count);

            var heartCard = shop.Cards[0];
            Assert.AreEqual("100", heartCard.PriceText);
            Assert.IsTrue(heartCard.CanBuy);
            Assert.IsTrue(shop.Cards[1].CanBuy, "energy costs 120 and there are 150 coins");
            heartCard.Click();
            Assert.AreEqual(ShopResult.Bought, shop.LastResult);
            Assert.AreEqual(6, Stats.Health.Max);
            Assert.AreEqual("200", heartCard.PriceText, "the card now shows the second tier");
            Assert.IsFalse(heartCard.CanBuy, "50 coins cannot pay 200");
            Assert.IsFalse(shop.Cards[1].CanBuy, "and not the 120 for energy either");

            UIRouter.Instance.Back();
            yield return WaitFrames(30);
            Assert.IsFalse(shop.IsVisible);
            Assert.IsFalse(PauseController.Instance.IsPaused);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator ShopDoesNotOpenWhileTheGameIsPaused()
        {
            yield return EnterWorld(true);
            Assert.IsTrue(PauseController.Instance.Pause(false));
            EventBus.Publish(new ShopRequested("dialogue.sol.start"));
            yield return null;
            Assert.IsFalse(Find<ShopScreen>().IsVisible);
            PauseController.Instance.Resume();
        }

        static IEnumerator WaitFrames(int count)
        {
            for (int i = 0; i < count; i++) yield return null;
        }
    }
}
