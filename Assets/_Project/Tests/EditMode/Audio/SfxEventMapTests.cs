using AuraKnight.Aura;
using AuraKnight.Audio;
using AuraKnight.Player;
using NUnit.Framework;

namespace AuraKnight.Tests.Audio
{
    public sealed class SfxEventMapTests
    {
        [TestCase("Wind", SfxId.AuraWind)]
        [TestCase("Fire", SfxId.AuraFire)]
        [TestCase("Water", SfxId.AuraWater)]
        [TestCase("None", SfxId.None)]
        [TestCase("garbage", SfxId.None)]
        [TestCase(null, SfxId.None)]
        public void AuraChange_MapsEachAuraToItsSound(string key, SfxId expected) =>
            Assert.AreEqual(expected, SfxEventMap.ForAuraChanged(key));

        [TestCase(AuraId.Wind, SfxId.SkillWind)]
        [TestCase(AuraId.Fire, SfxId.SkillFire)]
        [TestCase(AuraId.Water, SfxId.SkillWater)]
        [TestCase(AuraId.None, SfxId.None)]
        public void Skill_MapsEachAuraToItsSound(AuraId aura, SfxId expected) =>
            Assert.AreEqual(expected, SfxEventMap.ForSkill(aura));

        [TestCase(PlayerStateId.Jump, SfxId.Jump)]
        [TestCase(PlayerStateId.WallJump, SfxId.Jump)]
        [TestCase(PlayerStateId.Dash, SfxId.Dash)]
        [TestCase(PlayerStateId.Slide, SfxId.Slide)]
        [TestCase(PlayerStateId.WallSlide, SfxId.WallSlide)]
        [TestCase(PlayerStateId.Attack, SfxId.SwordSwing)]
        [TestCase(PlayerStateId.AirAttack, SfxId.SwordSwing)]
        [TestCase(PlayerStateId.Idle, SfxId.None)]
        [TestCase(PlayerStateId.Run, SfxId.None)]
        [TestCase(PlayerStateId.Fall, SfxId.None)]
        [TestCase(PlayerStateId.Hurt, SfxId.None)]
        [TestCase(PlayerStateId.Dead, SfxId.None)]
        [TestCase(PlayerStateId.Swim, SfxId.None)]
        public void PlayerState_MapsMovementStatesToSounds(PlayerStateId state, SfxId expected) =>
            Assert.AreEqual(expected, SfxEventMap.ForPlayerState(state));

        [Test]
        public void EnergySpent_OnlyOnRealDropsAfterTheFirstReading()
        {
            Assert.IsFalse(SfxEventMap.IsEnergySpent(-1f, 3f), "first reading seeds the tracker");
            Assert.IsFalse(SfxEventMap.IsEnergySpent(3f, 3f));
            Assert.IsFalse(SfxEventMap.IsEnergySpent(3f, 4f), "sword hits add energy");
            Assert.IsTrue(SfxEventMap.IsEnergySpent(4f, 2f));
        }

        [Test]
        public void CoinGain_IgnoresSpendingAndTheFirstReading()
        {
            Assert.IsFalse(SfxEventMap.IsCoinGain(-1, 50));
            Assert.IsFalse(SfxEventMap.IsCoinGain(50, 50));
            Assert.IsFalse(SfxEventMap.IsCoinGain(50, 20));
            Assert.IsTrue(SfxEventMap.IsCoinGain(50, 51));
        }
    }
}
