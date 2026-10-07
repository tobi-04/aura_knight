---
phase: 13
title: "QA Optimization and Release"
status: completed
effort: "8d (rải từ tuần 2) + toàn bộ tuần 8"
owner: "D (cả nhóm tuần 8)"
weeks: "2-8"
---

# Phase 13: QA Optimization and Release

## Context Links
- GDD §15 (test case, Definition of Done), §12.5 (tối ưu Android), §14 (P0/P1/P2), §16 (rủi ro)

## Overview
- Priority: P0 · Status: completed (code; mục cần máy thật/người chơi còn mở, xem plan.md)
- Test case, playtest theo từng mốc, profiling trên máy thật, sửa bug, build release APK, credits, video demo.

## Key Insights
- Game Unity khó viết unit test cho phần "feel". Ưu tiên **PlayMode test** cho logic thuần (Health, Wallet, ShopService, SaveSystem, AuraManager) + checklist thủ công cho movement/level.
- Playtest bằng người **chưa từng chơi** ở mỗi mốc. Nhóm tự chơi sẽ không thấy độ khó thật.

## Requirements
- EditMode/PlayMode tests (Unity Test Framework): `HealthTests`, `WalletTests`, `ShopServiceTests`, `SaveSystemTests` (round-trip + file hỏng), `AuraManagerTests` (unlock/switch/cost).
- Checklist thủ công từ GDD §15.1 (movement, gate, chuyển phòng 50 lần, app vào nền, 3 tỉ lệ màn hình).
- Profiling: ≥ 55 fps trung bình, không < 30 fps khi đánh boss; RAM < 600 MB; APK < 150 MB.
- Release: keystore riêng (ngoài repo), IL2CPP ARM64, Development Build = off, version `1.0.0 (1)`.

## Architecture
```
Assets/_Project/Tests/EditMode/*.cs   Assets/_Project/Tests/PlayMode/*.cs   (asmdef riêng)
docs/qa/test-cases.md · docs/qa/bug-log.md · docs/qa/playtest-<mốc>.md
```

## Related Code Files
- Create: `Assets/_Project/Tests/EditMode/AuraKnight.Tests.EditMode.asmdef` + test files
- Create: `Assets/_Project/Tests/PlayMode/AuraKnight.Tests.PlayMode.asmdef` + test files
- Create: `Assets/_Project/Scripts/AuraKnight.asmdef` (để test reference được)
- Create: `docs/qa/test-cases.md`, `docs/qa/bug-log.md`, `docs/qa/playtest-m1.md`, `-m2.md`, `-m3.md`
- Create: `Assets/_Project/Scripts/Editor/BuildScript.cs` (menu build APK dev/release)

## Implementation Steps
1. (Tuần 2) test-cases.md từ GDD §15.1; asmdef + test logic đầu tiên.
2. (Mỗi mốc: tuần 4, 6, 7) playtest 2–3 người mới, ghi `playtest-mX.md` (thời gian, số lần chết, chỗ kẹt).
3. (Tuần 4) **Hỗ trợ lead kiểm tra trigger cắt.**
4. (Tuần 5–7) Profiler qua USB trên máy yếu; xử lý theo §12.5 (atlas, pool, giới hạn light, chế độ tiết kiệm).
5. (Tuần 8) Bug bash cả nhóm → chỉ sửa bug, không thêm feature.
6. (Tuần 8) Build release, credits (từ LICENSES.md), video demo 3–5 phút, cập nhật README (cách cài APK).

## Todo List
- [ ] test-cases.md + bug-log.md
- [ ] asmdef + tests logic
- [ ] Playtest M1/M2/M3
- [ ] Profiling + tối ưu
- [ ] BuildScript dev/release
- [ ] Bug bash tuần 8
- [ ] Release APK + video + README

## Success Criteria
- Toàn bộ test tự động pass; checklist thủ công pass trên 2 máy.
- Đạt Definition of Done GDD §15.2.

## Risk Assessment
- Bug phát hiện muộn ở tuần 8 → playtest từ tuần 4, có bug-log ưu tiên theo mức (Crash > Softlock > Gameplay > Visual).

## Security Considerations
- Keystore và mật khẩu không commit, không ghi trong README; lưu trong password manager của nhóm.

## Next Steps
- Nộp bài. Nếu giảng viên yêu cầu Google Play (GDD §17): thêm build AAB + privacy policy.
