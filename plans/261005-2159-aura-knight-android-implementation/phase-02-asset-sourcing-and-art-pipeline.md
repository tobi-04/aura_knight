---
phase: 2
title: "Asset Sourcing and Art Pipeline"
status: pending
effort: "4d"
owner: "C"
weeks: "1 (khóa asset), 2-7 (tích hợp dần)"
---

# Phase 2: Asset Sourcing and Art Pipeline

## Context Links
- GDD §10 (mỹ thuật & asset), §9.1 (token màu), §0 #6–7
- Key art: `docs/reference/NỀN TẢNG Mobile.pdf` (trang 1 Leo, 8–10 Aura, 11 map, 16 hang)

## Overview
- Priority: P0 · Status: pending
- Chọn và **khóa** asset pack cho Leo + 4 vùng + quái + boss ngay tuần 1; dựng pipeline import; recolor Leo theo key art trang 1.

## Key Insights
- Key art trong PDF là tranh minh họa, không phải sprite sheet. Chỉ dùng cho menu, popup, map, loading.
- Asset không đồng nhất là rủi ro lớn nhất về hình ảnh, nên ưu tiên các pack **cùng tác giả**, cùng PPU 32 và cùng độ dày pixel.
- Glow Aura làm bằng Light2D + tint material, không vẽ 3 bộ sprite.

## Requirements
- Leo đủ animation: idle, run, jump, fall, attack×2, air-attack (lên/xuống), dash, slide, wall-slide, hurt, death, swim.
- Tileset 4 vùng + hub (Rule Tile), parallax 4 lớp mỗi vùng, sprite 7 loại quái + 4 boss (64×64 trở lên).
- Mỗi asset có license cho phép (CC0 / CC-BY / free-commercial) và ghi lại đầy đủ.

## Architecture
- Import preset `Sprite_Pixel32.preset`: PPU 32, Filter Point, Compression None, Mesh Full Rect cho tile.
- Sprite Atlas theo vùng: `Atlas_Hub/Forest/Cave/City/Castle`, `Atlas_Player`, `Atlas_UI`.
- Leo material: `Mat_PlayerAura` với shader tint vùng viền giáp, nhận màu từ `AuraManager` (P5).

## Related Code Files
- Create: `Assets/_Project/Art/LICENSES.md` (tên pack · tác giả · link · license · dùng ở đâu)
- Create: `Assets/_Project/Art/Presets/Sprite_Pixel32.preset`, `Assets/_Project/Art/Atlases/*.spriteatlasv2`
- Create: `Assets/_Project/Art/Characters/Leo/*` (sprite đã recolor + Animator Controller `Leo.controller`)
- Create: `Assets/_Project/Art/Tilesets/<Region>/*` + Rule Tile assets
- Create: `Assets/_Project/Art/KeyArt/*` (tách từ PDF bằng `pdfimages`, ảnh 1024–1600 px)

## Implementation Steps
1. Lập bảng ứng viên (≥2 lựa chọn cho mỗi vùng), kiểm tra license, chụp preview ghép cạnh nhau để so tông.
2. Họp nhóm chốt (cuối ngày 3 tuần 1). Sau đó **không đổi pack**.
3. Recolor Leo theo palette: tóc nâu, giáp `#1B2233`, viền `#C99A3B`, khăn xanh. Tách layer viền giáp để tint được.
4. Tạo preset import, áp cho toàn bộ thư mục Art.
5. Tạo Rule Tile cho mỗi tileset (đất, tường, platform một chiều, gai).
6. Animator Controller cho Leo với parameter: `Speed`, `VelY`, `Grounded`, `WallSlide`, `Dash`, `Slide`, `Attack`, `Hurt`, `Dead`, `Swim`.
7. Tách key art từ PDF (`pdfimages -png`) vào `Art/KeyArt/`.
8. Ghi `LICENSES.md`; D dùng file này làm credits (P13).

## Todo List
- [ ] Bảng ứng viên + license check
- [ ] Chốt pack (tuần 1)
- [ ] Recolor Leo + layer tint
- [ ] Preset + atlases
- [ ] Rule Tiles 5 tileset
- [ ] Animator Leo
- [ ] Key art export
- [ ] LICENSES.md

## Success Criteria
- Ảnh ghép thử Leo đứng trong 4 vùng nhìn cùng một "game"; cả nhóm đồng ý.
- 100% file trong `Art/` có dòng tương ứng trong `LICENSES.md`.

## Risk Assessment
- Không tìm được boss phù hợp → dùng sprite quái thường scale ×2 + recolor + Light2D, hoặc ghép từ nhiều phần (2D Animation).
- Thiếu animation swim/slide → tái dùng frame run/fall + xoay, chấp nhận ở mức P1.

## Security Considerations
- Không dùng asset "rip" từ game thương mại. Repo public nên mọi file đều bị công khai.

## Next Steps
- Cung cấp sprite cho P3 (Leo), P7/P8 (quái/boss), P9 (tileset), P10 (key art UI).
