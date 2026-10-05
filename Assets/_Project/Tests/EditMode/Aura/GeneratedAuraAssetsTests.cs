using AuraKnight.Aura;
using AuraKnight.Aura.Skills;
using AuraKnight.Combat;
using AuraKnight.Core;
using AuraKnight.Player;
using AuraKnight.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace AuraKnight.Tests.Aura
{
    /// <summary>Checks the committed output of AuraAssetGenerator / AuraTestSceneGenerator (GDD section 6 numbers).</summary>
    public sealed class GeneratedAuraAssetsTests
    {
        static AuraDefinition Load(AuraId id) =>
            AssetDatabase.LoadAssetAtPath<AuraDefinition>($"Assets/_Project/Data/Auras/{id}.asset");

        static Color Hex(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        static void AssertColor(string hex, Color actual)
        {
            var expected = Hex(hex);
            Assert.AreEqual(expected.r, actual.r, 0.01f);
            Assert.AreEqual(expected.g, actual.g, 0.01f);
            Assert.AreEqual(expected.b, actual.b, 0.01f);
        }

        [Test]
        public void DefinitionsMatchTheAuraTable()
        {
            var none = Load(AuraId.None);
            var wind = Load(AuraId.Wind);
            var fire = Load(AuraId.Fire);
            var water = Load(AuraId.Water);
            foreach (var d in new[] { none, wind, fire, water }) Assert.IsNotNull(d);

            Assert.AreEqual(AuraId.None, none.Id);
            Assert.AreEqual(3f, none.LightRadius);
            Assert.IsNull(none.SkillPrefab);
            Assert.AreEqual(0f, none.EnergyCost);

            AssertColor("#27D38C", wind.Color);
            Assert.AreEqual(4f, wind.LightRadius);
            Assert.AreEqual(25f, wind.EnergyCost);
            Assert.IsTrue(wind.Passives.doubleJump && wind.Passives.glide);
            Assert.IsFalse(wind.Passives.swim || wind.Passives.heatImmune);
            Assert.IsInstanceOf<WindGustSkill>(wind.SkillPrefab);

            AssertColor("#FF5C57", fire.Color);
            Assert.AreEqual(5f, fire.LightRadius);
            Assert.AreEqual(30f, fire.EnergyCost);
            Assert.AreEqual(1.2f, fire.Passives.speedMultiplier, 1e-4f);
            Assert.IsTrue(fire.Passives.heatImmune);
            Assert.IsFalse(fire.Passives.doubleJump);
            Assert.IsInstanceOf<FireballSkill>(fire.SkillPrefab);

            AssertColor("#27B5F7", water.Color);
            Assert.AreEqual(4f, water.LightRadius);
            Assert.AreEqual(35f, water.EnergyCost);
            Assert.IsTrue(water.Passives.swim && water.Passives.acidImmune);
            Assert.IsInstanceOf<WaterShieldSkill>(water.SkillPrefab);
            Assert.AreEqual(AuraId.Water, water.SkillPrefab.Aura);
        }

        [Test]
        public void SkillPrefabsAreWiredToTheirHitboxes()
        {
            var windHitbox = Load(AuraId.Wind).SkillPrefab.GetComponentInChildren<Hitbox>(true);
            Assert.IsNotNull(windHitbox);
            Assert.AreEqual(Team.Player, windHitbox.Team);
            Assert.AreEqual(1, windHitbox.Damage);
            Assert.AreEqual(WindGustSkill.Radius, windHitbox.GetComponent<CircleCollider2D>().radius, 1e-4f);

            var projectile = AssetDatabase.LoadAssetAtPath<FireballProjectile>("Assets/_Project/Prefabs/Aura/FireballProjectile.prefab");
            Assert.IsNotNull(projectile);
            var hitbox = projectile.GetComponent<Hitbox>();
            Assert.AreEqual(Team.Player, hitbox.Team);
            Assert.IsTrue(projectile.GetComponent<Collider2D>().isTrigger);
            var so = new SerializedObject(Load(AuraId.Fire).SkillPrefab);
            Assert.AreSame(projectile, so.FindProperty("projectilePrefab").objectReferenceValue);
        }

        [Test]
        public void PlayerPrefabCarriesTheAuraSystem()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            var manager = prefab.GetComponent<AuraManager>();
            Assert.IsNotNull(manager);
            Assert.IsNotNull(prefab.GetComponent<PlayerAuraBinder>());
            Assert.IsNotNull(prefab.GetComponent<OxygenMeter>());
            var visuals = prefab.GetComponent<AuraVisuals>();
            Assert.IsNotNull(visuals);

            var definitions = new SerializedObject(manager).FindProperty("definitions");
            Assert.AreEqual(4, definitions.arraySize);
            var light = prefab.GetComponentInChildren<Light2D>();
            Assert.IsNotNull(light);
            Assert.AreEqual(Light2D.LightType.Point, light.lightType);
            Assert.AreEqual(3f, light.pointLightOuterRadius, 1e-4f);
            Assert.AreEqual(Color.white, prefab.GetComponentInChildren<SpriteRenderer>().color);
        }

        const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";

        [TestCase("AuraGate_Burn", typeof(BurnableGate), AuraId.Fire, AuraInteraction.Burn)]
        [TestCase("AuraGate_Extinguish", typeof(ExtinguishableGate), AuraId.Water, AuraInteraction.Extinguish)]
        public void OneTimeGatePrefabsAskForTheRightAuraAndPersist(string name, System.Type type, AuraId aura, AuraInteraction kind)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Interactables/{name}.prefab");
            var gate = (OneTimeAuraGate)root.GetComponent(type);
            Assert.IsNotNull(gate);
            Assert.AreEqual(aura, gate.RequiredAura);
            Assert.AreEqual(kind, gate.Interaction);
            Assert.IsTrue(gate.GetComponent<Collider2D>().isTrigger);
            Assert.IsNotNull(gate.GetComponent<PersistentId>(), "one-time gates need a persistent id");
            Assert.AreEqual(LayerMask.NameToLayer(PhysicsLayers.Interactable), root.layer);
        }

        [Test]
        public void HeatVentPrefabIsAFireImmunityHazard()
        {
            var vent = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Interactables/AuraGate_HeatVent.prefab").GetComponent<HeatVent>();
            Assert.IsNotNull(vent);
            Assert.AreEqual(AuraId.Fire, vent.RequiredAura);
            Assert.IsNotNull(new SerializedObject(vent).FindProperty("ventHazard").objectReferenceValue);
        }

        [Test]
        public void WorldPrefabsExist()
        {
            foreach (var name in new[] { "WindCurrent", "LavaFreezable", "WaterVolume" })
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Interactables/{name}.prefab"), name);
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Interactables/WindCurrent.prefab").GetComponent<WindCurrent>());
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Interactables/WaterVolume.prefab").GetComponent<WaterVolume>());
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Interactables/LavaFreezable.prefab").GetComponent<LavaFreezable>());
        }

        [Test]
        public void TestSceneHasEveryGateTypeAndAWaterPool()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Test/Test_Aura.unity", OpenSceneMode.Additive);
            try
            {
                var kinds = new System.Collections.Generic.HashSet<AuraInteraction>();
                foreach (var g in Object.FindObjectsByType<OneTimeAuraGate>(FindObjectsInactive.Include)) kinds.Add(g.Interaction);
                Assert.IsNotEmpty(Object.FindObjectsByType<HeatVent>(FindObjectsInactive.Include));
                CollectionAssert.IsSupersetOf(kinds, new[] { AuraInteraction.Burn, AuraInteraction.Extinguish });
                Assert.IsNotEmpty(Object.FindObjectsByType<WaterVolume>());
                Assert.IsNotEmpty(Object.FindObjectsByType<LavaFreezable>());
                Assert.IsNotEmpty(Object.FindObjectsByType<WindCurrent>());
                var manager = Object.FindAnyObjectByType<AuraManager>();
                Assert.AreEqual(3, new SerializedObject(manager).FindProperty("debugUnlocked").arraySize);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
