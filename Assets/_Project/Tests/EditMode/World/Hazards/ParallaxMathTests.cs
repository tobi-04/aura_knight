using AuraKnight.World;
using NUnit.Framework;

namespace AuraKnight.Tests.World.Hazards
{
    public sealed class ParallaxMathTests
    {
        [Test]
        public void TheFourFactorsAreTheGddOnes()
        {
            CollectionAssert.AreEqual(new[] { 0.1f, 0.5f, 1f, 1.2f }, ParallaxMath.Factors);
        }

        [Test]
        public void AnActionLayerStaysGluedToTheWorld() =>
            Assert.AreEqual(100f, ParallaxMath.PositionX(100f, 137f, 1f), 1e-4f);

        [Test]
        public void AFarLayerFollowsTheCameraAlmostFully() =>
            Assert.AreEqual(100f + 30f * 0.9f, ParallaxMath.PositionX(100f, 130f, 0.1f), 1e-4f);

        [Test]
        public void ALayerMovesAtItsFactorOnScreen()
        {
            // Screen offset of the layer relative to the camera changes by (factor - 1) of the camera move... i.e. the layer travels factor times the camera.
            foreach (float f in ParallaxMath.Factors)
            {
                float a = ParallaxMath.PositionX(0f, 0f, f);
                float b = ParallaxMath.PositionX(0f, 10f, f);
                Assert.AreEqual(10f * (1f - f), b - a, 1e-4f);
            }
        }

        [Test]
        public void TheForegroundSlidesAgainstTheCamera() => Assert.Less(ParallaxMath.PositionX(0f, 10f, 1.2f), 0f);
    }
}
