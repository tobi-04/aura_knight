using AuraKnight.Audio;
using NUnit.Framework;

namespace AuraKnight.Tests.Audio
{
    public sealed class AudioMathTests
    {
        [TestCase(1f, 0f)]
        [TestCase(0.5f, -6.0206f)]
        [TestCase(0.1f, -20f)]
        public void LinearToDb_MatchesTwentyLogTen(float linear, float expectedDb) =>
            Assert.AreEqual(expectedDb, AudioMath.LinearToDb(linear), 0.01f);

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(0.00001f)]
        public void LinearToDb_ZeroAndBelowIsSilence(float linear) =>
            Assert.AreEqual(AudioMath.SilenceDb, AudioMath.LinearToDb(linear));

        [Test]
        public void LinearToDb_AboveOneClampsToZeroDb() => Assert.AreEqual(0f, AudioMath.LinearToDb(3f));

        [Test]
        public void RandomPitch_StaysWithinPlusMinusFivePercent()
        {
            Assert.AreEqual(0.95f, AudioMath.RandomPitch(0.05f, 0f), 1e-5f);
            Assert.AreEqual(1.00f, AudioMath.RandomPitch(0.05f, 0.5f), 1e-5f);
            Assert.AreEqual(1.05f, AudioMath.RandomPitch(0.05f, 1f), 1e-5f);
            for (float roll = -0.5f; roll <= 1.5f; roll += 0.05f)
            {
                float pitch = AudioMath.RandomPitch(0.05f, roll);
                Assert.That(pitch, Is.InRange(0.95f - 1e-5f, 1.05f + 1e-5f), $"roll {roll}");
            }
        }

        [Test]
        public void RandomPitch_ClampsVarianceAndAllowsNone()
        {
            Assert.AreEqual(1f, AudioMath.RandomPitch(0f, 0.9f));
            Assert.AreEqual(1f, AudioMath.RandomPitch(-1f, 0.9f));
            Assert.AreEqual(1f + AudioMath.MaxPitchVariance, AudioMath.RandomPitch(5f, 1f), 1e-5f);
        }

        [Test]
        public void LayerGains_AreEqualPower()
        {
            for (float mix = 0f; mix <= 1f; mix += 0.1f)
            {
                var (explore, combat) = AudioMath.LayerGains(mix);
                Assert.AreEqual(1f, explore * explore + combat * combat, 1e-4f, $"mix {mix}");
            }
            Assert.AreEqual((1f, 0f), (AudioMath.LayerGains(0f).explore, AudioMath.LayerGains(0f).combat));
            Assert.AreEqual(1f, AudioMath.LayerGains(1f).combat, 1e-5f);
            Assert.AreEqual(0f, AudioMath.LayerGains(1f).explore, 1e-5f);
        }

        [Test]
        public void BusOf_RoutesOnlyUiSoundsToTheUiBus()
        {
            Assert.AreEqual(SfxBus.Ui, AudioMath.BusOf(SfxId.UiTap));
            Assert.AreEqual(SfxBus.Ui, AudioMath.BusOf(SfxId.UiBack));
            Assert.AreEqual(SfxBus.Sfx, AudioMath.BusOf(SfxId.Jump));
            Assert.AreEqual(SfxBus.Sfx, AudioMath.BusOf(SfxId.BossRoar));
        }
    }
}
