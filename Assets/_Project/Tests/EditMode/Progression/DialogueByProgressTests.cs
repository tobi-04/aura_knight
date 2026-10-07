using System.Collections.Generic;
using AuraKnight.Core;
using AuraKnight.World;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Progression
{
    public sealed class DialogueByProgressTests
    {
        static DialogueByProgress Sol() => ScriptableObject.CreateInstance<DialogueByProgress>().SetEntries(new[]
        {
            new DialogueEntry { lineKey = "start" },
            new DialogueEntry { lineKey = "wind", requiredAuras = new List<string> { "Wind" } },
            new DialogueEntry { lineKey = "fire", requiredAuras = new List<string> { "Wind", "Fire" } },
            new DialogueEntry { lineKey = "water", requiredAuras = new List<string> { "Wind", "Fire", "Water" } },
            new DialogueEntry { lineKey = "end", requiredAuras = new List<string> { "Wind", "Fire", "Water" }, requiredBoss = "malakor" },
        });

        static GameState With(string[] auras, params string[] bosses)
        {
            var state = GameState.NewGame();
            state.unlockedAuras.AddRange(auras);
            state.defeatedBosses.AddRange(bosses);
            return state;
        }

        [Test]
        public void NewGameGetsTheFirstLine() => Assert.AreEqual("start", Sol().Select(GameState.NewGame()));

        [Test]
        public void EachAuraAdvancesTheHint()
        {
            var sol = Sol();
            Assert.AreEqual("wind", sol.Select(With(new[] { "Wind" })));
            Assert.AreEqual("fire", sol.Select(With(new[] { "Wind", "Fire" })));
            Assert.AreEqual("water", sol.Select(With(new[] { "Wind", "Fire", "Water" })));
        }

        [Test]
        public void TheFinalBossGivesTheEndingLine() =>
            Assert.AreEqual("end", Sol().Select(With(new[] { "Wind", "Fire", "Water" }, "malakor")));

        [Test]
        public void AnOtherBossDoesNotSkipAhead() =>
            Assert.AreEqual("water", Sol().Select(With(new[] { "Wind", "Fire", "Water" }, "forest_boss")));

        [Test]
        public void AuraOrderDoesNotMatterButAMissingOneHoldsBack() =>
            Assert.AreEqual("wind", Sol().Select(With(new[] { "Water", "Wind" })));

        [Test]
        public void NullStateAndEmptyAssetAreSafe()
        {
            Assert.AreEqual("start", Sol().Select(null));
            Assert.AreEqual(string.Empty, ScriptableObject.CreateInstance<DialogueByProgress>().Select(GameState.NewGame()));
        }

        [TestCase(0.5f, 0.5f, false)]
        [TestCase(0.59f, 0.6f, true)]
        [TestCase(0.6f, 0.9f, false)]
        [TestCase(0.9f, 0.1f, false)]
        public void UpPressIsAnEdgeAcrossTheThreshold(float previous, float current, bool expected) =>
            Assert.AreEqual(expected, NpcSol.UpPressed(previous, current));
    }
}
