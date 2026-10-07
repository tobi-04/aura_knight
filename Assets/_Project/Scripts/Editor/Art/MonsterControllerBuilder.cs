using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Base Animator Controllers for enemies and bosses (state names = clip names) plus one AnimatorOverrideController per
    /// variant that swaps in that variant's sprite clips. Enemies attach the override controller to their Animator.
    /// </summary>
    public static class MonsterControllerBuilder
    {
        public const string EnemyBasePath = ArtPaths.Root + "/Enemies/_Base/EnemyBase.controller";
        public const string BossBasePath = ArtPaths.Root + "/Bosses/_Base/BossBase.controller";

        static readonly string[] EnemyStates = { "Idle", "Move", "Attack", "Hurt", "Death" };
        static readonly string[] BossStates = { "Idle", "Move", "Attack1", "Attack2", "Attack3", "Hurt", "Death", "Exposed" };

        public static AnimatorController EnsureBase(bool boss)
        {
            string path = boss ? BossBasePath : EnemyBasePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path) ?? AnimatorController.CreateAnimatorControllerAtPath(path);
            foreach (var p in controller.parameters) controller.RemoveParameter(p);
            var machine = controller.layers[0].stateMachine;
            foreach (var child in machine.states) machine.RemoveState(child.state);
            foreach (var t in machine.anyStateTransitions) machine.RemoveAnyStateTransition(t);

            controller.AddParameter("Moving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
            if (boss)
            {
                controller.AddParameter("Exposed", AnimatorControllerParameterType.Bool);
                for (int i = 1; i <= 3; i++) controller.AddParameter("Attack" + i, AnimatorControllerParameterType.Trigger);
            }
            else controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

            var states = new Dictionary<string, AnimatorState>();
            int x = 0;
            foreach (var name in boss ? BossStates : EnemyStates)
            {
                var state = machine.AddState(name, new Vector3(250 + 220 * (x % 4), 40 + 70 * (x / 4), 0));
                state.motion = EnsureBaseClip(Path.GetDirectoryName(path).Replace('\\', '/'), name);
                state.writeDefaultValues = false;
                states[name] = state;
                x++;
            }
            machine.defaultState = states["Idle"];
            Link(states["Idle"], states["Move"], false, Cond.On("Moving"));
            Link(states["Move"], states["Idle"], false, Cond.Off("Moving"));
            AnyTo(machine, states["Death"], Cond.On("Dead"));
            AnyTo(machine, states["Hurt"], Cond.Trigger("Hurt"));
            foreach (var attack in boss ? new[] { "Attack1", "Attack2", "Attack3" } : new[] { "Attack" })
            {
                AnyTo(machine, states[attack], Cond.Trigger(attack));
                Link(states[attack], states["Idle"], true);
            }
            Link(states["Hurt"], states["Idle"], true);
            if (boss)
            {
                Link(states["Idle"], states["Exposed"], false, Cond.On("Exposed"));
                Link(states["Exposed"], states["Idle"], false, Cond.Off("Exposed"));
            }
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        /// <summary>Creates/refreshes <c>&lt;folder&gt;/&lt;Name&gt;.overrideController</c> mapping each base clip to the sheet clip of the same name.</summary>
        public static AnimatorOverrideController BuildOverride(string folder, string name, AnimatorController baseController,
            IReadOnlyDictionary<string, AnimationClip> clips)
        {
            string path = $"{folder}/{name}.overrideController";
            var over = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
            if (over == null)
            {
                over = new AnimatorOverrideController(baseController);
                AssetDatabase.CreateAsset(over, path);
            }
            over.runtimeAnimatorController = baseController;
            var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            over.GetOverrides(pairs);
            for (int i = 0; i < pairs.Count; i++)
            {
                string key = pairs[i].Key.name;
                if (!clips.TryGetValue(key, out var replacement)) clips.TryGetValue("Idle", out replacement); // e.g. no Exposed clip
                pairs[i] = new KeyValuePair<AnimationClip, AnimationClip>(pairs[i].Key, replacement);
            }
            over.ApplyOverrides(pairs);
            EditorUtility.SetDirty(over);
            return over;
        }

        static AnimationClip EnsureBaseClip(string folder, string name)
        {
            string path = $"{folder}/{name}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null) return clip;
            clip = new AnimationClip { name = name };
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        static void Link(AnimatorState from, AnimatorState to, bool exitTime, params Cond[] conditions)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = exitTime;
            t.exitTime = 1f;
            t.hasFixedDuration = true;
            t.duration = 0f;
            foreach (var c in conditions) t.AddCondition(c.Mode, 0, c.Param);
        }

        static void AnyTo(AnimatorStateMachine machine, AnimatorState to, params Cond[] conditions)
        {
            var t = machine.AddAnyStateTransition(to);
            t.hasExitTime = false;
            t.hasFixedDuration = true;
            t.duration = 0f;
            t.canTransitionToSelf = false;
            foreach (var c in conditions) t.AddCondition(c.Mode, 0, c.Param);
        }

        readonly struct Cond
        {
            public readonly string Param;
            public readonly AnimatorConditionMode Mode;

            Cond(string param, AnimatorConditionMode mode) { Param = param; Mode = mode; }

            public static Cond On(string p) => new Cond(p, AnimatorConditionMode.If);
            public static Cond Off(string p) => new Cond(p, AnimatorConditionMode.IfNot);
            public static Cond Trigger(string p) => new Cond(p, AnimatorConditionMode.If);
        }
    }
}
