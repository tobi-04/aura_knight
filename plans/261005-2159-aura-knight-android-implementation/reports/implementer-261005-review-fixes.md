# Review fixes — phases 1, 3, 4, 5, 6 (2026-10-05)

Source: `reviewer-261005-phases-1-3-4-5-6.md`. Nothing committed.

## Verification
| Check | Result |
|---|---|
| `tools/unity-batch.sh compile` | no `error CS`, no `warning CS` (two pre-existing warnings fixed too) |
| `test EditMode` | 413 passed / 0 failed (was 333; +80 new or updated) |
| `test PlayMode` | 14 passed / 0 failed. PlayMode runs headless in batch mode (about 10 s) |
| Generators rerun | ProjectSetup, Player, Aura, MovementTest, AuraTest, WorldScene (now also region start rooms), RoomIdValidator (0 errors) |
| `BuildDevelopmentApk` | Succeeded; `aapt dump permissions` shows `android.permission.VIBRATE` |
| `BuildReleaseApk` | exits 1 with "no custom keystore selected" (verified) |

## Critical
- **C1 respawn across regions: fixed.**
  - Altar directory: `RegionNode.altarIds` in the RegionGraph, with `TryGetRegionOfAltar`. Validated by `AltarValidator` and `RoomIdValidator` (graph rules, plus each region scene must contain exactly its listed altars).
  - `CheckpointService.Respawn()` is now a coroutine. It resolves `lastAltarId`, calls `WorldEntry.FindAltar` (RegionLoader.SetCurrentRegion, then waits until the scene is loaded and the altar is registered), and warps through `AltarArrival.Place` (RoomManager.Warp, then EnterRoom). It publishes `PlayerRespawned` only on arrival.
  - If the altar cannot be resolved it falls back to the Hub start altar and keeps retrying. Revive-in-place is removed: `DeadState` stays Dead and asks again after each 1.2 s.
  - Tests: sim test for no destination (Leo stays Dead, retries); PlayMode tests for cross-region respawn and for an unresolvable altar falling back to the hub.
- **C2 resume flow: fixed.**
  - New `World/WorldEntry` with `StartNewGame()`, `Continue()` and an `Entered` event. It sets the GameManager state, loads the region of `lastAltarId` next to Core, spawns (or reuses) the player, enters the room and sets Mode=Playing.
  - An unknown altar falls back to the hub. `GameManager.StartNewGame/Continue` publish `GameStateLoaded`.
  - The Core scene generator wires WorldEntry (graph, loader, player prefab).
  - Every region scene now gets a generated greybox start room plus its altar, via `RegionSceneGenerator`.
  - Tests: PlayMode for new game, continue, no save, unknown altar, and a second Continue refreshing a live player.

## Warnings
| ID | Status | Notes |
|---|---|---|
| W1 save | fixed | `FileSaveStorage` writes tmp then `File.Replace(tmp, path, .bak)`, with a copy fallback and a first-write path. `SaveSystem` falls back to the backup when the main file is bad. `HasSave` means a loadable save exists. 9 new tests (memory + temp-dir; the copy fallback is forced by an internal flag). |
| W2 init order | fixed | `[DefaultExecutionOrder(-100)]` on GameManager. New `GameStateLoaded` event. PlayerStats, AuraManager, the one-time gates and Shortcut re-read on it. PlayMode test covers PlayerStats and AuraManager. |
| W3 RegionLoader | fixed | Tracks loading and unloading ops. `RegionLoadPlan` now defers loading a region that is unloading and unloading one that is loading, and re-plans on completion. 6 pure plan tests. The loader's coroutine side is exercised by the PlayMode flows. |
| W4 haptics | fixed | 20 ms `VibrationEffect.createOneShot` through `AndroidJavaObject`, guarded by `UNITY_ANDROID && !UNITY_EDITOR`. Cached flag, `SetEnabled`, `Refresh`. Permission verified in the built APK. Not run on a device. |
| W5 pause / dead | fixed | New `PlayerActionRules`. `PlayerController.ControlsEnabled` (GameManager.Mode must be Playing when a manager exists). No input latching while off, and buffers are dropped when controls go off. AuraManager switch/cycle is blocked while Dead; skills are blocked while Dead or Hurt (`CastResult.Blocked`). Sword attack while swimming works: Swim goes to AirAttack, which keeps swim steering and returns to Swim. Tests in `PlayerActionGatingTests`, `AuraActionGatingTests`. |
| W6 HitStop | fixed | `HitStopTimer.Tick(..., paused)` does not lift a pause that began during the freeze. Added `SavedScale`, and `HitStop.GameplayScale` / `IsFrozen` for the future pause menu. The runner no longer uses `HideAndDontSave`, so it dies with play mode. |
| W7 layers | fixed | `PhysicsLayers` (Ground, Player, Enemy, Hazard, PlayerAttack, EnemyAttack, Interactable). `PhysicsLayerSetup` defines them in TagManager and sets the collision matrix from `CollidingPairs`; it runs in Aura/Setup Project. Motor mask defaults to Ground and the player prefab sets it explicitly. `Hurtbox`/`Hitbox` Team setters put objects on their team layers, and the Hitbox overlap mask is per team. The probe uses Interactable|Ground; `FindSolid` uses Ground. All generators and test scenes use layers. Matrix and mapping tests added. |
| W8 camera warp | fixed | All respawn/continue warps go through `RoomManager.Warp`. PlayMode test checks the camera follows the jump, and I confirmed it fails when the warp call is removed. |
| W9 Unlock persistence | fixed | `AuraManager.Unlock` writes GameState then `GameManager.Save()`. Doc comment: bosses call Unlock before publishing BossDefeated. PlayMode test checks the file on disk. |
| W10 singletons | fixed | `Singleton.IsDuplicate` is the one convention: the duplicate disables itself (so OnEnable never runs) and removes its component. Used by GameManager, RoomManager, CheckpointService, WorldEntry, AuraManager. PlayMode test checks the subscriber count. |
| W11 water count | fixed | Removed the reset on `PlayerRespawned`. WaterVolume enter/exit/OnDisable already keep the count correct, and the reset broke swimming when the altar is inside the same volume. |
| W12 altar re-entry | fixed | Same altar and a save already exists means no disk write; the heal still happens (PlayerStats). PlayMode tests count the writes. |

## Validated items and suggestions
- `Hitbox.Source` setter added. `FireballProjectile.Launch(pos, facing, owner)` passes Leo, and the fireball skill supplies it. Tested.
- The fireball passes through one-way platforms (`usedByEffector`). Tested.
- Fireballs are moved into the skill's scene (the "active scene" item).
- RoomManager confiner: damping is held for `max(blendSeconds, blendDamping)` (`RoomBlendTiming`). Room.Deactivate is delayed by the same time and skipped if the room is current again. PlayMode tests cover both.
- Validator rule: a Burn/Extinguish gate without a PersistentId is an error (`RoomInfo.GatesWithoutPersistentId`).
- Perf:
  - EventBus uses copy-on-write arrays; a test asserts no allocation per publish, and `SubscriberCount` was added.
  - SwipeDetector reuses one gesture.
  - The motor uses `usedByEffector` instead of GetComponent.
  - Fireball SyncTransforms runs once per step.
  - LightDropPickup caches the player on Enter.
- SRP:
  - `AuraGate` is removed and split into `OneTimeAuraGate` (base, persistence) with `BurnableGate` and `ExtinguishableGate`, plus `WindLiftZone`, `HeatVent` and a shared `GateSwitches` struct.
  - `PlayerCombat` is split into a partial class with `PlayerCombat.Reactions.cs` (155 + 59 lines).
  - Generators, prefabs, scenes and tests were updated.
- Tooling:
  - `unity-batch.sh` prints usage with no args and no `set -u` crash, stores the lock PID and reclaims dead locks, and refuses (exit 75) while `Temp/UnityLockfile` is held (all three verified by hand). `UNITY_TEST_FILTER` narrows a test run.
  - `.gitattributes` has `* text=auto` and the 8 LFS patterns.
  - The README documents the UnityYAMLMerge setup; the macOS path is `Contents/Helpers/UnityYAMLMerge`, which exists on this machine.
  - `BuildReleaseApk` refuses without a full custom keystore.

## Contract changes other phases must know
- `GameManager.StartNewGame/Continue` no longer set Mode=Playing; `WorldEntry` does after placing the player. `GameManager.UseSaveSystem` is a new test and slot seam.
- `PlayerCombat.RespawnHandler` now means "respawn started" (the handler publishes `PlayerRespawned` on arrival); `RequestRespawn()` returns bool.
- `CastResult.Blocked` is new (appended to the enum).
- `SaveSystem.HasSave` now parses the file.
- `RegionLoadPlan.Resolve` has a 6-argument overload; the old one still works.
- The standalone test scenes (Test_Movement, Test_Aura) now contain a `TestCheckpoint` (CheckpointService plus an altar at spawn), because respawn no longer revives in place.

## Follow-ups and limits
1. "Trigger re-fire on collider resize" (reviewer, unresolved) was not addressed.
2. Not verified on a device: `VibrationEffect`, and whether `File.Replace` works on Android IL2CPP. The copy fallback exists and is unit-tested.
3. The Windows UnityYAMLMerge path in the README is the standard one, unverified.
4. The region scenes (Region_*.unity) were modified by the generator (greybox start room and altar). Whoever owns them must keep a SunAltar with the listed altar id.
5. `WorldEntry` does not unload already-loaded regions on a second new game. Gates and shortcuts re-read the state on `GameStateLoaded` but can only open, never close. The menu flow should return to a freshly loaded Core.
6. A future pause menu must set `GameMode.Paused` and resume to `HitStop.GameplayScale`, not to a scale it read while frozen.
7. `PlayerController.cs` is 201 lines; `PlayerCombatSimulationTests` and `AuraSkillSimulationTests` were already over 200.
8. Respawn retries forever (1 s apart, warning every 5th try) when even the hub altar cannot load. That is by design (stay Dead), but a UI "cannot load world" fallback is a follow-up.

# Round 2 (second-pass review)

Verification: compile 0 errors / 0 warnings, EditMode 421/421 (was 413), PlayMode 20/20 (was 14), all generators and RoomIdValidator rerun (0 errors), dev APK Succeeded. Not committed.

| # | Item | Status |
|---|---|---|
| 1 | C-1 respawn handle | Fixed as requested, but the bug did not reproduce. I wrote the tests first (`RepeatedRespawnPlayModeTests`: die, respawn, die, respawn in the already-loaded hub with Leo Idle at the altar; and the local-altar path with no WorldEntry). Both PASSED on the old code. Unity's `StartCoroutine` does not complete this routine synchronously: the nested `yield return FindDestination(...)` always yields at least once, so the handle was already cleared by the time it was assigned. So that coroutine-semantics worry is not a live bug. I still replaced the handle with `bool respawning`, set before `StartCoroutine`, cleared in a `finally` inside the routine and in `OnDisable`, because the old pattern depended on that undocumented behaviour. The two tests stay as regression guards. |
| 2 | W-A stateSaved | Fixed. `GameManager.stateSaved` is false after StartNewGame, true after Continue and after a successful Save; the same-altar skip uses it. PlayMode test: a new game's first altar overwrites an old save (0 coins on disk). The old `HasSave` check is gone from that path. |
| 3 | W-F entry | Fixed. New `GameMode.Loading`, set in `Begin()` before the coroutine (and reset to Menu on failure). `PlayerCombat.ResetToFreshStart()` (RestoreAll, clear awaiting-respawn, no invulnerability, zero velocity, Idle) is called by `WorldEntry.Enter` for an existing player and by `OnRespawned`. PlayMode tests: Loading mode during entry; a dead player re-entering starts Idle and alive. |
| 4 | W-B version mismatch | Fixed. A wrong-version main file returns false with a log; the backup is used only for missing, empty or unparseable main. EditMode test. |
| 5 | W-C file storage | Fixed. First write uses `File.Move` (copy only if that throws). `.bak` is refreshed only when the current main still parses, in both the `File.Replace` path (passes a null backup name otherwise) and the copy fallback. Tests in both modes: a damaged main does not overwrite a good backup; first write leaves only the save file. Existing file-storage tests now use valid JSON. |
| 6 | W-D current-room respawn | Fixed. `RoomManager.EnterRoom(..., restartIfCurrent)`; `AltarArrival.Place` passes true, so entering the room Leo is already in calls the new `Room.Restart()` (enemies off then on, so OnEnable resets run) and publishes `RoomEntered` again. PlayMode test. Real enemy reset logic is for the enemies phase to hang on OnEnable. |
| 7 | W-E EventBus doc | Fixed (XML summary only, no behaviour change): a handler removed mid-publish is still called once for that event. |
| 8a | PlayerController ≤ 200 | Fixed (198 lines). |
| 8b | DeadState holds still once the respawn starts | Fixed: velocity zeroed and no gravity after `_requested`. EditMode test (height constant while the arrival is pending). |
| 8c | Hitbox and Hurtbox of different teams on one object | Fixed: `RoomInfo.MixedTeamCombatObjects` plus a `RoomValidator` rule, read from room prefabs by `RoomIdValidator`. Pure test. It only covers room prefabs, not scene objects or other prefab folders. |
| 8d | ControlsEnabled gates horizontal movement | Fixed: `MoveDirection` is 0 and swim steering is zero while controls are off. EditMode tests for run and swim. |
| 8e | Save after fallback-to-hub | Fixed in both `CheckpointService` (respawn fallback) and `WorldEntry` (entry fallback). The PlayMode fallback test passes; I did not add a separate assertion that the file on disk holds the hub id. |
