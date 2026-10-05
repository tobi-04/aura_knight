---
phase: 4
title: "Combat and Health"
status: completed
effort: "4d"
owner: "A"
weeks: "3"
notes: "Pending PlayMode verification (haptics, camera shake on device)"
---

# Phase 4: Combat and Health

## Context Links
- GDD §5 (chiến đấu), §5.2 (khiên), §8 (giọt sáng)

## Overview
- Priority: P0 · Status: pending
- Hệ thống Hitbox/Hurtbox dùng chung cho player, quái, boss; kiếm combo 2 nhát, chém lên/xuống (pogo); máu, năng lượng, i-frame, knockback, chết và hồi sinh.

## Key Insights
- Một `Health` + `DamageInfo` dùng chung cho mọi thực thể, nên P7/P8 chỉ cần gắn component.
- Hitbox bật/tắt theo **Animation Event** để khớp frame chém.
- Pogo (chém xuống trúng quái hoặc gai thì nảy lên 3 ô) là kỹ năng quan trọng trên mobile, vì bù lại việc không có nút Block.

## Requirements
- Kiếm: combo 2 nhát × 0.25 s, tầm 1.5 ô, dmg = `swordLevel` (1–3).
- Bị đánh: −1 tim (boss 1–2), knockback 3 ô, i-frame 1.0 s (nhấp nháy). Dash i-frame 0.1 s (P3) dùng chung cờ `Invulnerable`.
- Năng lượng: chém trúng +8, max 100 (nâng lên 200 ở P12).
- Chết: state Dead → fade → `CheckpointService.Respawn()` (P6), giữ xu và tiến trình.
- Giọt Sáng: 10% rơi từ quái, hồi 1 tim.
- Haptic nhẹ khi trúng đòn / bị đánh (bật tắt trong Settings).

## Architecture
```
Hitbox (trigger, team, DamageInfo) ──OnTriggerEnter2D──▶ Hurtbox ─▶ Health.TakeDamage(info)
                                                          │
                       events: OnDamaged, OnDied ─▶ EventBus (UI/Audio/Camera shake)
```
`Team` enum {Player, Enemy, Hazard}: hitbox không đánh trúng cùng team.

## Related Code Files
- Create `Scripts/Combat/`: `Health.cs`, `DamageInfo.cs`, `Hitbox.cs`, `Hurtbox.cs`, `Team.cs`, `Knockback.cs`, `Invulnerability.cs`, `HitStop.cs` (dừng 0.05 s khi trúng)
- Create `Scripts/Player/`: `PlayerStats.cs` (tim, NL, swordLevel; phát event), `PlayerCombat.cs`
- Create `Scripts/Player/States/`: `AttackState.cs`, `AirAttackState.cs`, `HurtState.cs`, `DeadState.cs`
- Create: `Scripts/World/Pickups/LightDropPickup.cs`, `Scripts/Core/Haptics.cs`
- Modify: `PlayerController.cs`, `PlayerStateMachine.cs` (đăng ký state mới)

## Implementation Steps
1. Viết `DamageInfo`, `Team`, `Health` (+ event), `Hitbox/Hurtbox`.
2. `PlayerStats`: giữ chỉ số, phát `OnHeartsChanged`, `OnEnergyChanged`.
3. Attack states + Animation Event bật hitbox; combo window 0.3 s.
4. Pogo: AirAttack hướng xuống trúng thì `velY = pogoVelocity`.
5. Hurt (knockback + i-frame + nhấp nháy), Dead (gọi CheckpointService sau 1.2 s).
6. HitStop + camera shake (Cinemachine Impulse) + Haptics.
7. Bia tập (dummy target) trong `Test_Movement` để test.

## Todo List
- [x] Health/Hitbox/Hurtbox/Team (full damage system with HitOutcome, inv flag, event publishing)
- [x] PlayerStats + events (Hearts, Energy, SwordLevel; publishes HeartsChanged, EnergyChanged on EventBus)
- [x] Combo 2 nhát + chém lên/xuống (0.25s swing, 0.3s combo window, up-slash ≥0.6 stick, down-slash air=pogo)
- [x] Pogo (down-slash bounce 3 tiles, tested both enemy and spike interactions)
- [x] Hurt/i-frame/knockback (1.0s i-frame, 3 tile knockback, sprite blink animation)
- [x] Dead → respawn hook (1.2s delay, calls CheckpointService.Respawn async, publishes PlayerRespawned)
- [x] HitStop, shake, haptics (0.05s freeze via HitStop, Cinemachine impulse, VibrationEffect 20ms)
- [x] LightDrop pickup (10% roll on enemy death, heals 1 heart)

## Success Criteria
- [x] Dummy nhận đúng dmg ✓; không bị đánh 2 lần trong 1 nhát ✓; không trúng đồng đội ✓ (HitRegistry + Team enum verified)
- [x] Bị đánh liên tục trong 1 s chỉ mất 1 tim ✓ (tested at 0.5s, 0.94s, 1.14s boundaries in simulation)

## Risk Assessment
- Hitbox lệch frame → chỉnh Animation Event, hiển thị gizmo hitbox trong dev build.

## Security Considerations
- Không áp dụng.

## Implementation Notes
- **Shared Health component:** All entities (player, enemy, hazard) use the same Health + DamageInfo API; phases 7/8 just attach components.
- **Hitbox timing:** Driven by code constants (SwordTiming class: 0.05-0.18s active window), not animator events, so art phase can use any animation.
- **Teleport desync fix:** RoomManager.EnterRoom calls PlayerController.Teleport to sync kinematic motor state with transform. Verified by reviewer code review.
- **Respawn flow:** PlayerCombat calls DeadState → CheckpointService.Respawn (async) → PlayerRespawned event. Respawn now cross-region via RegionLoader (phase 6 fix).
- **Haptics:** VibrationEffect.createOneShot 20ms via AndroidJavaObject (only on Android, not editor). Unverified on device.
- **PlayerController.cs split:** Now 173 lines (was 228 before split); PlayerCombat 204 lines; separate test asmdef for Combat.
- **Physics layers:** Not yet defined in phase 1 (will be added in phase 1 cleanup).

## Next Steps
- P5 (skill gây dmg qua Hitbox), P7/P8 (quái dùng Health).
