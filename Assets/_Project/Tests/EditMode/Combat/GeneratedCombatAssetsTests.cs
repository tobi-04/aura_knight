using AuraKnight.Combat;
using AuraKnight.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Tests.Combat
{
    /// <summary>Guards the combat wiring written by the editor generators (AuraKnight.Editor.MovementTestSceneGenerator.Generate).</summary>
    public sealed class GeneratedCombatAssetsTests
    {
        const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";

        static GameObject LoadPlayer()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            Assert.IsNotNull(prefab, "missing " + PlayerPrefabPath);
            return prefab;
        }

        [Test]
        public void PlayerPrefabHasHealthStatsAndHurtboxOnTeamPlayer()
        {
            var prefab = LoadPlayer();
            var health = prefab.GetComponent<Health>();
            Assert.IsNotNull(health);
            Assert.AreEqual(5, health.Max);
            Assert.AreEqual(1f, health.InvulnerableAfterHit);
            Assert.IsNotNull(prefab.GetComponent<PlayerStats>());
            var hurtbox = prefab.GetComponent<Hurtbox>();
            Assert.AreEqual(Team.Player, hurtbox.Team);
            Assert.AreSame(health, hurtbox.Health);
            Assert.IsNotNull(prefab.GetComponent<CombatFeedback>());
            Assert.IsNotNull(prefab.GetComponent<SpriteBlink>());
        }

        [Test]
        public void PlayerCombatIsWiredToTheSwordHitbox()
        {
            var prefab = LoadPlayer();
            var combat = new SerializedObject(prefab.GetComponent<PlayerCombat>());
            Assert.IsNotNull(combat.FindProperty("controller").objectReferenceValue);
            Assert.IsNotNull(combat.FindProperty("stats").objectReferenceValue);
            var sword = combat.FindProperty("swordHitbox").objectReferenceValue as Hitbox;
            Assert.IsNotNull(sword);
            Assert.AreEqual(Team.Player, sword.Team);
            var box = sword.GetComponent<BoxCollider2D>();
            Assert.IsTrue(box.isTrigger);
            Assert.AreEqual(SwordTiming.Reach, box.size.x, 1e-4f);
        }

        [Test]
        public void MovementConfigGivesTwoTileTapJumps() =>
            Assert.AreEqual(0.25f, AssetDatabase.LoadAssetAtPath<PlayerMovementConfig>(
                "Assets/_Project/Data/Player/PlayerMovementConfig.asset").JumpCutMultiplier, 1e-4f);
    }
}
