using System.Collections;
using AuraKnight.Aura;
using AuraKnight.Core;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AuraKnight.Tests.PlayMode
{
    /// <summary>When the live game writes the save: altar re-entry is free, an Aura unlock is written at once.</summary>
    public sealed class SaveFlowPlayModeTests : WorldPlayTestBase
    {
        sealed class CountingStorage : ISaveStorage
        {
            public string Text;
            public int Writes;
            public bool Exists() => Text != null;
            public string ReadText() => Text;
            public void WriteText(string text) { Text = text; Writes++; }
            public void Delete() => Text = null;
        }

        [UnityTest]
        public IEnumerator TouchingTheSameAltarAgainDoesNotWriteTheSaveAgain()
        {
            var storage = new CountingStorage();
            Manager.UseSaveSystem(new SaveSystem(storage));

            EventBus.Publish(new CheckpointReached("forest_altar_01"));
            Assert.AreEqual(1, storage.Writes, "a new altar is saved");
            Assert.AreEqual("forest_altar_01", Manager.State.lastAltarId);

            EventBus.Publish(new CheckpointReached("forest_altar_01"));
            EventBus.Publish(new CheckpointReached("forest_altar_01"));
            Assert.AreEqual(1, storage.Writes, "re-entering the same altar only heals, no disk write");

            EventBus.Publish(new CheckpointReached("hub_altar_01"));
            Assert.AreEqual(2, storage.Writes, "a different altar is saved again");
            yield return null;
        }

        [UnityTest]
        public IEnumerator FirstAltarIsSavedEvenWhenItIsTheDefaultOne()
        {
            var storage = new CountingStorage();
            Manager.UseSaveSystem(new SaveSystem(storage));
            EventBus.Publish(new CheckpointReached(GameState.StartAltarId)); // equals the default id, but nothing is on disk yet
            Assert.AreEqual(1, storage.Writes);
            yield return null;
        }

        [UnityTest]
        public IEnumerator UnlockingAnAuraWritesTheSaveWithoutWaitingForBossDefeated()
        {
            bool ok = false;
            yield return Enter(true, r => ok = r);
            Assert.IsTrue(ok);
            yield return SettlePhysics(); // the hub altar Leo stands on writes the first save
            Assert.IsTrue(new SaveSystem(new FileSaveStorage(SavePath)).TryLoad(out var before));
            CollectionAssert.DoesNotContain(before.unlockedAuras, "Fire");

            Assert.IsTrue(AuraManager.Instance.Unlock(AuraId.Fire));

            Assert.IsTrue(new SaveSystem(new FileSaveStorage(SavePath)).TryLoad(out var onDisk), "the unlock reached the disk");
            CollectionAssert.Contains(onDisk.unlockedAuras, "Fire");
            Assert.AreEqual("Fire", onDisk.currentAura);
        }
    }
}
