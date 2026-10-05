# Phase 3 report: Player Movement and Touch Input

Status: DONE_WITH_CONCERNS. Compile clean, EditMode 152/152 pass (includes the other agent's tests), generators run.

## Files (all under Assets/_Project)
- Scripts/Player: PlayerController, PlayerStateMachine, PlayerStateId, IPlayerState, IPlayerInput, PlayerInputReader, KinematicMotor2D, MotorContacts, PlayerMovementConfig, AirPhysics, JumpCut, HorizontalMotion, DashTracker, Countdown, PlayerAnimatorBridge, SimpleCameraFollow, MovementDebugGizmos, AssemblyInfo (InternalsVisibleTo for tests)
- Scripts/Player/States: Idle, Run, Jump, Fall, WallSlide, WallJump, Dash, Slide, PlayerStateBase, PlayerTransitions
- Scripts/UI/VirtualControls: DynamicJoystick, SwipeDetector, SwipeGesture, JoystickMath, SafeAreaFitter, SafeAreaMath, UiMetrics, VirtualControlPaths, VirtualControlsBuilder
- Scripts/Editor/Player: PlayerAssetGenerator, MovementTestSceneGenerator, PlayerGeneratorUtil
- Settings/Input/AuraKnight.inputactions (map "Gameplay"), Data/Player/PlayerMovementConfig.asset, Prefabs/Player/Player.prefab (+ Placeholders/LeoPlaceholder.png, WhiteSquare.png), Prefabs/UI/VirtualControls.prefab, Scenes/Test/Test_Movement.unity
- Tests/EditMode/Player: 11 test files + its own asmdef `AuraKnight.Tests.EditMode.Player` (needed to reference Unity.InputSystem; the shared tests asmdef is not ours)

Regenerate: `tools/unity-batch.sh exec AuraKnight.Editor.MovementTestSceneGenerator.Generate` (also regenerates assets; keeps an existing config asset so tuned values survive).

## Public API for later phases
PlayerController (namespace AuraKnight.Player):
- Hooks: `bool CanDoubleJump`, `bool CanGlide`, `float SpeedMultiplier` (default 1), `bool SwimMode`, `bool ExternalInvulnerable` (combat sets), `bool IsInvulnerable` (get; = external OR dash i-frames).
- State registration: `StateMachine.Register(PlayerStateId id, IPlayerState state)`; ids Attack, AirAttack, Hurt, Dead, Swim already exist in the enum. Switch with `StateMachine.TryChange(id)` (false if unregistered). `StateMachine.StateChanged(from,to)`. Setting `SwimMode` auto-enters Swim once registered and returns to Fall when cleared.
- For custom states: `Velocity`, `SetVelocity/SetVelocityX/SetVelocityY` (impulse), `ApplyGravity(maxFall)`, `RunHorizontal()`, `Face(dir)`, `Facing`, `Grounded`, `LastContacts`, `Dt`, `Land()`, `StartJump(v)`, `Teleport(pos)`, `Motor`, `Config`, `Input`.
- Buffers (0.12 s, latched in Update): `AttackBuffer` (combat consumes with `.Consume()`), `JumpBuffer`, `DashBuffer`, `SlideBuffer`; `Coyote`, `Dash` (DashTracker).
- IPlayerInput intents: Move, JumpHeld/Pressed/Released, DashPressed, AttackPressed, SkillPressed, SlideRequested, AuraWind/Fire/Water/Next/Prev Pressed, PausePressed, MapPressed. Pressed/Released are Update-only. Reader is on the Player prefab (`controller.Input`).
- Input actions: map "Gameplay"; actions Move, Jump, Attack, Dash, Skill, Slide, AuraWind, AuraFire, AuraWater, AuraNext, AuraPrev, Pause, Map. On-screen widgets feed a virtual Gamepad, so touch and gamepad share bindings.
- Layers: motor collides with everything non-trigger on `collisionMask` (default all). One-way platform = collider with PlatformEffector2D.

## Tests
152 pass / 0 fail. Player-related: config math, Countdown (coyote/buffer), DashTracker, jump simulation (held 4.5 +-0.1 at 50 and 60 Hz), JumpCut, HorizontalMotion, state machine, swipe/joystick/safe-area math, input asset + on-screen path resolution, prefab wiring, KinematicMotor2D (floor, wall, ceiling, corner correction, one-way, crouch/stand) and end-to-end PlayerController simulation against real Physics2D queries: dash 5 tiles in 10 steps, i-frames, cooldown, single air dash, coyote, buffer, double jump, glide, speed multiplier, 20x slide through 1-tile tunnel, slide persists under low ceiling, wall slide 3 u/s, wall-jump lock, wall-jump bot climbs a 20-tile shaft.

## Concerns / follow-ups
1. Tap-jump height is about 2.5 tiles, not 2: the GDD numbers (cut x0.5 after min hold 0.08 s) give ~2.5 mathematically. Test asserts 2.0..2.75. A cut multiplier near 0.25 gives ~2.0; tune on device (`jumpCutMultiplier` in the config asset) or confirm 2.5 is acceptable.
2. On-device feel tuning (step 7) and 60 fps / latency checks need a human with a phone. PlayMode behaviour was not run in batch; the on-screen controls (joystick, buttons, swipe, safe area, EventSystem wiring) are verified only structurally (prefab test, path-resolution test), not by touch.
3. GDD says LB/RT switch Aura; implemented as AuraPrev (LB, Q) / AuraNext (RT, E) plus d-pad Wind/Fire/Water. Aura buttons use dpad paths on the virtual gamepad.
4. Test scene and Player placeholder use Sprites-Default (unlit) material; no Light2D (editor asmdef lacks URP reference). Art phase should switch to lit materials.
5. UI built through a runtime `VirtualControlsBuilder` because the Editor asmdef has no UnityEngine.UI reference (asmdef not in my ownership). Labels use legacy Text placeholders; UI phase replaces with TMP and design tokens.
6. PlayerController.cs is 228 lines (target ~200); acceptable but can split buffers/API later.
7. Layers: Project has only the default layers. When Phase 1/9 adds Ground/Enemy layers, set `collisionMask` on the motor so enemies do not block the player.
8. Not committed (repo not initialised).
