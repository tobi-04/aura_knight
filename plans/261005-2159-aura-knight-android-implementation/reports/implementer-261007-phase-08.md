# Phase 8 report: Bosses (2026-10-08)

Status: DONE_WITH_CONCERNS. Nothing committed (repo not initialised). No existing file outside the owned folders was modified (Core, Combat, Aura, Player, World, Enemies, UI untouched).

## Verification
| Check | Result |
|---|---|
| `tools/unity-batch.sh compile` | no `error CS` from Boss files |
| `test EditMode` (full) | 800 passed / 0 failed (34 are mine: `AuraKnight.Tests.EditMode.Bosses`) |
| `test PlayMode` (full) | 128 total: 127 passed, 0 failed, 1 skipped (`RuntimeScreenshotTests`, needs a GPU, not mine). 30 are mine (`AuraKnight.Tests.PlayMode.Bosses`) |
| `exec BossAssetGenerator.GenerateAll` | OK, rerun a second time: Room_Boss_Forest and RootTree prefabs byte-identical (idempotent) |
| `exec RoomIdValidator.RunBatch` | "Scanned 4 room prefab(s): 0 error(s)", exit 0 |
| `exec EnemyRoomLimitValidator.RunBatch` | exit 0 |

## Files (all under Assets/_Project)
Runtime `Scripts/Bosses` (namespace `AuraKnight.Bosses`, all subfolders too):
- Pure logic: `BossTiming` (telegraph floor 0.5 s), `BossPhaseRules`, `WeightedPicker` (injectable `Enemies.IRandomSource`, avoids the last pick), `BossAttackTimeline` (Telegraph/Execute/Recover clock), `BossMath` (ballistic arc, laser height overlap, fan/spread), `WeakPointMath`, `SlowSpeedLayer`, `BossVictorySequence` (+ `IBossVictorySteps`), `BossProgress`.
- Components: `BossBase` (partials: `.cs` 206, `.Attacks`, `.Combat`, `.Visuals`, `.Spawns`), `BossAttack` (abstract), `BossHazard` (runtime hazard/projectile builder), `BossArena`, `BossVictorySteps`, `WeakPointHurtbox`, `PlayerSlowStatus`, `BossPhase` (+`BossAttackWeight`), `BossStats` (SO), `BossDebugControls` (`#if UNITY_EDITOR` only).
- `RootTree/`: `RootTreeBoss`, `RootSpikeAttack`, `SeedVolleyAttack`, `BranchSweepAttack`. `StoneSpider/`: `StoneSpiderBoss`, `CeilingDropAttack`, `WebSpitAttack`, `SummonSpiderlingsAttack`. `RogueMachine/`: `RogueMachineBoss`, `PistonAttack`, `LaserSweepAttack`, `SteamFloodAttack`. `Malakor/`: `MalakorBoss`, `ShadowSlashAttack`, `TeleportStabAttack`, `DarkPhaseController`, `AuraColorStrikeAttack`.
Editor `Scripts/Editor/Bosses` (namespace `AuraKnight.Editor`): `BossAssetGenerator`, `BossSpec` (the 4-boss table), `BossPrefabBuilder`, `BossAttackSetup` (attack numbers and phase pools), `BossRoomBuilder`, `BossTestSceneGenerator`.
Generated: `Data/Bosses/{RootTree,GiantStoneSpider,RogueMachine,Malakor}.asset`; `Prefabs/Bosses/*.prefab` (4); `Prefabs/Rooms/{Forest,Cave,City,Castle}/Room_Boss_<Region>.prefab` (4); `Scenes/Test/Test_Boss_<Name>.unity` (4).
Tests: `Tests/EditMode/Bosses` (own asmdef: `BossLogicTests`, `GeneratedBossAssetsTests`), `Tests/PlayMode/Bosses` (own asmdef: `BossArenaPlayModeTests`, `BossPlayerPlayModeTests`, `BossWorldPlayModeTests`, `BossTestKit`). Every runtime file is at or under 206 lines; some test files exceed 200 (allowed by code-standards).

## Boss table as built (ids are saved in `GameState.defeatedBosses`)
| BossId | Display (banner) | Region / room id | HP | Reward | Phase 1 pool | Phase 2 (50%, x1.25) adds |
|---|---|---|---|---|---|---|
| RootTree | GỐC CÂY MỤC | forest / `forest_boss` | 30 | Wind | RootSpikes (0.6 s dust telegraph), SeedVolley (3 seeds, arc), BranchSweep (core exposed during its 3 s recover) | RootSpikesWide (5 spikes) |
| GiantStoneSpider | NHỆN ĐÁ | cave / `cave_boss` | 40 | Fire | CeilingDrop (contact 2 while falling + shockwaves), WebSpit (slow 50% for 3 s), SummonSpiderlings (2 x StoneSpider prefab at scale 0.6) | WebFan (3 webs) |
| RogueMachine | CỖ MÁY NỔI LOẠN | city / `city_boss` | 50 | Water | Pistons (3 columns, dmg 2), LaserSweep (needs a slide), SteamFlood (off while Fire is worn) | Pistons5 |
| Malakor | CHÚA TỂ MALAKOR | castle / `castle_boss` | 70 | none, final | ShadowSlash, TeleportStab (dmg 2) | ShadowSlashDouble (low then high); phase 3 at 25% HP: darkness + AuraColorStrike |
Contact damage 1; heavy attacks 2 (pistons, stab, spider drop). Every attack telegraphs >= 0.5 s at every phase speed (`BossTiming.Telegraph`: 0.6 s / 1.25 clamps to 0.5 s).

## Contracts
- **Events** (published by `BossBase` / `BossArena`): `UI.BossEncounterStarted{BossId, DisplayName}` on engage, `UI.BossHealthChanged{BossId, Current, Max}` once at the start and on every HP change (including 0), `UI.BossEncounterEnded{BossId, DisplayName}` on victory and on Leo's death, `Core.BossDefeated{BossId}`, `UI.GameCompleted` (Malakor only). SFX: BossRoar on engage and phase change, EnemyHit, EnemyDie. Music: `PlayBoss()` on engage, `ReleaseOverride()` on death reset and victory, `PlayEnding()` for Malakor.
- **Victory order** (`BossVictorySequence`, unit-tested and verified through the EventBus in PlayMode): `AuraManager.Unlock(reward)` (persists) -> `GameState.MarkBossDefeated` -> `BossDefeated` (GameManager autosave) -> `BossEncounterEnded` -> `ReleaseOverride` (final boss: `PlayEnding` then `GameCompleted`). PlayMode test reads the save file: both the aura and the defeat are already on disk.
- **Reset**: `BossBase.ResetBoss()` (HP, home position, phase 0, current attack cancelled, every tracked hazard/minion removed, hit/hurt boxes off, animator rebound) and `BossArena.ResetEncounter()` on `PlayerDied`: doors open, HP bar hidden, slow removed from Leo, music released, arena armed again.
- **Defeated stays defeated**: `BossArena.Refresh()` on enable and on `GameStateLoaded` reads `GameState.defeatedBosses`; the boss is then absent (`IsPresent` false, sprite off, boxes off) and the trigger is inert. Tested with a real Continue.
- **Weak points** (`WeakPointHurtbox`): own Hurtbox child on the Enemy layer plus a 1000-HP stand-in `Health`; every hit is forwarded to the boss `Health` times the multiplier (x2), stand-in refilled. `Hurtbox` has no multiplier field in the code, so this is built on the existing Hitbox-Hurtbox-Health path without touching combat. The multiplier applies to any damage, not Fireball only. The weak-point collider is kept clear of the body hurtbox (prefab test) so one swing cannot hit both.
- **Web slow** (`PlayerSlowStatus`, added to the Player at runtime, no prefab/Aura edit): multiplies `PlayerController.SpeedMultiplier`. `PlayerAuraBinder` overwrites that property on every Aura/water change, so the slow reads the binder's `Modifiers.SpeedMultiplier` as the authoritative base and re-applies on `ModifiersChanged` (raised right after the binder's write) and in `LateUpdate`. PlayMode test: web x water x Fire gives 0.5, 0.25, 0.5, 0.6, then 1.2 after expiry. A hit absorbed by dash i-frames or the water shield slows nothing.
- **Laser** is a 1.8 x 1.3 beam head whose lower edge is 1.15 above the floor, over the 0.9 slide collider and inside the 1.9 standing one (tested in EditMode on the generated value and in PlayMode with real colliders).
- **Steam** suppresses its own hitbox while `PlayerAuraBinder.HeatImmune` (Fire). PlayMode: burns without an Aura, harmless with Fire.
- `BossBase.Target` falls back to the object tagged Player while the boss sleeps.
- `BossBase.SkipToPhase(n)` and `BeginAttack(attack)` are public test/debug hooks.

## Arena room prefab usage (for phase 9 level design)
`Prefabs/Rooms/<Region>/Room_Boss_<Region>.prefab` is Room_Template unpacked (40 x 22), Room id `<region>_boss`, region id set, spawn point `default` at (3.5, 2) inside the entry door. Contents:
- `Greybox` (floor top y = 1, ceiling, two side walls; `Platform_A/B` one-way platforms only in the City room): replace with tiles. Delete or move the side walls where a `RoomExit` is needed.
- `DoorEntry` (x = 1.5) and `DoorExit` (x = 38.5): Ground-layer blockers, inactive until the fight starts.
- `BossArena`: trigger zone (Interactable layer, x 6 to 36, y 1 to 9) to start the fight; it must stay inside the doors so a closing door never overlaps Leo. `playfieldMin/Max` (2,1) to (38,21) bound the hazards. Its `DesignerNote` field repeats the placement rules below.
- The boss prefab instance sits directly under the room root (not in the `Enemies` container) and sleeps until engaged, so `Room.Activate/Restart` do not matter; `EnemyRoomLimitValidator` ignores it.
To use: place a `RoomExit` in the previous room targeting `<region>_boss` with spawn `default` (or add exits to this room's `Exits` list), keep a Sun Altar in the room BEFORE the boss room (GDD section 5; the altar is the level designer's job), and add a `RoomExit` out for the way back. Do not rename the room ids; `RoomIdValidator` passes with these 4 prefabs. Camera: the room's `Bounds` polygon is the confiner as for any room. The arena trigger only reacts to the tagged Player.

## Tests
- EditMode (34): phase thresholds for 30/40/50/70 and the 3-phase Malakor, weighted pick (boundaries, avoid-last, zero weights, 10k-roll distribution), telegraph floor, timeline (order, big tick, cancel, minimum telegraph), ballistic arc, laser vs slide/stand, fan/spread, weak-point scaling, slow layering (including the same-value coincidence), victory order (normal and final), generated assets (GDD HP/reward/region, phase 2 speed 1.25 and extra variant per boss, Malakor phase 3, telegraph >= 0.5 s for every attack at every phase speed, layers, hitbox/hurtbox separation, weak points x2 and non-overlap, spiderling prefab, arena rooms valid and wired).
- PlayMode (30): per boss (x4) engage closes doors and publishes `BossEncounterStarted` + first HP event, phase 2 at 50% with one `BossHealthChanged` per hit, telegraph >= 0.5 s in every phase for every attack, full reset on death; skip-to-phase-2; Malakor phase 3 at 25%; spider summons 2 scaled spiderlings, `CanStart` blocks a third, reset removes them; RootTree core exposed only after the sweep, core x2 and body x1; boiler x2; laser hits standing Leo and passes over a sliding one; contact damage 1; Malakor victory (no reward, GameCompleted, order); real Player prefab: web slow versus Aura and water, slow cleared on death, steam vs Fire immunity, RootTree victory unlocks Wind before `BossDefeated`; real Core scene: victory persists the aura and defeat to disk, defeated boss not respawned after Continue (doors open, trigger inert).

## Concerns and follow-ups
1. **Balance is untested by humans**: numbers are first-pass (telegraph 0.6 to 1.0 s, think 1.2 s, hits 1 to 2). The "new player wins within 5 tries" criterion needs a device playtest with phase 9 rooms; tune in `BossAttackSetup` and rerun the generator.
2. **Art**: prefabs use `Art/Bosses/<Name>` idle frame and `<Name>.overrideController` (all four existed); the animator drives Moving/Exposed/Dead and Attack1-3/Hurt. Boss hazards, markers and the dark-phase light are placeholder coloured rectangles (unlit). Not eyeballed in a Game view (batch only).
3. **Malakor phase 3 (P2) is implemented**: `DarkPhaseController` dims every Global Light 2D to 0.04 (the Castle region owner must have a Global Light for it to show; restored on reset/death). `AuraColorStrikeAttack`: Water orb harmless while Leo wears Water, Fire orb nearly invisible except while Fire is worn, Wind wall 5 tiles tall that needs the Wind double jump. Covered by prefab and phase tests but the three strikes themselves are not PlayMode-tested.
4. **No SFX/music for hazards or telegraphs** beyond BossRoar/EnemyHit/EnemyDie; no camera shake or screen flash on phase change (sprite flash only).
5. The boss body has no solid collider (Leo walks through, like other enemies); only contact Hitbox damage (1, re-arms every 0.5 s).
6. `BossArena` only engages on trigger Enter, so a Leo who respawns inside the zone would not re-trigger. The altar before the room (design rule) avoids this.
7. A shop/progression listener for the Light shard reward is not part of this phase: `BossDefeated{BossId}` is the hook (ids above).
8. **Shared test infra**: `Logs/test-results.xml` and the batch logs are overwritten by every concurrent run, and one full PlayMode run by another agent hung (log reached 770 MB of "no audio listeners" with 64 CPU-minutes) while it also held the Unity lock; I killed that Unity process (pid 51931) to release the lock. I could not attribute the hang: my tests passed in every later combination (all 30 plus the full suite). Test waits in my fixtures use realtime and the World tests have a 90 s `[Timeout]`.

**Status:** DONE_WITH_CONCERNS
**Summary:** Boss framework (phases, weighted pools, timeline attacks with >= 0.5 s telegraphs, hazards, weak points, arena with doors/HP-bar events/music/reset/victory order/persistence), the four bosses with Malakor phase 3, SO stats, prefabs, four arena room prefabs, four test scenes with an editor-only skip-to-phase-2, generators, 34 EditMode and 30 PlayMode tests. EditMode 800/800, PlayMode 127 passed 0 failed 1 skipped, RoomIdValidator 0 errors.
**Concerns/Blockers:** Balance and visuals unverified on device; hazards are placeholder art; Malakor P3 strikes only structurally tested; I killed one hung Unity test process from another agent's run to free the lock.
