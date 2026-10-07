using System.IO;
using AuraKnight.Enemies;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Tests.Integration
{
    /// <summary>
    /// Enemy ids (data, prefabs) and art ids (folders under Art/Enemies) differ for three variants; the generator maps them and
    /// copies the art id to EnemyStats.artId. These tests guard that every prefab really ended up on its art.
    /// </summary>
    public sealed class GeneratedEnemyArtTests
    {
        static readonly (string id, string art)[] Map =
        {
            ("BugThorn", "ThornBug"), ("PatrolBot", "PatrolRobot"), ("PoisonShroom", "MushroomHopper"), ("NightKnight", "NightKnight"),
            ("Bat", "Bat"), ("Ghost", "Ghost"), ("StoneSpider", "StoneSpider"), ("ScrapZapper", "ScrapZapper"),
        };

        static (string id, string art)[] Variants() => Map;

        [TestCaseSource(nameof(Variants))]
        public void StatsCarryTheArtIdAndItsFolderExists((string id, string art) v)
        {
            var stats = AssetDatabase.LoadAssetAtPath<EnemyStats>($"Assets/_Project/Data/Enemies/{v.id}.asset");
            Assert.IsNotNull(stats, v.id);
            Assert.AreEqual(v.art, stats.artId);
            Assert.IsTrue(File.Exists($"Assets/_Project/Art/Enemies/{v.art}/{v.art}.png"), $"sheet for {v.art}");
        }

        [TestCaseSource(nameof(Variants))]
        public void PrefabUsesTheRealIdleSpriteLitMaterialAndOverrideController((string id, string art) v)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Enemies/{v.id}.prefab");
            Assert.IsNotNull(prefab, v.id);
            var renderer = prefab.transform.Find("Visual").GetComponent<SpriteRenderer>();
            Assert.IsNotNull(renderer.sprite, "sprite");
            Assert.AreEqual(v.art + "_Idle_0", renderer.sprite.name, "not the placeholder square");
            Assert.AreNotEqual("WhiteSquare", renderer.sprite.name);
            Assert.AreEqual("Mat_SpriteLit", renderer.sharedMaterial != null ? renderer.sharedMaterial.name : null);
            Assert.AreEqual(Color.white, renderer.color, "art is not tinted");

            var animator = prefab.transform.Find("Visual").GetComponent<Animator>();
            Assert.IsInstanceOf<AnimatorOverrideController>(animator.runtimeAnimatorController, "override controller");
            Assert.AreEqual(v.art, animator.runtimeAnimatorController.name, "the variant's own override controller");
        }
    }
}
