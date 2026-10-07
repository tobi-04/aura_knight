# Phase 7 report: Enemy AI archetypes (2026-10-07)

Status: DONE_WITH_CONCERNS. Nothing committed (repo not initialised). No existing Core/World/Player/Combat/Aura file was modified.

## Verification
| Check | Result |
|---|---|
| `tools/unity-batch.sh compile` | no `error CS`, no warnings from my files |
| `test EditMode` (full) | 590 passed / 0 failed (77 are mine: `AuraKnight.Tests.Enemies`) |
| `test PlayMode` (full) | 51 passed / 0 failed (17 are mine: 14 arena tests + 3 Core-flow tests) |
| `exec EnemyAssetGenerator.GenerateAll` | OK (8 variants, pickups, controller, Test_Enemies.unity), rerun twice |
| `exec RoomIdValidator.RunBatch` | exit 0, 0 errors |
| `exec EnemyRoomLimitValidator.RunBatch` | exit 0 |

## Files (all under Assets/_Project)
Runtime, `Scripts/Enemies` (namespace `AuraKnight.Enemies`, modifiers in `.Modifiers`):
- Pure logic: `EnemyState`, `EnemyArchetype`, `EnemyStateMachine`, `PatrolRules`, `EnemyDetection`, `HopArc`, `DiveRules`, `ZapCycle`, `WaypointPath`, `DropTable`, `DropResult`, `IRandomSource`, `UnityRandomSource`, `EnemyLimits`, `Modifiers/ShieldRule`
- Components: `EnemyBase` (partial: `.cs` 184 lines, `.Reactions.cs`, `.Sensing.cs`), `WalkerEnemy`, `HopperEnemy`, `FlyerEnemy`, `CrawlerEnemy` (+ static zapper mode), `EnemyAnimatorBridge`, `EnemyDrops`, `Modifiers/{FrontShield,PhaseThroughWalls,LifeSteal}`
- Data: `EnemyStats` (ScriptableObject), `CoinsCollected` (event struct)
- `Scripts/World/Pickups` (namespace `AuraKnight.World.Pickups`): `CoinPickup`, `CoinPickupPool`, `CoinMagnet` (pure), `CoinCollector`
Editor, `Scripts/Editor/Enemies` (namespace `AuraKnight.Editor`): `EnemyAssetGenerator`, `EnemyVariantSpec` (the 8-variant table), `EnemyPrefabBuilder`, `EnemyPickupPrefabBuilder`, `EnemyAnimatorControllerBuilder`, `EnemyTestSceneGenerator`, `EnemyRoomLimitValidator`
Generated: `Data/Enemies/{BugThorn,PatrolBot,NightKnight,PoisonShroom,Bat,Ghost,StoneSpider,ScrapZapper}.asset`, `EnemyAnimator.controller` (+ `Animation/*.anim` placeholders), `EnemyBody.physicsMaterial2D`; `Prefabs/Enemies/*.prefab` (8); `Prefabs/Pickups/{CoinPickup,LightDrop}.prefab`; `Scenes/Test/Test_Enemies.unity`
Tests: `Tests/EditMode/Enemies` (own asmdef `AuraKnight.Tests.EditMode.Enemies`: EnemyLogicTests, CoinTests, GeneratedEnemyAssetsTests), `Tests/PlayMode/Enemies` (own asmdef `AuraKnight.Tests.PlayMode.Enemies`: EnemyBehaviourPlayModeTests, EnemyWorldPlayModeTests, helpers)

## How to place an enemy in a room (for phase 9 / bosses)
1. Drop `Prefabs/Enemies/<Variant>.prefab` under the room prefab's **Enemies** container (the child `Room.enemiesContainer` points at). Position it where it should start: the spawn point is the local position at first enable, relative to the container's parent, so it moves with the room.
2. Walker patrol segment: `WalkerEnemy.patrolLeft/patrolRight` (tiles either side of spawn, default 3). Crawler path: children of the prefab's `Path` object (first point at the root, last = other end; `loopPath` to loop instead of ping-pong). Static zapper needs nothing.
3. From code: `Instantiate(prefab, container)` then `enemy.SetSpawnPoint(worldPos)`.
4. Budget: at most 6 enemies per room (`EnemyLimits.MaxActivePerRoom`). `Aura/Enemies/Validate Room Limits` / `EnemyRoomLimitValidator.RunBatch` scans room prefabs and fails on more than 6 (counts all `EnemyBase` in the prefab, including inactive).

## Contracts
- Layers: root and Hurtbox child on `Enemy`, contact Hitbox child (and `ZapHitbox`) on `EnemyAttack`. A Hitbox and a Hurtbox never share an object (asserted over all prefabs). Root solid collider (walkers, hopper, flyers) collides with `Ground` only; Leo does not collide with enemies, only contact Hitbox damage (1 heart, re-arms every 0.5 s; Leo's i-frames absorb the rest).
- Reset contract: `EnemyBase.OnEnable` (and public `ResetEnemy()`) fully resets: HP to `EnemyStats.maxHp`, position to spawn, velocity, state Patrol, facing, hurtbox/contact hitbox re-enabled (off/on clears the hit set), body collider, sprite, subclass timers (`OnReset`). `Room.Restart()` or re-entering a room therefore revives everything. **A dead enemy stays active but inert** (hurtbox, hitbox, body and physics off, sprite fades out in 0.3 s); it does not `SetActive(false)` itself because that would stop the container toggle from reviving it.
- Execution order: `EnemyBase` is `[DefaultExecutionOrder(-10)]` so modifiers' `OnEnable` runs after the base reset. Subclasses must not declare `Awake/OnEnable/OnDisable/Update/FixedUpdate` (Unity would skip the base private versions); use the `On*` hooks.
- Coin event: `AuraKnight.Enemies.CoinsCollected { int Amount }` is published by `CoinCollector.Collect(amount)` for every pickup. With a `GameManager` it also adds to `State.coins` (saturating) and publishes the existing `Core.CoinsChanged { Coins }`; without one (test scenes) only `CoinsCollected` fires. Phase 12's Wallet replaces the body of `CoinCollector.Collect` only. `CoinPickup` is pulled to Leo within 2 tiles (`CoinMagnet.Radius`), pooled (`CoinPickupPool`, one coin prefab game-wide), self-despawns after 25 s.
- Drops: `EnemyStats.DropTable` (`coinsMin..coinsMax` inclusive + `LightDropPickup.DropChance` 10%), RNG injectable through `EnemyBase.Random` (`IRandomSource`). Coins pop out as individual 1-value pickups; Light Drop is instantiated in place.
- Hit reaction: `Health.Damaged` -> Hurt state for `Knockback.Duration` (0.25 s), pushed `info.KnockbackTiles * stats.knockbackScale` away (sign from `DamageInfo.Direction`). Crawlers/zapper have `knockbackScale 0` and are not stunned (flash only).
- Animator hook: `EnemyAnimatorBridge` drives `Moving` (bool), `Attack`/`Hurt` (triggers), `Dead` (bool) on the Visual child's Animator using `Data/Enemies/EnemyAnimator.controller` (states Idle, Move, Attack, Hurt, Death with empty placeholder clips). The prefab generator uses `Art/Enemies/<Variant>/<Variant>.overrideController` if it exists when the generator runs (none existed now, so the hook is unused). Art needs to override the five placeholder clips of the base controller.
- Visuals: tinted white-square placeholders from the player placeholder sprite, default sprite material (no `Art/Materials` existed).

## Variant numbers (GDD 7.3, asserted in `GeneratedEnemyAssetsTests`)
| Variant | Class | HP | Dmg | Coins |
|---|---|---|---|---|
| BugThorn | Walker | 2 | 1 | 3-5 |
| PatrolBot | Walker | 4 | 1 | 3-5 |
| NightKnight (FrontShield, turn delay 0.7 s, knockback x0.5) | Walker | 6 | 1 | 3-5 |
| PoisonShroom | Hopper (1.5 s) | 2 | 1 | 3 |
| Bat (LifeSteal 1) | Flyer | 2 | 1 | 4-6 |
| Ghost (PhaseThroughWalls) | Flyer | 4 | 1 | 4-6 |
| StoneSpider | Crawler | 3 | 1 | 4 |
| ScrapZapper | Static (2-tile ring every 3 s, 0.6 s telegraph) | 4 | 1 | 4 |
The task text said "coins 3-6" generically; I used the GDD per-variant ranges.

## Test coverage
- EditMode (77): state machine legality/timers/events, patrol turns (ledge, wall, bounds, charge block), detection range and vertical tolerance, drop table with fake RNG (bounds, 10% edge, inverted range, 100-roll statistics), hop arc (landing distance, apex, clamp), dive aim/end rules, zap cadence (10 fires in 30 s), waypoint path (legs, ping-pong, loop, no overshoot, degenerate paths), shield direction rule, room cap, coin saturation/magnet/pool/drops, generated asset wiring (numbers, layers, hitbox/hurtbox separation, modifiers, gravity, zapper ring, spider path, controller states).
- PlayMode (17): walker turns at a ledge (stays on a 6-tile platform for 7 s) and at a wall; walker charges Leo; a player-team hit (the same `Hitbox` mechanism as the sword) kills BugThorn in two hits, 5 coins and a Light Drop spawn, a stand-in Leo collects exactly those 5 through `CoinsCollected`; front shield blocks front hits, takes back/top hits; hopper arcs and lands; bat dives and returns; ghost crosses a wall that stops the bat; bat life steal; zapper telegraphs then zaps at about 2.8 s and every 3 s; spider follows its waypoints and is not stunned; walker knockback; falling enemy is sent home; reset after death; **inside the real Core scene**: `Room.Restart()` revives a dead enemy and heals a damaged one back to spawn, room deactivate/activate, `CoinCollector` credits `GameManager.State.coins` and publishes `CoinsChanged`.

## Concerns and follow-ups
1. **Kills are not persistent until an altar.** Any container toggle (re-entering a room, not only `Room.Restart`) revives all enemies. GDD says they respawn when Leo rests or dies. A per-room "killed since last altar" ledger (keyed by `PersistentId`, cleared on `CheckpointReached`/`PlayerRespawned`) belongs to the phase 9 room/level work; `EnemyBase` already reports deaths via `Died`.
2. Sword test uses a Team.Player `Hitbox` placed by the test, not the real Player prefab driving `PlayerCombat` (needs input simulation). Hit direction contract used by the shield: `DamageInfo.Direction` points attacker to victim, sword passes `(Facing, 0)`; verified by unit test, not through the Player prefab.
3. "Slide through" for the Night Knight works through its `turnDelaySeconds` (0.7 s) once Leo is behind it; there is no collision with enemies so Leo slides through freely. Tune on device.
4. Crawler waypoint offsets are read once per enemy lifetime from the `Path` children (they are children of the enemy, so editing them at runtime does nothing). Surfaces are followed purely by waypoints; there is no wall/ceiling detection and the sprite is not rotated to the surface (art/rotation later).
5. Enemy body uses a dynamic Rigidbody2D (gravity 3) with a zero-friction material; no slope/one-way-platform handling. Hopper and walker knockback can push them off ledges (intended).
6. The 5-minute stuck/fall soak is covered only by the 30-tile fall reset and short PlayMode runs, not a literal 5-minute run.
7. `Aura/Enemies/...` menu items and `Test_Enemies.unity` are not added to the build settings (same as the other Test_* scenes).
8. `Logs/test-results.xml` and `Logs/batch-exec-GenerateAll.log` are shared by every concurrent agent; I copied results right after each run. The three PlayMode failures I hit once in the full run were caused by earlier world fixtures leaving Core/Region scenes loaded near the origin; the arena fixture now unloads them first.

**Status:** DONE_WITH_CONCERNS
**Summary:** Four archetype classes, EnemyBase with the full state loop, drops and reset-on-enable, three modifiers, 8 variant stats/prefabs from the GDD table, coin pickup with pool and `CoinCollector`/`CoinsCollected`, generators, Test_Enemies scene, room-limit validator. Compile clean, EditMode 590/590, PlayMode 51/51, generators and validators rerun.
**Concerns/Blockers:** Kills are not persistent until an altar rest (phase 9 ledger needed); real Player-prefab sword path and a literal 5-minute soak not covered; animation clips await the Art pass.
