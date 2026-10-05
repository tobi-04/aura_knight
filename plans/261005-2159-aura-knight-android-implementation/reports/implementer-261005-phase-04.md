# Phase 4 report: Combat and Health

Status: DONE_WITH_CONCERNS. Compile clean (no error CS). EditMode 256/256 pass (152 existing + 104 new). Generator `AuraKnight.Editor.MovementTestSceneGenerator.Generate` rerun OK (Player.prefab and Test_Movement.unity regenerated with combat). Not committed (repo not initialised).

## Files (all under Assets/_Project)
Created
- Scripts/Combat (namespace AuraKnight.Combat): Team, DamageInfo, HitOutcome, DamageRules, Health, Hurtbox, Hitbox (+HitReport), HitRegistry, Invulnerability, Knockback, HitStop, HitStopTimer, CombatFeedback, SpriteBlink (+BlinkPattern), DummyTarget, ComboTracker, SwordTiming, SwordShape, AttackDirection (+AttackAim), EnergyPool
- Scripts/Player: PlayerStats, PlayerStatsSeed, PlayerCombat (204 lines), PlayerController.Api.cs (partial split of the old controller), States/{AttackState (also holds AttackExit helper), AirAttackState, HurtState, DeadState}
- Scripts/World/Pickups/LightDropPickup.cs, Scripts/Core/Haptics.cs
- Scripts/Editor/Player/PlayerCombatPrefabBuilder.cs, CombatTestObjects.cs
- Tests/EditMode/Combat (own asmdef `AuraKnight.Tests.EditMode.Combat`, references the Player tests asmdef to reuse PlayerSimHarness/FakeInput): HealthTests, CombatBasicsTests, SwordLogicTests, PlayerStatsLogicTests, HapticsTests, HitboxTests, LightDropPickupTests, CombatSimHarness, PlayerCombatSimulationTests, GeneratedCombatAssetsTests

Modified
- Scripts/Player/PlayerController.cs (228 -> 173 lines, now `partial`): `event Action<float> Stepped` (fires each physics step after timers tick, before the state runs); SyncSwimState no longer pulls Leo out of Hurt/Dead.
- PlayerMovementConfig.cs + Data/Player/PlayerMovementConfig.asset: `jumpCutMultiplier` 0.5 -> 0.25 (tap jump now 2.0 +-0.25 tiles; measured in sim). Tests tightened (PlayerControllerSimulationTests, AirPhysicsSimulationTests, PlayerMovementConfigTests; JumpCut unit tests now use the config value instead of a hard-coded 0.5).
- Scripts/Player/AssemblyInfo.cs (InternalsVisibleTo for the Combat tests asmdef), Scripts/Editor/AuraKnight.Editor.asmdef (+Unity.Cinemachine), PlayerAssetGenerator.cs (calls the builder), PlayerGeneratorUtil.cs (+SetInt/SetFloat/SetBool), MovementTestSceneGenerator.cs (adds combat targets + comment fix).
- No file under Core/* or World/* existing was touched (Haptics and LightDropPickup are new).

## Scene / prefab
Player.prefab now has Health (5 hearts, 1.0 s i-frames), Hurtbox (Team.Player), PlayerStats, PlayerCombat, CombatFeedback + CinemachineImpulseSource, SpriteBlink, and child `SwordHitbox` (trigger box, Team.Player). Test_Movement has, west of spawn: ground dummy (-6), floating dummy (-4, y 4.5), spike strip (-9, hazard hitbox 1 heart, pogo-able). Dummies are triggers (Leo walks through), heal after 3 s, log damage.

## Public API for phases 5 / 7 / 8 (all in AuraKnight.Combat unless noted)
- `Team { Player, Enemy, Hazard }`; same team never damages (`DamageRules.CanDamage`).
- `DamageInfo(int amount, Team team, GameObject source = null, Vector2 direction = default, float knockbackTiles = 3)`. Boss: pass amount 2.
- `Health` (MonoBehaviour): `Initialize(max, current=-1)`, `Current/Max/IsDead`, `TakeDamage(in DamageInfo) -> HitOutcome {Ignored, Absorbed, Touched, Damaged, Killed}`, `Heal(int)`, `Refill()`, events `Damaged(info, applied)`, `Died(info)`, `Changed(cur, max)`; `Invulnerability`, `InvulnerableAfterHit` (inspector, enemies default 0), `InvulnerabilityGate`, `SelfTicking` (true: ticks itself on frame time; player turns it off and ticks in the fixed step).
- `Hurtbox`: same GameObject as the collider (trigger or solid); `Team`, `AllowsPogo`, `Health` (auto-found in parents incl. inactive), `Receive(in DamageInfo)`. No Health = damage-free pogo surface (spikes).
- `Hitbox` (needs a Collider2D, usually trigger): `Team`, `Damage`, `Activate(int amount, Vector2 direction = default)` (re-arming clears the hit set; zero direction = away from hitbox), `Deactivate()`, `Poll()`, event `Hit(HitReport{Target, Outcome, Info})`, `AutoPoll` (default true: polls in FixedUpdate), `RearmInterval` and `activeOnEnable` for contact damage (enemy bodies / spikes). One hit per target per activation; overlap is polled, not trigger-callback driven.
- Enemy recipe: Hurtbox(Team.Enemy) + Health on the body; attack/contact Hitbox(Team.Enemy) child, `Activate` during active frames or `activeOnEnable` + `RearmInterval`. On `Health.Died` roll `AuraKnight.World.Pickups.LightDropPickup.RollDrop()` (10%) and spawn the pickup prefab.
- Skills (phase 5): spawn a Hitbox with Team.Player and `Activate(n)`; spend energy with `PlayerStats.TrySpendEnergy(float) -> bool` (false, nothing spent, if short; NaN/negative rejected). Also `AddEnergy`, `Energy.Current/Max`, `Heal`, `RestoreAll`, `SwordLevel`, `Initialize(PlayerStatsSeed)` / `RefreshFromGameState()` to re-seed after shop upgrades (`Energy.SetMax(200, true)` is already supported).
- EventBus (published by PlayerStats): `HeartsChanged`, `EnergyChanged` (also once on Initialize so HUD syncs), `PlayerDamaged(applied, hearts)`, `PlayerDied`; consumed: `CheckpointReached` (refills unless dead), `PlayerRespawned`.
- `PlayerController`: `Stepped` event, `ExternalInvulnerable` is driven by combat (hurt i-frames OR dead), `IsInvulnerable` also covers dash. New states registered by PlayerCombat: Attack, AirAttack, Hurt, Dead. Swim (phase 5) just needs `StateMachine.Register`.
- `Haptics.Enabled` (PlayerPrefs "settings.haptics", default on) for the Settings screen; `Haptics.Pulse()` is a throttled (0.1 s) `Handheld.Vibrate()` on Android devices only, no-op elsewhere. `HitStop.Request(0.05f)` is timeScale-safe (unscaled, extends not stacks, ignored while paused, only restores if timeScale is still 0). Camera shake uses `CinemachineImpulseSource.GenerateImpulseWithForce` (Unity.Cinemachine 6.6 verified); the camera needs a `CinemachineImpulseListener` to show it (test scene uses SimpleCameraFollow, so no visible shake there).

## Behaviour implemented
- Sword: press ATK -> Attack (grounded) / AirAttack (airborne) from Idle/Run/Jump/Fall; 0.25 s swing, hitbox armed 0.05-0.18 s in code (no animator needed), reach 1.5 tiles past the body edge, damage = swordLevel; 2-hit combo with 0.3 s window (a press in the last 0.12 s of a swing chains); stick up (>=0.6, dominant axis) = up-slash; stick down in the air = down-slash. Hit on enemy/hazard with down-slash sets vertical speed so the bounce is 3 tiles (v = sqrt(2 g 3), measured 3.0 +-0.3 in sim). Landed damage = +8 energy.
- Hurt: -N hearts, knockback 3 tiles over 0.25 s away from attacker, i-frames 1.0 s (ExternalInvulnerable + SpriteBlink), HitStop/haptic/impulse via CombatFeedback; interrupts a swing (hitbox disarmed, combo reset). Two hits within 1 s cost one heart (tested at 0.5 s, 0.94 s, 1.14 s).
- Dead: gravity only, after 1.2 s `CheckpointService.Respawn()`; on `PlayerRespawned` the motor is resynced to the transform, hearts/energy restored, state Idle. Coins/progress untouched.
- PlayerStats seeds from GameManager.State (maxHearts/maxEnergy/swordLevel, clamped to 9/200/3) else 5/100/1.

## Tests
256 pass / 0 fail. New: Health rules, i-frame timer, blink pattern, hit registry, team rule, knockback, hit-stop timer, combo timing, aim, sword shape/timing, energy pool, stats seed, haptics pref, Hitbox against real Physics2D (once per activation, same team ignored, Touched for hazards, rearm), LightDrop, generated-prefab wiring, and a 40-case player simulation (swing damage and reach, combo, chaining, up/down slash, pogo on enemy and spikes and none on non-pogo/allies, energy, knockback distance both sides, double-hit within 1 s, dash i-frames, hurt interrupt, death, 1.2 s respawn, respawn fallback, checkpoint refill, events on the bus). The one RED-first caveat: tests were written before the code, but I ran the Unity batch only once the full set compiled, so the "failing first" evidence is the initial compile/test run, not per-test.

## Concerns / follow-ups
1. **Motor teleport desync (phase 6 / RoomManager):** `WorldTags.Teleport` moves only the transform/rigidbody; `KinematicMotor2D` keeps its own position and would snap Leo back on the next move. I handle this for respawn (PlayerCombat calls `controller.Teleport(transform.position)` on `PlayerRespawned`), but `RoomManager.EnterRoom` (RoomManager.cs:84) has the same problem for normal room transitions unless it calls `PlayerController.Teleport`. Not mine to change; needs a decision in phase 6/9 (e.g. WorldTags.Teleport also teleports a `KinematicMotor2D` when present).
2. Respawn fallback: if `CheckpointService` is absent or has no altar (e.g. Test_Movement), Leo revives in place after 1.2 s with a warning and `PlayerRespawned` is still published, so the scene is not soft-locked.
3. `Handheld.Vibrate()` is a long (about 0.4 s) buzz on Android, not a "light" tap; a native amplitude-controlled vibration needs an Android plugin or `VibrationEffect` via AndroidJavaObject (later polish). Untested on a device.
4. Pogo does not refresh the air dash / double jump (not in the spec); easy to add in `PlayerCombat.OnSwordHit` if design wants Hollow-Knight style.
5. Fade-to-black during the 1.2 s death is for the UI phase (listen to `PlayerDied` / `PlayerRespawned`).
6. No Animator events: hitbox timing is in code (`SwordTiming`); art can later drive the same constants. Attack/Hurt animator bools are not yet in `PlayerAnimatorBridge` (not my file's scope this phase).
7. Sword hitbox collider and per-swing hit feel need on-device tuning (reach/thickness constants in `SwordShape`, `SwordTiming`); PlayMode behaviour (real trigger/Update loop, Cinemachine impulse, hit-stop) was not run in batch.
8. PlayerCombat.cs is 204 lines (target about 200). PlayerController.cs was split (173 lines) as requested.

**Status:** DONE_WITH_CONCERNS
**Summary:** Phase 4 combat is in: shared Health/Hitbox/Hurtbox/Team/DamageInfo, 2-hit sword with up/down slash and pogo, hurt/i-frames/knockback, death and checkpoint respawn, energy, Light Drop, hit stop/impulse/haptics, jump-tap tuning to 2.0 tiles. Compile clean, EditMode 256/256 pass, player prefab and Test_Movement regenerated.
**Concerns/Blockers:** RoomManager's teleport leaves the kinematic motor stale (affects room transitions, handled only for respawn); haptics and camera shake unverified on device; nothing committed.
