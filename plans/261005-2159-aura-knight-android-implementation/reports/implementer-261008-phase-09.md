# Phase 9 report: Level content, four regions (2026-10-08)

Status: DONE_WITH_CONCERNS. Nothing committed. No cut was needed: all 34 rooms are built (no 5-rooms-per-region fallback).

## Verification
| Check | Result |
|---|---|
| `tools/unity-batch.sh compile` | 0 errors, 0 warnings |
| `test EditMode` | 948 passed / 0 failed (was 800; +148: pure hazard/parallax/lighting/seal logic tests in `Tests/EditMode/World/Hazards`, the rest in the new `AuraKnight.Tests.EditMode.Levels`) |
| `test PlayMode` | 155 total: 154 passed, 0 failed, 1 skipped (`RuntimeScreenshotTests`, GPU; it also passes alone with `UNITY_GRAPHICS=1`). 27 are new (`AuraKnight.Tests.PlayMode.Levels`) |
| `exec AuraKnight.Editor.Tools.RegenerateAll.Run` | 12 steps clean, "Validators clean (34 room(s))", rerun twice |
| `RoomIdValidator`, `EnemyRoomLimitValidator`, `LevelGenerator.Validate` | 0 errors |

## Room counts (GDD 7.2, boss arenas included in each region)
Hub 3 (hub_01..03); Forest 7 + boss (forest_01..07, forest_boss); Cave 7 + boss; City 7 + boss; Castle 6 + boss (castle_01..06, castle_boss) = **34**. Vertical 22 x 44: forest_04, city_04, castle_04; everything else 40 x 22. 38 enemies in total (max 6 per room, validator), 2 altars per region (entry + before the boss), 2 secret chests per region (`chest_<region>_01/02`), 1 shortcut per region.

## Gating proof (all numbers from GDD 4)
- **Cave:** `cave_01` has a 3 x 6 smooth wall (`W`, new `SmoothWall` marker) on the floor, open above. Normal jump 4.5, jump + dash about 10 across, Wind double jump about 7.2.
  - EditMode `GatePhysicsTests` run the real `PlayerController` against the real room colliders: 70 random 14 s sessions plus a grid of about 2500 run-up / jump / dash timings plus 20 s of wall-leaning jump mashing, none crosses without Wind; with Wind a double jump timing crosses. Control run: the same wall without `SmoothWall` is cleared by wall jumps (that is why `SmoothWall` exists; I had to change `KinematicMotor2D`, see deviations).
  - `LevelValidator`: `requires: 2=wind` is checked with the generous `Max` model (4 up, 10 across, no Wind) and fails the build if reachable. A platform next to the wall was caught by it while I authored (and is a negative test now).
- **City:** `Z` barricade 2 x 5 in `hub_03`, floor to the slab over the corridor (so no jumping or wall-jumping over). PlayMode: `BurnableGate` accepts only Burn + Fire (Wind, Water, None and Extinguish refused), collider disappears, stays open after Continue.
- **Castle:** `G` seal gate in a roofed dead end of `hub_03` with 3 `Q` seals (Wind seal on a ledge 6 above the slab corridor, Fire on the middle platform, Water in a pool). A seal lights only while Leo wears its Aura; Auras not earned cannot be worn. PlayMode: gate opens only with all 3 lit, state saved.
- `LevelProgression` plays the whole game on paper (boss reward = Aura): no Aura reaches only hub, Forest and `cave_01`; Wind opens the Cave; Wind + Water do not open the barricade; Fire opens the City; all three open the Castle; at the end all 34 rooms and Malakor are reachable.
- Secrets: Wind chimneys (3 wide, 8 tall, smooth walls, updraft inside), Water chimneys (same, flooded), Fire thorn closets, castle fire-trap closet (Water). Each is proven unreachable without its Aura and reachable with it.

## No softlocks
Every pit is either a floor-level `K` kill zone (lethal hitbox, Leo respawns at the last altar; PlayMode tests it end to end) or has stairs/floor; chimneys open at floor level; shortcut pockets open from inside. Arrival spots are validated: no enemy within 7 columns, no hazard within 2. Every RoomExit resolves to a real spawn (`LevelPrefabValidator`), and PlayMode walks every door of all five regions both ways (spawn right, no bounce, region preloaded in time).

## Files (all under Assets/_Project unless noted)
Runtime, namespace `AuraKnight.World` / `.Hazards`: `World/Hazards/` Spikes (+lethal option for pits), CollapseCycle, CollapsingPlatform (0.6 s / 3 s, waits while Leo stands on the spot), StalactiteCycle, FallingStalactite (0.5 s shake), PistonCycle, Piston, PingPongPath, MovingSpikeFloor, AcidPool (Water-immune); `World/` ParallaxMath, ParallaxLayer (0.1 / 0.5 / 1.0 / 1.2, vertical locked to camera, only the current room draws), RegionLightingTable, RegionLighting (one Global Light 2D per region scene, on only for the current region: 1.0 hub, 0.6, 0.25, 0.45, 0.05), RegionPreloadZone, SealRules, AuraSeal, SealGate; `Player/SmoothWall.cs`. All under 200 lines.
Editor, `Scripts/Editor/World/Levels/` (asmdef `AuraKnight.Editor.World`, +URP 2D ref): RoomFile, RoomFileParser, RoomHeaderFields, LevelAbilities, LevelRegions, LevelCatalog, LevelReachability, LevelProgression, LevelValidator, LevelPrefabValidator, HazardPrefabs, GatePrefabs, PrefabKit, TilesetArt, RoomGeometry, RoomBuild, RoomPrefabBuilder, RoomTerrain, RoomHazards, RoomProps, RoomEnemies, RoomLinks, RoomBackdrop, BossRoomLinker, LevelSceneBuilder, LevelGenerator, AssemblyInfo.
Data: `Data/Levels/<Region>/<id>.room.txt` (30 files, 980 lines, the source of truth). Generated: `Prefabs/Rooms/*/Room_<id>.prefab` (30), 4 boss arenas finished, `Prefabs/Hazards/*` (7), `Prefabs/Interactables/{SealGate,AuraSeal}`, `Region_*` scenes, `RegionGraph` altars, `RoomMapData_*` (rebuilt, the map shows the real rooms).
Tests: `Tests/EditMode/World/Hazards/*` (7 files), `Tests/EditMode/Levels/*` (9 files + asmdef), `Tests/PlayMode/Levels/*` (5 files + asmdef).
Docs: `docs/level-map.md` (map, legend, per-room tables, gates, secrets, shortcuts), `system-architecture.md` (§9 chain, new §11), `code-standards.md`, `development-roadmap.md`, `project-changelog.md`.

## Modified existing files (deviations from "own files only")
- `Scripts/Player/KinematicMotor2D.cs` (+`GripsWall`), `PlayerController.Api.cs` (`WallContactToward` uses it): without a non-grippable marker the wall-jump chain climbs the 6-tall wall (shown by the control test), so the Cave gate would not need Wind. Minimal and covered by the existing simulation tests.
- `Scripts/Editor/Tools/RegenerateAll.cs`: new steps `bosses`, `progression`, `levels`, `map`, validators extended; `RegenerateAllOrderTests` updated. Tools asmdef already referenced everything needed.
- `Scripts/Editor/World/WorldAssetGenerator.cs`: altars in the RegionGraph come from the room files; the altar and shortcut prefabs got a greybox look (they were invisible); `RegionSceneGenerator.cs`: no greybox StartRoom for regions that have room files.
- `Scripts/Editor/Tools/ShotJob.cs`, `ScreenshotSetup.cs`: `-shotFocus x,y` and `-shotOrtho size` so one room can be framed.
- Existing PlayMode tests that clashed with the now real world: `RoomSwitchPlayModeTests` (extra room id `hub_99`), `Bosses/BossTestKit.SpawnRoom` (renames the copy's room id), `WorldFlowPlayModeTests` (forest_01 is a gateway room that keeps the hub preloaded, so "hub unloaded" was replaced by "unrelated regions are not loaded").
- Steam vent and fire trap are NOT new classes: they reuse `HeatVent` (Fire immune, 2 s cycle) and `ExtinguishableGate` (Water puts it out), as asked.
- One-way platforms are thin boxes with `PlatformEffector2D` (collision is explicit boxes, not tile colliders, so the physics match the grid the tests reason about); tiles are visual only.

## What I looked at (screenshots, honest)
Edit-mode `shot` of hub_01, hub_03, cave_01, forest_03, city_02, castle_03 (1920x1080, camera framed per room) and the runtime HUD shot in hub_01. They show the region tiles, parallax, hazards, gates and enemies in place; castle at 0.05 is nearly black as designed (only the ghosts and the fire trap read); cave at 0.25 is dark but readable. The edit-mode shots draw all four parallax layers at their anchor, so top and bottom teeth of the foreground layer sit mid-screen; in play they follow the camera (runtime shot looks right). Not checked: city/castle in motion, boss arena visuals, any device.

## Concerns
1. **No human playtest.** Distances are validated against the movement numbers and played by a physics bot only at the Cave wall; difficulty, pacing and the 60-90 minute target are unmeasured. Enemy density is low (38 total) because enemies are kept 8 columns from door spawns.
2. Pogo: spikes and enemies are sword-pogo targets (phase 4 rule). None are near the Cave wall, but the solver does not model pogo; a future layout edit could reintroduce a vault.
3. Art: Leo, tiles and parallax are the phase 2 generated art; props (altar, gates, seals, pistons, chests) are coloured squares; no decor layer.
4. `Aura` unlock popup pauses the game: tests resume it explicitly; fine in play.
5. The room layouts were composed with a throwaway authoring script (kept out of the repo) and then emitted as the `.room.txt` files; those files are meant to be edited by hand from now on and are validated at generate time.
6. `tools/unity-batch.sh exec` prints `exit=0` even when the batch aborted; I check the log for `Aborting`/`Step ... failed` (memory note saved). Early PlayMode runs in this phase passed against stale prefabs because of that; the final runs were after a verified clean `RegenerateAll`.

**Status:** DONE_WITH_CONCERNS
**Summary:** 34 rooms across Hub and four regions are generated from `Data/Levels/*.room.txt`, with hazards, parallax, region lighting, the Cave smooth wall, the hub barricade and the 3-seal castle gate. Gating is proven by a grid solver (comfortable and physical models), a real-controller simulation, a whole-game walk and PlayMode tests. EditMode 948/948, PlayMode 154 passed + 1 skipped (GPU), compile clean, `RegenerateAll` clean.
**Concerns/Blockers:** no human playtest (balance, pacing, 60-90 min target unverified); small edit to Player motor for non-grippable walls and edits to four existing tests that clashed with the real world; placeholder art for props.
