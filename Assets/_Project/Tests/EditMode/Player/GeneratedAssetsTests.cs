using System.Linq;
using AuraKnight.Player;
using AuraKnight.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;

namespace AuraKnight.Tests.Player
{
    /// <summary>Guards the output of the editor generators (run AuraKnight.Editor.MovementTestSceneGenerator.Generate).</summary>
    public sealed class GeneratedAssetsTests
    {
        static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.IsNotNull(asset, "missing generated asset " + path);
            return asset;
        }

        [Test]
        public void PlayerPrefabHasWiredComponents()
        {
            var prefab = Load<GameObject>("Assets/_Project/Prefabs/Player/Player.prefab");
            Assert.IsNotNull(prefab.GetComponent<PlayerController>());
            Assert.IsNotNull(prefab.GetComponent<KinematicMotor2D>());
            Assert.IsNotNull(prefab.GetComponent<PlayerInputReader>());
            Assert.IsNotNull(prefab.GetComponent<PlayerAnimatorBridge>());
            Assert.AreEqual(RigidbodyType2D.Kinematic, prefab.GetComponent<Rigidbody2D>().bodyType);
            Assert.AreEqual(new Vector2(0.8f, 1.9f), prefab.GetComponent<CapsuleCollider2D>().size);
            var animator = prefab.GetComponentInChildren<Animator>();
            Assert.IsNotNull(animator, "Leo's visual carries the Animator driven by PlayerAnimatorBridge");
            Assert.IsNotNull(animator.runtimeAnimatorController);
            var renderer = prefab.GetComponentInChildren<SpriteRenderer>();
            Assert.AreEqual("Leo_Idle_0", renderer.sprite.name);
            Assert.AreEqual("Mat_SpriteLit", renderer.sharedMaterial.name, "lit material so Light2D and the Aura tint show");

            var controller = new SerializedObject(prefab.GetComponent<PlayerController>());
            Assert.IsNotNull(controller.FindProperty("config").objectReferenceValue);
            var reader = new SerializedObject(prefab.GetComponent<PlayerInputReader>());
            Assert.IsNotNull(reader.FindProperty("actions").objectReferenceValue);
        }

        [Test]
        public void VirtualControlsPrefabMeetsTouchTargetRules()
        {
            var prefab = Load<GameObject>("Assets/_Project/Prefabs/UI/VirtualControls.prefab");
            Assert.IsNotNull(prefab.GetComponentInChildren<DynamicJoystick>(true));
            Assert.IsNotNull(prefab.GetComponentInChildren<SwipeDetector>(true));
            Assert.IsNotNull(prefab.GetComponentInChildren<SafeAreaFitter>(true));

            var buttons = prefab.GetComponentsInChildren<OnScreenButton>(true);
            Assert.AreEqual(9, buttons.Length);
            foreach (var b in buttons)
                Assert.GreaterOrEqual(((RectTransform)b.transform).sizeDelta.x, 64f, b.name + " below 64 dp");
            var jump = buttons.Single(b => b.name == "Jump");
            Assert.AreEqual(96f, ((RectTransform)jump.transform).sizeDelta.x);
            CollectionAssert.AreEquivalent(
                VirtualControlPaths.All.Where(p => p != VirtualControlPaths.LeftStick && p != VirtualControlPaths.Slide),
                buttons.Select(b => b.controlPath));
        }

        [Test]
        public void MovementConfigAssetExists() =>
            Load<PlayerMovementConfig>("Assets/_Project/Data/Player/PlayerMovementConfig.asset");

        [Test]
        public void TestSceneExists() =>
            Load<SceneAsset>("Assets/_Project/Scenes/Test/Test_Movement.unity");
    }
}
