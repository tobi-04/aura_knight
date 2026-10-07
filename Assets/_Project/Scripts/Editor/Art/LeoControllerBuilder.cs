using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Builds Leo.controller. Parameters are driven by PlayerAnimatorBridge: Speed, VelY (floats); Grounded, WallSlide, Dash,
    /// Slide, Swim, Attack, AirAttack, Hurt, Dead (bools); ComboStep (1-2) and AimDir (0 forward, 1 up, 2 down) while attacking.
    /// </summary>
    public static class LeoControllerBuilder
    {
        public static AnimatorController Build(Dictionary<string, AnimationClip> clips)
        {
            string path = ArtPaths.LeoController;
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path) ?? AnimatorController.CreateAnimatorControllerAtPath(path);
            for (int i = controller.layers.Length - 1; i > 0; i--) controller.RemoveLayer(i);
            foreach (var p in controller.parameters) controller.RemoveParameter(p);
            foreach (var f in new[] { "Speed", "VelY" }) controller.AddParameter(f, AnimatorControllerParameterType.Float);
            foreach (var b in new[] { "Grounded", "WallSlide", "Dash", "Slide", "Swim", "Attack", "AirAttack", "Hurt", "Dead" })
                controller.AddParameter(b, AnimatorControllerParameterType.Bool);
            foreach (var n in new[] { "ComboStep", "AimDir" }) controller.AddParameter(n, AnimatorControllerParameterType.Int);

            var machine = controller.layers[0].stateMachine;
            foreach (var child in machine.states) machine.RemoveState(child.state);
            foreach (var t in machine.anyStateTransitions) machine.RemoveAnyStateTransition(t);

            var s = new Dictionary<string, AnimatorState>();
            int x = 0;
            foreach (var name in new[] { "Idle", "Run", "Jump", "Fall", "WallSlide", "Dash", "Slide", "Swim", "Attack1", "Attack2",
                         "AttackUp", "AirAttack", "AirAttackUp", "AirAttackDown", "Hurt", "Death" })
            {
                var state = machine.AddState(name, new Vector3(250 + 220 * (x % 4), 40 + 70 * (x / 4), 0));
                state.motion = clips[name];
                state.writeDefaultValues = false;
                s[name] = state;
                x++;
            }
            machine.defaultState = s["Idle"];
            AddLocomotion(s);
            AddAnyStateRules(machine, s);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        static void AddLocomotion(Dictionary<string, AnimatorState> s)
        {
            Edge(s["Idle"], s["Run"], Gt("Speed", 0.1f), On("Grounded"));
            Edge(s["Run"], s["Idle"], Lt("Speed", 0.1f));
            foreach (var from in new[] { "Idle", "Run", "Fall" }) Edge(s[from], s["Jump"], Off("Grounded"), Gt("VelY", 0.1f));
            foreach (var from in new[] { "Idle", "Run", "Jump" }) Edge(s[from], s["Fall"], Off("Grounded"), Lt("VelY", 0.1f));
            foreach (var from in new[] { "Jump", "Fall" }) Edge(s[from], s["Idle"], On("Grounded"));
            // States entered from Any State fall back to Idle once their flag clears; Idle then routes to Fall/Run as needed.
            Edge(s["WallSlide"], s["Idle"], Off("WallSlide"));
            Edge(s["Dash"], s["Idle"], Off("Dash"));
            Edge(s["Slide"], s["Idle"], Off("Slide"));
            Edge(s["Swim"], s["Idle"], Off("Swim"));
            Edge(s["Hurt"], s["Idle"], Off("Hurt"));
            foreach (var attack in new[] { "Attack1", "Attack2", "AttackUp" }) Edge(s[attack], s["Idle"], Off("Attack"));
            foreach (var attack in new[] { "AirAttack", "AirAttackUp", "AirAttackDown" }) Edge(s[attack], s["Idle"], Off("AirAttack"));
        }

        static void AddAnyStateRules(AnimatorStateMachine machine, Dictionary<string, AnimatorState> s)
        {
            Any(machine, s["Death"], On("Dead"));
            Any(machine, s["Hurt"], On("Hurt"), Off("Dead"));
            Any(machine, s["Attack1"], On("Attack"), Eq("AimDir", 0), Lt("ComboStep", 2));
            Any(machine, s["Attack2"], On("Attack"), Eq("AimDir", 0), Gt("ComboStep", 1));
            Any(machine, s["AttackUp"], On("Attack"), Eq("AimDir", 1));
            Any(machine, s["AirAttack"], On("AirAttack"), Eq("AimDir", 0));
            Any(machine, s["AirAttackUp"], On("AirAttack"), Eq("AimDir", 1));
            Any(machine, s["AirAttackDown"], On("AirAttack"), Eq("AimDir", 2));
            foreach (var state in new[] { "Dash", "Slide", "WallSlide", "Swim" }) Any(machine, s[state], On(state), Off("Hurt"));
        }

        readonly struct Cond
        {
            public readonly string Param;
            public readonly AnimatorConditionMode Mode;
            public readonly float Value;

            public Cond(string param, AnimatorConditionMode mode, float value) { Param = param; Mode = mode; Value = value; }
        }

        static Cond On(string p) => new Cond(p, AnimatorConditionMode.If, 0);
        static Cond Off(string p) => new Cond(p, AnimatorConditionMode.IfNot, 0);
        static Cond Gt(string p, float v) => new Cond(p, AnimatorConditionMode.Greater, v);
        static Cond Lt(string p, float v) => new Cond(p, AnimatorConditionMode.Less, v);
        static Cond Eq(string p, int v) => new Cond(p, AnimatorConditionMode.Equals, v);

        static void Edge(AnimatorState from, AnimatorState to, params Cond[] conditions) => Configure(from.AddTransition(to), conditions);

        static void Any(AnimatorStateMachine machine, AnimatorState to, params Cond[] conditions)
        {
            var t = machine.AddAnyStateTransition(to);
            t.canTransitionToSelf = false;
            Configure(t, conditions);
        }

        static void Configure(AnimatorStateTransition t, Cond[] conditions)
        {
            t.hasExitTime = false;
            t.hasFixedDuration = true;
            t.duration = 0f;
            foreach (var c in conditions) t.AddCondition(c.Mode, c.Value, c.Param);
        }
    }
}
