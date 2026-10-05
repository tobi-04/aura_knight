---
phase: 3
title: "Player Movement and Touch Input"
status: completed
effort: "8d"
owner: "A"
weeks: "1-2"
notes: "Pending on-device feel tuning (PlayMode validation, 60 fps check)"
---

# Phase 3: Player Movement and Touch Input

## Context Links
- GDD §3 (điều khiển Android), §4 (bảng thông số di chuyển), §15.1 (test case movement)

## Overview
- Priority: P0 · Status: pending
- Character controller kinematic 2D cho Leo + state machine + nút ảo/vuốt + gamepad.

## Key Insights
- Dùng **Rigidbody2D Kinematic + cast thủ công** (BoxCast/CapsuleCast) thay vì Dynamic, để thông số nhảy đúng chính xác theo GDD (g = 73.5, v₀ = 25.7) và không trượt dốc hay nảy ngoài ý muốn.
- Mọi thông số nằm trong `PlayerMovementConfig` (ScriptableObject) để tinh chỉnh trên máy thật mà không phải sửa code.
- Input tách hẳn khỏi logic: `PlayerInputReader` phát intent (`Move`, `JumpPressed/Released`, `Dash`, `Attack`, `Skill`, `SlideSwipe`). Nút ảo và gamepad đều đi qua Input System.

## Requirements
- Thông số đúng §4: chạy 8 u/s, nhảy 2–4.5 ô (apex 0.35 s), rơi ×1.6, max 20 u/s, dash 5 ô / 0.2 s (i-frame 0.1 s, CD 0.8 s, 1 lần trên không), slide 0.45 s qua khe 1 ô, wall slide 3 u/s, wall jump (±11, 22) + khóa 0.15 s.
- Coyote 0.1 s, jump buffer 0.12 s (áp cho JUMP/ATK/DASH).
- Hook mở rộng cho P5: `CanDoubleJump`, `CanGlide`, `SpeedMultiplier`, `SwimMode`.
- Nút ảo theo bố cục §3.1: joystick động ở nửa trái màn hình, các nút ≥ 64 dp (JUMP 96 dp), tôn trọng Safe Area.

## Architecture
```
PlayerInputReader ──intent──▶ PlayerController ──▶ PlayerStateMachine
       ▲                         │  (MotorKinematic2D: cast + resolve)
On-Screen Stick/Button           ▼
Gamepad (Input Actions)    PlayerAnimatorBridge ─▶ Animator (P2)
```
States: `Idle, Run, Jump, Fall, WallSlide, WallJump, Dash, Slide, Swim(P5), Attack(P4), Hurt(P4), Dead(P4)`. Mỗi state là một class `IPlayerState { Enter, Tick, FixedTick, Exit }`.

## Related Code Files
- Create `Scripts/Player/`: `PlayerController.cs`, `PlayerStateMachine.cs`, `IPlayerState.cs`, `KinematicMotor2D.cs`, `PlayerMovementConfig.cs`, `PlayerInputReader.cs`, `PlayerAnimatorBridge.cs`
- Create `Scripts/Player/States/`: `IdleState.cs`, `RunState.cs`, `JumpState.cs`, `FallState.cs`, `WallSlideState.cs`, `WallJumpState.cs`, `DashState.cs`, `SlideState.cs`
- Create `Scripts/UI/VirtualControls/`: `DynamicJoystick.cs`, `SwipeDetector.cs`, `SafeAreaFitter.cs`
- Create: `Settings/Input/AuraKnight.inputactions`, `Data/Player/PlayerMovementConfig.asset`, `Prefabs/Player/Player.prefab`, `Prefabs/UI/VirtualControls.prefab`
- Create: `Scenes/Test/Test_Movement.unity` (lưới gizmo đo ô, khe 1 ô, trục wall jump 20 ô)

## Implementation Steps
1. Input Actions map `Gameplay` (Move, Jump, Attack, Dash, Skill, AuraWind/Fire/Water, Pause, Map) + binding gamepad.
2. `KinematicMotor2D`: di chuyển bằng cast, phát hiện ground/ceiling/wall bằng skin width 0.02, hỗ trợ platform một chiều.
3. `PlayerMovementConfig`: tính sẵn g và v₀ từ `maxJumpHeight` + `timeToApex` (công thức trong comment).
4. Lần lượt cài các state theo thứ tự Idle/Run → Jump/Fall (căn lực + coyote + buffer) → Dash → WallSlide/WallJump → Slide (đổi collider, kiểm tra trần, giữ slide khi trần thấp).
5. `DynamicJoystick` + nút On-Screen Button; `SwipeDetector` (vuốt xuống ≥ 60 dp trong ≤ 0.25 s ở nửa phải màn hình → Slide).
6. Scene `Test_Movement` + gizmo hiển thị độ cao nhảy, khoảng dash.
7. Build lên máy thật, cùng D chỉnh feel (cuối tuần 2).

## Todo List
- [x] Input Actions + gamepad (AuraKnight.inputactions, virtual gamepad bindings)
- [x] KinematicMotor2D (full cast-based kinematic motor with floor/wall/ceiling detection)
- [x] Config SO (PlayerMovementConfig.asset, tuned jumpCutMultiplier to 0.25)
- [x] Run/Jump/Fall + coyote/buffer (0.1s coyote, 0.12s buffer, tested 2.0±0.25 tap / 4.5±0.1 hold)
- [x] Dash (+i-frame flag, 5 tiles in 0.2s, 0.1s i-frame, 0.8s CD, single air dash)
- [x] Wall slide/jump (3 u/s slide, wall-jump ±11 u/s vertical, 0.15s lock)
- [x] Slide + kiểm tra trần (0.45s through 1-tile khe, persists under low ceiling)
- [x] Joystick động, nút, swipe, safe area (dynamic joystick, on-screen buttons, swipe detector)
- [ ] Build máy thật + 60 fps feel tuning (cần máy thật)

## Success Criteria
- [x] Đo trong `Test_Movement`: nhảy nhấp ≈ 2 ô (±0.25) ✓, giữ = 4.5 ô (±0.1) ✓, dash = 5 ô (±0.2) ✓ (all tested in EditMode)
- [x] Slide qua khe 1 ô 20 lần liên tiếp không kẹt ✓; wall jump leo hết trục 20 ô ✓ (tested in sim)
- [ ] 60 fps trên máy tầm trung; độ trễ input không cảm nhận được (cần máy thật)

## Risk Assessment
- Bắt dính ở góc tile (corner snag) → bo góc collider + corner correction 0.2 ô khi nhảy đụng mép trần.
- Nút ảo khó chơi → buffer rộng hơn, cho chỉnh size/độ mờ (P10).

## Security Considerations
- Không áp dụng.

## Implementation Notes
- **Jump cut multiplier tuned:** Changed from 0.5 → 0.25 to achieve 2.0±0.25 tile tap height (GDD spec ambiguity resolved by testing).
- **Kinematic motor layers:** When phase 1 adds Ground/Enemy layers, set motor `collisionMask` to exclude enemies from blocking player movement.
- **Input System:** PlayerInputReader provides high-level intents (Move, JumpPressed, DashPressed, etc.) consumed by states; on-screen buttons and gamepad both feed virtual gamepad.
- **Separate test assembly:** AuraKnight.Tests.EditMode.Player uses InternalsVisibleTo to test state machine; needs `UNITY_INCLUDE_TESTS` define.
- **On-screen controls:** Built runtime by VirtualControlsBuilder (Editor asmdef lacks UI reference); phase 2 (art) replaces placeholder Text with TMP + design tokens.

## Next Steps
- P4 (Attack/Hurt states), P5 (hook Aura), P9 (level design using movement tuning).
