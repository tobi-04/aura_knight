using System.Collections;
using AuraKnight.Core;
using AuraKnight.Progression;
using AuraKnight.UI;
using AuraKnight.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AuraKnight.Tests.PlayMode.Progression
{
    public sealed class ChestAndNpcPlayModeTests : ProgressionPlayModeBase
    {
        [UnityTest]
        public IEnumerator ChestPaysOnceAndStaysOpenAfterContinue()
        {
            yield return EnterWorld(true);
            int before = Manager.State.coins;
            var chest = MakeChest("chest_test_01", new Vector3(500f, 500f, 0f));
            yield return null;
            Assert.IsFalse(chest.IsOpen);

            var outcome = chest.TryOpen();

            Assert.IsTrue(outcome.Opened);
            Assert.IsTrue(chest.IsOpen);
            Assert.That(outcome.Coins, Is.InRange(100, 150));
            Assert.AreEqual(before + outcome.Coins, Manager.State.coins);
            Assert.IsFalse(chest.TryOpen().Opened, "second touch gives nothing");
            Assert.AreEqual(before + outcome.Coins, Manager.State.coins);
            Object.Destroy(chest.gameObject);
            yield return null;

            yield return EnterWorld(false);
            int afterContinue = Manager.State.coins;
            Assert.AreEqual(before + outcome.Coins, afterContinue, "coins were saved with the chest");
            var again = MakeChest("chest_test_01", new Vector3(500f, 500f, 0f));
            yield return null;
            Assert.IsTrue(again.IsOpen, "the chest shows open after Continue");
            Assert.IsFalse(again.TryOpen().Opened);
            Assert.AreEqual(afterContinue, Manager.State.coins);
        }

        [UnityTest]
        public IEnumerator TouchingTheChestWithLeoOpensIt()
        {
            yield return EnterWorld(true);
            var chest = MakeChest("chest_test_02", Player.transform.position, 6f);
            TreasureChest opened = null;
            chest.Opened += _ => opened = chest;
            yield return WaitUntil(() => opened != null, "Leo touching the chest");
            Assert.IsTrue(chest.IsOpen);
            Assert.IsTrue(Manager.State.openedChests.Contains("chest_test_02"));
        }

        [UnityTest]
        public IEnumerator AFreeUpgradeChestRaisesTheLivePlayer()
        {
            yield return EnterWorld(true);
            var chest = MakeChest("chest_test_03", new Vector3(500f, 500f, 0f));
            typeof(TreasureChest).GetField("reward", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(chest, ChestReward.FreeUpgrade(ShopEffect.Heart));
            yield return null;
            int coins = Manager.State.coins;
            var outcome = chest.TryOpen();
            Assert.IsTrue(outcome.UpgradeGranted);
            Assert.AreEqual(6, Stats.Health.Max);
            Assert.AreEqual(coins, Manager.State.coins, "an upgrade chest pays no coins");
        }

        [UnityTest]
        public IEnumerator SolPicksHerLineByProgressAndOpensTheShop()
        {
            yield return EnterWorld(true);
            var dialogue = ScriptableObject.CreateInstance<DialogueByProgress>().SetEntries(new[]
            {
                new DialogueEntry { lineKey = "dialogue.sol.start" },
                new DialogueEntry { lineKey = "dialogue.sol.wind", requiredAuras = new System.Collections.Generic.List<string> { "Wind" } },
            });
            var go = new GameObject("Sol", typeof(BoxCollider2D));
            var sol = go.AddComponent<NpcSol>();
            typeof(NpcSol).GetField("dialogue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(sol, dialogue);
            string heard = null;
            EventBus.Subscribe<ShopRequested>(e => heard = e.DialogueKey);

            sol.Talk();
            Assert.AreEqual("dialogue.sol.start", heard);
            yield return null;
            var shop = Find<ShopScreen>();
            Assert.IsTrue(shop.IsVisible);
            Assert.AreEqual("dialogue.sol.start", shop.DialogueKey);
            UIRouter.Instance.Back();
            yield return null;
            for (int i = 0; i < 30; i++) yield return null;

            Manager.State.unlockedAuras.Add("Wind");
            sol.Talk();
            Assert.AreEqual("dialogue.sol.wind", heard, "a later line once Wind is unlocked");
            Object.Destroy(go);
        }
    }
}
