using System.Linq;
using AuraKnight.Editor.Tools;
using NUnit.Framework;

namespace AuraKnight.Tests.Integration
{
    public sealed class RegenerateAllOrderTests
    {
        [Test]
        public void StepsRunInDependencyOrder()
        {
            var names = RegenerateAll.Steps.Select(s => s.Name).ToArray();
            CollectionAssert.AreEqual(new[] { "art", "player", "aura", "enemies", "audio", "world-core", "ui", "validators" }, names);
        }

        [Test]
        public void StepNamesAreUniqueAndEveryStepRuns()
        {
            var steps = RegenerateAll.Steps;
            Assert.AreEqual(steps.Count, steps.Select(s => s.Name).Distinct().Count());
            Assert.IsTrue(steps.All(s => s.Run != null));
        }

        [Test]
        public void AStepNeedsANameAndAnAction()
        {
            Assert.Throws<System.ArgumentException>(() => new RegenerateStep(" ", () => { }));
            Assert.Throws<System.ArgumentNullException>(() => new RegenerateStep("x", null));
        }

        [TestCase("1920x1080", 1920, 1080)]
        [TestCase("2340x1080,2520X1080", 2340, 1080)]
        public void SizesParse(string text, int w, int h)
        {
            var sizes = ShotJob.ParseSizes(text);
            Assert.AreEqual(w, sizes[0].x);
            Assert.AreEqual(h, sizes[0].y);
        }

        [TestCase("abc")]
        [TestCase("1920")]
        [TestCase("0x1080")]
        [TestCase("99999x1080")]
        public void MalformedSizesAreRejected(string text) => Assert.Throws<System.FormatException>(() => ShotJob.ParseSizes(text));

        [Test]
        public void NoSizesMeansDefaults() => Assert.IsNull(ShotJob.ParseSizes(null));

        [Test]
        public void DefaultJobsCoverTheRequestedScenes()
        {
            var names = ShotJob.Defaults().Select(j => j.Name).ToArray();
            CollectionAssert.AreEqual(new[] { "MainMenu", "Core_Region_Hub", "Test_Movement", "Test_Aura", "Test_Enemies" }, names);
        }
    }
}
