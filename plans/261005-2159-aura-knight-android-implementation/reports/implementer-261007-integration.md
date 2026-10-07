# Integration fixes + screenshot tool (2026-10-07)

## Generator order (`Aura/Regenerate All`, `AuraKnight.Editor.Tools.RegenerateAll.Run`)
art > player (+movement scene) > aura (+aura scene) > enemies (+Test_Enemies) > audio (+Core Audio object, probe) > world-core > ui > validators.
List is `RegenerateAll.BuildSteps()`; append a `RegenerateStep` (and the module's asmdef in `AuraKnight.Editor.Tools.asmdef`) for bosses/progression. Boss/progression generators are not included yet.
Fix: `PlayerAssetGenerator.Generate` now calls `PlayerSfxProbeInstaller.Apply()` after rebuilding the prefab. Aura components and Leo art/Animator/Mat_SpriteLit were already built by that generator; tests now guard all of it.

## Enemy naming map (single source: `EnemyVariants.All[*].ArtId`, copied to new field `EnemyStats.artId`)
BugThorn>ThornBug, PatrolBot>PatrolRobot, PoisonShroom>MushroomHopper; NightKnight, Bat, Ghost, StoneSpider, ScrapZapper identical.
Prefabs now use `<ArtId>_Idle_0`, the variant `.overrideController`, `Mat_SpriteLit`, white colour, scale 1. Placeholder fallback (with warning) only if art is missing. `EnemyAssetGenerator` no longer regenerates the Player. `Test_Enemies` got a Global Light 2D (lit material rendered enemies black without it; found by the screenshots).

## Screenshots (`tools/unity-batch.sh shot`, no -nographics; works on this Mac, Metal)
15 PNGs in Logs/screenshots: MainMenu, Core_Region_Hub (Player at altar + HUD from Core UI_Root), Test_Movement, Test_Aura, Test_Enemies at 1920x1080, 2340x1080, 2520x1080. Each has >2000 distinct colours (blank images fail the run). Viewed MainMenu, Core, Test_Enemies: correct.
Limitations: edit mode (static state, HUD at default values, no hearts); Overlay canvases converted to Screen Space Camera and ScaleWithScreenSize scalers replaced by an equivalent constant factor (stock scaler reads display size); Cinemachine disabled, camera placed by hand; Core/Region have no Global Light 2D so lit sprites are only lit by Leo's glow, and the Hub floor is not visible in the Core shot (rooms inactive in edit mode).

## Tests
compile clean. EditMode 800 total, 799 pass; the 1 failure is `BossLogicTests.SlowLayersOnTopOfAnotherWritersSpeedAndRestoresIt` (Bosses agent, in progress). PlayMode 96/96. New: Tests/EditMode/Integration (36 tests: enemy art x8 x2, player prefab, step order, size parsing).

## Idempotence
Two RegenerateAll runs ran clean. Byte-identical output is NOT achievable: Unity assigns random fileIDs to objects created in code, so prefabs/scenes/controllers (Leo/EnemyBase/BossBase.controller via Art builders, which also rebuild states each run) differ in fileID numbers only (verified for Player.prefab, Leo.controller, EnemyBase.controller with ids normalised). Other agents' concurrent edits also changed the git status, so a clean second-run `git status` was not verifiable.
First run also touched scenes/UI prefabs/controllers/preset (re-serialisation by this Unity version plus regenerated content).

## Files
New: Scripts/Editor/Tools/{AuraKnight.Editor.Tools.asmdef,RegenerateAll,SceneScreenshot,ScreenshotSetup,ScreenshotRender,ShotJob}.cs, Scripts/Editor/Enemies/EnemyArt.cs, Tests/EditMode/Integration/*.
Modified: Player/PlayerAssetGenerator.cs, Enemies/{EnemyVariantSpec,EnemyAssetGenerator,EnemyPrefabBuilder,EnemyAnimatorControllerBuilder,EnemyTestSceneGenerator}.cs, Scripts/Enemies/EnemyStats.cs (+artId), tools/unity-batch.sh (+shot), docs/system-architecture.md (128 lines), docs/code-standards.md (92 lines). Regenerated: Prefabs/Enemies, Data/Enemies, Player prefab, Test scenes, others via RegenerateAll.
