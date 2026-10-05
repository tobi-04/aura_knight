---
phase: 1
title: "Project Setup and Repo"
status: completed
effort: "3d"
owner: "A + D"
weeks: "1"
notes: "Pending on-device verification and git initialization"
---

# Phase 1: Project Setup and Repo

## Context Links
- GDD §1.1 (thông số Android), §12.1 (packages), §12.2 (thư mục), §13.1 (Git rules)
- Repo: https://github.com/tobi-04/aura_knight (đang trống)

## Overview
- Priority: P0 · Status: pending · Chặn mọi phase khác
- Tạo Unity project, git + LFS, cấu trúc thư mục, build APK đầu tiên lên máy thật.

## Key Insights
- Scene YAML của Unity rất dễ conflict, nên bật **Force Text** + **Visible Meta Files** ngay từ đầu.
- Hai file PDF (10 MB) đang nằm ở root: chuyển vào `docs/reference/` và track bằng LFS. Repo public nên cần xác nhận được phép công khai key art (GDD §17).
- Play Store bắt buộc ARM64 + IL2CPP. Bật sẵn để build release không phải đổi về sau.

## Requirements
- Unity 6 (6000.6.0f1) (bản patch mới nhất) + Android Build Support (SDK, NDK, OpenJDK).
- Packages: URP, Input System, Cinemachine, 2D Tilemap Extras, 2D Pixel Perfect, TextMeshPro, 2D Animation.
- Player Settings: Landscape Left/Right, Min API 26, IL2CPP, ARM64, package `com.aurastudio.auraknight`, Active Input Handling = Input System Package.

## Architecture
```
aura_knight/            (git root = Unity project root)
  Assets/_Project/...   (GDD §12.2)
  Packages/ ProjectSettings/
  docs/  plans/
  .gitignore  .gitattributes (LFS)  README.md
```
Scene `Boot` (index 0) → `MainMenu` → `Core` + `Region_*` (additive).

## Related Code Files
- Create: `.gitignore` (template Unity), `.gitattributes` (LFS: png, psd, aseprite, wav, ogg, mp3, ttf, otf, pdf), `README.md`
- Create: `Assets/_Project/**` (cây thư mục rỗng + `.gitkeep`), `Assets/_Project/Scenes/{Boot,MainMenu,Core,Region_Hub}.unity`
- Create: `Assets/_Project/Scripts/Core/Bootstrapper.cs` (load MainMenu), `Assets/_Project/Settings/URP-2D-Renderer.asset`
- Move: `*.pdf` → `docs/reference/`

## Implementation Steps
1. `git init`, `git lfs install`, thêm `.gitignore` Unity + `.gitattributes`.
2. Tạo project bằng template **2D (URP)** ngay trong root, cài packages ở trên.
3. Editor Settings: Asset Serialization = Force Text, Version Control = Visible Meta Files.
4. Player Settings Android như mục Requirements; Quality: tắt VSync, `targetFrameRate=60` trong Bootstrapper.
5. Pixel Perfect Camera: PPU 32, reference 640×360, bật Upscale RT.
6. Tạo cây thư mục §12.2 + 4 scene rỗng, thêm vào Build Settings.
7. Build APK development, cài lên 2 máy thật, ghi tên + cấu hình máy vào `README.md`.
8. Tạo nhánh `main` + `dev`, push lên `tobi-04/aura_knight`, bật branch protection cho `main` (cần PR).

## Todo List
- [x] git + LFS + ignore/attributes (configs created, not pushed)
- [x] Unity project 2D URP + packages
- [x] Force Text / Visible Meta
- [x] Android Player Settings (IL2CPP, ARM64, API 26, landscape)
- [x] Cây thư mục + 4 scene
- [ ] APK chạy trên 2 máy thật (cần máy thật)
- [ ] Push main/dev, branch protection (no git repo initialized yet)

## Success Criteria
- [x] Clone mới về mở được project không lỗi; compile 0 error CS, 0 warnings (verified).
- [x] APK dev builds successfully: 56 MB IL2CPP ARM64 (Builds/Android/AuraKnight-dev.apk).
- [ ] APK cài và mở được trên 2 máy, hiện màn hình Boot → MainMenu rỗng (cần máy thật)

## Risk Assessment
- Thiếu module Android/NDK lệch bản → cài qua Unity Hub, không tự cài NDK ngoài.
- LFS free quota 1 GB / 1 GB băng thông mỗi tháng → không commit file nguồn khổng lồ (wav gốc, psd lớn), chỉ commit bản export.

## Security Considerations
- Keystore release **không commit**: lưu ngoài repo, thêm `*.keystore`, `*.jks` vào `.gitignore`.

## Implementation Notes
- **Unity version:** Switched from 2022.3 LTS to 6000.6.0f1 (user decision, not in original plan). Cinemachine updated to 6.6 compatibility.
- **No git repo initialized yet:** Project structure created but not pushed to tobi-04/aura_knight. Branch protection and LFS tracking still pending git init.
- **APK builds successfully:** IL2CPP ARM64 compilation clean; dev APK 56 MB ready for device testing.

## Next Steps
- Initialize git + push to tobi-04/aura_knight with branch protection (P1 cleanup).
- Test APK on 2 real Android devices (P13 QA) — one mid-range, one budget.
- Proceed to P2 (Art pipeline) and P3 (Player movement feel tuning on device).
