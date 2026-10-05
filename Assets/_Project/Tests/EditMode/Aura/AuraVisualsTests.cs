using System;
using AuraKnight.Aura;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace AuraKnight.Tests.Aura
{
    public sealed class AuraVisualsTests : AuraTestBase
    {
        AuraVisuals _visuals;
        Light2D _light;
        SpriteRenderer _renderer;

        [SetUp]
        public void SetUp()
        {
            var go = Make("Player", Vector2.zero);
            _renderer = go.AddComponent<SpriteRenderer>();
            _light = go.AddComponent<Light2D>();
            _visuals = go.AddComponent<AuraVisuals>();
            SetRef(_visuals, "glow", _light);
            SetRef(_visuals, "body", _renderer);
        }

        static AuraDefinition Definition(Color color, float radius)
        {
            var d = ScriptableObject.CreateInstance<AuraDefinition>();
            var so = new UnityEditor.SerializedObject(d);
            so.FindProperty("color").colorValue = color;
            so.FindProperty("lightRadius").floatValue = radius;
            so.ApplyModifiedPropertiesWithoutUndo();
            return d;
        }

        [Test]
        public void AppliesColourRadiusAndTintFromTheDefinition()
        {
            var def = Definition(new Color(0.15f, 0.83f, 0.55f), 4f);
            _visuals.Apply(def, flash: false);
            Assert.AreEqual(4f, _light.pointLightOuterRadius, 1e-4f);
            Assert.AreEqual(def.Color, _light.color);
            var block = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(block);
            Assert.AreEqual(def.Color.g, block.GetColor("_Color").g, 1e-3f);
            UnityEngine.Object.DestroyImmediate(def);
        }

        [Test]
        public void MissingDefinitionFallsBackToPaleGoldGlow()
        {
            _visuals.Apply(null, flash: false);
            Assert.AreEqual(AuraVisuals.FallbackRadius, _light.pointLightOuterRadius, 1e-4f);
            Assert.AreEqual(AuraVisuals.FallbackColor, _light.color);
        }

        [Test]
        public void SwitchFlashesForTwoTenthsOfASecond()
        {
            var def = Definition(Color.red, 5f);
            _visuals.Apply(null, flash: false);
            float calm = _light.intensity;
            _visuals.Apply(def, flash: true);
            Assert.Greater(_light.intensity, calm + 1f);
            _visuals.Advance(0.1f);
            Assert.Greater(_light.intensity, calm);
            Assert.Less(_light.intensity, calm + AuraFlash.PeakIntensityBoost);
            _visuals.Advance(0.11f);
            Assert.AreEqual(calm, _light.intensity, 1e-4f);
            Assert.AreEqual(0f, _visuals.FlashRemaining);
            UnityEngine.Object.DestroyImmediate(def);
        }

        [Test]
        public void FlashCurveFallsLinearlyToZero()
        {
            Assert.AreEqual(1f, AuraFlash.Strength(0f));
            Assert.AreEqual(0.5f, AuraFlash.Strength(0.1f), 1e-4f);
            Assert.AreEqual(0f, AuraFlash.Strength(0.2f));
            Assert.AreEqual(0f, AuraFlash.Strength(5f));
        }

        [Test]
        public void HundredSwitchesDoNotAllocate()
        {
            var a = Definition(Color.red, 5f);
            var b = Definition(Color.cyan, 4f);
            _visuals.Apply(a, true);
            _visuals.Apply(b, true); // warm up
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++)
            {
                _visuals.Apply(i % 2 == 0 ? a : b, true);
                _visuals.Advance(0.05f);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.Less(allocated, 1024, "per-switch visuals must be allocation free");
            UnityEngine.Object.DestroyImmediate(a);
            UnityEngine.Object.DestroyImmediate(b);
        }
    }
}
