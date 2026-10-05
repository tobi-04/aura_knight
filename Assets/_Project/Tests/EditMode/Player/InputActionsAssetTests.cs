using System.Linq;
using AuraKnight.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AuraKnight.Tests.Player
{
    public sealed class InputActionsAssetTests
    {
        const string AssetPath = "Assets/_Project/Settings/Input/AuraKnight.inputactions";

        static readonly string[] RequiredActions =
        {
            "Move", "Jump", "Attack", "Dash", "Skill", "Slide", "AuraWind", "AuraFire",
            "AuraWater", "AuraNext", "AuraPrev", "Pause", "Map"
        };

        InputActionAsset _asset;

        [SetUp]
        public void SetUp()
        {
            _asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);
            Assert.IsNotNull(_asset, "input actions asset missing at " + AssetPath);
        }

        [Test]
        public void GameplayMapHasEveryIntentAction()
        {
            var map = _asset.FindActionMap("Gameplay");
            Assert.IsNotNull(map);
            foreach (var name in RequiredActions)
                Assert.IsNotNull(map.FindAction(name), "missing action " + name);
        }

        [TestCase("Jump", "<Gamepad>/buttonSouth")]
        [TestCase("Attack", "<Gamepad>/buttonWest")]
        [TestCase("Dash", "<Gamepad>/rightShoulder")]
        [TestCase("Skill", "<Gamepad>/buttonNorth")]
        [TestCase("AuraPrev", "<Gamepad>/leftShoulder")]
        [TestCase("AuraNext", "<Gamepad>/rightTrigger")]
        [TestCase("Move", "<Gamepad>/leftStick")]
        public void GamepadLayoutMatchesGdd(string action, string path)
        {
            var a = _asset.FindAction("Gameplay/" + action);
            Assert.IsTrue(a.bindings.Any(b => b.path == path), $"{action} lacks {path}");
        }

        [TestCase("Jump", "<Keyboard>/space")]
        [TestCase("Attack", "<Keyboard>/j")]
        [TestCase("Dash", "<Keyboard>/k")]
        [TestCase("Skill", "<Keyboard>/l")]
        [TestCase("Slide", "<Keyboard>/c")]
        [TestCase("AuraWind", "<Keyboard>/1")]
        [TestCase("AuraFire", "<Keyboard>/2")]
        [TestCase("AuraWater", "<Keyboard>/3")]
        public void KeyboardBindingsForEditorTesting(string action, string path)
        {
            var a = _asset.FindAction("Gameplay/" + action);
            Assert.IsTrue(a.bindings.Any(b => b.path == path), $"{action} lacks {path}");
        }

        [Test]
        public void MoveHasWasdAndArrowComposites()
        {
            var move = _asset.FindAction("Gameplay/Move");
            Assert.AreEqual(2, move.bindings.Count(b => b.isComposite));
            Assert.IsTrue(move.bindings.Any(b => b.path == "<Keyboard>/a"));
            Assert.IsTrue(move.bindings.Any(b => b.path == "<Keyboard>/leftArrow"));
        }

        [Test]
        public void EveryOnScreenPathResolvesOnAVirtualGamepad()
        {
            var pad = InputSystem.AddDevice<Gamepad>("TestVirtualPad");
            try
            {
                foreach (var path in VirtualControlPaths.All)
                    Assert.IsNotNull(InputControlPath.TryFindControl(pad, path), "unresolvable on-screen path " + path);
            }
            finally { InputSystem.RemoveDevice(pad); }
        }

        [Test]
        public void OnScreenPathsAreBoundByActions()
        {
            var bound = _asset.FindActionMap("Gameplay").bindings.Select(b => b.path).ToHashSet();
            foreach (var path in VirtualControlPaths.All)
                Assert.IsTrue(bound.Contains(path), "no action bound to on-screen path " + path);
        }
    }
}
