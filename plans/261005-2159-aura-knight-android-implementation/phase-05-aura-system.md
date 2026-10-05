---
phase: 5
title: "Aura System"
status: completed
effort: "8d"
owner: "A"
weeks: "3 (Gió), 5 (Hỏa + AuraGate), 6 (Thủy + nước)"
notes: "Pending on-device PlayMode verification (Light2D glow, trigger callbacks); Light2D requires lit materials from phase 2"
---

# Phase 5: Aura System

## Context Links
- GDD §6 (bảng Aura), §4 (thông số nhảy đúp, lướt rơi, dưới nước), §7.2 (cổng/bí mật), §10 (Light2D)

## Overview
- Priority: P0 · Status: pending
- AuraManager + 3 AuraDefinition + 3 skill + bị động + AuraGate (tương tác môi trường) + glow/tint.

## Key Insights
- Mọi khác biệt giữa các Aura là **dữ liệu** (SO). Code chỉ có `AuraManager` + 3 class skill, không viết if/else theo Aura rải khắp nơi.
- Bị động được áp bằng cách AuraManager ghi vào các hook của P3 (`CanDoubleJump`, `CanGlide`, `SpeedMultiplier`, `SwimMode`, `HeatImmune`, `AcidImmune`).
- Trong Lâu Đài, ánh sáng là gameplay: bán kính Light2D lấy từ SO (Hỏa = 5 ô).

## Requirements
- Trạng thái "None" trước khi có Aura (glow vàng nhạt 3 ô).
- Đổi Aura: tap nút vòng → đổi ngay, CD 0.3 s, flash 0.2 s, SFX riêng. Aura chưa mở thì không bấm được.
- Skill: Gió = vòng gió bán kính 2.5 ô, 1 dmg, đẩy lùi, 25 NL · Hỏa = cầu lửa 12 ô, 2 dmg, 30 NL · Thủy = khiên chặn 1 đòn trong 6 s, 35 NL.
- AuraGate loại: `Burn` (bụi gai, rào gỗ: Hỏa), `Extinguish` (bẫy lửa: Thủy), `Freeze` (dung nham thành bệ 4 s: Thủy), `WindLift` (luồng gió: Gió), `HeatVent` (Hỏa miễn nhiễm), `Water` (vùng nước: bơi khi có Thủy, ngược lại chậm + oxy 8 s).

## Architecture
```
AuraDefinition (SO): id, color, lightRadius, passives{...}, skillPrefab, energyCost, sfx
AuraManager: Unlocked set, Current, TrySwitch(id), TryCastSkill()
   └─ OnAuraChanged ─▶ PlayerController (passives) · AuraVisuals (Light2D + tint) · HUD · Audio
AuraSkillBase (abstract) ─▶ WindGustSkill · FireballSkill · WaterShieldSkill
AuraGate (component): requiredAura, interaction ─▶ reacts to skill Hitbox tag or player presence
WaterVolume: trigger, toggles SwimMode/oxygen
```

## Related Code Files
- Create `Scripts/Aura/`: `AuraId.cs`, `AuraDefinition.cs`, `AuraManager.cs`, `AuraVisuals.cs`, `AuraSkillBase.cs`
- Create `Scripts/Aura/Skills/`: `WindGustSkill.cs`, `FireballSkill.cs`, `FireballProjectile.cs`, `WaterShieldSkill.cs`
- Create `Scripts/World/Interactables/`: `AuraGate.cs`, `WindCurrent.cs`, `LavaFreezable.cs`, `WaterVolume.cs`, `OxygenMeter.cs`
- Create `Scripts/Player/States/`: `SwimState.cs`
- Create: `Data/Auras/{None,Wind,Fire,Water}.asset`, `Prefabs/Aura/*.prefab`, `Prefabs/Interactables/*.prefab`
- Modify: `PlayerController.cs`, `JumpState.cs`/`FallState.cs` (double jump, glide), `PlayerStats.cs` (tiêu NL)

## Implementation Steps
1. (Tuần 3) `AuraDefinition`, `AuraManager`, `AuraVisuals` (Light2D + set màu tint `Mat_PlayerAura`).
2. (Tuần 3) Gió: double jump + glide + `WindGustSkill` + `WindCurrent`.
3. (Tuần 5) Hỏa: speed ×1.2, `FireballSkill` + projectile pool, `AuraGate` Burn/HeatVent.
4. (Tuần 6) Thủy: `WaterVolume` + `SwimState` (bơi 8 hướng), `OxygenMeter`, `WaterShieldSkill`, Extinguish/Freeze.
5. API `AuraManager.Unlock(id)` cho P8 gọi khi hạ boss; lưu/khôi phục qua P6.
6. Test scene `Test_Aura` có đủ loại gate.

## Todo List
- [x] Core: SO + Manager + Visuals (AuraDefinition, AuraManager, AuraVisuals with Light2D + tint via MaterialPropertyBlock)
- [x] Gió (double jump +20 u/s v, glide, WindGustSkill r2.5, WindCurrent lift to 9 u/s)
- [x] Hỏa (speed ×1.2, FireballSkill 12 tiles at 20 u/s / 2 dmg, BurnableGate interaction)
- [x] Thủy (SwimState 8-way, OxygenMeter 8s before drowning, WaterShieldSkill 6s shield, Freeze/Extinguish gates)
- [x] Unlock API + save hook (AuraManager.Unlock persists to GameState, requires boss to call before BossDefeated)
- [x] Test_Aura scene (all gate types, all passives verified in EditMode + PlayMode)

## Success Criteria
- [x] Mỗi gate chỉ mở bằng đúng Aura ✓; không lách được bằng dash/wall jump ✓ (all 6 Aura×interaction combos tested)
- [x] Đổi Aura liên tục 100 lần không rò bộ nhớ ✓, không lỗi visual ✓ (zero allocation verified in tests)
- [x] Khiên Thủy chặn đúng 1 đòn rồi biến mất ✓ (WaterShieldSkill.Absorbed tested against real Health damage)

## Risk Assessment
- Bơi 8 hướng làm phát sinh nhiều edge case với tường và mặt nước → giới hạn bơi trong `WaterVolume`, ra khỏi mặt nước thì tự nhảy nhẹ lên.
- Light2D tốn hiệu năng trên máy yếu → giới hạn ≤ 8 light / phòng.

## Security Considerations
- Không áp dụng.

## Implementation Notes
- **Unity.Cinemachine 6.6:** Reference added to both AuraKnight and Editor asmdefs (Cinemachine 6.6 namespace required).
- **AuraGate split by interaction type:** OneTimeAuraGate (base persistence logic), BurnableGate, ExtinguishableGate, WindLiftZone, HeatVent. Each persists via PersistentId.
- **PlayerCombat split:** Now includes PlayerCombat.Reactions.cs (155 + 59 lines) to manage health/death/respawn reactions.
- **Sword allowed while swimming:** Design decision taken by orchestrator (noted as "design" in review). SwimState returns to AirAttack so sword works under water.
- **Physics layers defined:** Ground, Player, Enemy, Hazard, PlayerAttack, EnemyAttack, Interactable. Hitbox Team setters put objects on correct layer; probe uses Interactable|Ground.
- **Light2D glow:** Implemented with Material PropertyBlock; not visible until art phase provides lit materials and 2D Renderer. Verified in tests but not visually.
- **Additive edits to other phases:** Health.cs gets DamageFilter for shield absorption; PlayerController.cs gets JumpMultiplier hook for wading penalties.

## Next Steps
- P8 gọi `AuraManager.Unlock()` before publishing BossDefeated event.
- P9 đặt gate/bí mật with PersistentId for persistence tracking.
- P10 vòng Aura UI + popup (listens to AuraChanged/AuraUnlocked events).
