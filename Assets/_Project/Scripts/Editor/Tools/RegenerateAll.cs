using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AuraKnight.Editor.Tools
{
    /// <summary>One regeneration stage: a name for the log and the generator call.</summary>
    public readonly struct RegenerateStep
    {
        public readonly string Name;
        public readonly Action Run;

        public RegenerateStep(string name, Action run)
        {
            Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("A step needs a name", nameof(name)) : name;
            Run = run ?? throw new ArgumentNullException(nameof(run));
        }
    }

    /// <summary>
    /// Runs every generator in dependency order so a fresh checkout (or a changed generator) yields consistent assets and scenes.
    /// Each generator is idempotent, so a second run leaves the working tree unchanged. A failing step logs its name and rethrows,
    /// which makes the batch process exit non-zero. Menu: Aura/Regenerate All. Batch: tools/unity-batch.sh exec AuraKnight.Editor.Tools.RegenerateAll.Run
    /// To add a module (bosses, progression, ...): append a <see cref="RegenerateStep"/> in <see cref="BuildSteps"/> after the stage it
    /// depends on and add that module's editor asmdef to AuraKnight.Editor.Tools.asmdef.
    /// </summary>
    public static class RegenerateAll
    {
        /// <summary>The stages in execution order (rebuilt on each access so tests see the real list).</summary>
        public static IReadOnlyList<RegenerateStep> Steps => BuildSteps();

        static List<RegenerateStep> BuildSteps() => new List<RegenerateStep>
        {
            // Sheets, slices, clips, controllers, Mat_SpriteLit. Everything below loads this output by path.
            new RegenerateStep("art", ArtPipeline.GenerateAll),
            // Player prefab (Leo art + Animator, Aura components, PlayerSfxProbe) and the movement test scene.
            new RegenerateStep("player", () => { PlayerAssetGenerator.Generate(); MovementTestSceneGenerator.Generate(); }),
            // Aura definitions / skill prefabs / interactables (the Player step already built them); the Aura test scene instantiates the Player.
            new RegenerateStep("aura", () => { AuraAssetGenerator.Generate(); AuraTestSceneGenerator.Generate(); }),
            // Enemy stats (with artId), pickups, prefabs on the art controllers, Test_Enemies.
            new RegenerateStep("enemies", EnemyAssetGenerator.GenerateAll),
            // Mixer, SfxLibrary, music, the Audio object in Core, probe on the Player prefab.
            new RegenerateStep("audio", AudioAssetGenerator.GenerateAll),
            // RegionGraph, room template, altars, region start rooms, Test_Rooms, Core managers + camera.
            new RegenerateStep("world-core", WorldSceneGenerator.GenerateAll),
            // Fonts, theme, HUD, screens, UI_Root in Core and the MainMenu screens (needs Core to exist).
            new RegenerateStep("ui", UiGenerator.GenerateAll),
            new RegenerateStep("validators", RunValidators),
        };

        [MenuItem("Aura/Regenerate All")]
        public static void Run()
        {
            var steps = BuildSteps();
            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                Debug.Log($"[RegenerateAll] ({i + 1}/{steps.Count}) {step.Name}");
                try { step.Run(); }
                catch (Exception e)
                {
                    Debug.LogError($"[RegenerateAll] Step '{step.Name}' failed: {e}");
                    throw;
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[RegenerateAll] Done: {string.Join(" > ", steps.Select(s => s.Name))}");
        }

        static void RunValidators()
        {
            var errors = new List<string>();
            errors.AddRange(RoomIdValidator.Validate(out int rooms));
            errors.AddRange(EnemyRoomLimitValidator.Validate());
            if (errors.Count > 0)
                throw new InvalidOperationException($"{errors.Count} validation error(s):\n{string.Join("\n", errors)}");
            Debug.Log($"[RegenerateAll] Validators clean ({rooms} room(s)).");
        }
    }
}
