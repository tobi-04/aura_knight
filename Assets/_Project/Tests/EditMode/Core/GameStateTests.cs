using AuraKnight.Core;
using NUnit.Framework;

namespace AuraKnight.Tests.Core
{
    public sealed class GameStateTests
    {
        [Test]
        public void NewGame_HasDocumentedDefaults()
        {
            var s = GameState.NewGame();
            Assert.AreEqual(GameState.CurrentVersion, s.version);
            Assert.AreEqual(1, s.version);
            Assert.AreEqual("hub_altar_01", s.lastAltarId);
            Assert.AreEqual(5, s.maxHearts);
            Assert.AreEqual(100, s.maxEnergy);
            Assert.AreEqual(1, s.swordLevel);
            Assert.AreEqual(0, s.coins);
            Assert.AreEqual("None", s.currentAura);
            Assert.IsEmpty(s.unlockedAuras);
            Assert.IsEmpty(s.defeatedBosses);
            Assert.IsEmpty(s.visitedRooms);
            Assert.IsEmpty(s.openedChests);
            Assert.IsEmpty(s.openedShortcuts);
            Assert.IsEmpty(s.openedGates);
            Assert.AreEqual(0f, s.playTimeSeconds);
        }

        [Test]
        public void GetPurchaseCount_UnknownKey_IsZero()
        {
            Assert.AreEqual(0, GameState.NewGame().GetPurchaseCount("heart"));
        }

        [Test]
        public void AddPurchase_AccumulatesPerKey()
        {
            var s = GameState.NewGame();
            s.AddPurchase("heart");
            s.AddPurchase("heart");
            s.AddPurchase("energy", 3);
            Assert.AreEqual(2, s.GetPurchaseCount("heart"));
            Assert.AreEqual(3, s.GetPurchaseCount("energy"));
            Assert.AreEqual(2, s.shopPurchases.Count);
        }

        [Test]
        public void MarkHelpers_AreIdempotent()
        {
            var s = GameState.NewGame();
            Assert.IsTrue(s.MarkRoomVisited("hub_01"));
            Assert.IsFalse(s.MarkRoomVisited("hub_01"));
            Assert.IsTrue(s.MarkBossDefeated("RootTree"));
            Assert.IsFalse(s.MarkBossDefeated("RootTree"));
            Assert.IsTrue(s.MarkShortcutOpened("forest_sc_1"));
            Assert.IsFalse(s.MarkShortcutOpened("forest_sc_1"));
            Assert.IsTrue(s.HasOpenedShortcut("forest_sc_1"));
            Assert.AreEqual(1, s.visitedRooms.Count);
        }

        [Test]
        public void MarkHelpers_RejectEmptyIds()
        {
            var s = GameState.NewGame();
            Assert.IsFalse(s.MarkRoomVisited(""));
            Assert.IsFalse(s.MarkBossDefeated(null));
            Assert.IsEmpty(s.visitedRooms);
        }
    }
}
