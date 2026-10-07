---
phase: 7
title: "Enemy AI Archetypes"
status: completed
effort: "6d"
owner: "B"
weeks: "2-4"
---

# Phase 7: Enemy AI Archetypes

## Context Links
- GDD §7.3 (4 kiểu gốc, 7 biến thể, chỉ số), §5.1 (dmg người chơi)

## Overview
- Priority: P0 · Status: completed (code; mục cần máy thật/người chơi còn mở, xem plan.md)
- 4 kiểu AI gốc (Walker, Hopper, Flyer, Crawler/Static), 7 biến thể dùng `EnemyStats` SO + override sprite/animator.

## Key Insights
- Biến thể = SO + Animator Override Controller. Code chỉ cần viết **4 class**.
- Hai biến thể có hành vi đặc biệt, làm bằng component phụ thay vì class mới: `FrontShield` (Hiệp Sĩ Bóng Đêm), `PhaseThroughWalls` (Bóng Ma), `LifeSteal` (Dơi).
- Quái chỉ chạy AI khi phòng của nó đang active (P6), giúp tiết kiệm CPU.

## Requirements
- State machine chung: `Patrol → Detect → Attack → Cooldown → Hurt → Dead`.
- Walker: tuần tra, quay đầu ở mép vực/tường, lao tới khi Leo ở trong 6 ô. Hopper: nhảy cung mỗi 1.5 s. Flyer: lơ lửng, lao chéo rồi bay lên. Crawler: bò dọc bề mặt (tường/trần). Static: phóng điện vòng 2 ô mỗi 3 s.
- Chết: rơi xu (3–6) + 10% Giọt Sáng; respawn khi Leo nghỉ ở bàn thờ hoặc chết.
- Tối đa 6 quái active / phòng.

## Architecture
```
EnemyBase (Health, Hurtbox, Hitbox chạm, EnemyStats, StateMachine, DropTable)
 ├─ WalkerEnemy  ├─ HopperEnemy  ├─ FlyerEnemy  └─ CrawlerEnemy (+ StaticZapper mode)
Modifiers: FrontShield · PhaseThroughWalls · LifeSteal
EnemyStats SO: hp, contactDmg, speed, detectRange, coinsMin/Max, ...
```

## Related Code Files
- Create `Scripts/Enemies/`: `EnemyBase.cs`, `EnemyStateMachine.cs`, `EnemyStats.cs`, `DropTable.cs`, `WalkerEnemy.cs`, `HopperEnemy.cs`, `FlyerEnemy.cs`, `CrawlerEnemy.cs`
- Create `Scripts/Enemies/Modifiers/`: `FrontShield.cs`, `PhaseThroughWalls.cs`, `LifeSteal.cs`
- Create: `Scripts/World/Pickups/CoinPickup.cs` (hút về Leo khi trong 2 ô, có pool)
- Create: `Data/Enemies/{BugThorn,PoisonShroom,Bat,StoneSpider,PatrolBot,ScrapZapper,NightKnight,Ghost}.asset`
- Create: `Prefabs/Enemies/*.prefab` (8 prefab biến thể)

## Implementation Steps
1. EnemyBase + state machine + DropTable + CoinPickup (pool).
2. Walker (tuần 2) → Hopper (tuần 2) cho Rừng.
3. Flyer + Crawler (tuần 4) cho Hang.
4. Static mode + PatrolBot (tuần 4–5) cho Đô Thị; FrontShield, PhaseThroughWalls (tuần 5–6) cho Lâu Đài.
5. Scene `Test_Enemies` chứa mọi biến thể.

## Todo List
- [ ] EnemyBase + FSM + drops
- [ ] Walker, Hopper
- [ ] Flyer, Crawler
- [ ] Static zapper
- [ ] Modifiers (shield, phase, lifesteal)
- [ ] 8 SO + prefab
- [ ] Test scene

## Success Criteria
- Mỗi biến thể chơi được ở Test_Enemies; không quái nào rơi khỏi map hay kẹt trong tường sau 5 phút.
- Hiệp Sĩ Bóng Đêm không nhận dmg từ phía trước, nhận dmg từ sau lưng / khi bị slide qua.

## Risk Assessment
- Crawler đi vòng góc lồi/lõm dễ lỗi → đi theo waypoint vẽ sẵn trên tường thay vì bám bề mặt tự do.

## Security Considerations
- Không áp dụng.

## Next Steps
- P8 dùng EnemyBase/Health, P9 đặt quái vào phòng.
