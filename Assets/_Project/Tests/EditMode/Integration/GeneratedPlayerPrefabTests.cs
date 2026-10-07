using AuraKnight.Audio;
using AuraKnight.Aura;
using AuraKnight.Player;
using AuraKnight.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Tests.Integration
{
    /// <summary>The Player prefab is rebuilt from scratch by its generator; everything other modules add must come back each time.</summary>
    public sealed class GeneratedPlayerPrefabTests
    {
        static GameObject Prefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/Player.prefab");
            Assert.IsNotNull(prefab, "Player prefab");
            return prefab;
        }

        [Test]
        public void SurvivesRegenerationWithAudioProbeAndAuraComponents()
        {
            var prefab = Prefab();
            Assert.IsNotNull(prefab.GetComponent<PlayerController>());
            Assert.IsNotNull(prefab.GetComponent<PlayerSfxProbe>(), "audio probe");
            Assert.IsNotNull(prefab.GetComponent<AuraManager>(), "aura manager");
            Assert.IsNotNull(prefab.GetComponent<AuraVisuals>(), "aura visuals");
            Assert.IsNotNull(prefab.GetComponent<OxygenMeter>(), "oxygen meter");
        }

        [Test]
        public void LeoUsesTheArtSpriteAnimatorAndLitMaterial()
        {
            var visual = Prefab().transform.Find("Visual");
            var renderer = visual.GetComponent<SpriteRenderer>();
            Assert.AreEqual("Leo_Idle_0", renderer.sprite != null ? renderer.sprite.name : null);
            Assert.AreEqual("Mat_SpriteLit", renderer.sharedMaterial != null ? renderer.sharedMaterial.name : null);
            var animator = visual.GetComponent<Animator>();
            Assert.IsNotNull(animator, "Animator on Visual");
            Assert.AreEqual("Leo", animator.runtimeAnimatorController.name);
        }
    }
}
