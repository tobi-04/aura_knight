using AuraKnight.Core;
using AuraKnight.UI;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.UI
{
    public sealed class UiMathTests
    {
        [Test]
        public void EaseOutIsClampedAndMonotonic()
        {
            Assert.AreEqual(0f, UITween.EaseOut(-1f));
            Assert.AreEqual(1f, UITween.EaseOut(2f));
            float last = -1f;
            for (float t = 0f; t <= 1f; t += 0.1f)
            {
                float v = UITween.EaseOut(t);
                Assert.GreaterOrEqual(v, last);
                last = v;
            }
        }

        [Test]
        public void StepReachesTheEndInExactlyTwoHundredMilliseconds()
        {
            float p = 0f;
            for (int i = 0; i < 10; i++) p = UITween.Step(p, 0.02f, true);
            Assert.AreEqual(1f, p, 1e-4f);
            for (int i = 0; i < 10; i++) p = UITween.Step(p, 0.02f, false);
            Assert.AreEqual(0f, p, 1e-4f);
            Assert.AreEqual(1f, UITween.Step(0.2f, 0.01f, true, 0f), "zero duration snaps");
        }

        [Test]
        public void SlideIsSixteenPixelsBelowWhenHiddenAndZeroWhenShown()
        {
            Assert.AreEqual(-16f, UITween.SlideOffset(0f), 1e-4f);
            Assert.AreEqual(0f, UITween.SlideOffset(1f), 1e-4f);
            Assert.AreEqual(-8f, UITween.SlideOffset(0f, 8f), 1e-4f);
        }

        [Test]
        public void TypewriterRevealsAtTheGivenSpeedAndNeverOverruns()
        {
            Assert.AreEqual(0, UITween.Typewriter(0f, 40f, 10));
            Assert.AreEqual(4, UITween.Typewriter(0.1f, 40f, 10));
            Assert.AreEqual(10, UITween.Typewriter(5f, 40f, 10));
            Assert.AreEqual(0, UITween.Typewriter(1f, 40f, 0));
        }

        [Test]
        public void TypewriterSequenceTapCompletesThenAdvancesAndSkipFinishes()
        {
            var seq = new TypewriterSequence(new[] { 20, 20, 20, 20 }, 40f, 1f);
            seq.Tick(0.25f);
            Assert.AreEqual(10, seq.VisibleChars);
            Assert.IsFalse(seq.TypingDone);
            seq.Tap();
            Assert.IsTrue(seq.TypingDone);
            Assert.AreEqual(0, seq.Index, "first tap only completes the typing");
            seq.Tap();
            Assert.AreEqual(1, seq.Index);
            seq.Tick(0.6f);
            seq.Tick(1.1f); // typing done, then the hold elapses
            Assert.AreEqual(2, seq.Index);
            seq.Skip();
            Assert.IsTrue(seq.Finished);
        }

        [Test]
        public void TypewriterSequenceFinishesAfterTheLastCard()
        {
            var seq = new TypewriterSequence(new[] { 4 }, 100f, 0.5f);
            seq.Tick(1f);
            seq.Tick(1f);
            Assert.IsTrue(seq.Finished);
            Assert.IsTrue(new TypewriterSequence(null).Finished, "no cards = already finished");
        }

        [Test]
        public void SplashTimelineFadesStudioThenTitleThenEnds()
        {
            Assert.AreEqual(0f, SplashTimeline.StudioAlpha(0f));
            Assert.AreEqual(1f, SplashTimeline.StudioAlpha(0.6f));
            Assert.AreEqual(0f, SplashTimeline.StudioAlpha(SplashTimeline.TitleStart), 1e-4f);
            Assert.AreEqual(0f, SplashTimeline.TitleAlpha(0.5f));
            Assert.AreEqual(1f, SplashTimeline.TitleAlpha(SplashTimeline.TitleStart + SplashTimeline.TitleFade), 1e-4f);
            Assert.IsFalse(SplashTimeline.IsDone(1f));
            Assert.IsTrue(SplashTimeline.IsDone(SplashTimeline.Total));
        }

        [Test]
        public void CoverCropsInsteadOfStretching()
        {
            var wide = CoverMath.UvRect(0.5f, 1f, new Vector2(0.5f, 0.5f)); // tall art in a wide box: crop height
            Assert.AreEqual(1f, wide.width);
            Assert.AreEqual(0.5f, wide.height, 1e-4f);
            Assert.AreEqual(0.25f, wide.y, 1e-4f);
            var tall = CoverMath.UvRect(2f, 1f, new Vector2(0f, 0.5f)); // wide art in a square: crop width, keep the left
            Assert.AreEqual(0.5f, tall.width, 1e-4f);
            Assert.AreEqual(0f, tall.x);
            Assert.AreEqual(new Rect(0, 0, 1, 1), CoverMath.UvRect(0f, 1f, Vector2.zero));
        }

        [Test]
        public void TouchTargetMinimumIsSixtyFourDpOnEveryDensity()
        {
            Assert.AreEqual(64f, TouchTargets.MinCanvasUnits(160f, 1f), 1e-3f);
            Assert.AreEqual(64f * 420f / 160f / 1.118f, TouchTargets.MinCanvasUnits(420f, 1.118f), 1e-2f);
            Assert.AreEqual(64f, TouchTargets.MinCanvasUnits(0f, 1f), 1e-3f, "unknown dpi falls back to the baseline");
        }

        [Test]
        public void PauseGateCapturesTheGameplayScaleOnceAndRestoresIt()
        {
            var gate = new PauseGate();
            Assert.IsTrue(gate.Begin(0.5f, GameMode.Playing));
            Assert.IsFalse(gate.Begin(0f, GameMode.Paused), "a second begin must not overwrite the capture");
            Assert.IsTrue(gate.End(out float scale, out var mode));
            Assert.AreEqual(0.5f, scale);
            Assert.AreEqual(GameMode.Playing, mode);
            Assert.IsFalse(gate.End(out _, out _));
        }

        [Test]
        public void PauseGateNeverResumesToZeroOrToPaused()
        {
            var gate = new PauseGate();
            gate.Begin(0f, GameMode.Paused);
            gate.End(out float scale, out var mode);
            Assert.AreEqual(1f, scale);
            Assert.AreEqual(GameMode.Playing, mode);
        }
    }
}
