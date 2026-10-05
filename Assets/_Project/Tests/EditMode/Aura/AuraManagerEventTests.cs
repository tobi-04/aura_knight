using System.Collections.Generic;
using AuraKnight.Aura;
using AuraKnight.Core;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Aura
{
    public sealed class AuraManagerEventTests : AuraTestBase
    {
        readonly List<string> _changed = new List<string>();
        readonly List<string> _unlocked = new List<string>();
        AuraManager _manager;

        [SetUp]
        public void SetUp()
        {
            _changed.Clear();
            _unlocked.Clear();
            EventBus.Subscribe<AuraChanged>(OnChanged);
            EventBus.Subscribe<AuraUnlocked>(OnUnlocked);
            _manager = Make("Manager", Vector2.zero).AddComponent<AuraManager>();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Unsubscribe<AuraChanged>(OnChanged);
            EventBus.Unsubscribe<AuraUnlocked>(OnUnlocked);
        }

        void OnChanged(AuraChanged e) => _changed.Add(e.AuraId);
        void OnUnlocked(AuraUnlocked e) => _unlocked.Add(e.AuraId);

        [Test]
        public void FirstUnlockPublishesUnlockedAndChanged()
        {
            Assert.IsTrue(_manager.Unlock(AuraId.Wind));
            CollectionAssert.AreEqual(new[] { "Wind" }, _unlocked);
            CollectionAssert.AreEqual(new[] { "Wind" }, _changed);
            Assert.AreEqual(AuraId.Wind, _manager.Current);
        }

        [Test]
        public void SecondUnlockOnlyPublishesUnlocked()
        {
            _manager.Unlock(AuraId.Wind);
            _changed.Clear();
            Assert.IsTrue(_manager.Unlock(AuraId.Fire));
            CollectionAssert.AreEqual(new[] { "Wind", "Fire" }, _unlocked);
            Assert.IsEmpty(_changed);
            Assert.AreEqual(AuraId.Wind, _manager.Current);
        }

        [Test]
        public void DuplicateAndInvalidUnlocksPublishNothing()
        {
            _manager.Unlock(AuraId.Wind);
            _unlocked.Clear();
            Assert.IsFalse(_manager.Unlock(AuraId.Wind));
            Assert.IsFalse(_manager.Unlock(AuraId.None));
            Assert.IsEmpty(_unlocked);
        }

        [Test]
        public void SwitchPublishesChangedOnceAndLockedSwitchPublishesNothing()
        {
            _manager.Unlock(AuraId.Wind);
            _manager.Unlock(AuraId.Water);
            _manager.State.Tick(1f);
            _changed.Clear();

            Assert.IsFalse(_manager.TrySwitch(AuraId.Fire), "locked");
            Assert.IsTrue(_manager.TrySwitch(AuraId.Water));
            Assert.IsFalse(_manager.TrySwitch(AuraId.Wind), "0.3 s cooldown");
            CollectionAssert.AreEqual(new[] { "Water" }, _changed);
        }

        [Test]
        public void CastWithoutSkillsOrStatsIsRefusedSafely()
        {
            _manager.Unlock(AuraId.Fire);
            Assert.AreEqual(CastResult.NoAura, _manager.TryCastSkill());
        }

        [Test]
        public void PassivesAreNoneWhenNoDefinitionIsAssigned()
        {
            var passives = _manager.Passives;
            Assert.IsFalse(passives.doubleJump);
            Assert.AreEqual(1f, passives.SpeedOrOne);
        }
    }
}
