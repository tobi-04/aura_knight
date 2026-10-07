---
title: "Aura Knight Android - 8 week implementation"
description: "Unity 6 (6000.6.0f1) + C# Android 2D Metroidvania: 4 vùng, 4 boss, 3 Aura, nhóm 4 người trong 8 tuần"
status: in-progress
priority: P1
effort: 8w
branch: dev
tags: [unity, android, csharp, game, 2d, metroidvania]
blockedBy: []
blocks: []
work_type: feature
spec_waived: "SDD mode disabled (takumi.sddMode: off)"
created: 2026-10-05
---

# Aura Knight Android - 8 week implementation

## Overview

Dựng toàn bộ game **Aura Knight: Mảnh Vỡ Ánh Sáng** cho Android theo `docs/game-design-document.md` (GDD, nguồn yêu cầu duy nhất, mục tham chiếu §N). Biên bản quyết định nằm ở `plans/reports/brainstorm-261005-aura-knight-android-scope.md`.

- Engine: Unity 6 (6000.6.0f1), URP 2D, Input System, Cinemachine, Tilemap, TextMeshPro
- Cấu trúc: scene `Core` luôn được giữ, mỗi vùng là một scene additive gồm các room prefab nối liền (GDD §7.1, §12)
- Owner (GDD §13.1): **A** gameplay · **B** level/AI · **C** art/UI · **D** audio/QA/build
- Ưu tiên P0/P1/P2 và trigger cắt cuối tuần 4 (GDD §14). Feature freeze cuối tuần 7

## Lịch theo tuần & owner

| Tuần | Phase chính | Mốc |
|------|-------------|-----|
| 1 | P1 (A+D), P2 (C), bắt đầu P3 (A), P6 (B) | APK rỗng chạy trên máy thật |
| 2 | P3, P7 (Walker/Hopper), P10 (HUD/menu), P11 (SFX movement) | Leo chạy/nhảy/dash bằng nút ảo |
| 3 | P4, P5 (Gió), P8 (Gốc Cây), P9 (Rừng) | — |
| 4 | P6 (save), P12 (shop), P9 (Hang), P13 (playtest 1) | **Mốc 1: Rừng chơi hết**, check trigger cắt |
| 5 | P5 (Hỏa + AuraGate), P8 (Nhện), P9 (Đô Thị) | — |
| 6 | P5 (Thủy + nước), P8 (Cỗ Máy), P9 (Lâu Đài) | **Mốc 2: 3 vùng** |
| 7 | P8 (Malakor), P10 (cutscene/credits), P11 (BGM) | **Mốc 3: content complete, freeze** |
| 8 | P13 | APK release + video demo |

## Song song & phụ thuộc

- P1 chặn mọi phase khác. P2 chặn phần art của P9/P10.
- Code nền (A): P3 → P4 → P5. B làm P6 (room/transition) song song với P3, rồi P7 → P8.
- P9 cần P3, P6, P7; mỗi vùng cần Aura tương ứng từ P5 và boss từ P8.
- P10, P11 làm song song với tất cả, chỉ lắng nghe event (EventBus) nên không chặn gameplay.
- P13 chạy liên tục từ tuần 2, chốt ở tuần 8.

## Progress

**2026-10-08:** Code xong cả 13 phase. Compile sạch, EditMode 964/964, PlayMode 166 pass + 2 skip (test chụp ảnh cần GPU), APK dev 54.9 MB, review 8/10 (0 critical, 3 warning đã sửa ở d02c5d7). 34 phòng, không cần cắt. Lệch plan: làm trong 1 đợt thay vì 8 tuần; ánh sáng Lâu Đài 0.05 → 0.15.

**Còn lại (cần người/máy thật):** profiling fps/RAM trên 2 máy, 3 tỉ lệ màn hình thật, playtest M1–M3, checklist thủ công `docs/qa/device-checklist.md`, keystore + APK release, video demo. Key art: nhóm xác nhận được phép dùng (2026-10-08).

## Phases

| Phase | Name | Status |
|-------|------|--------|
| 1 | [Project Setup and Repo](./phase-01-project-setup-and-repo.md) | Completed* |
| 2 | [Asset Sourcing and Art Pipeline](./phase-02-asset-sourcing-and-art-pipeline.md) | Completed* |
| 3 | [Player Movement and Touch Input](./phase-03-player-movement-and-touch-input.md) | Completed* |
| 4 | [Combat and Health](./phase-04-combat-and-health.md) | Completed* |
| 5 | [Aura System](./phase-05-aura-system.md) | Completed* |
| 6 | [World Framework and Save](./phase-06-world-framework-and-save.md) | Completed* |
| 7 | [Enemy AI Archetypes](./phase-07-enemy-ai-archetypes.md) | Completed* |
| 8 | [Bosses](./phase-08-bosses.md) | Completed* |
| 9 | [Level Content Four Regions](./phase-09-level-content-four-regions.md) | Completed* |
| 10 | [UI Screens and HUD](./phase-10-ui-screens-and-hud.md) | Completed* |
| 11 | [Audio](./phase-11-audio.md) | Completed* |
| 12 | [Progression Shop and Map](./phase-12-progression-shop-and-map.md) | Completed* |
| 13 | [QA Optimization and Release](./phase-13-qa-optimization-and-release.md) | Completed* |

**\* Marked "Completed" with device/on-device feel verification pending (phase 13 QA scope).**

## Dependencies

- Không có plan khác (cross-plan: none).
- Bên ngoài: Unity 6 (6000.6.0f1) + Android Build Support (SDK/NDK/OpenJDK), Git LFS, ≥2 máy Android thật (1 máy yếu), asset pack miễn phí (P2).
- Câu hỏi còn mở (GDD §17): có nộp Google Play không, có bắt buộc tự vẽ asset không, bản quyền key art PDF.
