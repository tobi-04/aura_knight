---
phase: 8
title: "Bosses"
status: completed
effort: "10d"
owner: "B"
weeks: "3 (Gốc Cây), 5 (Nhện), 6 (Cỗ Máy), 7 (Malakor)"
---

# Phase 8: Bosses

## Context Links
- GDD §7.4 (bảng boss), §6 (Aura dùng chống boss), §9.4 (Boss intro, HP bar)

## Overview
- Priority: P0 (Malakor phase 3 là P2) · Status: completed (code; mục cần máy thật/người chơi còn mở, xem plan.md)
- 4 boss trên framework chung: 3 đòn, 2 phase (phase 2 ở 50% HP thì nhanh hơn 25% + thêm biến thể đòn), phòng boss khóa cửa, hạ boss thì nhận Aura + mảnh vỡ.

## Key Insights
- **BossBase + chuỗi `BossAttack` (SO hoặc component)** chọn theo pattern có trọng số. Mỗi boss chỉ khác ở bộ attack và chỉ số.
- Mỗi đòn đều có **telegraph** (báo trước ≥ 0.5 s) vì trên mobile phản xạ chậm hơn PC.
- Dmg ×2 khi trúng điểm yếu (Cỗ Máy) làm bằng Hurtbox phụ có `damageMultiplier`.

## Requirements
- Gốc Cây (30 HP): rễ đâm (telegraph 0.6 s), ném 3 hạt cung, quét cành; lõi mở sau đòn 3.
- Nhện (40 HP): rơi từ trần, phun tơ (chậm 50%), gọi 2 nhện nhỏ.
- Cỗ Máy (50 HP): piston 3 cột, laser ngang (phải slide), xả hơi toàn sàn (Hỏa miễn nhiễm); lò hơi nhận ×2 từ Cầu Lửa.
- Malakor (70 HP): chém bóng tầm xa, dịch chuyển + đâm; phase 3 (P2) tắt sáng + đòn theo màu Aura.
- Vào phòng → cửa đóng → Boss intro (`BOSS / TÊN`) → HP bar. Chết thì cửa mở lại, boss reset. Thắng → `AuraManager.Unlock` + `OnBossDefeated` + autosave.

## Architecture
```
BossArena (doors, trigger, camera zone) ─▶ BossBase (Health, phases[], attackPool[])
BossBase.Tick: if idle → pick weighted BossAttack in current phase → Run coroutine → recover
BossAttack (abstract MonoBehaviour): Telegraph(), Execute(), Recover()
Boss classes: RootTreeBoss · StoneSpiderBoss · RogueMachineBoss · MalakorBoss (chỉ setup + attack riêng)
```

## Related Code Files
- Create `Scripts/Bosses/`: `BossBase.cs`, `BossPhase.cs`, `BossAttack.cs`, `BossArena.cs`, `BossStats.cs`, `WeakPointHurtbox.cs`
- Create `Scripts/Bosses/RootTree/`: `RootTreeBoss.cs`, `RootSpikeAttack.cs`, `SeedVolleyAttack.cs`, `BranchSweepAttack.cs`
- Create `Scripts/Bosses/StoneSpider/`: `StoneSpiderBoss.cs`, `CeilingDropAttack.cs`, `WebSpitAttack.cs`, `SummonSpiderlingsAttack.cs`
- Create `Scripts/Bosses/RogueMachine/`: `RogueMachineBoss.cs`, `PistonAttack.cs`, `LaserSweepAttack.cs`, `SteamFloodAttack.cs`
- Create `Scripts/Bosses/Malakor/`: `MalakorBoss.cs`, `ShadowSlashAttack.cs`, `TeleportStabAttack.cs`, `DarkPhaseController.cs` (P2)
- Create: `Data/Bosses/*.asset`, `Prefabs/Bosses/*.prefab`, `Prefabs/Rooms/<Region>/Room_Boss.prefab`

## Implementation Steps
1. (Tuần 3) BossBase/BossAttack/BossArena + Gốc Cây đủ 3 đòn, 2 phase.
2. (Tuần 5) Nhện (dùng lại Crawler path để bò trần).
3. (Tuần 6) Cỗ Máy + WeakPointHurtbox.
4. (Tuần 7) Malakor phase 1–2; phase 3 chỉ làm nếu còn thời gian.
5. Mỗi boss có test scene riêng và nút debug "skip to phase 2".
6. Cùng D cân bằng: người chưa chơi phải thắng được trong ≤ 5 lần thử.

## Todo List
- [ ] Framework boss + arena + HP bar hook
- [ ] Gốc Cây
- [ ] Nhện Đá
- [ ] Cỗ Máy
- [ ] Malakor (P1–2), phase 3 (P2)
- [ ] Debug tools + cân bằng

## Success Criteria
- Mỗi đòn có telegraph nhìn thấy được; người test mới thắng mỗi boss trong ≤ 5 lần.
- Thắng boss → Aura mở, save, cửa mở; chết → reset sạch (không còn đạn/nhện nhỏ sót lại).

## Risk Assessment
- Boss trễ nhất là Malakor (tuần 7) → nếu trễ, Malakor dùng lại 2 đòn đầu với tốc độ phase 2 + thêm quái triệu hồi.
- Sprite boss thiếu animation → telegraph bằng Light2D nhấp nháy + rung, không cần nhiều frame.

## Security Considerations
- Không áp dụng.

## Next Steps
- P9 đặt phòng boss; P10 boss intro/HP bar; P11 nhạc boss.
