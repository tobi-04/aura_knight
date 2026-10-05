---
phase: 9
title: "Level Content Four Regions"
status: pending
effort: "15d"
owner: "B (layout), C (art pass)"
weeks: "1-7"
---

# Phase 9: Level Content Four Regions

## Context Links
- GDD §7.1–7.2 (bản đồ, bảng vùng, gating, bí mật), §4 (thông số dùng để thiết kế khoảng cách), §10 (parallax, Global Light), §14 (trigger cắt)

## Overview
- Priority: P0 (5 phòng/vùng) → P1 (đủ 34 phòng) · Status: pending
- Dựng Hub (3) + Rừng (8) + Hang (8) + Đô Thị (8) + Lâu Đài (7) + 4 phòng boss theo quy trình grey-box → playtest → art pass.

## Key Insights
- **Grey-box trước, art sau.** Chỉ thêm art khi layout đã được playtest.
- Khoảng cách thiết kế lấy từ thông số P3: nhảy thường 4.5 ô cao / khoảng 5 ô xa; dash 5 ô; tầm xa tối đa không có Aura khoảng 10 ô; nhảy đúp lên được khoảng 7.2 ô. **Mọi cổng gating phải vượt ngưỡng này** (vd vách 6 ô ở lối vào Hang).
- Mỗi vùng: 1 bàn thờ đầu vùng + 1 trước boss, 1 shortcut về hub, 2 rương bí mật (cần Aura của vùng sau, nên phải quay lại).

## Requirements
- Kích thước phòng chuẩn 40×22 ô, phòng dọc 22×44 ô.
- Mỗi phòng: tilemap 3 lớp (Ground/Collision, Decor, Foreground) + parallax 4 lớp (×1.2/1.0/0.5/0.1) + Global Light của vùng (0.6 / 0.25 / 0.45 / 0.05).
- Bẫy theo vùng: Rừng (gai, đất sụt 0.6 s / hồi 3 s), Hang (thạch nhũ rơi, chông, hồ ngầm), Đô Thị (hơi nóng chu kỳ 2 s, piston, axit, ngập nước), Lâu Đài (bóng tối, sàn gai chuyển động, bẫy lửa).
- Cổng: Hang = vách 6 ô (Gió); Đô Thị = Rào Gỗ ở hub (Hỏa); Lâu Đài = cổng 3 ấn (Gió + Hỏa + Thủy).

## Architecture
```
Region_Hub:    hub_01 (spawn, altar) · hub_02 (shop, NPC) · hub_03 (cổng Lâu Đài, Rào Gỗ → Đô Thị)
Region_Forest: forest_01..07 + forest_boss   (mở sẵn)
Region_Cave:   cave_01..07  + cave_boss      (cần Gió)
Region_City:   city_01..07  + city_boss      (cần Hỏa)
Region_Castle: castle_01..06 + castle_boss   (cần 3 Aura)
```
Bản đồ phòng (ô lưới) vẽ trước trong `docs/level-map.md`, đây là nguồn chung cho B, C và map screen (P12).

## Related Code Files
- Create `Scripts/World/Hazards/`: `Spikes.cs`, `CollapsingPlatform.cs`, `FallingStalactite.cs`, `SteamVent.cs`, `Piston.cs`, `AcidPool.cs`, `MovingSpikeFloor.cs`, `FireTrap.cs`
- Create: `Scripts/World/ParallaxLayer.cs`, `Scripts/World/RegionLighting.cs`, `Scripts/World/SealGate.cs` (cổng 3 ấn)
- Create: `Prefabs/Rooms/<Region>/Room_XX.prefab` (34 + 4 boss), `Prefabs/Hazards/*.prefab`
- Create: `docs/level-map.md` (sơ đồ phòng + cổng + bí mật)
- Modify: `Scenes/Region_*.unity` (chỉ B sửa)

## Implementation Steps
1. (Tuần 1) Vẽ `docs/level-map.md`: tất cả phòng, exit, gate, bàn thờ, rương.
2. (Tuần 1–2) Grey-box Hub + Rừng, hazard Rừng, đặt Walker/Hopper.
3. (Tuần 3) Phòng boss Rừng + art pass Rừng (C) → **Mốc 1 cuối tuần 4: Rừng chơi hết**.
4. **Cuối tuần 4 check trigger cắt (GDD §14):** nếu Rừng chưa xong thì các vùng sau chỉ làm 5 phòng.
5. (Tuần 4–5) Hang; (5–6) Đô Thị; (6–7) Lâu Đài, mỗi vùng theo đúng quy trình grey-box → test → art.
6. Đặt bí mật quay lại (backtrack) và shortcut sau khi cả 4 vùng đã có grey-box.
7. Chạy `Aura/Validate Rooms` trước mỗi lần merge.

## Todo List
- [ ] level-map.md
- [ ] Hub (3 phòng) + Rào Gỗ + cổng 3 ấn
- [ ] Rừng 8 + boss room + hazard
- [ ] Check trigger cắt tuần 4
- [ ] Hang 8 + boss room + hazard
- [ ] Đô Thị 8 + boss room + hazard
- [ ] Lâu Đài 7 + boss room + hazard
- [ ] Bí mật / shortcut / rương
- [ ] Art pass + parallax + lighting 5 vùng

## Success Criteria
- Đi từ đầu đến Malakor chỉ theo đường chính được; không thể vào vùng khi chưa có Aura yêu cầu (đã thử lách).
- Không có softlock: mọi hố/phòng đều có đường thoát hoặc là bẫy chết hồi sinh ở bàn thờ.
- Thời gian chơi đường chính 60–90 phút (người test mới).

## Risk Assessment
- Đây là phase lớn nhất và rủi ro trễ cao nhất → trigger cắt tuần 4; C hỗ trợ art pass để B tập trung layout.
- Xung đột scene → chỉ B sửa scene vùng; C làm art trong room prefab theo lịch đã hẹn trước, không làm cùng lúc với B.

## Security Considerations
- Không áp dụng.

## Next Steps
- P12 map screen lấy dữ liệu phòng; P13 playtest từng mốc.
