using System.Collections.Generic;
using AuraKnight.Aura;
using AuraKnight.Combat;
using NUnit.Framework;

namespace AuraKnight.Tests.Aura
{
    public sealed class AuraStateTests
    {
        static AuraState WithAuras(params AuraId[] ids)
        {
            var state = new AuraState();
            foreach (var id in ids) state.Unlock(id);
            return state;
        }

        [Test]
        public void StartsWithNoneAndNothingElseUnlocked()
        {
            var state = new AuraState();
            Assert.AreEqual(AuraId.None, state.Current);
            Assert.IsTrue(state.IsUnlocked(AuraId.None));
            Assert.IsFalse(state.IsUnlocked(AuraId.Wind));
            Assert.AreEqual(0, state.UnlockedCount);
        }

        [Test]
        public void LockedAuraIsRejected()
        {
            var state = WithAuras(AuraId.Wind);
            Assert.AreEqual(SwitchResult.Locked, state.TrySwitch(AuraId.Fire));
            Assert.AreEqual(AuraId.Wind, state.Current);
        }

        [Test]
        public void SwitchHonoursThreeTenthsSecondCooldown()
        {
            var state = WithAuras(AuraId.Wind, AuraId.Fire, AuraId.Water);
            Assert.AreEqual(SwitchResult.Switched, state.TrySwitch(AuraId.Fire));
            Assert.AreEqual(SwitchResult.Cooldown, state.TrySwitch(AuraId.Water));
            state.Tick(0.29f);
            Assert.AreEqual(SwitchResult.Cooldown, state.TrySwitch(AuraId.Water));
            state.Tick(0.02f);
            Assert.AreEqual(SwitchResult.Switched, state.TrySwitch(AuraId.Water));
            Assert.AreEqual(AuraId.Water, state.Current);
        }

        [Test]
        public void SwitchingToCurrentAuraDoesNothing()
        {
            var state = WithAuras(AuraId.Wind);
            Assert.AreEqual(SwitchResult.AlreadyCurrent, state.TrySwitch(AuraId.Wind));
            Assert.AreEqual(0f, state.SwitchCooldownRemaining);
        }

        [Test]
        public void FirstUnlockAutoSwitchesLaterOnesDoNot()
        {
            var state = new AuraState();
            Assert.AreEqual(UnlockResult.UnlockedAndSwitched, state.Unlock(AuraId.Fire));
            Assert.AreEqual(AuraId.Fire, state.Current);
            Assert.AreEqual(UnlockResult.Unlocked, state.Unlock(AuraId.Water));
            Assert.AreEqual(AuraId.Fire, state.Current);
            Assert.AreEqual(UnlockResult.AlreadyUnlocked, state.Unlock(AuraId.Water));
        }

        [Test]
        public void UnlockRejectsNoneAndOutOfRangeIds()
        {
            var state = new AuraState();
            Assert.AreEqual(UnlockResult.Invalid, state.Unlock(AuraId.None));
            Assert.AreEqual(UnlockResult.Invalid, state.Unlock((AuraId)99));
            Assert.IsFalse(state.IsUnlocked((AuraId)(-1)));
        }

        [Test]
        public void CycleWalksUnlockedAurasInOrderAndWraps()
        {
            var state = WithAuras(AuraId.Wind, AuraId.Water); // Fire stays locked
            var seen = new List<AuraId>();
            for (int i = 0; i < 4; i++)
            {
                state.Tick(1f);
                state.TryCycle(1);
                seen.Add(state.Current);
            }
            CollectionAssert.AreEqual(new[] { AuraId.Water, AuraId.Wind, AuraId.Water, AuraId.Wind }, seen);
        }

        [Test]
        public void CyclePreviousGoesBackwardsAndSkipsNone()
        {
            var state = WithAuras(AuraId.Wind, AuraId.Fire, AuraId.Water);
            state.Tick(1f);
            state.TryCycle(-1);
            Assert.AreEqual(AuraId.Water, state.Current); // wraps past Wind, never lands on None
            state.Tick(1f);
            state.TryCycle(-1);
            Assert.AreEqual(AuraId.Fire, state.Current);
        }

        [Test]
        public void CycleWithSingleAuraStaysPut()
        {
            var state = WithAuras(AuraId.Wind);
            state.Tick(1f);
            Assert.AreEqual(SwitchResult.AlreadyCurrent, state.TryCycle(1));
            Assert.AreEqual(AuraId.Wind, state.Current);
        }

        [Test]
        public void CycleFromNoneWithoutAurasDoesNothing()
        {
            var state = new AuraState();
            Assert.AreEqual(SwitchResult.AlreadyCurrent, state.TryCycle(1));
            Assert.AreEqual(AuraId.None, state.Current);
        }

        [Test]
        public void CastRefusedWithoutAura()
        {
            var state = new AuraState();
            Assert.AreEqual(CastResult.NoAura, state.TryCast(25f, 0.5f, _ => true));
        }

        [Test]
        public void CastRefusedWhenEnergyIsInsufficientAndSpendsNothing()
        {
            var state = WithAuras(AuraId.Wind);
            var energy = new EnergyPool(100f);
            energy.TrySpend(80f); // 20 left, wind costs 25
            Assert.AreEqual(CastResult.NotEnoughEnergy, state.TryCast(25f, 0.5f, energy.TrySpend));
            Assert.AreEqual(20f, energy.Current, 1e-4f);
            Assert.AreEqual(0f, state.SkillCooldownRemaining);
        }

        [Test]
        public void CastSpendsExactCostAndStartsSkillCooldown()
        {
            var state = WithAuras(AuraId.Fire);
            var energy = new EnergyPool(100f);
            Assert.AreEqual(CastResult.Cast, state.TryCast(30f, 0.4f, energy.TrySpend));
            Assert.AreEqual(70f, energy.Current, 1e-4f);
            Assert.AreEqual(CastResult.SkillCooldown, state.TryCast(30f, 0.4f, energy.TrySpend));
            Assert.AreEqual(70f, energy.Current, 1e-4f);
            state.Tick(0.41f);
            Assert.AreEqual(CastResult.Cast, state.TryCast(30f, 0.4f, energy.TrySpend));
            Assert.AreEqual(40f, energy.Current, 1e-4f);
        }

        [Test]
        public void SaveLoadRoundTripKeepsUnlockedSetAndCurrent()
        {
            var original = WithAuras(AuraId.Wind, AuraId.Water);
            original.Tick(1f);
            original.TrySwitch(AuraId.Water);
            var saved = new List<string>();
            original.ExportUnlocked(saved);
            CollectionAssert.AreEqual(new[] { "Wind", "Water" }, saved);

            var restored = new AuraState();
            restored.Load(saved, AuraIds.ToKey(original.Current));
            Assert.AreEqual(AuraId.Water, restored.Current);
            Assert.IsTrue(restored.IsUnlocked(AuraId.Wind));
            Assert.IsTrue(restored.IsUnlocked(AuraId.Water));
            Assert.IsFalse(restored.IsUnlocked(AuraId.Fire));
            Assert.AreEqual(SwitchResult.Switched, restored.TrySwitch(AuraId.Wind), "loading must not leave a cooldown running");
        }

        [Test]
        public void LoadIgnoresUnknownIdsAndFallsBackWhenCurrentIsLocked()
        {
            var state = new AuraState();
            state.Load(new[] { "Banana", "Fire", null, "None" }, "Water");
            Assert.IsTrue(state.IsUnlocked(AuraId.Fire));
            Assert.IsFalse(state.IsUnlocked(AuraId.Water));
            Assert.AreEqual(AuraId.Fire, state.Current);

            state.Load(null, "Wind");
            Assert.AreEqual(AuraId.None, state.Current);
        }

        [Test]
        public void ExportNeverIncludesNone()
        {
            var saved = new List<string> { "stale" };
            new AuraState().ExportUnlocked(saved);
            Assert.IsEmpty(saved);
        }

        [Test]
        public void IdKeysUseEnumNamesAndParseBack()
        {
            foreach (var id in AuraIds.All)
            {
                Assert.AreEqual(id.ToString(), AuraIds.ToKey(id));
                Assert.IsTrue(AuraIds.TryParse(AuraIds.ToKey(id), out var parsed));
                Assert.AreEqual(id, parsed);
            }
            Assert.IsFalse(AuraIds.TryParse("wind", out _), "ids are case sensitive like the save file");
            Assert.AreEqual(AuraId.None, AuraIds.ParseOrNone(""));
        }
    }
}
