using AuraKnight.Progression;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Progression
{
    public sealed class MapViewportTests
    {
        static MapViewport Make()
        {
            var view = new MapViewport();
            view.SetContentSize(new Vector2(1000f, 600f));
            return view;
        }

        [Test]
        public void ZoomIsClampedToItsLimits()
        {
            var view = Make();
            view.ZoomBy(100f, Vector2.zero);
            Assert.AreEqual(MapViewport.DefaultMaxZoom, view.Zoom, 1e-4f);
            view.ZoomBy(0.0001f, Vector2.zero);
            Assert.AreEqual(MapViewport.DefaultMinZoom, view.Zoom, 1e-4f);
        }

        [Test]
        public void ZoomingKeepsThePointUnderThePivotFixed()
        {
            var view = Make();
            var pivot = new Vector2(200f, 100f);
            var contentPoint = new Vector2(50f, 30f);
            view.Drag(new Vector2(40f, -20f));
            Vector2 before = view.Pan + contentPoint * view.Zoom;
            view.ZoomBy(2f, before); // pivot on the point itself
            Vector2 after = view.Pan + contentPoint * view.Zoom;
            Assert.AreEqual(before.x, after.x, 1e-3f);
            Assert.AreEqual(before.y, after.y, 1e-3f);
            view.ZoomBy(1.5f, pivot);
            Assert.AreEqual(3f, view.Zoom, 1e-4f);
        }

        [Test]
        public void PinchUsesTheDistanceRatio()
        {
            var view = Make();
            view.Pinch(100f, 150f, Vector2.zero);
            Assert.AreEqual(1.5f, view.Zoom, 1e-4f);
            view.Pinch(150f, 75f, Vector2.zero);
            Assert.AreEqual(0.75f, view.Zoom, 1e-4f);
        }

        [TestCase(0f, 10f)]
        [TestCase(10f, 0f)]
        [TestCase(-5f, 10f)]
        [TestCase(float.NaN, 10f)]
        public void InvalidPinchDistancesAreIgnored(float previous, float current)
        {
            var view = Make();
            view.Pinch(previous, current, Vector2.zero);
            Assert.AreEqual(1f, view.Zoom);
        }

        [TestCase(0f)]
        [TestCase(-2f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidZoomFactorsAreIgnored(float factor)
        {
            var view = Make();
            view.ZoomBy(factor, Vector2.zero);
            Assert.AreEqual(1f, view.Zoom);
        }

        [Test]
        public void DragMovesButCannotLeaveTheMap()
        {
            var view = Make();
            view.Drag(new Vector2(30f, -10f));
            Assert.AreEqual(new Vector2(30f, -10f), view.Pan);
            view.Drag(new Vector2(99999f, -99999f));
            Assert.AreEqual(new Vector2(500f, -300f), view.Pan, "half the content size at zoom 1");
        }

        [Test]
        public void FocusCentresAContentPointAndResetGoesHome()
        {
            var view = Make();
            view.ZoomBy(2f, Vector2.zero);
            view.FocusOn(new Vector2(100f, -50f));
            Assert.AreEqual(new Vector2(-200f, 100f), view.Pan);
            view.Reset();
            Assert.AreEqual(1f, view.Zoom);
            Assert.AreEqual(Vector2.zero, view.Pan);
        }

        [Test]
        public void ShrinkingTheContentReclampsThePan()
        {
            var view = Make();
            view.Drag(new Vector2(400f, 0f));
            view.SetContentSize(new Vector2(200f, 200f));
            Assert.AreEqual(100f, view.Pan.x);
        }
    }
}
