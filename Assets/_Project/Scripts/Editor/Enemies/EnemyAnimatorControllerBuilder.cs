using AuraKnight.Enemies;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Builds the shared Data/Enemies/EnemyAnimator.controller (states Idle, Move, Attack, Hurt, Death driven by the
    /// parameters <see cref="EnemyAnimatorBridge"/> sets) with short empty placeholder clips. The Art pass supplies real
    /// clips through an AnimatorOverrideController per variant (see <see cref="OverrideFor"/>).
    /// </summary>
    static class EnemyAnimatorControllerBuilder
    {
        public const string ControllerPath = EnemyAssetGenerator.DataFolder + "/EnemyAnimator.controller";
        const string ClipFolder = EnemyAssetGenerator.DataFolder + "/Animation";

        public static AnimatorController Build()
        {
            PlayerGeneratorUtil.EnsureFolder(ClipFolder);
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (existing != null) return existing;

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Moving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);

            var machine = controller.layers[0].stateMachine;
            var idle = AddState(machine, "Idle", 1f, true);
            var move = AddState(machine, "Move", 0.5f, true);
            var attack = AddState(machine, "Attack", 0.3f, false);
            var hurt = AddState(machine, "Hurt", 0.25f, false);
            var death = AddState(machine, "Death", 0.3f, false);
            machine.defaultState = idle;

            Link(idle, move, "Moving", AnimatorConditionMode.If);
            Link(move, idle, "Moving", AnimatorConditionMode.IfNot);
            FromAny(machine, attack, "Attack", AnimatorConditionMode.If);
            FromAny(machine, hurt, "Hurt", AnimatorConditionMode.If);
            FromAny(machine, death, "Dead", AnimatorConditionMode.If);
            Link(death, idle, "Dead", AnimatorConditionMode.IfNot);
            ExitTo(attack, idle);
            ExitTo(hurt, idle);
            AssetDatabase.SaveAssets();
            return controller;
        }

        /// <summary>The Art-delivered override controller for an art id (see <see cref="EnemyArt"/>), or null (the base controller is used then).</summary>
        public static RuntimeAnimatorController OverrideFor(string artId) =>
            AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(EnemyArt.OverrideControllerPath(artId));

        static AnimatorState AddState(AnimatorStateMachine machine, string name, float length, bool loop)
        {
            var clip = new AnimationClip { name = name, frameRate = 12f };
            var curve = AnimationCurve.Constant(0f, length, 0f);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "localPosition.z"), curve);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, $"{ClipFolder}/{name}.anim");
            var state = machine.AddState(name);
            state.motion = clip;
            return state;
        }

        static void Link(AnimatorState from, AnimatorState to, string parameter, AnimatorConditionMode mode)
        {
            var t = from.AddTransition(to);
            Configure(t, parameter, mode);
        }

        static void FromAny(AnimatorStateMachine machine, AnimatorState to, string parameter, AnimatorConditionMode mode)
        {
            var t = machine.AddAnyStateTransition(to);
            t.canTransitionToSelf = false;
            Configure(t, parameter, mode);
        }

        static void Configure(AnimatorStateTransition t, string parameter, AnimatorConditionMode mode)
        {
            t.hasExitTime = false;
            t.duration = 0f;
            t.AddCondition(mode, 0f, parameter);
        }

        static void ExitTo(AnimatorState from, AnimatorState to)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = true;
            t.exitTime = 1f;
            t.duration = 0f;
        }
    }
}
